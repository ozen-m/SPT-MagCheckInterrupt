using Comfort.Common;
using Diz.LanguageExtensions;
using EFT.InventoryLogic;
using MagCheckInterrupt.Patches;
using MagCheckInterrupt.Utils;
using UnityEngine;

namespace MagCheckInterrupt.Components;

public class MagCheckReloadOperation(FirearmController controller) : UtilityOperation(controller)
{
    private bool _ammoDetailsShown;
    private bool _reloadCalled;
    private AddSuboperation _swapAddSuboperation;

    #region Slow Down Animation Fields
    private float _currentSpeed = 1f;
    private float _targetSpeed = 1f;
    private SpeedState _animSpeedState = SpeedState.Normal;
    #endregion

#if DEBUG
    private static PlayerStateDebug _playerStateDebug;

    public new void Start(EUtilityType utilityType)
    {
        base.Start(utilityType);

        L.Debug("MagCheckReloadOperation::Start");
        if (_playerStateDebug == null)
        {
            _playerStateDebug = PlayerStateDebug.Create(Player);
        }
        else
        {
            _playerStateDebug.SetPlayer(Player);
        }
    }
#endif

    public override void Update(float deltaTime)
    {
        if (State != EOperationState.Ready) return;

        var normalizedTime = FirearmsAnimator.GetNormalizedTime(FirearmsAnimator.HANDS_LAYER_INDEX);
        if (!_ammoDetailsShown && normalizedTime > ConfigUtil.ReloadWindowStart.Value)
        {
            // Show ammo details at the start of the reload window
            if (Player.FirstPersonPointOfView)
            {
                AmmoDetailsPatch.ShowLastAmmoDetail();
            }

            _ammoDetailsShown = true;
        }

        if (!ConfigUtil.SlowAnimation.Value) return;

        switch (_animSpeedState)
        {
            case SpeedState.Normal when normalizedTime > ConfigUtil.SlowAnimationStart.Value:
                _targetSpeed = ConfigUtil.SlowPercentage.Value;
                _animSpeedState = SpeedState.Slowed;
                break;
            case SpeedState.Slowed when normalizedTime >= ConfigUtil.SlowAnimationEnd.Value:
                _targetSpeed = 1f;
                _animSpeedState = SpeedState.Restored;
                break;
        }

        // Smoothing
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, ConfigUtil.SlowSmoothing.Value * deltaTime);
        FirearmsAnimator.SetAnimationSpeed(_currentSpeed); // Set every frame okay?
    }

    public override void Reset()
    {
        _reloadCalled = false;
        _swapAddSuboperation = null;
        _ammoDetailsState = AmmoDetailsState.NotShown;
        _currentSpeed = 1f;
        _targetSpeed = 1f;
        _animSpeedState = SpeedState.Normal;
        _reloadFixed = false;
        _inInventory = false;
        base.Reset();
    }

    public override void OnUtilityOperationStartEvent()
    {
        base.OnUtilityOperationStartEvent();

        if (!Player.FirstPersonPointOfView || ConfigUtil.ReloadMode.Value != KeybindsUtil.EReloadMode.Press) return;

        if (KeybindsUtil.AreCheckAndReloadKeysConflicting())
        {
            ReloadConflictPatch.SkipReload();
        }
    }

    public override bool CanStartReload()
    {
        if (State != EOperationState.Ready) return false;

        var normalizedTime = FirearmsAnimator.GetNormalizedTime(FirearmsAnimator.HANDS_LAYER_INDEX);
        return normalizedTime > ConfigUtil.ReloadWindowStart.Value && normalizedTime < ConfigUtil.ReloadWindowEnd.Value;
    }

    /// <summary>
    /// Based on <see cref="Idling.ReloadMag"/>
    /// </summary>
    public override void ReloadMag(Magazine magazine, ItemAddress itemAddress, Callback finishCallback, Callback startCallback)
    {
        L.Debug("MagCheckReloadOperation::ReloadMag");
        FirearmsAnimator.SetAnimationSpeed(1f);

        DisableAimingOnReload();
        SetTriggerPressed(false);
        var reloadResult = ReloadExternalMagResult.Run(
            Player.InventoryController,
            Weapon,
            magazine,
            false,
            Weapon.MalfState.IsKnownMalfunction(Player.ProfileId),
            itemAddress
        );
        if (reloadResult.Failed)
        {
            L.Error($"MagCheckReloadOperation::ReloadMag Failed to reload. Error: {reloadResult.Error}");
            finishCallback?.Invoke(reloadResult);
            return;
        }

        this.TransitionToReload(false, false);
        State = EOperationState.Finished;
        Controller.InitiateOperation<ReloadExternalMagOperation>().Start(reloadResult.Value, finishCallback);
        startCallback?.Succeed();
    }

    /// <summary>
    /// Based on <see cref="Idling.QuickReloadMag"/>
    /// </summary>
    public override void QuickReloadMag(Magazine magazine, Callback finishCallback, Callback startCallback)
    {
        L.Debug("MagCheckReloadOperation::QuickReloadMag");
        FirearmsAnimator.SetAnimationSpeed(1f);

        DisableAimingOnReload();
        SetTriggerPressed(false);
        var reloadResult = ReloadExternalMagResult.Run(
            Player.InventoryController,
            Weapon,
            magazine,
            true,
            Weapon.MalfState.IsKnownMalfunction(Player.ProfileId),
            null
        );
        if (reloadResult.Failed)
        {
            L.Error($"MagCheckReloadOperation::QuickReloadMag Failed to reload. Error: {reloadResult.Error}");
            finishCallback?.Invoke(reloadResult);
            return;
        }

        this.TransitionToReload(true, false);
        State = EOperationState.Finished;
        Controller.InitiateOperation<ReloadExternalMagOperation>().Start(reloadResult.Value, finishCallback);
        startCallback?.Succeed();
    }

    /// <summary>
    /// UI Fixes' reload in place feature uses a SwapOperation.
    /// This override handles that swap.
    /// </summary>
    public override void Execute(IInventoryOperation operation, Callback callback)
    {
        if (operation is not IOneItemOperation { Item1: Magazine } oneItemOperation)
        {
            base.Execute(operation, callback);
            return;
        }

        // Insert magazine operation
        if (oneItemOperation.To1 is not null && oneItemOperation.To1.IsChildOf(Weapon))
        {
            L.Debug("MagCheckReloadOperation::Execute Insert mag operation");
            FirearmsAnimator.SetAnimationSpeed(1f);

            if (_swapAddSuboperation is null)
            {
                // Should not be null at this point, start idle operation
                L.Error("MagCheckReloadOperation::Execute Something unexpected happened while swapping magazines!");
                callback.Fail("Remove magazine operation is missing during execution of swap reload"); // Throws

                State = EOperationState.Finished;
                Controller.InitiateOperation<Idling>().Start();
                return;
            }

            State = EOperationState.Finished;
            Controller.InitiateOperation<SwapReloadOperation>()
               .Start((Magazine)_swapAddSuboperation._item, (Slot)_swapAddSuboperation._to.Container, callback);
            return;
        }

        // PullOutMagOperation operation
        if (oneItemOperation.From1 is not null
            && oneItemOperation.From1.IsChildOf(Weapon)
            && oneItemOperation is AddSuboperation removeOp)
        {
            L.Debug("MagCheckReloadOperation::Execute Remove mag operation...skipped");
            _swapAddSuboperation = removeOp;
        }

        callback.Succeed();
    }

    public override void SetInventoryOpened(bool opened)
    {
        Controller.InventoryOpened = opened;
        FirearmsAnimator.SetInventory(opened);
    }

    public override void FastForward()
    {
        // Fika runs FastForward before calling ReloadMag,
        // so we need a packet to set _reloadCalled or else it finishes this operation and ReloadMag won't be called.
        L.Debug("MagCheckReloadOperation::FastForward");
        if (_reloadCalled)
        {
            L.Debug("MagCheckReloadOperation::FastForward Skipped FastForward");
            return;
        }

        FirearmsAnimator.SetAnimationSpeed(1f);
        State = EOperationState.Ready;
        OnIdleStartEvent();
    }

    public override void OnIdleStartEvent()
    {
        L.Debug("MagCheckReloadOperation::OnIdleStartEvent");

        base.OnIdleStartEvent();

        if (!Player.FirstPersonPointOfView || ConfigUtil.ReloadMode.Value != KeybindsUtil.EReloadMode.Release) return;

        if (KeybindsUtil.AreCheckAndReloadKeysConflicting())
        {
            ReloadConflictPatch.SkipReload();
        }
    }

    public void SetReloadCalled()
    {
        _reloadCalled = true;
    }

    private enum SpeedState
    {
        Normal,
        Slowed,
        Restored,
    }
}
