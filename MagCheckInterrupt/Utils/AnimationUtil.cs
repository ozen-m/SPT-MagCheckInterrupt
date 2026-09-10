using EFT.InventoryLogic;
using MagCheckInterrupt.Components;
using MagCheckInterrupt.External;
using MagCheckInterrupt.Patches;

namespace MagCheckInterrupt.Utils;

public static class AnimationUtil
{
    public static void ShowAmmoDetails(FirearmController controller)
    {
        if (ConfigUtil.FloatingAmmoDetails.Value)
        {
            FloatingPanelController.Instance.ShowAmmoDetails(controller);
        }
        else
        {
            AmmoDetailsPatch.ShowLastAmmoDetails();
        }
    }

    public static void HideAmmoDetails()
    {
        if (ConfigUtil.FloatingAmmoDetails.Value)
        {
            FloatingPanelController.Instance.Hide();
        }
        else
        {
            AmmoDetailsPatch.HideAmmoCount();
        }
    }

    public static float GetHandsNormalizedTime(this ObjectInHandsAnimator objectInHandsAnimator)
    {
        return objectInHandsAnimator.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX).normalizedTime;
    }

    public static bool IsMagazineCheckAnimation(this ObjectInHandsAnimator objectInHandsAnimator)
    {
        var currentStateHash = objectInHandsAnimator.Animator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX).shortNameHash;
        return HashLookup.IsMagazineCheckAnimation(currentStateHash);
    }

    /// <summary>
    /// Performs the animation from a magazine check to a reload.
    /// Also includes, hiding the ammo count and sending of Fika packet.
    /// </summary>
    /// <param name="isFast">True if the reload is a quick reload</param>
    /// <param name="isSwap">True if triggered by a swap operation</param>
    public static void TransitionToReload(this FirearmOperation operation, bool isFast = false, bool isSwap = false)
    {
        if (operation.FirearmsAnimator.Animator is not UnityAnimatorWrapper wrapper)
        {
            var typeFullName = operation.FirearmsAnimator.Animator.GetType().FullName;
            L.Error($"MagCheckReloadOperation::TransitionToReload Cannot transition directly into a reload. {typeFullName}");
            return;
        }

        DoReloadCrossfade(operation, wrapper, isFast, isSwap);

        if (!operation.Player.FirstPersonPointOfView)
        {
            return;
        }

        // We don't want observed players re-sending packets and
        // our packet needs to be sent first before Fika's reload packet (ReloadMag.startCallback).
        // But if it's a swap reload, no need to send a packet.
        if (!isSwap && FikaHandler.IsPresent)
        {
            FikaHandler.SendReloadCalledPacket();
        }

        HideAmmoDetails();
    }

    private static void DoReloadCrossfade(FirearmOperation operation, UnityAnimatorWrapper wrapper, bool isFast, bool isSwap)
    {
        if (wrapper.TryGetReloadOutHash(out var reloadOutHash, isFast, operation.Weapon))
        {
            // ReloadExternalMagOperation.Start calls FirearmsAnimator.Reload(bool b), so we need to skip the reload animation and do our own crossfade.
            // But if it's a swap reload, no need to skip the reload animation, it's not called by the insert mag operation.
            if (!isSwap)
            {
                SkipReloadAnimation(isFast);
            }

            wrapper._animator.CrossFade(
                reloadOutHash,
                isSwap ? 0.15f : 0.10f, // Anything more than 0.10f looks like a magazine swap
                FirearmsAnimator.HANDS_LAYER_INDEX,
                0.50f // Skip mag out from weapon animation
            );
        }
        else if (isSwap && operation.FirearmsAnimator.GetBoltCatch())
        {
            // Special case: For weapons such as the SKS with an empty mag, no bullet in the chamber, with its bolt caught,
            // don't do crossfade and instead play the reload animation.
            operation.FirearmsAnimator.PullOutMagInInventoryMode();
            operation.FirearmsAnimator.ResetInsertMagInInventoryMode();
        }
    }

    private static bool TryGetReloadOutHash(
        this UnityAnimatorWrapper unityAnimator,
        out int reloadOutHash,
        bool isFast = false,
        Weapon weapon = null
    )
    {
        var currentStateHash = unityAnimator.GetCurrentAnimatorStateInfo(FirearmsAnimator.HANDS_LAYER_INDEX).shortNameHash;
        if (currentStateHash == HashLookup.ChamberCatchCheckHash)
        {
            // Special case: For weapons such as the SKS, with its bolt caught
            reloadOutHash = HashLookup.ChamberCatchReloadStartHash;
            return false;
        }
        if (HashLookup.TryGetReloadOutHash(currentStateHash, isFast, out reloadOutHash))
        {
            return true;
        }

        L.Error(
            $"Unsupported mag check hash: {AnimationControllerStatesTable.GetAnimStateByNameHash(currentStateHash)} ({currentStateHash}) (isFast: {isFast}) for weapon: {weapon?.ToFullString()}"
        );
        reloadOutHash = -1;
        return false;
    }

    private static void SkipReloadAnimation(bool isFast)
    {
        if (isFast)
        {
            ReloadFastAnimationPatch.SkipReloadAnimation();
        }
        else
        {
            ReloadAnimationPatch.SkipReloadAnimation();
        }
    }
}
