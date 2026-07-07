using System;
using System.Collections.Generic;
using CandideServer.Entities.Controllers;
using HarmonyLib;
using Shared.Entity;

namespace BetterCarts.Patches;

[HarmonyPatch(typeof(ServerCart2Controller), "PickupEntity")]
internal static class ChainOverflowPatch {
    private const int MaxChainWalkFallback = 256;

    [ThreadStatic]
    private static bool _walkingChain;

    private static void Postfix(ServerCart2Controller __instance, EntityWrapper entity, ref bool __result) {
        if (__result || _walkingChain) {
            return;
        }
        if (!ModConfig.Enabled.Value || !ModConfig.ChainOverflowEnabled.Value) {
            return;
        }
        // PickupEntity calls below re-enter this postfix; the flag stops the nested walk
        _walkingChain = true;
        try {
            __result = TryPickupIntoChain(__instance, entity);
        }
        finally {
            _walkingChain = false;
        }
    }

    private static bool TryPickupIntoChain(ServerCart2Controller source, EntityWrapper entity) {
        HashSet<Guid> visited = new HashSet<Guid> { source.Entity.Id };
        if (WalkChain(source, entity, visited, followers: true)) {
            return true;
        }
        return WalkChain(source, entity, visited, followers: false);
    }

    private static bool WalkChain(ServerCart2Controller source, EntityWrapper entity, HashSet<Guid> visited, bool followers) {
        ServerCart2Controller current = source;
        for (int i = 0; i < MaxChainWalkFallback; i++) {
            Guid? nextId = followers ? current.FollowerCartId : current.FollowingId;
            if (!nextId.HasValue || !visited.Add(nextId.Value)) {
                return false;
            }
            EntityWrapper nextEntity = source.Entity.System.GetEntityById(nextId);
            if (nextEntity == null || nextEntity.Removed) {
                return false;
            }
            if (!(nextEntity.Controller is ServerCart2Controller nextCart)) {
                return false;
            }
            if (CartAccess.PickupEntity(nextCart, entity)) {
                return true;
            }
            current = nextCart;
        }
        return false;
    }
}