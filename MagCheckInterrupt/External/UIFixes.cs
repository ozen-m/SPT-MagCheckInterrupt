using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using MagCheckInterrupt.Components;
using MagCheckInterrupt.Utils;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.External;

public class UIFixes
{
    public static void Init()
    {
        LoggerUtil.Info("Initializing UI Fixes compatibility");

        new SwapReloadPatch().Enable();
    }
}

#region PATCHES
[IgnoreAutoPatch]
public class SwapReloadPatch : ModulePatch
{
    private static bool _skipPatch;

    protected override MethodBase GetTargetMethod()
    {
        if (Fika.IsPresent)
        {
            var type = Type.GetType("Fika.Core.Main.ClientClasses.HandsControllers.FikaClientFirearmController, Fika.Core");
            return AccessTools.Method(type, "ReloadMag");
        }

        return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.ReloadMag));
    }

    [PatchPostfix]
    [HarmonyPriority(Priority.Last - 10)]
    public static void Postfix(
        Player.FirearmController __instance,
        MagazineItemClass magazine,
        ItemAddress itemAddress,
        Callback callback,
        bool __runOriginal
    )
    {
        if (_skipPatch)
        {
            _skipPatch = false;
            return;
        }

        // If original ran, no reload in place occured
        if (__runOriginal || __instance.CurrentOperation is not MagCheckReloadOperation operation) return;

        // Our operation does not support swap and is not called, so queue reload for now.
        // We finish MagCheckReloadOperation then re-run ReloadMag to swap reload.
        _skipPatch = true;
        operation.FastForward();
        __instance.ReloadMag(magazine, itemAddress, callback);
    }
}
#endregion
