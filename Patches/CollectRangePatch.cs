using CandideServer.Entities.Controllers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;

namespace BetterCarts.Patches;

[HarmonyPatch(typeof(ServerCart2Controller), nameof(ServerCart2Controller.Update), typeof(GameTime))]
internal static class CollectRangePatch
{
    private static void Postfix(ServerCart2Controller __instance)
    {
        if (!ModConfig.Enabled.Value || !ModConfig.CollectRangeEnabled.Value)
        {
            return;
        }
        int range = ModConfig.CollectRange.Value;
        if (range <= 0)
        {
            return;
        }
        EntityWrapper cart = __instance.Entity;
        if (cart == null || cart.Removed)
        {
            return;
        }
        float radius = range * WorldInfo.TileSize;
        foreach (EntityWrapper item in cart.System.GetEntitiesTouchingCircleArea(cart.Position2, radius, cart.Position.Z, cart.Position.Z + 8f))
        {
            if (!ServerCart2Controller.CanBeAutoPicked(item))
            {
                continue;
            }
            if (!CartAccess.PickupEntity(__instance, item))
            {
                break;
            }
        }
    }
}
