using BepInEx;
using BepInEx.Bootstrap;
using EFT.InputSystem;
using HarmonyLib;
using MagCheckInterrupt.Components;
using MagCheckInterrupt.External;
using MagCheckInterrupt.Utils;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt;

[BepInPlugin("com.ozen.magcheckinterrupt", "MagCheckInterrupt", "1.1.0")]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]
public class MagCheckInterrupt : BaseUnityPlugin
{
    private PatchManager _patchManager;

    protected void Awake()
    {
        L.SetLogger(Logger);

        ConfigUtil.Init(Config);

        _patchManager = new PatchManager(this, true);
        _patchManager.EnablePatches();

        if (Chainloader.PluginInfos.ContainsKey("com.fika.core"))
        {
            External.Fika.Init();
        }
        if (Chainloader.PluginInfos.ContainsKey("com.tyfon.uifixes"))
        {
            UIFixes.Init();
        }

#if DEBUG
        // Hot reloading
        var currentPreset = (InputPreset)AccessTools.Field(typeof(InputManager), "_currentPreset").GetValue(null);
        if (currentPreset != null)
        {
            KeybindsUtil.UpdateKeys(currentPreset);
        }
#endif
    }

#if DEBUG
    protected void OnDestroy()
    {
        var floatingAmmoController = FloatingPanelController.Instance;
        if (floatingAmmoController != null)
        {
            Destroy(floatingAmmoController.gameObject);
        }

        if (Chainloader.PluginInfos.ContainsKey("com.tyfon.uifixes"))
        {
            UIFixes.Disable();
        }
        if (Chainloader.PluginInfos.ContainsKey("com.fika.core"))
        {
            External.Fika.Disable();
        }

        _patchManager?.DisablePatches();
        _patchManager = null;
    }
#endif
}
