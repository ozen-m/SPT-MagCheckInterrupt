using Comfort.Common;
using EFT.InventoryLogic;
using MagCheckInterrupt.Patches;
using MagCheckInterrupt.Utils;
using UnityEngine;
using static EFT.Player;

namespace MagCheckInterrupt.Components;

public class MagCheckReloadOperation(FirearmController controller) : FirearmController.GClass2038(controller)
{
#if DEBUG
    private static PlayerStateDebug _playerStateDebug;
#endif

    private bool _ammoDetailsShown;
    private bool _reloadCalled;

    // Slow down animation fields
    private float _currentSpeed = 1f;
    private float _targetSpeed = 1f;
    private SpeedState _animSpeedState = SpeedState.Normal;

    public new void Start(EUtilityType utilityType)
    {
        base.Start(utilityType);

#if DEBUG
        LoggerUtil.Debug("MagCheckReloadOperation::Start");
        if (_playerStateDebug == null)
        {
            _playerStateDebug = PlayerStateDebug.Create(Player_0);
        }
#endif
    }

    public override void Update(float deltaTime)
    {
        if (State != EOperationState.Ready) return;

        var currentStateInfo = FirearmsAnimator_0.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX);
        var normalizedTime = currentStateInfo.normalizedTime;

        // Show ammo details at the start of the reload window
        if (!_ammoDetailsShown && normalizedTime > ConfigUtil.ReloadWindowStart.Value)
        {
            if (Player_0.FirstPersonPointOfView)
            {
                AmmoDetailsPatch.ShowLastAmmoDetail();
            }

            _ammoDetailsShown = true;
        }

        if (!ConfigUtil.SlowAnimation.Value) return;

        switch (_animSpeedState)
        {
            case SpeedState.Normal:
                if (normalizedTime > ConfigUtil.SlowAnimationStart.Value)
                {
                    _targetSpeed = ConfigUtil.SlowPercentage.Value;
                    _animSpeedState = SpeedState.Slowed;
                }
                break;
            case SpeedState.Slowed:
                if (normalizedTime >= ConfigUtil.SlowAnimationEnd.Value)
                {
                    _targetSpeed = 1f;
                    _animSpeedState = SpeedState.Restored;
                }
                break;
            case SpeedState.Restored:
            default:
                break;
        }

        // Smoothing
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, ConfigUtil.SlowSmoothing.Value * deltaTime);
        FirearmsAnimator_0.SetAnimationSpeed(_currentSpeed); // Set every frame okay?
    }

    public override void Reset()
    {
        _ammoDetailsShown = false;
        _reloadCalled = false;
        _animSpeedState = SpeedState.Normal;
        _currentSpeed = 1f;
        _targetSpeed = 1f;
        Bool_0 = false;
        Bool_1 = false;
        base.Reset();
    }

    public override bool CanStartReload()
    {
        if (State != EOperationState.Ready) return false;

        var currentStateInfo = FirearmsAnimator_0.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX);
        var normalizedTime = currentStateInfo.normalizedTime;
        return normalizedTime > ConfigUtil.ReloadWindowStart.Value && normalizedTime < ConfigUtil.ReloadWindowEnd.Value;
    }

    /// <summary>
    /// Based on GClass2037.ReloadMag
    /// </summary>
    public override void ReloadMag(MagazineItemClass magazine, ItemAddress itemAddress, Callback finishCallback, Callback startCallback)
    {
        LoggerUtil.Debug("MagCheckReloadOperation::ReloadMag");
        FirearmsAnimator_0.SetAnimationSpeed(1f);

        var reloadResult = FirearmController.GClass2006.Run(
            Player_0.InventoryController,
            Weapon_0,
            magazine,
            false,
            Weapon_0.MalfState.IsKnownMalfunction(Player_0.ProfileId),
            itemAddress
        );
        if (reloadResult.Failed)
        {
            LoggerUtil.Error($"MagCheckReloadOperation::ReloadMag Failed to run ReloadMag. Error: {reloadResult.Error}");
            finishCallback?.Invoke(reloadResult);
            return;
        }

        if (FirearmsAnimator_0.Animator is GClass1446 animatorWrapper)
        {
            // GClass2016.Start calls FirearmsAnimator.Reload(bool b)
            // so we need to skip the reload animation and do our own crossfade
            ReloadAnimationPatch.SkipReloadAnimation();
            animatorWrapper.Animator_0.CrossFade(
                ReloadOutHash,
                0.10f, // Note: Anything more than 0.10f looks like a magazine swap
                FirearmsAnimator.HANDS_LAYER_INDEX,
                0.50f // Skip mag out from weapon animation
            );
        }
        else
        {
            var typeFullName = FirearmsAnimator_0.Animator.GetType().FullName;
            LoggerUtil.Warning($"MagCheckReloadOperation::ReloadMag Cannot transition directly into a reload. {typeFullName}");
        }

        if (Player_0.FirstPersonPointOfView)
        {
            AmmoDetailsPatch.HideAmmoCount();

            // We don't want observed players re-sending packets and
            // our packet needs to be sent first before Fika's reload packet (startCallback)
            if (External.Fika.IsPresent)
            {
                External.Fika.SendReloadCalledPacket();
            }
        }

        State = EOperationState.Finished;
        FirearmController_0.InitiateOperation<FirearmController.GClass2016>().Start(reloadResult.Value, finishCallback);
        startCallback?.Succeed();
    }

    public override void SetInventoryOpened(bool opened)
    {
        FirearmController_0.InventoryOpened = opened;
        FirearmsAnimator_0.SetInventory(opened);
    }

    public override void FastForward()
    {
        // Fika runs FastForward before calling ReloadMag,
        // so we need a packet to set _reloadCalled or else it finishes this operation and ReloadMag won't be called.
        LoggerUtil.Debug("MagCheckReloadOperation::FastForward");
        if (_reloadCalled)
        {
            LoggerUtil.Debug("MagCheckReloadOperation::FastForward Skipped FastForward");
            return;
        }

        FirearmsAnimator_0.SetAnimationSpeed(1f);
        State = EOperationState.Ready;
        OnIdleStartEvent();
    }

    public override void OnIdleStartEvent()
    {
        LoggerUtil.Debug("MagCheckReloadOperation::OnIdleStartEvent");
        base.OnIdleStartEvent();
    }

    public void SetReloadCalled()
    {
        _reloadCalled = true;
    }

    private static readonly int _externalMagCheckHash = Animator.StringToHash("CHECK");
    private static readonly int _externalMagWithInternalSupportCheckHash = Animator.StringToHash("CHECK MAG");
    private static readonly int _externalMagReloadOutHash = Animator.StringToHash("RELOAD OUT");
    private static readonly int _externalMagWithInternalSupportReloadOutHash = Animator.StringToHash("RELOAD OUT MAG");

    private int ReloadOutHash
    {
        get
        {
            var magCheckHash = FirearmsAnimator_0.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX).shortNameHash;
            if (magCheckHash == _externalMagCheckHash)
            {
                return _externalMagReloadOutHash;
            }
            if (magCheckHash == _externalMagWithInternalSupportCheckHash)
            {
                return _externalMagWithInternalSupportReloadOutHash;
            }

            LoggerUtil.Error($"Unsupported mag check hash: {magCheckHash} for weapon: {Weapon_0.ToFullString()}");
            OnIdleStartEvent();
            return _externalMagReloadOutHash;
        }
    }

    private enum SpeedState : byte
    {
        Normal,
        Slowed,
        Restored,
    }
}
