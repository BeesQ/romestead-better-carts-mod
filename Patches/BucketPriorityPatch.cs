using System;
using System.Collections.Generic;
using Candide.Entities.Controllers.Legacy;
using Candide.Entities.Controllers.Other;
using Candide.Entities.PlayerState;
using Candide.GameModels;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;
using Shared.Entity.Components;
using Shared.Helpers;

namespace BetterCarts.Patches;

[HarmonyPatch(typeof(GrabActionHelper), nameof(GrabActionHelper.TryPlayerGrabActionProximity))]
internal static class BucketPriorityPatch {
    private const float HeightTolerance = 16f;
    private const float CargoScanRadius = 32f;

    // vanilla only looks HeightTolerance above the player, which is exactly what hides a stacked cart from its own grab; the carrier test in CollectCargo is what really scopes this scan, so the band just has to clear the tallest tower a capacity of 128 can build
    private const float CargoCeiling = 1024f;

    private static readonly List<EntityWrapper> Cargo = new List<EntityWrapper>();
    private static readonly List<Guid> Extras = new List<Guid>();

    private static void Postfix(EntityWrapper grabbingEntity, float radius, ref EntityWrapper __result) {
        if (!ModConfig.Enabled.Value) {
            return;
        }
        if (grabbingEntity == null || grabbingEntity.Removed) {
            return;
        }
        if (__result != null && !ModConfig.BucketPriorityEnabled.Value) {
            return;
        }

        EntityWrapper cart = __result != null
            ? CartCarrying(__result)
            : NearestCart(grabbingEntity, radius);
        if (cart == null) {
            return;
        }

        CollectCargo(cart);
        if (Cargo.Count == 0) {
            return;
        }

        if (ModConfig.BucketPriorityEnabled.Value) {
            EntityWrapper bucket = Lowest(grabbingEntity, bucketsOnly: true);
            if (bucket != null) {
                __result = bucket;
                return;
            }
        }

        if (__result == null) {
            __result = Lowest(grabbingEntity, bucketsOnly: false);
        }
    }

    private static EntityWrapper CartCarrying(EntityWrapper item) {
        if (!item.MaskRef.HasFlag(Component.Movable)) {
            return null;
        }
        EntityWrapper carrier = item.CarrierEntity;
        return carrier != null && carrier.Controller is Cart2Controller ? carrier : null;
    }

    private static EntityWrapper NearestCart(EntityWrapper grabbingEntity, float radius) {
        EntityWrapper best = null;
        float bestDistance = float.MaxValue;

        foreach (EntityWrapper other in grabbingEntity.System.GetEntitiesTouchingCircleArea(
                     grabbingEntity.Position2, radius,
                     grabbingEntity.Position.Z, grabbingEntity.Position.Z + HeightTolerance)) {
            if (other == null || other.Removed || !(other.Controller is Cart2Controller)) {
                continue;
            }
            float distance = Vector2.Distance(other.Position2, grabbingEntity.Position2);
            if (distance < bestDistance) {
                bestDistance = distance;
                best = other;
            }
        }

        return best;
    }

    private static void CollectCargo(EntityWrapper cart) {
        Cargo.Clear();
        CartCargoSync.Unpack(cart.Controller.Parameters.GetString(CartCargoSync.CargoKey), Extras);

        foreach (EntityWrapper item in cart.System.GetEntitiesTouchingCircleArea(
                     cart.Position2, CargoScanRadius,
                     cart.Position.Z, cart.Position.Z + CargoCeiling)) {
            if (item == null || item.Removed) {
                continue;
            }
            if (!GameState.Entities.ContainsKey(item.Id)) {
                continue;
            }
            if (!item.MaskRef.HasFlag(Component.Movable) || !item.Carriable) {
                continue;
            }
            if (item.CarrierId != cart.Id) {
                continue;
            }
            Cargo.Add(item);
        }
    }

    // the ring offset is rotated by the cart's mesh matrix before it becomes a world position, so items on one layer do not share an exact world Z and height cannot order the stack; bc_cargo index maps straight to seat and layer, and anything absent from it sits in a vanilla or third-party slot below the stack
    private static EntityWrapper Lowest(EntityWrapper grabbingEntity, bool bucketsOnly) {
        EntityWrapper best = null;
        int bestRank = 0;
        float bestDistance = 0f;

        foreach (EntityWrapper item in Cargo) {
            if (bucketsOnly && !IsEmptyBucket(item)) {
                continue;
            }
            if (!grabbingEntity.CanAttach(item, allowThrown: false, playerAttach: true)) {
                continue;
            }

            int rank = Extras.IndexOf(item.Id);
            float distance = Vector2.Distance(item.Position2, grabbingEntity.Position2);

            bool better;
            if (best == null) {
                better = true;
            }
            else if (rank != bestRank) {
                better = rank < bestRank;
            }
            else {
                better = distance < bestDistance;
            }

            if (better) {
                best = item;
                bestRank = rank;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static bool IsEmptyBucket(EntityWrapper item) {
        return item.Controller is BucketController bucket
            && bucket.Content == BucketEntityHelperShared.BucketContentType.Empty;
    }
}