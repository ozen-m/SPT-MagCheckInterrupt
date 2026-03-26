using System;
using EFT;
using HarmonyLib;

namespace MagCheckInterrupt.External;

public static class Fika
{
    // TODO: Sync settings!
    private static readonly Type _observedFirearmControllerType =
        AccessTools.TypeByName("Fika.Core.Main.ObservedClasses.HandsControllers.ObservedFirearmController");

    public static bool IsObservedFirearmController(Player.FirearmController instance)
    {
        return _observedFirearmControllerType != null && _observedFirearmControllerType.IsInstanceOfType(instance);
    }
}
