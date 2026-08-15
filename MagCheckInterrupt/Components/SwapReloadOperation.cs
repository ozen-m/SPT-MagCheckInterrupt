using System;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.InventoryLogic;
using MagCheckInterrupt.Utils;

namespace MagCheckInterrupt.Components;

/// <summary>
/// Combines a remove and insert magazine operation
/// </summary>
public class SwapReloadOperation(FirearmController controller) : FirearmOperation(controller)
{
    private Slot _weaponMagazineSlot;
    private Callback _finishCallback;
    private InsertMagResult _insertMagResult;

    private bool _isMagazineWithBelt;
    private bool _magPulledOutFromWeapon;
    private bool _magPuttedToRig;
    private bool _magAppeared;
    private bool _shellEjected;
    private bool _magInsertedToWeapon;
    private bool _addedAmmoInChamber;
    private bool _ammoRemovedFromChamber;

    /// <summary>
    /// Does both <see cref="PullOutMagOperation"/> and <see cref="InsertMagOperation"/>
    /// </summary>
    /// <param name="magazine">Removed magazine</param>
    /// <param name="from">Slot magazine was removed from</param>
    public virtual void Start(Magazine magazine, Slot from, Callback finishCallback)
    {
        L.Debug("SwapReloadOperation::Start");

        _weaponMagazineSlot = from;
        _finishCallback = finishCallback;
        _isMagazineWithBelt = magazine.IsMagazineWithBelt;
        base.Start();
        this.TransitionToReload(false, true);

        FirearmsAnimator.SetFire(false);
        FirearmsAnimator.SetIsExternalMag(true);
        FirearmsAnimator.SetCanReload(true); // True to proceed with the next magazine
        Player.MovementContext.SetBlindFire(0);
        Player.BodyAnimatorCommon.SetFloat(PlayerAnimator.RELOAD_FLOAT_PARAM_HASH, 1f);
        Controller.bool_1 = true; // Block trigger

        if (Weapon.IsBeltMachineGun)
        {
            Controller.IsAiming = false;
        }

        if (!Weapon.MustBoltBeOpennedForExternalReload)
        {
            _shellEjected = true;
            _ammoRemovedFromChamber = true;
            if (Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire)
            {
                FirearmsAnimator.SetLayerWeight(FirearmsAnimator.MALFUNCTION_LAYER_INDEX, 0);
            }
        }
        else if (Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire)
        {
            FirearmsAnimator.SetAmmoInChamber(1f);
            FirearmsAnimator.SetLayerWeight(FirearmsAnimator.MALFUNCTION_LAYER_INDEX, 0);
        }

        // isReleased
        if (Weapon.IsBoltCatch
            && Weapon.ChamberAmmoCount == 1
            && !Weapon.ManualBoltCatch
            && !Weapon.MustBoltBeOpennedForExternalReload
            && !Weapon.MustBoltBeOpennedForInternalReload)
        {
            FirearmsAnimator.SetBoltCatch(false);
        }
    }

    public override void Reset()
    {
        L.Debug("SwapReloadOperation::Reset");

        _weaponMagazineSlot = null;
        _insertMagResult = null;
        _finishCallback = null;
        _isMagazineWithBelt = false;
        _magPulledOutFromWeapon = false;
        _magPuttedToRig = false;
        _magAppeared = false;
        _shellEjected = false;
        _magInsertedToWeapon = false;
        _addedAmmoInChamber = false;
        _ammoRemovedFromChamber = false;
        base.Reset();
    }

    public override void OnMagPulledOutFromWeapon()
    {
        L.Debug("SwapReloadOperation::OnMagPulledOutFromWeapon");

        if (_magPulledOutFromWeapon) return;

        _magPulledOutFromWeapon = true;
        FirearmsAnimator.SetAmmoOnMag(0);
        FirearmsAnimator.SetMagInWeapon(false);
        if (Controller.HasBipod)
        {
            Controller.FirearmsAnimator.SetBipod(Controller.BipodState);
        }
    }

    public override void OnMagPuttedToRig()
    {
        L.Debug("SwapReloadOperation::OnMagPuttedToRig");

        if (_magPuttedToRig) return;

        _magPuttedToRig = true;
        Firearms.RemoveMod(_weaponMagazineSlot);

        if (_isMagazineWithBelt)
        {
            Controller._weaponPrefab.UpdateAnimatorHierarchy();
            if (Controller.HasBipod)
            {
                Controller.FirearmsAnimator.SetBipod(Controller.BipodState);
            }
        }

        // Run insert magazine
        var insertResult = InsertMagResult.Run(Player.InventoryController, Weapon, Player.ProfileId);
        if (insertResult.Failed)
        {
            L.Error($"MagCheckReloadOperation::OnMagPuttedToRig Insert mag operation failed: {insertResult.Error}");
            FirearmsAnimator.SetCanReload(false);
            _finishCallback.Invoke(insertResult);

            State = EOperationState.Finished;
            Controller.InitiateOperation<Idling>().Start();
            return;
        }

        _insertMagResult = insertResult.Value;
    }

    public override void OnShellEjectEvent()
    {
        L.Debug("SwapReloadOperation::OnShellEjectEvent");

        if (_shellEjected) return;

        _shellEjected = true;
        if (Weapon.MustBoltBeOpennedForExternalReload && Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire)
        {
            Firearms.CreatePatronInShellPort(Weapon.MalfState.MalfunctionedAmmo, 0);
            Firearms.StartSpawnMisfiredCartridge(Player.Velocity * 0.66f);
            return;
        }

        foreach (var chamber in Weapon.Chambers)
        {
            if (chamber.ContainedItem is not Ammo { IsUsed: false } ammoItemClass) continue;

            Firearms.MoveAmmoFromChamberToShellPort(ammoItemClass.IsUsed, 0);
            Firearms.StartSpawnShell(Player.Velocity * 0.66f, 0);
            return;
        }

        L.Warning("SwapReloadOperation::OnShellEjectEvent No unused ammo found in chambers?");
    }

    public override void RemoveAmmoFromChamber()
    {
        L.Debug("SwapReloadOperation::RemoveAmmoFromChamber");

        if (_ammoRemovedFromChamber) return;

        _ammoRemovedFromChamber = true;
        if (Weapon.MustBoltBeOpennedForExternalReload && Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire)
        {
            method_2();
            FirearmsAnimator.SetAmmoInChamber(Weapon.ChamberAmmoCount);
            return;
        }

        foreach (var slot in Weapon.Chambers)
        {
            if (slot.ContainedItem is Ammo { IsUsed: false } ammoInChamber && slot.RemoveItem(false).Succeeded)
            {
                // Below line is missing in GClass2050.RemoveAmmoFromChamber, but is in GClass2016.RemoveAmmoFromChamber
                Firearms.ThrowPatronAsLoot(ammoInChamber, Player, "SwapReloadOperation.RemoveAmmoFromChamber");
                break;
            }
        }

        FirearmsAnimator.SetAmmoInChamber(Weapon.ChamberAmmoCount);
    }

    public override void OnMagAppeared()
    {
        L.Debug("SwapReloadOperation::OnMagAppeared");

        if (_magAppeared) return;

        _magAppeared = true;
        InsertMagazine();
        Firearms.SetupMod(
            _insertMagResult.MagazineSlot.Slot,
            Singleton<ObjectsFactory>.Instance.CreateItem(_insertMagResult.Magazine, true)
        );

        if (_insertMagResult.Magazine.IsMagazineWithBelt)
        {
            Controller._weaponPrefab.UpdateAnimatorHierarchy();
            if (Controller.HasBipod)
            {
                Controller.FirearmsAnimator.SetBipod(Controller.BipodState);
            }
        }
    }

    public override void OnMagInsertedToWeapon()
    {
        L.Debug("SwapReloadOperation::OnMagInsertedToWeapon");

        if (_magInsertedToWeapon) return;

        _magInsertedToWeapon = true;
        FirearmsAnimator.SetAmmoOnMag(_insertMagResult.MagazineAmmoCount + (_insertMagResult.HasNewAmmo ? 1 : 0));
        FirearmsAnimator.SetMagInWeapon(true);
        FirearmsAnimator.SetAmmoCompatible(_insertMagResult.AmmoCompatible);

        if (!_insertMagResult.HasNewAmmo
            && (Weapon.MalfState.State != Weapon.EMalfunctionState.Misfire
                || !Weapon.MalfState.IsKnownMalfunction(Player.ProfileId)
                || !_insertMagResult.AmmoCompatible
                || _insertMagResult.Magazine.Count <= 0))
        {
            EndOperation();
        }

        if (Controller.HasBipod)
        {
            Controller.FirearmsAnimator.SetBipod(Controller.BipodState);
        }
    }

    public override void OnOnOffBoltCatchEvent(bool isCaught)
    {
        L.Debug("SwapReloadOperation::OnOnOffBoltCatchEvent");
        FirearmsAnimator.SetBoltCatch(isCaught);
    }

    public override void OnAddAmmoInChamber()
    {
        L.Debug("SwapReloadOperation::OnAddAmmoInChamber");

        if (_addedAmmoInChamber) return;

        _addedAmmoInChamber = true;
        FirearmsAnimator.SetAmmoOnMag(_insertMagResult.Magazine.Count);
        if (Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire)
        {
            method_2();
        }
        if (_insertMagResult.HasNewAmmo)
        {
            Firearms.SetRoundIntoWeapon(_insertMagResult.NewAmmo, 0);
        }
        FirearmsAnimator.SetAmmoInChamber(_insertMagResult.Weapon.ChamberAmmoCount);
        EndOperation();
    }

    public override void SetInventoryOpened(bool opened)
    {
        L.Debug("SwapReloadOperation::SetInventoryOpened");

        Controller.InventoryOpened = opened;
        FirearmsAnimator.SetInventory(opened);
    }

    public override void HideWeapon(Action onHidden, bool fastDrop, Item nextControllerItem = null)
    {
        L.Debug("SwapReloadOperation::HideWeapon");

        State = EOperationState.Finished;
        Controller.RecalculateErgonomic();
        Controller.IsTriggerPressed = false;
        Controller.IsAiming = false;
        State = EOperationState.Finished; // Idk why BSG set this twice
        Controller.InitiateOperation<Remove>().Start(onHidden, fastDrop, nextControllerItem);
    }

    public override bool CanChangeLightState(LightsState[] lightsStates)
    {
        return false;
    }

    public override void FastForward()
    {
        L.Debug("SwapReloadOperation::FastForward");

        RemoveAmmoFromChamber();
        OnShellEjectEvent();
        OnMagPulledOutFromWeapon();
        OnMagPuttedToRig();
        OnMagAppeared();
        OnMagInsertedToWeapon();
        OnAddAmmoInChamber();
        if (State != EOperationState.Finished)
        {
            EndOperation();
        }
    }

    private void InsertMagazine()
    {
        L.Debug("SwapReloadOperation::InsertMagazine");

        FirearmsAnimator.SetMagTypeNew(_insertMagResult.Magazine.magAnimationIndex);
        FirearmsAnimator.SetMagTypeCurrent(_insertMagResult.Magazine.magAnimationIndex);

        // isReleased
        if (Weapon.IsBoltCatch
            && Weapon.ChamberAmmoCount == 1
            && !_insertMagResult.HasNewAmmo
            && !Weapon.ManualBoltCatch
            && !Weapon.MustBoltBeOpennedForExternalReload
            && !Weapon.MustBoltBeOpennedForInternalReload)
        {
            FirearmsAnimator.SetBoltCatch(false);
        }

        // isMalfunction
        if (Weapon.MalfState.State == Weapon.EMalfunctionState.Misfire
            && Weapon.MalfState.IsKnownMalfunction(Player.ProfileId)
            && _insertMagResult.Magazine.Count > 0
            && _insertMagResult.AmmoCompatible)
        {
            FirearmsAnimator.SetAmmoInChamber(0f);
            FirearmsAnimator.SetLayerWeight(FirearmsAnimator.MALFUNCTION_LAYER_INDEX, 0);
        }
    }

    private void EndOperation()
    {
        L.Debug("SwapReloadOperation::EndOperation");

        State = EOperationState.Finished;
        Controller.RecalculateErgonomic();
        Controller.InitiateOperation<Idling>().Start(null);
        _finishCallback?.Succeed();
        Controller.WeaponModified();
    }
}
