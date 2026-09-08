using System.Collections.Generic;
using UnityEngine;

namespace MagCheckInterrupt.Utils;

public static class HashLookup
{
    public static readonly int ChamberCatchCheckHash = Animator.StringToHash("CHECK CHAMBER CATCHED");
    public static readonly int ChamberCatchReloadStartHash = Animator.StringToHash("RELOAD CATCH START");

    private static readonly Dictionary<int, int> _checkToReloadOut = new()
    {
        [Animator.StringToHash("CHECK")] = Animator.StringToHash("RELOAD OUT"),
        [Animator.StringToHash("CHECK MAG")] = Animator.StringToHash("RELOAD OUT MAG"),
        [1180283072] = 905331964, // WTT-Content Backport AS Val Mod4
    };

    private static readonly Dictionary<int, int> _checkToReloadOutFast = new()
    {
        [Animator.StringToHash("CHECK")] = Animator.StringToHash("RELOAD OUT ALL"),
        [Animator.StringToHash("CHECK MAG")] = Animator.StringToHash("RELOAD OUT ALL MAG"),
        [1180283072] = Animator.StringToHash("RELOAD OUT ALL"), // WTT-Content Backport AS Val Mod4
    };

    private static readonly HashSet<int> MagCheckHashes = [.. _checkToReloadOut.Keys, ChamberCatchCheckHash];

    public static bool IsMagazineCheckAnimation(int hash)
    {
        return MagCheckHashes.Contains(hash);
    }

    public static bool TryGetReloadOutHash(int checkHash, bool isFast, out int reloadOutHash)
    {
        return isFast
            ? _checkToReloadOutFast.TryGetValue(checkHash, out reloadOutHash)
            : _checkToReloadOut.TryGetValue(checkHash, out reloadOutHash);
    }
}
