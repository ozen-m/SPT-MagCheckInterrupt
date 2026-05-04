using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MagCheckInterrupt.Components;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.Patches;

/// <summary>
/// This patch is for delaying the display of the ammo count panel (bottom right).
/// </summary>
public class AmmoDetailsPatch : ModulePatch
{
    private static readonly AccessTools.FieldRef<EftBattleUIScreen, AmmoCountPanel> _ammoCountPanelField =
        AccessTools.FieldRefAccess<EftBattleUIScreen, AmmoCountPanel>("_ammoCountPanel");

    private static AmmoDetails _lastAmmoDetail;
    private static bool _hasLastAmmoDetail;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GamePlayerOwner), nameof(GamePlayerOwner.method_8));
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
        if (!ShouldDelayAmmoDetails())
        {
            return true;
        }

        _lastAmmoDetail = new AmmoDetails(ammoCount, maxAmmoCount, mastering, details, foldingMechanimWeapon);
        _hasLastAmmoDetail = true;
        return false;
    }

    public static void ShowLastAmmoDetail()
    {
        if (!_hasLastAmmoDetail)
        {
            return;
        }

        Singleton<CommonUI>.Instance.EftBattleUIScreen.ShowAmmoDetails(
            _lastAmmoDetail.AmmoCount,
            _lastAmmoDetail.MaxAmmoCount,
            _lastAmmoDetail.Mastering,
            _lastAmmoDetail.Details,
            _lastAmmoDetail.FoldingMechanimWeapon
        );
    }

    public static void ClearLastAmmoDetail()
    {
        _hasLastAmmoDetail = false;
    }

    public static void HideAmmoCount()
    {
        var ammoCountPanel = _ammoCountPanelField(Singleton<CommonUI>.Instance.EftBattleUIScreen);
        ammoCountPanel.Hide();
    }

    private static bool ShouldDelayAmmoDetails()
    {
        return Singleton<GameWorld>.Instance?.MainPlayer?.HandsController is FirearmController
        {
            CurrentOperation: MagCheckReloadOperation,
        };
    }

    private readonly struct AmmoDetails(int ammoCount, int maxAmmoCount, int mastering, string details, bool foldingMechanimWeapon)
    {
        public readonly int AmmoCount = ammoCount;
        public readonly int MaxAmmoCount = maxAmmoCount;
        public readonly int Mastering = mastering;
        public readonly string Details = details;
        public readonly bool FoldingMechanimWeapon = foldingMechanimWeapon;
    }
}
