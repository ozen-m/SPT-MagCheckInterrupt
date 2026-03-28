using System.Text;
using UnityEngine;
using static EFT.Player;

namespace MagCheckInterrupt.Components;

public class AnimationDebugUI : MonoBehaviour
{
    private readonly StringBuilder _debugText = new();
    private bool _init;
    private FirearmsAnimator _playerAnimator;
    private AbstractHandsController _firearmController;

    public static AnimationDebugUI Create(GameObject target, FirearmsAnimator animator, AbstractHandsController firearmController)
    {
        var debugGui = target.GetOrAddComponent<AnimationDebugUI>();
        debugGui._playerAnimator = animator;
        debugGui._firearmController = firearmController;
        debugGui._init = true;
        // Or maybe just pass in Player

        return debugGui;
    }

    public void SetAnimatorAndController(FirearmsAnimator animator, AbstractHandsController firearmController)
    {
        _playerAnimator = animator;
        _firearmController = firearmController;
    }

    public void Update()
    {
        if (!_init) return;

        GetAnimatorDebug(_playerAnimator, _firearmController, _debugText);
    }

    public void OnGUI()
    {
        GUI.Box(new Rect(10, 50, 800, 1300), string.Empty);
        GUI.Label(new Rect(15, 55, 790, 1290), _debugText.ToString());
    }

    private static void GetAnimatorDebug(FirearmsAnimator firearmsAnimator, AbstractHandsController firearmController, StringBuilder sb)
    {
        sb.Clear();
        GClass1492.smethod_3(sb, firearmController);
        GClass1492.smethod_4(sb, "Hands Animator", firearmsAnimator.Animator);
    }
}
