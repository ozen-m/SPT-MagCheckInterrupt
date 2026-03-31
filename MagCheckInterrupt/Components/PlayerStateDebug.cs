using System.Text;
using EFT;
using UnityEngine;

namespace MagCheckInterrupt.Components;

public class PlayerStateDebug : MonoBehaviour
{
    private readonly StringBuilder _debugText = new();
    private bool _init;
    private Player _player;

    public static PlayerStateDebug Create(Player player)
    {
        var debugGui = player.GetOrAddComponent<PlayerStateDebug>();
        debugGui._player = player;
        debugGui._init = true;

        return debugGui;
    }

    public void SetPlayer(Player player)
    {
        _player = player;
    }

    public void Update()
    {
        if (!_init) return;

        GetPlayerStateSnapshot(_debugText, _player);
    }

    public void OnGUI()
    {
        GUI.Box(new Rect(10, 50, 800, 1300), string.Empty);
        GUI.Label(new Rect(15, 55, 790, 1290), _debugText.ToString());
    }

    private static void GetPlayerStateSnapshot(StringBuilder sb, Player player)
    {
        sb.Clear();
        GClass1492.smethod_0(sb, player);
        // var firearmController = player.HandsController as FirearmController;
        // if (firearmController != null)
        // {
        //     GClass1492.smethod_1(sb, player.ProfileId, firearmController);
        // }
        GClass1492.smethod_3(sb, player.HandsController);
        GClass1492.smethod_4(sb, "Hands Animator", player.HandsAnimator.Animator);
        // GClass1492.smethod_2(sb, player.MovementContext);
        // GClass1492.smethod_4(sb, "Body Animator", player.BodyAnimatorCommon);
    }
}
