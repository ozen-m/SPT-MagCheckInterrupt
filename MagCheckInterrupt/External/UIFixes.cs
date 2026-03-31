using System.Reflection;
using EFT;
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

        new CanExecuteSwapPatch().Enable();
    }
}

#region PATCHES
/// <summary>
/// This allows execution of SwapOperations during a MagCheckReloadOperation.
/// </summary>
[IgnoreAutoPatch]
public class CanExecuteSwapPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.CanExecute));
    }

    [PatchPostfix]
    public static void Postfix(Player.FirearmController __instance, GInterface438 operation, ref bool __result)
    {
        if (__result) return;
        if (__instance.CurrentOperation is not MagCheckReloadOperation) return;
        if (operation is not (SwapOperationClass or GClass3397 or GClass3398)) return;

        __result = true;
    }
}
#endregion
