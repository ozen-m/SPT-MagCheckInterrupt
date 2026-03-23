using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt;

[BepInPlugin("com.ozen.magcheckinterrupt", "Mag Check Interrupt", "1.0.0")]
public class MagCheckInterrupt : BaseUnityPlugin
{
    public static ManualLogSource LogSource { get; private set; }

    public static ConfigEntry<float> ReloadWindowStart { get; private set; }
    public static ConfigEntry<float> ReloadWindowEnd { get; private set; }
    public static ConfigEntry<bool> SlowAnimation { get; private set; }
    public static ConfigEntry<float> SlowAnimationStart { get; private set; }
    public static ConfigEntry<float> SlowAnimationEnd { get; private set; }
    public static ConfigEntry<float> SlowPercentage { get; private set; }
    public static ConfigEntry<float> SlowSmoothing { get; private set; }

    protected void Awake()
    {
        LogSource = Logger;

        ReloadWindowStart = Config.Bind(
            "General",
            "Reload Window Start",
            0.1f,
            new ConfigDescription(
                "How early you can reload during the check magazine animation",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 32, ShowRangeAsPercent = true, IsAdvanced = true  }
            )
        );
        ReloadWindowEnd = Config.Bind(
            "General",
            "Reload Window End",
            0.6f,
            new ConfigDescription(
                "How late you can reload during the check magazine animation",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 31, ShowRangeAsPercent = true, IsAdvanced = true  }
            )
        );
        SlowAnimation = Config.Bind(
            "Slow Animation",
            "Enable",
            true,
            new ConfigDescription(
                "Slow down the check magazine animation for a certain time",
                null,
                new ConfigurationManagerAttributes { Order = 15 }
            )
        );
        SlowPercentage = Config.Bind(
            "Slow Animation",
            "Slow Percentage",
            0.25f,
            new ConfigDescription(
                "Multiplier for the check magazine animation speed",
                new AcceptableValueRange<float>(0.01f, 1f),
                new ConfigurationManagerAttributes { Order = 14, ShowRangeAsPercent = true, IsAdvanced = true }
            )
        );
        SlowAnimationStart = Config.Bind(
            "Slow Animation",
            "Start",
            0.3f,
            new ConfigDescription(
                "When to start slowing down the check magazine animation",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 13, ShowRangeAsPercent = true, IsAdvanced = true }
            )
        );
        SlowAnimationEnd = Config.Bind(
            "Slow Animation",
            "End",
            0.4f,
            new ConfigDescription(
                "When to restore speed of the check magazine animation",
                new AcceptableValueRange<float>(0f, 1f),
                new ConfigurationManagerAttributes { Order = 12, ShowRangeAsPercent = true, IsAdvanced = true }
            )
        );
        SlowSmoothing = Config.Bind(
            "Slow Animation",
            "Smoothing Max Delta",
            2f,
            new ConfigDescription(
                "Max delta for the smoothing of the slow animation. A higher value slows/restores the animation faster",
                new AcceptableValueRange<float>(0.01f, 10f),
                new ConfigurationManagerAttributes { Order = 11, IsAdvanced = true }
            )
        );

        var patchManager = new PatchManager(this, true);
        patchManager.EnablePatches();
    }
}
