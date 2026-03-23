using System.Text;
using EFT;
using UnityEngine;

namespace MagCheckInterrupt.Components;

public class AnimationDebugUI : MonoBehaviour
{
    private bool _init;
    private string _debugString = string.Empty;
    private FirearmsAnimator _playerAnimator;
    private Player.AbstractHandsController _firearmController;

    private readonly StringBuilder _sb = new();

    public static AnimationDebugUI Create(GameObject target, FirearmsAnimator animator, Player.AbstractHandsController firearmController)
    {
        var debugGui = target.GetOrAddComponent<AnimationDebugUI>();
        debugGui._playerAnimator = animator;
        debugGui._firearmController = firearmController;
        debugGui._init = true;
        // Or maybe just pass in Player

        return debugGui;
    }

    public void SetAnimatorAndController(FirearmsAnimator animator, Player.AbstractHandsController firearmController)
    {
        _playerAnimator = animator;
        _firearmController = firearmController;
    }

    public void Update()
    {
        if (!_init) return;

        GetAnimatorDebug(_playerAnimator, _firearmController, _sb);
        _debugString = _sb.ToString();
    }

    public void OnGUI()
    {
        GUI.Box(new Rect(10, 50, 800, 1080), string.Empty);
        GUI.Label(new Rect(15, 55, 790, 1070), _debugString);
    }

    private static void GetAnimatorDebug(FirearmsAnimator firearmsAnimator, Player.AbstractHandsController firearmController, StringBuilder sb)
    {
        sb.Clear();
        GClass1492.smethod_3(sb, firearmController);
        GClass1492.smethod_4(sb, "Hands Animator", firearmsAnimator.Animator);
    }
}
