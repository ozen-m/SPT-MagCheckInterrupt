using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using EFT.Communications;
using UnityEngine;

namespace MagCheckInterrupt.Utils;

public static class ConfigUtil
{
    public static ConfigEntry<KeybindsUtil.EReloadMode> ReloadMode { get; private set; }
    public static ConfigEntry<float> ReloadWindowStart { get; private set; }
    public static ConfigEntry<float> ReloadWindowEnd { get; private set; }
    public static ConfigEntry<bool> SlowAnimation { get; private set; }
    public static ConfigEntry<float> SlowAnimationStart { get; private set; }
    public static ConfigEntry<float> SlowAnimationEnd { get; private set; }
    public static ConfigEntry<float> SlowPercentage { get; private set; }
    public static ConfigEntry<float> SlowSmoothing { get; private set; }
    public static ConfigEntry<bool> FloatingAmmoDetails { get; private set; }
    public static ConfigEntry<float> OffsetPosX { get; private set; }
    public static ConfigEntry<float> OffsetPosY { get; private set; }
    public static ConfigEntry<float> OffsetPosZ { get; private set; }
    public static ConfigEntry<float> Scale { get; private set; }

    private static readonly List<ConfigEntryBase> _syncedConfigs = [];
    private static ConfigFile _configFile;
    private static ConfigEntry<bool> _fikaHostConfig;
    private static GUIStyle _centeredStyle;

    public static void Init(ConfigFile configFile)
    {
        _configFile = configFile;

        _fikaHostConfig = _configFile.Bind(
            string.Empty,
            "FikaHostConfig",
            false,
            new ConfigDescription(
                "This is displayed when a Fika Client is using the Host's config",
                null,
                new ConfigurationManagerAttributes
                {
                    Order = 33,
                    Browsable = false,
                    HideDefaultButton = true,
                    HideSettingName = true,
                    CustomDrawer = (_) =>
                    {
                        _centeredStyle ??= new GUIStyle(GUI.skin.label)
                        {
                            richText = true,
                            wordWrap = true,
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 18,
                        };

                        GUILayout.Label("<color=#4DA6FF><b>Configuration is set by the Fika Host</b></color>", _centeredStyle);
                    },
                }
            )
        );

        ReloadMode = _configFile.Bind(
            "General",
            "Reload Mode",
            KeybindsUtil.EReloadMode.Press,
            new ConfigDescription(
                "Special case for when reload and mag check keybinds are conflicting",
                null,
                new ConfigurationManagerAttributes { Order = 41 }
            )
        );
        ReloadWindowStart = _configFile.Bind(
            "General",
            "Reload Window Start",
            0.23f,
            new ConfigDescription(
                "How early you can reload during the magazine check animation, in normalized time",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 32, ShowRangeAsPercent = true, IsAdvanced = true }
            )
        );
        ReloadWindowEnd = _configFile.Bind(
            "General",
            "Reload Window End",
            0.6f,
            new ConfigDescription(
                "How late you can reload during the magazine check animation, in normalized time",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 31, ShowRangeAsPercent = true, IsAdvanced = true }
            )
        );

        SlowAnimation = _configFile.Bind(
            "Slow Animation",
            "Enable",
            true,
            new ConfigDescription(
                "Slow down the magazine check animation for a certain time",
                null,
                new ConfigurationManagerAttributes { Order = 15 }
            )
        );
        SlowPercentage = _configFile.Bind(
            "Slow Animation",
            "Slow Percentage",
            0.25f,
            new ConfigDescription(
                "Multiplier for the magazine check animation speed when Slow Animation is enabled",
                new AcceptableValueRange<float>(0.01f, 1f),
                new ConfigurationManagerAttributes { Order = 14, ShowRangeAsPercent = true, IsAdvanced = true }
            )
        );
        SlowAnimationStart = _configFile.Bind(
            "Slow Animation",
            "Start",
            0.3f,
            new ConfigDescription(
                "When to start slowing down the magazine check animation, in normalized time",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 13, ShowRangeAsPercent = true, IsAdvanced = true }
            )
        );
        SlowAnimationEnd = _configFile.Bind(
            "Slow Animation",
            "End",
            0.4f,
            new ConfigDescription(
                "When to restore speed of the magazine check animation, in normalized time",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 12, ShowRangeAsPercent = true, IsAdvanced = true }
            )
        );
        SlowSmoothing = _configFile.Bind(
            "Slow Animation",
            "Smoothing Max Delta",
            2f,
            new ConfigDescription(
                "Max delta for the smoothing of the slow animation. A higher value slows/restores the animation faster",
                new AcceptableValueRange<float>(0.01f, 10f),
                new ConfigurationManagerAttributes { Order = 11, IsAdvanced = true }
            )
        );

        FloatingAmmoDetails = _configFile.Bind(
            "Ammo Details",
            "Follows Magazine",
            true,
            new ConfigDescription(
                "If enabled, the ammo details UI follows the magazine",
                null,
                new ConfigurationManagerAttributes { Order = 5 }
            )
        );
        OffsetPosX = _configFile.Bind(
            "Ammo Details",
            "Position Offset X",
            -0.02f,
            new ConfigDescription(
                string.Empty,
                null,
                new ConfigurationManagerAttributes { Order = 4, IsAdvanced = true }
            )
        );
        OffsetPosY = _configFile.Bind(
            "Ammo Details",
            "Position Offset Y",
            0.075f,
            new ConfigDescription(
                string.Empty,
                null,
                new ConfigurationManagerAttributes { Order = 3, IsAdvanced = true }
            )
        );
        OffsetPosZ = _configFile.Bind(
            "Ammo Details",
            "Position Offset Z",
            -0.01f,
            new ConfigDescription(
                string.Empty,
                null,
                new ConfigurationManagerAttributes { Order = 2, IsAdvanced = true }
            )
        );
        Scale = _configFile.Bind(
            "Ammo Details",
            "Scale",
            1f,
            new ConfigDescription(
                string.Empty,
                new AcceptableValueRange<float>(0.0001f, 2f),
                new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }
            )
        );

        _syncedConfigs.Add(ReloadWindowStart);
        _syncedConfigs.Add(ReloadWindowEnd);
        _syncedConfigs.Add(SlowAnimation);
        _syncedConfigs.Add(SlowAnimationStart);
        _syncedConfigs.Add(SlowAnimationEnd);
        _syncedConfigs.Add(SlowPercentage);
        _syncedConfigs.Add(SlowSmoothing);
    }

    public static void RegisterSettingsChanged(EventHandler<SettingChangedEventArgs> eventArgs)
    {
        _configFile.SettingChanged += eventArgs;
    }

    public static void SetBrowsable(bool isBrowsable)
    {
        foreach (var config in _syncedConfigs)
        {
            foreach (var tag in config.Description.Tags)
            {
                if (tag is not ConfigurationManagerAttributes attr) continue;

                attr.Browsable = isBrowsable;
                break;
            }
        }
    }

    public static void DisplayIsUsingFikaHostConfig(bool usingFikaHost)
    {
        foreach (var tag in _fikaHostConfig.Description.Tags)
        {
            if (tag is not ConfigurationManagerAttributes attr) continue;

            attr.Browsable = usingFikaHost;
            return;
        }
    }

    public static bool SetConfigValues(string[] values)
    {
        if (values.Length != _syncedConfigs.Count)
        {
            L.Error(
                $"ConfigUtil::SetConfigValues ArgumentOutOfRange {nameof(values)}. Arg: {values.Length} != {_syncedConfigs.Count}"
            );
            NotificationManager.DisplayWarningNotification(
                "MagCheckInterrupt: Unable to set config values. Different mod version with the host?",
                ENotificationDurationType.Long
            );
            return false;
        }

        for (var i = 0; i < _syncedConfigs.Count; i++)
        {
            var config = _syncedConfigs[i];
            config.SetSerializedValue(values[i]);
        }

        return true;
    }

    public static string[] GetConfigValues(string[] preAllocated = null)
    {
        var configValues = preAllocated ?? new string[_syncedConfigs.Count];
        for (var i = 0; i < _syncedConfigs.Count; i++)
        {
            var config = _syncedConfigs[i];
            configValues[i] = config.GetSerializedValue();
        }

        return configValues;
    }
}
