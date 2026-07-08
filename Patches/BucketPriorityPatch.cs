using Candide.Entities.Controllers.Legacy;
using Candide.Entities.Controllers.Other;
using Candide.Entities.PlayerState;
using Candide.GameModels;
using CandideCreator.Shared.Helpers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;
using Shared.Entity.Components;
using Shared.Helpers;

namespace BetterCarts.Patches;

[HarmonyPatch(typeof(GrabActionHelper), nameof(GrabActionHelper.TryPlayerGrabActionProximity))]
internal static class BucketPriorityPatch {
    private const float HeightTolerance = 16f;

    private static bool Prefix(EntityWrapper grabbingEntity, float radius, ref EntityWrapper __result) {
        if (!ModConfig.Enabled.Value || !ModConfig.BucketPriorityEnabled.Value) {
            return true;
        }
        if (grabbingEntity == null || grabbingEntity.Removed) {
            return true;
        }

        float bottom = grabbingEntity.Position.Z;
        float top = grabbingEntity.Position.Z + HeightTolerance;

        EntityWrapper nearest = null;
        float nearestDistance = float.MaxValue;
        EntityWrapper nearestCartBucket = null;
        float nearestCartBucketDistance = float.MaxValue;

        foreach (EntityWrapper item in grabbingEntity.System.GetEntitiesTouchingCircleArea(grabbingEntity.Position2, radius, bottom, top)) {
            if (item == null || grabbingEntity.Equals(item)) {
                continue;
            }
            if (!GameState.Entities.ContainsKey(item.Id)) {
                continue;
            }
            if (!item.MaskRef.HasFlag(Component.Movable) || !item.Carriable) {
                continue;
            }
            if (!IntervalHelper.IntervalOverlap(bottom, top, item.PositionZ, item.PositionZ + item.Shape.Height3D - 0.5f)) {
                continue;
            }

            float distance = Vector2.Distance(item.Position2, grabbingEntity.Position2);
            if (distance < nearestDistance) {
                nearestDistance = distance;
                nearest = item;
            }
            if (distance < nearestCartBucketDistance
                && IsEmptyBucketOnCart(item)
                && grabbingEntity.CanAttach(item, allowThrown: false, playerAttach: true)) {
                nearestCartBucketDistance = distance;
                nearestCartBucket = item;
            }
        }

        // Mirror vanilla's pick and only swap in the empty bucket when vanilla would already be unloading a cart item.
        if (nearest == null) {
            return true;
        }
        if (!grabbingEntity.CanAttach(nearest, allowThrown: false, playerAttach: true)) {
            return true;
        }
        if (!IsCarriedByCart(nearest)) {
            return true;
        }
        if (nearestCartBucket == null) {
            return true;
        }

        __result = nearestCartBucket;
        return false;
    }

    private static bool IsCarriedByCart(EntityWrapper item) {
        EntityWrapper carrier = item.CarryingEntity;
        return carrier != null && carrier.Controller is Cart2Controller;
    }

    private static bool IsEmptyBucketOnCart(EntityWrapper item) {
        if (!(item.Controller is BucketController bucket)) {
            return false;
        }
        if (bucket.Content != BucketEntityHelperShared.BucketContentType.Empty) {
            return false;
        }
        return IsCarriedByCart(item);
    }
}