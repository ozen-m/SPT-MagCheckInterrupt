using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.Patches;

/// <summary>
/// This patch is for delaying the display of the ammo count panel (bottom right).
/// </summary>
public class AmmoDetailsPatch : ModulePatch
{
    private static AmmoDetails _lastAmmoDetails;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GamePlayerOwner), nameof(GamePlayerOwner.PlayerOnOnShowAmmoDetails));
    }

    [PatchPrefix]
    public static bool Prefix(
        GamePlayerOwner __instance,
        int ammoCount,
        int maxAmmoCount,
        int mastering,
        string details,
        bool foldingMechanimWeapon
    )
    {
        // Show for stationary weapons
        if (__instance.Player.MovementContext._stationaryWeapon != null)
        {
            return true;
        }

        _lastAmmoDetails = new AmmoDetails(ammoCount, maxAmmoCount, mastering, details, foldingMechanimWeapon);
        return false;
    }

    public static void ShowLastAmmoDetails()
    {
        Singleton<CommonUI>.Instance.EftBattleUIScreen.ShowAmmoDetails(
            _lastAmmoDetails.AmmoCount,
            _lastAmmoDetails.MaxAmmoCount,
            _lastAmmoDetails.Mastering,
            _lastAmmoDetails.Details,
            _lastAmmoDetails.FoldingMechanimWeapon
        );
    }

    public static AmmoDetails GetLastAmmoDetails()
    {
        return _lastAmmoDetails;
    }

    public static void HideAmmoCount()
    {
        Singleton<CommonUI>.Instance.EftBattleUIScreen._ammoCountPanel.Hide();
    }

    public readonly struct AmmoDetails(int ammoCount, int maxAmmoCount, int mastering, string details, bool foldingMechanimWeapon)
    {
        public readonly int AmmoCount = ammoCount;
        public readonly int MaxAmmoCount = maxAmmoCount;
        public readonly int Mastering = mastering;
        public readonly string Details = details;
        public readonly bool FoldingMechanimWeapon = foldingMechanimWeapon;
    }
}
