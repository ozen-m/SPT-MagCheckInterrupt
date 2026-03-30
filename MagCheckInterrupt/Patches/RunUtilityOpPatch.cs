using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using MagCheckInterrupt.Components;
using MagCheckInterrupt.Utils;
using SPT.Reflection.Patching;
using static EFT.Player.FirearmController;

namespace MagCheckInterrupt.Patches;

/// <summary>
/// This patch replaces utility operations with our mag check reload operation.
/// </summary>
public class RunUtilityOpPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GClass2037).GetMethod(nameof(GClass2037.RunUtilityOperation));
    }

    [PatchPrefix]
    public static bool Prefix(GClass2037 __instance, GClass2038.EUtilityType utilityType)
    {
        if (utilityType != GClass2038.EUtilityType.CheckMagazine)
        {
            return true;
        }

        if (__instance.Player_0.IsAI)
        {
            return true;
        }

        if (!WeaponUsesExternalMag(__instance.Weapon_0))
        {
            // Show ammo details since we skip it through `AmmoDetailsPatch`
            if (__instance.Player_0.FirstPersonPointOfView)
            {
                AmmoDetailsPatch.ShowLastAmmoDetail();
            }

            return true;
        }

        LoggerUtil.Debug("RunUtilityOpPatch::Prefix Initiate MagCheckReloadOperation");
        __instance.Player_0.BodyAnimatorCommon.SetFloat(PlayerAnimator.RELOAD_FLOAT_PARAM_HASH, 1f);
        __instance.State = Player.EOperationState.Finished;
        __instance.FirearmController_0.InitiateOperation<MagCheckReloadOperation>().Start(utilityType);
        return false;
    }

    private static bool WeaponUsesExternalMag(Weapon weapon)
    {
        return weapon.ReloadMode == Weapon.EReloadMode.ExternalMagazine
            || weapon.ReloadMode == Weapon.EReloadMode.ExternalMagazineWithInternalReloadSupport;
    }
}
