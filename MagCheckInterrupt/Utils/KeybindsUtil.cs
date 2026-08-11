using System.ComponentModel;
using EFT.InputSystem;

namespace MagCheckInterrupt.Utils;

public static class KeybindsUtil
{
    private static InputKeyCombination _reloadKeybind;
    private static InputKeyCombination _checkKeybind;

    public static void UpdateKeys(InputPreset keys)
    {
        foreach (var key in keys._workingKeyCombinations)
        {
            if (key is not InputKeyCombination keybind) continue;

            switch (keybind.GameKey)
            {
                case EGameKey.ReloadWeapon:
                    _reloadKeybind = keybind;
                    continue;
                case EGameKey.CheckAmmo:
                    _checkKeybind = keybind;
                    continue;
            }
        }
    }

    public static bool AreCheckAndReloadKeysConflicting()
    {
        return _reloadKeybind._state.GetKeysStatus(out var reloadStatus)
               && (reloadStatus == EKeyPress.Down || reloadStatus == EKeyPress.Hold)
               && _checkKeybind._state.GetKeysStatus(out var checkStatus)
               && (checkStatus == EKeyPress.Down || checkStatus == EKeyPress.Hold);
    }

    public enum EReloadMode
    {
        [Description("Press to Reload")]
        Press,

        [Description("Release to Reload")]
        Release,
    }
}
