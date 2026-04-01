using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;
using MagCheckInterrupt.Components;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.Patches;

public class OperationFactoryPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.GetOperationFactoryDelegates));
    }

    [PatchPostfix]
    public static void Postfix(
        Player.FirearmController __instance,
        Dictionary<Type, Player.ItemHandsController.OperationFactoryDelegate> __result
    )
    {
        __result.TryAdd(typeof(MagCheckReloadOperation), () => new MagCheckReloadOperation(__instance));
        __result.TryAdd(typeof(SwapReloadOperation), () => new SwapReloadOperation(__instance));
    }
}
