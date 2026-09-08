using System.Reflection;
using EFT.InventoryLogic.Operations;
using HarmonyLib;
using MagCheckInterrupt.Components;
using MagCheckInterrupt.Utils;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.External;

public static class UIFixes
{
    public static void Init()
    {
        L.Info("Initializing UI Fixes compatibility");

        new CanExecuteSwapPatch().Enable();
    }

    public static void Disable()
    {
        new CanExecuteSwapPatch().Disable();
    }
}

#region PATCHES
/// <summary>
/// This allows execution of <see cref="SwapOperationClass"/> during a <see cref="MagCheckReloadOperation"/>.
/// </summary>
[IgnoreAutoPatch]
public class CanExecuteSwapPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(FirearmController), nameof(FirearmController.CanExecute));
    }

    [PatchPostfix]
    public static void Postfix(FirearmController __instance, IInventoryOperation operation, ref bool __result)
    {
        if (__result)
        {
            return;
        }
        if (__instance.CurrentOperation is not MagCheckReloadOperation)
        {
            return;
        }
        if (operation is not (SwapOperation or AddSuboperation or RemoveSuboperation))
        {
            return;
        }

        __result = true;
    }
}
#endregion
