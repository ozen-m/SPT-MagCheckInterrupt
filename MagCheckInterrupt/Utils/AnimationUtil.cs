using EFT.InventoryLogic;
using MagCheckInterrupt.Patches;
using UnityEngine;
using static EFT.Player;
using AnimatorWrapper = GClass1446;

namespace MagCheckInterrupt.Utils;

public static class AnimationUtil
{
    private static readonly int _magCheckHash = Animator.StringToHash("CHECK");
    private static readonly int _magReloadOutHash = Animator.StringToHash("RELOAD OUT");
    private static readonly int _magWithInternalCheckHash = Animator.StringToHash("CHECK MAG");
    private static readonly int _magWithInternalReloadOutHash = Animator.StringToHash("RELOAD OUT MAG");

    /// <summary>
    /// Performs the animation from a magazine check to a reload.
    /// Also includes, hiding the ammo count and sending of Fika packet.
    /// </summary>
    /// <param name="isSwap">True if the reload is triggered by a swap operation, done by UI Fixes; otherwise, false (normal reload)</param>
    public static void TransitionToReload(this FirearmController.GClass2013 animationOperation, bool isSwap)
    {
        var animator = animationOperation.FirearmsAnimator_0.Animator;
        if (animator is AnimatorWrapper wrapper && wrapper.GetReloadOutHash(out var reloadOutHash, animationOperation.Weapon_0))
        {
            // GClass2016.Start calls FirearmsAnimator.Reload(bool b), so we need to skip the reload animation and do our own crossfade.
            // But if it's a swap reload, no need to skip the reload animation, it's not called by the insert mag operation.
            if (!isSwap)
            {
                ReloadAnimationPatch.SkipReloadAnimation();
            }
            wrapper.Animator_0.CrossFade(
                reloadOutHash,
                isSwap ? 0.15f : 0.10f, // Note: Anything more than 0.10f looks like a magazine swap
                FirearmsAnimator.HANDS_LAYER_INDEX,
                0.50f // Skip mag out from weapon animation
            );
        }
        else
        {
            var typeFullName = animator.GetType().FullName;
            LoggerUtil.Warning($"MagCheckReloadOperation::TransitionToReload Cannot transition directly into a reload. {typeFullName}");
        }

        if (!animationOperation.Player_0.FirstPersonPointOfView) return;

        // We don't want observed players re-sending packets and
        // our packet needs to be sent first before Fika's reload packet (ReloadMag.startCallback).
        // But if it's a swap reload, no need to send a packet.
        if (!isSwap && External.Fika.IsPresent)
        {
            External.Fika.SendReloadCalledPacket();
        }

        AmmoDetailsPatch.HideAmmoCount();
    }

    public static bool GetReloadOutHash(this IAnimator animator, out int reloadOutHash, Weapon weapon = null)
    {
        var currentStateHash = animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX).shortNameHash;
        if (currentStateHash == _magCheckHash)
        {
            reloadOutHash = _magReloadOutHash;
            return true;
        }
        if (currentStateHash == _magWithInternalCheckHash)
        {
            reloadOutHash = _magWithInternalReloadOutHash;
            return true;
        }

        LoggerUtil.Error(
            $"Unsupported mag check hash: {GClass758.GetAnimStateByNameHash(currentStateHash)} ({currentStateHash}) for weapon: {weapon?.ToFullString()}"
        );
        reloadOutHash = -1;
        return false;
    }
}
