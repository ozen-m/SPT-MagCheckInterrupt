using System.Reflection;
using EFT.InputSystem;
using MagCheckInterrupt.Utils;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.Patches;

public class UpdateBindingsPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(InputPreset).GetMethod(nameof(InputPreset.UpdateBindings));
    }

    [PatchPostfix]
    public static void Postfix(InputPreset __instance)
    {
        KeybindsUtil.UpdateKeys(__instance);
    }
}
