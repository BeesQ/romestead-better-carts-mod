using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CandideCreator.Shared.Helpers;
using CandideServer.Entities.Controllers;
using CandideServer.ServerManagers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;
using Shared.Helpers;

namespace BetterCarts.Patches;

[HarmonyPatch(typeof(ServerCart2Controller), nameof(ServerCart2Controller.Update), typeof(GameTime))]
internal static class ConnectRangePatch {
    private const int MaxChainWalk = 256;
    private const float ConnectAccel = 260f;
    private const float MaxConnectSpeed = 130f;
    private const float TurnAccel = 2600f;
    private const float MaxTurnSpeed = 1300f;

    private static readonly float TurnAccelRad = MathHelper.ToRadians(TurnAccel);
    private static readonly float MaxTurnSpeedRad = MathHelper.ToRadians(MaxTurnSpeed);

    private sealed class SpeedHolder {
        public float MoveSpeed;
        public float TurnSpeed;
    }

    private static readonly ConditionalWeakTable<ServerCart2Controller, SpeedHolder> Speeds =
        new ConditionalWeakTable<ServerCart2Controller, SpeedHolder>();

    private static void Postfix(ServerCart2Controller __instance) {
        if (__instance == null) {
            return;
        }

        SpeedHolder holder = Speeds.GetOrCreateValue(__instance);
        EntityWrapper cart = __instance.Entity;

        bool active = ModConfig.Enabled.Value
            && ModConfig.ConnectRangeEnabled.Value
            && ModConfig.ConnectRange.Value > 0
            && cart != null
            && !cart.Removed
            && !__instance.FollowingId.HasValue;

        if (!active) {
            if (holder.MoveSpeed > 0f || holder.TurnSpeed > 0f) {
                holder.MoveSpeed = 0f;
                holder.TurnSpeed = 0f;

                if (cart != null && !cart.Removed && !__instance.FollowingId.HasValue) {
                    cart.Velocity = new Vector3(0f, 0f, cart.Velocity.Z);
                }
            }

            return;
        }

        EntityWrapper target = FindTarget(cart);
        if (target == null) {
            if (holder.MoveSpeed > 0f || holder.TurnSpeed > 0f) {
                holder.MoveSpeed = 0f;
                holder.TurnSpeed = 0f;
                cart.Velocity = new Vector3(0f, 0f, cart.Velocity.Z);
            }

            return;
        }

        Vector2 toTarget = target.Position2 - cart.Position2;
        float distance = toTarget.Length();
        if (distance <= 0.01f) {
            return;
        }

        Vector2 direction = toTarget / distance;

        holder.MoveSpeed = Math.Min(holder.MoveSpeed + ConnectAccel * cart.Fdt, MaxConnectSpeed);
        cart.Velocity = new Vector3(direction.X * holder.MoveSpeed, direction.Y * holder.MoveSpeed, cart.Velocity.Z);

        float targetDirection = direction.ToFloatDirection();
        holder.TurnSpeed = Math.Min(holder.TurnSpeed + TurnAccelRad * cart.Fdt, MaxTurnSpeedRad);
        cart.Direction = RotateTowards(cart.Direction, targetDirection, holder.TurnSpeed * cart.Fdt, out bool reachedDirection);

        if (reachedDirection) {
            holder.TurnSpeed = 0f;
        }
    }

    private static float RotateTowards(float current, float target, float maxDelta, out bool reached) {
        if (maxDelta <= 0f) {
            reached = false;
            return current;
        }

        float delta = MathHelper.WrapAngle(target - current);
        if (Math.Abs(delta) <= maxDelta) {
            reached = true;
            return MathHelper.WrapAngle(target);
        }

        reached = false;
        return MathHelper.WrapAngle(current + Math.Sign(delta) * maxDelta);
    }

    private static EntityWrapper FindTarget(EntityWrapper cart) {
        int maxRangeTiles = ModConfig.ConnectRange.Value;
        float maxRadius = maxRangeTiles * WorldInfo.TileSize;

        EntityWrapper best = null;
        float bestDistanceSquared = float.MaxValue;

        foreach (EntityWrapper other in cart.System.GetEntitiesTouchingCircleArea(cart.Position2, maxRadius, cart.Position.Z, cart.Position.Z + 8f)) {
            if (other == null || other.Removed || other.Id == cart.Id) {
                continue;
            }

            if (!(other.Controller is ServerCart2Controller otherCart)) {
                continue;
            }

            // Only the tail of a chain can accept a new follower.
            if (otherCart.FollowerCartId.HasValue) {
                continue;
            }

            int chainLength = GetPulledCartChainLength(otherCart, cart.System);
            int effectiveRangeTiles = GetEffectiveConnectRange(maxRangeTiles, chainLength);
            if (effectiveRangeTiles <= 0) {
                continue;
            }

            float effectiveRadius = effectiveRangeTiles * WorldInfo.TileSize;
            float effectiveRadiusSquared = effectiveRadius * effectiveRadius;
            float distanceSquared = Vector2.DistanceSquared(other.Position2, cart.Position2);

            if (distanceSquared > effectiveRadiusSquared) {
                continue;
            }

            if (distanceSquared < bestDistanceSquared) {
                bestDistanceSquared = distanceSquared;
                best = other;
            }
        }

        return best;
    }

    private static int GetEffectiveConnectRange(int maxRangeTiles, int chainLength) {
        if (maxRangeTiles <= 0 || chainLength <= 0) {
            return 0;
        }

        return Math.Min(maxRangeTiles, chainLength);
    }

    private static int GetPulledCartChainLength(ServerCart2Controller cart, EntitySystem system) {
        HashSet<Guid> visited = new HashSet<Guid>();
        ServerCart2Controller current = cart;
        int cartCount = 0;

        for (int i = 0; i < MaxChainWalk; i++) {
            cartCount++;

            Guid? followingId = current.FollowingId;
            if (!followingId.HasValue) {
                return 0;
            }

            if (!visited.Add(followingId.Value)) {
                return 0;
            }

            EntityWrapper next = system.GetEntityById(followingId);
            if (next == null || next.Removed) {
                return 0;
            }

            if (!(next.Controller is ServerCart2Controller nextCart)) {
                return cartCount;
            }

            current = nextCart;
        }

        return 0;
    }
}