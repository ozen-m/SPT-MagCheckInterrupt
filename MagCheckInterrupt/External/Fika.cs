using System;
using System.Reflection;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Communications;
using Fika.Core.Main.Components;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using MagCheckInterrupt.Components;
using MagCheckInterrupt.Net;
using MagCheckInterrupt.Utils;
using SPT.Reflection.Patching;

namespace MagCheckInterrupt.External;

public static class Fika
{
    /// <summary>
    /// For the host, the host's config settings.<br/>
    /// For the client, the client's original config settings
    /// </summary>
    private static string[] _cachedConfigValues;

    private static bool _configReceivedFromHost;

    public static void Init()
    {
        LoggerUtil.Info("Initializing Fika compatibility");

        // Thanks Tyfon for lending me this config sync code!

        // Calling new Action() myself is required.
        // Otherwise, the compiler will generate a static class to cache the action, and that class
        // is walked by tarkov at load, forcing a fika dll load, which pukes when fika is missing!
        FikaEventDispatcher.SubscribeEvent(new Action<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreated));
        FikaEventDispatcher.SubscribeEvent(new Action<PeerConnectedEvent>(OnPeerConnected));
        FikaEventDispatcher.SubscribeEvent(new Action<FikaRaidStartedEvent>(OnRaidStarted));
        FikaEventDispatcher.SubscribeEvent(new Action<FikaGameEndedEvent>(OnGameEnded));

        new ReloadMagPatch().Enable();
    }

    #region HANDLERS
    private static void OnFikaNetworkManagerCreated(FikaNetworkManagerCreatedEvent eventArgs)
    {
        switch (eventArgs.Manager)
        {
            case FikaClient client:
                ConfigUtil.SetReadOnly(true);
                ConfigUtil.DisplayIsUsingFikaHostConfig(true);
                _cachedConfigValues = ConfigUtil.GetConfigValues();
                client.RegisterPacket(new Action<ConfigPacket>(OnReceiveConfigPacket));
                break;
            case FikaServer:
                _cachedConfigValues = ConfigUtil.GetConfigValues();
                ConfigUtil.RegisterSettingsChanged(OnHostSettingsChanged);
                break;
        }

        eventArgs.Manager.RegisterPacket(new Action<ToReloadPacket>(OnReceiveToReloadPacket));
    }

    private static void OnPeerConnected(PeerConnectedEvent eventArgs)
    {
        if (!FikaBackendUtils.IsServer) return;

        LoggerUtil.Info($"Peer connected, sending config to peer {eventArgs.Peer.Id}");
        ConfigPacket packet = new(_cachedConfigValues);
        Singleton<FikaServer>.Instance.SendDataToPeer(ref packet, DeliveryMethod.ReliableUnordered, eventArgs.Peer);
    }

    private static void OnRaidStarted(FikaRaidStartedEvent ev)
    {
        if (ev.IsServer || _configReceivedFromHost) return;

        LoggerUtil.Error("Config packet not received! MagCheckInterrupt missing from host?");
        NotificationManagerClass.DisplayWarningNotification(
            "MagCheckInterrupt config sync failed, desync will occur! MagCheckInterrupt is required on the host or you have a different mod version with the host.",
            ENotificationDurationType.Infinite
        );
    }

    private static void OnReceiveConfigPacket(ConfigPacket packet)
    {
        LoggerUtil.Info("Received config packet, setting values from host");
        _configReceivedFromHost = ConfigUtil.SetConfigValues(packet.Config);
    }

    private static void OnReceiveToReloadPacket(ToReloadPacket packet)
    {
        if (!CoopHandler.TryGetCoopHandler(out var coopHandler)) return;
        if (!coopHandler.Players.TryGetValue(packet.NetId, out var player)) return;
        if (player.HandsController is not Player.FirearmController firearmController) return;
        if (firearmController.CurrentOperation is not MagCheckReloadOperation operation) return;

        operation.SetToReload();
    }

    private static void OnHostSettingsChanged(object sender, SettingChangedEventArgs eventArgs)
    {
        _cachedConfigValues = ConfigUtil.GetConfigValues(_cachedConfigValues);
        ConfigPacket packet = new(_cachedConfigValues);
        Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableUnordered, true);
    }

    private static void OnGameEnded(FikaGameEndedEvent eventArgs)
    {
        if (eventArgs.IsServer) return;

        ConfigUtil.SetConfigValues(_cachedConfigValues);
        ConfigUtil.DisplayIsUsingFikaHostConfig(false);
        ConfigUtil.SetReadOnly(false);
    }
    #endregion
}

#region PATCHES
/// <summary>
/// Fika runs FastForward before calling ReloadMag,
/// so we need to send a packet to set ToReload() on other clients.
/// </summary>
/// <seealso cref="MagCheckReloadOperation.FastForward"/>
[IgnoreAutoPatch]
public class ReloadMagPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(MagCheckReloadOperation).GetMethod(nameof(MagCheckReloadOperation.ReloadMag));
    }

    [PatchPrefix]
    protected static void Prefix(MagCheckReloadOperation __instance, ref Callback startCallback
    )
    {
        if (!__instance.Player_0.IsYourPlayer) return;

        // We need our packet sent first before Fika's reload packet
        startCallback = (Callback)Delegate.Combine(
            (Callback)SendMagInterruptPacket,
            startCallback
        );
    }

    private static void SendMagInterruptPacket(IResult result)
    {
        if (result.Failed) return;

        var networkManager = Singleton<IFikaNetworkManager>.Instance;
        var packet = new ToReloadPacket(networkManager.NetId);
        Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
    }
}
#endregion
