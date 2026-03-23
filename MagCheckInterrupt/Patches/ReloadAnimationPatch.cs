using System.Reflection;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.Patches;

/// <summary>
/// This patch skips the reload animation.
/// </summary>
public class ReloadAnimationPatch : ModulePatch
{
    private static bool _shouldSkipReloadAnimation;

    protected override MethodBase GetTargetMethod()
    {
        return typeof(FirearmsAnimator).GetMethod(nameof(FirearmsAnimator.Reload), [typeof(bool)]);
    }

    [PatchPrefix]
    public static bool Prefix(FirearmsAnimator __instance)
    {
        if (!_shouldSkipReloadAnimation) return true;

        _shouldSkipReloadAnimation = false;
        return false;
    }

    public static void SkipReloadAnimation()
    {
        _shouldSkipReloadAnimation = true;
    }
}
