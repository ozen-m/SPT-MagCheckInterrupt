using System.Reflection;
using EFT;
using HarmonyLib;
using MagCheckInterrupt.Components;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.Patches;

/// <summary>
/// This patch allows the player to quick reload during a MagCheckReloadOperation
/// </summary>
public class CanQuickReloadPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(FirearmHandsInputTranslator), nameof(FirearmHandsInputTranslator.InIdleStateForInvokeOperation));
    }

    [PatchPostfix]
    protected static void Postfix(FirearmHandsInputTranslator __instance, ref bool __result)
    {
        if (__result) return;

        if (__instance._controller is FirearmController { CurrentOperation: MagCheckReloadOperation })
        {
            __result = true;
        }
    }
}
