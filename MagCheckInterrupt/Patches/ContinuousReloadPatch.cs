using System.Reflection;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.Patches;

public class ContinuousReloadPatch : ModulePatch
{
    private static bool _toSkipReload;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(Class1730).GetMethod(nameof(Class1730.method_13));
    }

    [PatchPrefix]
    protected static bool Prefix(Class1730 __instance)
    {
        if (!_toSkipReload) return true;

        _toSkipReload = false;
        return false;
    }

    public static void SkipReload()
    {
        _toSkipReload = true;
    }
}
