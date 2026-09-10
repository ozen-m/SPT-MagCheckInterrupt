/*
    Magazine Check Interrupt - Seamlessly transition from a magazine check to a reload!
    Copyright (C) 2026  ozen-m

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using BepInEx;
using BepInEx.Bootstrap;
using EFT.InputSystem;
using HarmonyLib;
using MagCheckInterrupt.Components;
using MagCheckInterrupt.External;
using MagCheckInterrupt.Utils;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt;

[BepInPlugin("com.ozen.magcheckinterrupt", "Magazine Check Interrupt", "1.1.1")]
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
            FikaHandler.Init();
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

    protected void OnApplicationQuit()
    {
        FikaHandler.RestoreConfig();
    }

#if DEBUG
    protected void OnDestroy()
    {
        Destroy(PlayerStateDebug.Instance);

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
            FikaHandler.Disable();
        }

        _patchManager?.DisablePatches();
        _patchManager = null;
    }
#endif
}
