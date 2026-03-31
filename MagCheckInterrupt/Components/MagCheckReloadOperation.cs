using Comfort.Common;
using EFT.InventoryLogic;
using MagCheckInterrupt.Patches;
using MagCheckInterrupt.Utils;
using UnityEngine;
using static EFT.Player;

namespace MagCheckInterrupt.Components;

public class MagCheckReloadOperation(FirearmController controller) : FirearmController.GClass2038(controller)
{
    private static readonly int _magCheckHash = Animator.StringToHash("CHECK");
    private static readonly int _magReloadOutHash = Animator.StringToHash("RELOAD OUT");
    private static readonly int _magWithInternalCheckHash = Animator.StringToHash("CHECK MAG");
    private static readonly int _magWithInternalReloadOutHash = Animator.StringToHash("RELOAD OUT MAG");
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
        else
        {
            _playerStateDebug.SetPlayer(Player_0);
        }
#endif
    }

    public override void Update(float deltaTime)
    {
        if (State != EOperationState.Ready) return;

        var normalizedTime = FirearmsAnimator_0.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX).normalizedTime;
        if (!_ammoDetailsShown && normalizedTime > ConfigUtil.ReloadWindowStart.Value)
        {
            // Show ammo details at the start of the reload window
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

        var normalizedTime = FirearmsAnimator_0.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX).normalizedTime;
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
            LoggerUtil.Error($"MagCheckReloadOperation::ReloadMag Failed to reload. Error: {reloadResult.Error}");
            finishCallback?.Invoke(reloadResult);
            return;
        }

        TransitionToReload(false);
        State = EOperationState.Finished;
        FirearmController_0.InitiateOperation<FirearmController.GClass2016>().Start(reloadResult.Value, finishCallback);
        startCallback?.Succeed();
    }

    /// <summary>
    /// UI Fixes' reload in place feature uses a SwapOperation.
    /// This override handles that swap.
    /// </summary>
    public override void Execute(GInterface438 operation, Callback callback)
    {
        if (operation is not GInterface443 { Item1: MagazineItemClass } oneItemOperation)
        {
            base.Execute(operation, callback);
            return;
        }

#if DEBUG
        // This operation only runs for weapons with ReloadMode.ExternalMagazine/ExternalMagazineWithInternalReloadSupport,
        // see Patches.RunUtilityOpPatch
        var reloadMode = Weapon_0.ReloadMode;
        if (reloadMode == Weapon.EReloadMode.InternalMagazine || reloadMode == Weapon.EReloadMode.OnlyBarrel)
        {
            callback.Succeed();
            return;
        }
#endif

        // Insert magazine operation
        if (oneItemOperation.To1 is not null && oneItemOperation.To1.IsChildOf(Weapon_0))
        {
            LoggerUtil.Debug("MagCheckReloadOperation::Execute Insert mag operation");
            FirearmsAnimator_0.SetAnimationSpeed(1f);

            var insertResult = FirearmController.GClass2005.Run(Player_0.InventoryController, Weapon_0, Player_0.ProfileId);
            if (insertResult.Failed)
            {
                callback.Invoke(insertResult);
                return;
            }

            TransitionToReload(true);
            State = EOperationState.Finished;
            FirearmController_0.InitiateOperation<FirearmController.GClass2039>().Start(insertResult.Value, callback);
            return;
        }

#if DEBUG
        // Remove magazine operation
        if (oneItemOperation.From1 is not null && oneItemOperation.From1.IsChildOf(Weapon_0))
        {
            LoggerUtil.Debug("MagCheckReloadOperation::Execute Remove mag operation...skipped");
        }
#endif

        callback.Succeed();
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

    /// <summary>
    /// Performs the animation from a magazine check to a reload.
    /// Also includes, hiding the ammo count and sending of Fika packet.
    /// </summary>
    /// <param name="isSwap">True if the reload is triggered by a swap operation, done by UI Fixes; otherwise, false (normal reload)</param>
    private void TransitionToReload(bool isSwap)
    {
        if (FirearmsAnimator_0.Animator is GClass1446 animatorWrapper && GetReloadOutHash(out var reloadOutHash))
        {
            // GClass2016.Start calls FirearmsAnimator.Reload(bool b), so we need to skip the reload animation and do our own crossfade.
            // But if it's a swap reload, no need to skip the reload animation, it's not called by the insert mag operation.
            if (!isSwap)
            {
                ReloadAnimationPatch.SkipReloadAnimation();
            }
            animatorWrapper.Animator_0.CrossFade(
                reloadOutHash,
                isSwap ? 0.15f : 0.10f, // Note: Anything more than 0.10f looks like a magazine swap
                FirearmsAnimator.HANDS_LAYER_INDEX,
                0.50f // Skip mag out from weapon animation
            );
        }
        else
        {
            var typeFullName = FirearmsAnimator_0.Animator.GetType().FullName;
            LoggerUtil.Warning($"MagCheckReloadOperation::TransitionToReload Cannot transition directly into a reload. {typeFullName}");
        }

        if (!Player_0.FirstPersonPointOfView) return;

        // We don't want observed players re-sending packets and
        // our packet needs to be sent first before Fika's reload packet (ReloadMag.startCallback).
        // But if it's a swap reload, no need to send a packet.
        if (!isSwap && External.Fika.IsPresent)
        {
            External.Fika.SendReloadCalledPacket();
        }

        AmmoDetailsPatch.HideAmmoCount();
    }

    private bool GetReloadOutHash(out int reloadOutHash)
    {
        var currentStateHash = FirearmsAnimator_0.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX).shortNameHash;
        if (currentStateHash == _magCheckHash)
        {
            reloadOutHash = _magReloadOutHash;
            return true;
        }
        if (currentStateHash == _magWithInternalCheckHash)
        {
            reloadOutHash = _magWithInternalReloadOutHash;
            return true;
        }

        LoggerUtil.Error(
            $"Unsupported mag check hash: {GClass758.GetAnimStateByNameHash(currentStateHash)} ({currentStateHash}) for weapon: {Weapon_0.ToFullString()}"
        );
        reloadOutHash = -1;
        return false;
    }

    private enum SpeedState : byte
    {
        Normal,
        Slowed,
        Restored,
    }
}
