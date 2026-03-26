using Comfort.Common;
using EFT.InventoryLogic;
using MagCheckInterrupt.External;
using MagCheckInterrupt.Patches;
using MagCheckInterrupt.Utils;
using UnityEngine;
using static EFT.Player;

namespace MagCheckInterrupt.Components;

public class MagCheckReloadOperation(FirearmController controller) : FirearmController.GClass2038(controller)
{
    private static readonly int _checkAnimationHash = Animator.StringToHash("CHECK");

    private bool _ammoDetailsShown;

    // Slow down animation fields
    private float _currentSpeed = 1f;
    private float _targetSpeed = 1f;
    private SpeedState _animSpeedState = SpeedState.Normal;

#if DEBUG
    private static AnimationDebugUI _animationDebugUI;
#endif

    public new void Start(EUtilityType utilityType)
    {
        base.Start(utilityType);

#if DEBUG
        LoggerUtil.Debug("MagCheckReloadOperation::Start");
        if (_animationDebugUI == null)
        {
            _animationDebugUI = AnimationDebugUI.Create(Player_0.gameObject, FirearmsAnimator_0, FirearmController_0);
        }
        else
        {
            _animationDebugUI.SetAnimatorAndController(FirearmsAnimator_0, FirearmController_0);
        }
#endif
    }

    public override void Update(float deltaTime)
    {
        if (State is EOperationState.Finished or not EOperationState.Ready)
        {
            return;
        }

        var currentStateInfo = FirearmsAnimator_0.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX);
        if (!IsInCheckAnimation(currentStateInfo))
        {
            return;
        }
        var normalizedTime = currentStateInfo.normalizedTime;

        // Show ammo details at the start of the reload window
        if (!_ammoDetailsShown && normalizedTime > MagCheckInterrupt.ReloadWindowStart.Value)
        {
            if (Player_0.FirstPersonPointOfView)
            {
                AmmoDetailsPatch.ShowLastAmmoDetail();
            }

            _ammoDetailsShown = true;
        }

        if (!MagCheckInterrupt.SlowAnimation.Value)
        {
            return;
        }

        switch (_animSpeedState)
        {
            case SpeedState.Normal:
                if (normalizedTime > MagCheckInterrupt.SlowAnimationStart.Value)
                {
                    _targetSpeed = MagCheckInterrupt.SlowPercentage.Value;
                    _animSpeedState = SpeedState.Slowed;
                }
                break;
            case SpeedState.Slowed:
                if (normalizedTime >= MagCheckInterrupt.SlowAnimationEnd.Value)
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
        _currentSpeed = Mathf.MoveTowards(
            _currentSpeed,
            _targetSpeed,
            MagCheckInterrupt.SlowSmoothing.Value * deltaTime
        );
        FirearmsAnimator_0.SetAnimationSpeed(_currentSpeed); // Set every frame okay?
    }

    public override void Reset()
    {
        _ammoDetailsShown = false;
        _animSpeedState = SpeedState.Normal;
        _currentSpeed = 1f;
        _targetSpeed = 1f;
        Bool_0 = false;
        Bool_1 = false;
        base.Reset();
    }

    public override bool CanStartReload()
    {
        var currentStateInfo = FirearmsAnimator_0.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX);
        var normalizedTime = currentStateInfo.normalizedTime;

        return IsInCheckAnimation(currentStateInfo)
               && normalizedTime > MagCheckInterrupt.ReloadWindowStart.Value
               && normalizedTime < MagCheckInterrupt.ReloadWindowEnd.Value;
    }

    /// <summary>
    /// Based on GClass2037.ReloadMag
    /// TODO: Does not get called in UIFixes reload in place
    /// </summary>
    public override void ReloadMag(
        MagazineItemClass magazine,
        ItemAddress itemAddress,
        Callback finishCallback,
        Callback startCallback
    )
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
            LoggerUtil.Error(
                $"MagCheckReloadOperation::ReloadMag Failed to run ReloadMag. Error: {reloadResult.Error}"
            );
            finishCallback?.Invoke(reloadResult);
            return;
        }

        if (FirearmsAnimator_0.Animator is GClass1446 animatorWrapper)
        {
            // GClass2016.Start calls FirearmsAnimator.Reload(bool b)
            // so we need to skip the reload animation and do our own crossfade
            ReloadAnimationPatch.SkipReloadAnimation();
            animatorWrapper.Animator_0.CrossFade(
                525784070, // Hash for `RELOAD OUT`
                0.10f, // Note: Anything more than 0.10f looks like a magazine swap
                FirearmsAnimator.HANDS_LAYER_INDEX,
                0.50f // Skip mag out from weapon animation
            );
        }
        else
        {
            var typeFullName = FirearmsAnimator_0.Animator.GetType().FullName;
            LoggerUtil.Warning(
                $"MagCheckReloadOperation::ReloadMag Cannot transition directly into a reload. {typeFullName}"
            );
        }

        if (Player_0.FirstPersonPointOfView)
        {
            AmmoDetailsPatch.HideAmmoCount();
        }

        State = EOperationState.Finished;
        FirearmController_0
            .InitiateOperation<FirearmController.GClass2016>()
            .Start(reloadResult.Value, finishCallback);
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
        // so we need to check if FirearmController is Fika's ObservedFirearmController
        // or else it finishes this operation and ReloadMag won't be called.
        // TODO: Could pose problems if FastForward is really needed.
        LoggerUtil.Debug("MagCheckReloadOperation::FastForward");
        if (Fika.IsObservedFirearmController(FirearmController_0))
        {
            LoggerUtil.Debug("MagCheckReloadOperation::FastForward Skipped FastForward");
            return;
        }

        State = EOperationState.Ready;
        OnIdleStartEvent();
    }

    public override void OnIdleStartEvent()
    {
        LoggerUtil.Debug("MagCheckReloadOperation::OnIdleStartEvent");
        base.OnIdleStartEvent();
    }

    /// <summary>
    /// AnimatorStateInfoWrapper.IsName(string name)
    /// </summary>
    private static bool IsInCheckAnimation(AnimatorStateInfoWrapper stateInfo)
    {
        return _checkAnimationHash == stateInfo.fullPathHash
               || _checkAnimationHash == stateInfo.shortNameHash
               || _checkAnimationHash == stateInfo.nameHash;
    }

    private enum SpeedState
    {
        Normal,
        Slowed,
        Restored
    }
}
