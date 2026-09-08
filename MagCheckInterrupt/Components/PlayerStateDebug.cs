using System.Text;
using EFT;
using UnityEngine;

namespace MagCheckInterrupt.Components;

public class PlayerStateDebug : MonoBehaviour
{
    private readonly StringBuilder _debugText = new();
    private bool _init;
    private Player _player;

    public static PlayerStateDebug Instance
    {
        get
        {
            if (field == null)
            {
                field = Create(GamePlayerOwner.MyPlayer);
            }
            return field;
        }
    }

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
        PlayerDebugSnapshotCreator.AppendGeneralInfo(sb, player);
        // var firearmController = player.HandsController as FirearmController;
        // if (firearmController != null)
        // {
        //     PlayerDebugSnapshotCreator.AppendWeaponLogicalState(sb, player.ProfileId, firearmController);
        // }
        PlayerDebugSnapshotCreator.AppendAnimationEventsHistory(sb, player.HandsController);
        PlayerDebugSnapshotCreator.AppendAnimatorState(sb, "Hands Animator", player.HandsAnimator.Animator);
        // PlayerDebugSnapshotCreator.AppendMovementContextLogicalState(sb, player.MovementContext);
        // PlayerDebugSnapshotCreator.AppendAnimatorState(sb, "Body Animator", player.BodyAnimatorCommon);
    }
}
