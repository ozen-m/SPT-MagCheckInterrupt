using System.Reflection;
using EFT;
using MagCheckInterrupt.Components;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.Patches;

/// <summary>
/// This patch skips reloading if the reload and check mag keys conflict on end of a <see cref="MagCheckReloadOperation"/>
/// </summary>
/// <seealso cref="MagCheckReloadOperation.OnIdleStartEvent"/>
public class ReloadConflictPatch : ModulePatch
{
    private static bool _toSkip;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(FirearmHandsInputTranslator).GetMethod(nameof(FirearmHandsInputTranslator.Reload));
    }

    [PatchPrefix]
    protected static bool Prefix(FirearmHandsInputTranslator __instance)
    {
        if (!_toSkip)
        {
            return true;
        }

        _toSkip = false;
        return false;
    }

    public static void SkipReload()
    {
        _toSkip = true;
    }
}
