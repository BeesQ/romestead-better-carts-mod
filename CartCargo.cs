using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CandideServer.Entities.Controllers;
using CandideServer.World;
using Microsoft.Xna.Framework;
using Shared.Entity;
using Shared.Entity.Components;

namespace BetterCarts;

internal static class CartCargo {
    private const int SweepIntervalMs = 100;
    private const float ExtraRingRadius = 4.5f;
    private const float ExtraRingHeight = 6f;
    private const float ScanTiles = 2f;

    private sealed class CartState {
        internal readonly List<Guid> Extras = new List<Guid>();
        internal long NextSweepTick;
        internal bool Adopted;
        internal long OccupiedStamp = -1;
        internal int Occupied;
    }

    private static readonly ConditionalWeakTable<ServerCart2Controller, CartState> States =
        new ConditionalWeakTable<ServerCart2Controller, CartState>();

    private static readonly List<EntityWrapper> ReuseCount = new List<EntityWrapper>();
    private static readonly List<EntityWrapper> ReuseSweep = new List<EntityWrapper>();
    private static readonly List<EntityWrapper> ReuseUnslotted = new List<EntityWrapper>();
    private static readonly List<EntityWrapper> ReuseExtras = new List<EntityWrapper>();

    internal static bool HasFreeSlot(ServerCart2Controller cart) {
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return false;
        }
        return GetOccupied(cart) < CartCapacity.GetKnownCapacity(cartEntity);
    }

    // vanilla runs PickupEntity for every touching entity every tick, so the count is memoized and only invalidated when it really changes
    internal static int GetOccupied(ServerCart2Controller cart) {
        CartState state = States.GetOrCreateValue(cart);
        long now = Environment.TickCount64;
        if (state.OccupiedStamp == now) {
            return state.Occupied;
        }
        CollectCarried(cart, ReuseCount);
        state.Occupied = ReuseCount.Count;
        state.OccupiedStamp = now;
        return state.Occupied;
    }

    internal static void Invalidate(ServerCart2Controller cart) {
        States.GetOrCreateValue(cart).OccupiedStamp = -1;
    }

    internal static bool HasExtras(ServerCart2Controller cart) {
        return States.GetOrCreateValue(cart).Extras.Count > 0;
    }

    internal static bool Adopted(ServerCart2Controller cart) {
        return States.GetOrCreateValue(cart).Adopted;
    }

    internal static void PinExtra(ServerCart2Controller cart, EntityWrapper item) {
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || item == null) {
            return;
        }
        Pin(cartEntity, item);
        CartState state = States.GetOrCreateValue(cart);
        if (!state.Extras.Contains(item.Id)) {
            state.Extras.Add(item.Id);
        }
        state.OccupiedStamp = -1;
    }

    internal static void Tick(ServerCart2Controller cart) {
        CartState state = States.GetOrCreateValue(cart);
        HoldExtras(cart, state);
        long now = Environment.TickCount64;
        if (now < state.NextSweepTick) {
            return;
        }
        state.NextSweepTick = now + SweepIntervalMs;
        Sweep(cart, state);
    }

    internal static void ReleaseAll(ServerCart2Controller cart) {
        CartState state = States.GetOrCreateValue(cart);
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity != null) {
            foreach (Guid id in state.Extras) {
                EntityWrapper item = cartEntity.System.GetEntityById(id);
                if (item != null && !item.Removed) {
                    Release(cartEntity, item);
                }
            }
        }
        state.Extras.Clear();
        state.OccupiedStamp = -1;
    }

    private static void HoldExtras(ServerCart2Controller cart, CartState state) {
        if (state.Extras.Count == 0) {
            return;
        }
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return;
        }
        ReuseExtras.Clear();
        foreach (Guid id in state.Extras) {
            EntityWrapper item = cartEntity.System.GetEntityById(id);
            if (item == null || item.Removed || item.CarrierId != cartEntity.Id) {
                continue;
            }
            ReuseExtras.Add(item);
        }
        if (ReuseExtras.Count != state.Extras.Count) {
            state.Extras.Clear();
            foreach (EntityWrapper item in ReuseExtras) {
                state.Extras.Add(item.Id);
            }
            state.OccupiedStamp = -1;
        }
        for (int i = 0; i < ReuseExtras.Count; i++) {
            Pin(cartEntity, ReuseExtras[i]);
            Place(cartEntity, ReuseExtras[i], i, ReuseExtras.Count);
        }
    }

    private static void Sweep(ServerCart2Controller cart, CartState state) {
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return;
        }
        CollectCarried(cart, ReuseSweep);
        state.Occupied = ReuseSweep.Count;
        state.OccupiedStamp = Environment.TickCount64;

        // cargo the mod pinned carries no slot parameter; vanilla c1..c5 and other mods' extra slots all do, so a parameter lookup separates them without knowing any mod's keys
        ReuseUnslotted.Clear();
        foreach (EntityWrapper item in ReuseSweep) {
            if (!IsSlotted(cart, item.Id)) {
                ReuseUnslotted.Add(item);
            }
        }

        int capacity = CartCapacity.GetEnforcedCapacity(cartEntity);
        int slotted = ReuseSweep.Count - ReuseUnslotted.Count;
        int keep = capacity <= 0 ? 0 : Math.Max(0, capacity - slotted);

        state.Extras.Clear();
        for (int i = 0; i < ReuseUnslotted.Count; i++) {
            if (i < keep) {
                state.Extras.Add(ReuseUnslotted[i].Id);
                continue;
            }
            Release(cartEntity, ReuseUnslotted[i]);
            state.OccupiedStamp = -1;
        }
        state.Adopted = true;

        if (ReuseUnslotted.Count == 0) {
            CartCapacity.Observe(cartEntity.BaseGuid, CartCapacity.Blessed, ReuseSweep.Count);
        }
        CartCapacity.NoteLiveType(cartEntity.BaseGuid);
        CartCapacity.FlushCache();
    }

    private static void CollectCarried(ServerCart2Controller cart, List<EntityWrapper> into) {
        into.Clear();
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return;
        }
        var collisions = ServerWorldHandler.GetEntityCollisionsOrNull(cartEntity.WorldId);
        if (collisions == null) {
            return;
        }
        int radius = (int)(WorldInfo.TileSize * ScanTiles);
        Rectangle around = new Rectangle((int)cartEntity.Position2.X - radius, (int)cartEntity.Position2.Y - radius,
            radius * 2, radius * 2);
        collisions.GetEntitiesInRectangleArea(around, into);
        for (int i = into.Count - 1; i >= 0; i--) {
            EntityWrapper item = into[i];
            if (item == null || item.Removed || !item.Mask.HasFlags(Component.Movable) || item.CarrierId != cartEntity.Id) {
                into.RemoveAt(i);
            }
        }
    }

    private static bool IsSlotted(ServerCart2Controller cart, Guid itemId) {
        var parameters = cart.Parameters;
        if (parameters == null) {
            return false;
        }
        var dictionary = parameters.Dictionary;
        if (dictionary == null || dictionary.Count == 0) {
            return false;
        }
        string id = itemId.ToString();
        foreach (string value in dictionary.Values) {
            if (string.Equals(value, id, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }
        return false;
    }

    private static void Pin(EntityWrapper cartEntity, EntityWrapper item) {
        item.IsThrown = false;
        item.ThrowerId = null;
        item.NoEntityCollision = true;
        item.NoTerrainCollision = true;
        item.CarrierId = cartEntity.Id;
    }

    private static void Release(EntityWrapper cartEntity, EntityWrapper item) {
        if (item.CarrierId == cartEntity.Id) {
            item.CarrierId = null;
        }
        item.NoEntityCollision = false;
        item.NoTerrainCollision = false;
    }

    // vanilla's four ride a client-side ring of radius 2.5 at Z 6, spread over 4 direction units; extras get their own wider ring so the two never overlap
    private static void Place(EntityWrapper cartEntity, EntityWrapper item, int index, int count) {
        float angle = (index + 0.5f) * 4f / count * MathHelper.PiOver2;
        item.Position = cartEntity.Position + new Vector3(
            (float)Math.Cos(angle) * ExtraRingRadius,
            (float)Math.Sin(angle) * ExtraRingRadius,
            ExtraRingHeight);
        item.Velocity = cartEntity.Velocity;
        item.System.CollisionGroup.UpdatePositionAndVelocity(item);
    }
}