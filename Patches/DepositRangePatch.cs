using System.Runtime.CompilerServices;
using CandideServer;
using CandideServer.Entities.Controllers;
using CandideServer.Entities.ControllerSets.MaterialStorage;
using CandideServer.Helpers;
using CandideServer.Models;
using CandideServer.ServerManagers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;
using Shared.Entity.Components;

namespace BetterCarts.Patches;

[HarmonyPatch(typeof(ServerMaterialStoragePitController), nameof(ServerMaterialStoragePitController.Update), typeof(GameTime))]
internal static class DepositRangePatch
{
    private const float CheckTime = 0.1f;
    private const float MaxDepositZ = 24f;

    private sealed class TimerHolder
    {
        public float Value;
    }

    private static readonly ConditionalWeakTable<ServerMaterialStoragePitController, TimerHolder> Timers =
        new ConditionalWeakTable<ServerMaterialStoragePitController, TimerHolder>();

    private static readonly AccessTools.FieldRef<ServerMaterialStoragePitController, Rectangle> WorldBoundsRef =
        AccessTools.FieldRefAccess<ServerMaterialStoragePitController, Rectangle>("_worldBounds");

    private static readonly AccessTools.FieldRef<ServerMaterialStoragePitController, IOStorageType> StorageTypeRef =
        AccessTools.FieldRefAccess<ServerMaterialStoragePitController, IOStorageType>("_storageType");

    private static void Postfix(ServerMaterialStoragePitController __instance)
    {
        if (!ModConfig.Enabled.Value || !ModConfig.DepositRangeEnabled.Value)
        {
            return;
        }
        int range = ModConfig.DepositRange.Value;
        if (range <= 0)
        {
            return;
        }
        if (StorageTypeRef(__instance) != IOStorageType.Storage)
        {
            return;
        }
        var building = ServerTempState.CurrentlyUpdatingBuilding;
        if (building == null || !building.InstanceModel.ResourceStorageId.HasValue)
        {
            return;
        }
        EntityWrapper pit = __instance.Entity;
        if (pit == null || pit.Removed)
        {
            return;
        }
        TimerHolder timer = Timers.GetOrCreateValue(__instance);
        timer.Value -= pit.Fdt;
        if (timer.Value > 0f)
        {
            return;
        }
        timer.Value += CheckTime;
        if (!ServerGameState.TryGetResourceStorage(building.InstanceModel.ResourceStorageId.Value, out var storage))
        {
            return;
        }
        int reach = (int)(range * WorldInfo.TileSize);
        Rectangle zone = WorldBoundsRef(__instance);
        Rectangle extendedZone = zone;
        extendedZone.Inflate(reach, reach);
        Vector2 center = new Vector2(zone.X + zone.Width / 2f, zone.Y + zone.Height / 2f);
        float radius = new Vector2(extendedZone.Width, extendedZone.Height).Length() / 2f;
        foreach (EntityWrapper item in pit.System.GetEntitiesTouchingCircleArea(center, radius, pit.PositionZ - 8f, pit.PositionZ + MaxDepositZ))
        {
            if (item.PositionZ > MaxDepositZ || item.Id == pit.Id)
            {
                continue;
            }
            if (!item.MaskRef.HasFlag(Component.Movable) || !item.Carriable)
            {
                continue;
            }
            if (!extendedZone.Contains((int)item.Position2.X, (int)item.Position2.Y))
            {
                continue;
            }
            // items inside the vanilla zone stay with the vanilla pass (bucket + throw-out logic)
            if (zone.Contains((int)item.Position2.X, (int)item.Position2.Y))
            {
                continue;
            }
            EntityWrapper carrier = item.CarrierEntity;
            if (carrier == null || !(carrier.Controller is ServerCart2Controller))
            {
                continue;
            }
            if (item.ConstructionMaterials == null || item.ConstructionMaterials.IsEmpty)
            {
                continue;
            }
            if (!storage.CanAdjustByConstructionMaterialsAggregate(item.ConstructionMaterials))
            {
                continue;
            }
            var model = ServerEntityHelper.GetServerModelFromWrapperOrNull(item);
            if (model == null)
            {
                continue;
            }
            if (BuildingsServerManager.TryDepositResourceEntityIntoStorage(model, building.InstanceModel.Id))
            {
                ServerSendMessageHelper.PlaySoundOnPosition(model.Position, model.WorldId, "event:/hits/impact/impact_storage");
                break;
            }
        }
    }
}
