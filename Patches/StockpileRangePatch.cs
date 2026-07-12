using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CandideCreator.Shared.Helpers;
using CandideServer;
using CandideServer.Entities.Controllers;
using CandideServer.Entities.ControllerSets.MaterialStorage;
using CandideServer.Helpers;
using CandideServer.MessageModels.Entities;
using CandideServer.Models;
using CandideServer.ServerManagers;
using CandideServer.World;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Data;
using Shared.Entity;
using Shared.Entity.Base;
using Shared.Helpers;
using Shared.Models.Items;

namespace BetterCarts.Patches;

internal static class StockpileRangePatch {
    private const int FillIntervalMs = 100;
    private const float CheckTime = 0.1f;
    private const float MaxTakeZ = 24f;
    private const int MaxChainWalk = 256;

    private sealed class TimerHolder {
        public float Value;
    }

    private sealed class FillState {
        public long NextTick;
    }

    private static readonly ConditionalWeakTable<ServerMaterialStorageStackController, TimerHolder> StackTimers =
        new ConditionalWeakTable<ServerMaterialStorageStackController, TimerHolder>();

    private static readonly ConditionalWeakTable<object, FillState> FillTimers =
        new ConditionalWeakTable<object, FillState>();

    private static readonly List<EntityWrapper> ReuseList = new List<EntityWrapper>();
    private static readonly List<RequestSpawnEntityMessage> ReuseSpawnList = new List<RequestSpawnEntityMessage>();
    private static readonly ResourceAmount[] ReuseTake = new ResourceAmount[1];
    private static readonly HashSet<Guid> ReuseVisited = new HashSet<Guid>();

    private static readonly HashSet<string> BucketResources = new HashSet<string> {
        "resource:clay",
        "resource:ash",
        "resource:water"
    };

    private static readonly AccessTools.FieldRef<ServerMaterialStorageStackController, IOStorageType> StorageTypeRef =
        AccessTools.FieldRefAccess<ServerMaterialStorageStackController, IOStorageType>("_storageType");

    [HarmonyPatch(typeof(ServerMaterialStorageStackController), nameof(ServerMaterialStorageStackController.Update), typeof(GameTime))]
    private static class TakeSolidsFromOutputStacks {
        private static void Postfix(ServerMaterialStorageStackController __instance) {
            TryFillBuckets();
            if (!ModConfig.Enabled.Value || !ModConfig.StockpileRangeEnabled.Value) {
                return;
            }
            int range = ModConfig.StockpileRange.Value;
            if (range <= 0) {
                return;
            }
            bool whilePulled = ModConfig.StockpileWhilePulled.Value;
            bool whileParked = ModConfig.StockpileWhileParked.Value;
            if (!whilePulled && !whileParked) {
                return;
            }
            if (StorageTypeRef(__instance) != IOStorageType.Output) {
                return;
            }
            var building = ServerTempState.CurrentlyUpdatingBuilding;
            if (building == null || !building.InstanceModel.OutputResourceStorageId.HasValue) {
                return;
            }
            Guid outputStorageId = building.InstanceModel.OutputResourceStorageId.Value;
            // storage-style buildings register ONE storage as both input and output - taking from them would loop with Deposit Range
            if (building.InstanceModel.ResourceStorageId.HasValue && building.InstanceModel.ResourceStorageId.Value == outputStorageId) {
                return;
            }
            EntityWrapper stack = __instance.Entity;
            if (stack == null || stack.Removed) {
                return;
            }
            TimerHolder timer = StackTimers.GetOrCreateValue(__instance);
            timer.Value -= stack.Fdt;
            if (timer.Value > 0f) {
                return;
            }
            timer.Value += CheckTime;
            var worldModel = ServerTempState.CurrentlyUpdatingWorldModel;
            if (worldModel == null) {
                return;
            }
            Guid targetWorldId = worldModel.ParentWorldId ?? worldModel.Id;
            if (!ServerGameState.TryGetResourceStorage(outputStorageId, out var storage)) {
                return;
            }
            string solidResourceId = null;
            Guid solidBaseGuid = Guid.Empty;
            foreach (KeyValuePair<string, int> resourceAmount in storage.ResourceAmounts) {
                if (resourceAmount.Value <= 0 || IsBucketResource(resourceAmount.Key)) {
                    continue;
                }
                var resource = ConstructionResourcesDataBase.GetConstructionResourceOrNull(resourceAmount.Key);
                if (resource.HasValue && resource.Value.DefaultBaseGuid.HasValue) {
                    solidResourceId = resourceAmount.Key;
                    solidBaseGuid = resource.Value.DefaultBaseGuid.Value;
                    break;
                }
            }
            if (solidResourceId == null) {
                return;
            }
            var collisions = ServerWorldHandler.GetEntityCollisionsOrNull(targetWorldId);
            if (collisions == null) {
                return;
            }
            float tileSize = WorldInfo.TileSize;
            Rectangle tileBounds = building.InstanceModel.TileBounds;
            Vector3 stackWorldPosition = tileBounds.Location.ToVector3Xy() * tileSize + stack.Position;
            Rectangle buildingZone = new Rectangle((int)(tileBounds.X * tileSize), (int)(tileBounds.Y * tileSize),
                (int)(tileBounds.Width * tileSize), (int)(tileBounds.Height * tileSize));
            int reach = (int)(range * tileSize);
            Rectangle takeZone = new Rectangle((int)(stackWorldPosition.X - tileSize * 0.5f),
                (int)(stackWorldPosition.Y - tileSize * 0.5f), (int)tileSize, (int)tileSize);
            takeZone.Inflate(reach, reach);
            Vector2 stackCenter = new Vector2(stackWorldPosition.X, stackWorldPosition.Y);
            ReuseList.Clear();
            collisions.GetEntitiesInRectangleArea(takeZone, ReuseList);
            ServerCart2Controller bestCart = null;
            float bestCartDistanceSquared = float.MaxValue;
            foreach (EntityWrapper candidate in ReuseList) {
                if (candidate.Removed || candidate.PositionZ > MaxTakeZ) {
                    continue;
                }
                if (!(candidate.Controller is ServerCart2Controller cart)) {
                    continue;
                }
                if (buildingZone.Contains((int)candidate.Position2.X, (int)candidate.Position2.Y)) {
                    continue;
                }
                if (!IsEligible(cart, whilePulled, whileParked) || !HasChainCapacity(cart)) {
                    continue;
                }
                float distanceSquared = Vector2.DistanceSquared(candidate.Position2, stackCenter);
                if (distanceSquared < bestCartDistanceSquared) {
                    bestCartDistanceSquared = distanceSquared;
                    bestCart = cart;
                }
            }
            if (bestCart == null) {
                return;
            }
            ReuseTake[0] = new ResourceAmount { ResourceId = solidResourceId, Amount = 1 };
            if (InternalResourceStorageServerManager.TryRemoveResources_Destructive(storage, ReuseTake)) {
                EntityWrapper cartEntity = bestCart.Entity;
                ReuseSpawnList.Clear();
                ReuseSpawnList.Add(new RequestSpawnEntityMessage {
                    Position = cartEntity.Position,
                    Velocity = Vector3.Zero,
                    WorldId = targetWorldId,
                    EntityBaseId = solidBaseGuid
                });
                EntityServerManager.SpawnEntities(ReuseSpawnList, targetWorldId);
            }
        }
    }

    // the Clay Pit's vat (ServerMaterialStorageFluidContainerController) has an EMPTY server class and no Output stack, so the shared base tick is the only per-building trigger for bucket resources
    [HarmonyPatch(typeof(AbstractController), nameof(AbstractController.Update), typeof(GameTime))]
    private static class FillBucketsOnBuildingTick {
        private static void Postfix() {
            TryFillBuckets();
        }
    }

    private static void TryFillBuckets() {
        var building = ServerTempState.CurrentlyUpdatingBuilding;
        if (building == null) {
            return;
        }
        if (!ModConfig.Enabled.Value || !ModConfig.StockpileRangeEnabled.Value) {
            return;
        }
        int range = ModConfig.StockpileRange.Value;
        if (range <= 0) {
            return;
        }
        bool whilePulled = ModConfig.StockpileWhilePulled.Value;
        bool whileParked = ModConfig.StockpileWhileParked.Value;
        if (!whilePulled && !whileParked) {
            return;
        }
        if (!building.InstanceModel.OutputResourceStorageId.HasValue) {
            return;
        }
        Guid outputStorageId = building.InstanceModel.OutputResourceStorageId.Value;
        if (building.InstanceModel.ResourceStorageId.HasValue && building.InstanceModel.ResourceStorageId.Value == outputStorageId) {
            return;
        }
        FillState state = FillTimers.GetOrCreateValue(building);
        long now = Environment.TickCount64;
        if (now < state.NextTick) {
            return;
        }
        state.NextTick = now + FillIntervalMs;
        var worldModel = ServerTempState.CurrentlyUpdatingWorldModel;
        if (worldModel == null) {
            return;
        }
        Guid targetWorldId = worldModel.ParentWorldId ?? worldModel.Id;
        if (!ServerGameState.TryGetResourceStorage(outputStorageId, out var storage)) {
            return;
        }
        string bucketResourceId = null;
        foreach (KeyValuePair<string, int> resourceAmount in storage.ResourceAmounts) {
            if (resourceAmount.Value > 0 && IsBucketResource(resourceAmount.Key)) {
                bucketResourceId = resourceAmount.Key;
                break;
            }
        }
        if (bucketResourceId == null) {
            return;
        }
        var collisions = ServerWorldHandler.GetEntityCollisionsOrNull(targetWorldId);
        if (collisions == null) {
            return;
        }
        float tileSize = WorldInfo.TileSize;
        Rectangle tileBounds = building.InstanceModel.TileBounds;
        Rectangle buildingZone = new Rectangle((int)(tileBounds.X * tileSize), (int)(tileBounds.Y * tileSize),
            (int)(tileBounds.Width * tileSize), (int)(tileBounds.Height * tileSize));
        Rectangle takeZone = buildingZone;
        int reach = (int)(range * tileSize);
        takeZone.Inflate(reach, reach);
        Vector2 zoneCenter = buildingZone.Center.ToVector2();
        ReuseList.Clear();
        collisions.GetEntitiesInRectangleArea(takeZone, ReuseList);
        ServerBucketController bestBucket = null;
        float bestBucketDistanceSquared = float.MaxValue;
        foreach (EntityWrapper candidate in ReuseList) {
            if (candidate.Removed || candidate.PositionZ > MaxTakeZ) {
                continue;
            }
            if (!(candidate.Controller is ServerBucketController bucket)
                || bucket.Content != BucketEntityHelperShared.BucketContentType.Empty) {
                continue;
            }
            EntityWrapper carrier = candidate.CarrierEntity;
            if (carrier == null || carrier.Removed || !(carrier.Controller is ServerCart2Controller carrierCart)) {
                continue;
            }
            if (buildingZone.Contains((int)carrier.Position2.X, (int)carrier.Position2.Y)) {
                continue;
            }
            if (!IsEligible(carrierCart, whilePulled, whileParked)) {
                continue;
            }
            float distanceSquared = Vector2.DistanceSquared(carrier.Position2, zoneCenter);
            if (distanceSquared < bestBucketDistanceSquared) {
                bestBucketDistanceSquared = distanceSquared;
                bestBucket = bucket;
            }
        }
        if (bestBucket == null) {
            return;
        }
        ReuseTake[0] = new ResourceAmount { ResourceId = bucketResourceId, Amount = 1 };
        if (InternalResourceStorageServerManager.TryRemoveResources_Destructive(storage, ReuseTake)) {
            bestBucket.SetContentType(BucketEntityHelperShared.BucketContentType.Resource, bucketResourceId, 1, syncOverNetwork: true);
            ServerSendMessageHelper.PlaySoundOnPosition(bestBucket.Entity.Position, targetWorldId, GetFillSound(bucketResourceId));
        }
    }

    private static bool IsBucketResource(string resourceId) {
        if (BucketResources.Contains(resourceId)) {
            return true;
        }
        var resource = ConstructionResourcesDataBase.GetConstructionResourceOrNull(resourceId);
        return !resource.HasValue || !resource.Value.DefaultBaseGuid.HasValue;
    }

    private static bool IsEligible(ServerCart2Controller cart, bool whilePulled, bool whileParked) {
        return IsChainPulled(cart) ? whilePulled : whileParked;
    }

    private static bool IsChainPulled(ServerCart2Controller cart) {
        ReuseVisited.Clear();
        ServerCart2Controller current = cart;
        for (int i = 0; i < MaxChainWalk; i++) {
            Guid? followingId = current.FollowingId;
            if (!followingId.HasValue || !ReuseVisited.Add(followingId.Value)) {
                return false;
            }
            EntityWrapper next = cart.Entity.System.GetEntityById(followingId);
            if (next == null || next.Removed) {
                return false;
            }
            if (!(next.Controller is ServerCart2Controller nextCart)) {
                return true;
            }
            current = nextCart;
        }
        return false;
    }

    private static bool HasChainCapacity(ServerCart2Controller cart) {
        if (HasFreeSlot(cart)) {
            return true;
        }
        if (!ModConfig.ChainOverflowEnabled.Value) {
            return false;
        }
        ReuseVisited.Clear();
        ReuseVisited.Add(cart.Entity.Id);
        return WalkHasFreeSlot(cart, followers: true) || WalkHasFreeSlot(cart, followers: false);
    }

    private static bool WalkHasFreeSlot(ServerCart2Controller source, bool followers) {
        ServerCart2Controller current = source;
        for (int i = 0; i < MaxChainWalk; i++) {
            Guid? nextId = followers ? current.FollowerCartId : current.FollowingId;
            if (!nextId.HasValue || !ReuseVisited.Add(nextId.Value)) {
                return false;
            }
            EntityWrapper nextEntity = source.Entity.System.GetEntityById(nextId);
            if (nextEntity == null || nextEntity.Removed) {
                return false;
            }
            if (!(nextEntity.Controller is ServerCart2Controller nextCart)) {
                return false;
            }
            if (HasFreeSlot(nextCart)) {
                return true;
            }
            current = nextCart;
        }
        return false;
    }

    private static bool HasFreeSlot(ServerCart2Controller cart) {
        if (!cart.Carried1.HasValue || !cart.Carried2.HasValue || !cart.Carried3.HasValue || !cart.Carried4.HasValue) {
            return true;
        }
        return !cart.Carried5.HasValue && WorldFlagsHelper.HasFlag(ServerGameState.WorldFlags, "worship_flag:cart_capacity");
    }

    private static string GetFillSound(string resourceId) {
        if (resourceId == "resource:ash") {
            return "event:/items/bucket/fill_bucket_sand";
        }
        if (resourceId == "resource:water") {
            return "event:/items/bucket/fill_bucket_water";
        }
        return "event:/items/bucket/fill_bucket_construction_material";
    }
}
