using CandideServer.Entities.Controllers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;

namespace BetterCarts.Patches;

internal static class CartCapacityPatch {
    [HarmonyPatch(typeof(ServerCart2Controller), "PickupEntity")]
    private static class Capacity {
        // Priority.First keeps this ahead of Iron Cart's false-returning prefix; it must return true unless it deliberately blocks
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(ServerCart2Controller __instance, ref bool __result, out bool __state) {
            __state = false;
            int capacity = CartCapacity.GetEnforcedCapacity(__instance.Entity);
            if (capacity <= 0 || CartCargo.GetOccupied(__instance) < capacity) {
                return true;
            }
            __state = true;
            __result = false;
            ModLog.OnChange("block:" + __instance.Entity.Id,
                "BLOCK cart=" + __instance.Entity.Id + " occupied=" + CartCargo.GetOccupied(__instance)
                + " >= cap=" + capacity);
            return false;
        }

        // Priority.First puts the extend ahead of ChainOverflowPatch, so a Cart fills its own configured slots before it spills
        [HarmonyPriority(Priority.First)]
        private static void Postfix(ServerCart2Controller __instance, EntityWrapper entity, ref bool __result, bool __state) {
            if (__result) {
                CartCargo.Invalidate(__instance);
                return;
            }
            if (__state) {
                return;
            }
            Learn(__instance);
            int capacity = CartCapacity.GetEnforcedCapacity(__instance.Entity);
            if (capacity <= 0) {
                return;
            }
            if (entity == null || entity.Removed || entity.CarrierId.HasValue) {
                return;
            }
            if (!CartCargo.CanTakeExtra(__instance)) {
                return;
            }
            if (CartCargo.GetOccupied(__instance) >= capacity) {
                return;
            }
            ModLog.Info("EXTEND cart=" + __instance.Entity.Id + " taking " + entity.Id + " (cap=" + capacity
                + " occupied=" + CartCargo.GetOccupied(__instance) + ")");
            CartCargo.PinExtra(__instance, entity);
            __result = true;
        }

        // a refusal means every slot this Cart knows about is taken, so the current count IS its capacity - but only while the mod holds nothing extra on it
        private static void Learn(ServerCart2Controller cart) {
            if (CartCargo.HasExtras(cart) || !CartCargo.Adopted(cart)) {
                return;
            }
            EntityWrapper cartEntity = cart.Entity;
            if (cartEntity == null || cartEntity.Removed) {
                return;
            }
            int occupied = CartCargo.GetOccupied(cart);
            ModLog.OnChange("refuse:" + cartEntity.Id, "REFUSAL cart=" + cartEntity.Id + " occupied=" + occupied
                + " blessed=" + CartCapacity.Blessed + " -> exact for type " + cartEntity.BaseGuid);
            CartCapacity.ObserveExact(cartEntity.BaseGuid, CartCapacity.Blessed, occupied);
        }
    }

    [HarmonyPatch(typeof(ServerCart2Controller), nameof(ServerCart2Controller.Update), typeof(GameTime))]
    private static class Extras {
        private static void Postfix(ServerCart2Controller __instance) {
            CartCargo.Tick(__instance);
        }
    }

    [HarmonyPatch(typeof(ServerCart2Controller), nameof(ServerCart2Controller.OnRemove))]
    private static class ReleaseOnRemove {
        private static void Postfix(ServerCart2Controller __instance) {
            CartCargo.ReleaseAll(__instance);
        }
    }

    // discovery needs a loaded doodad database, and a Cart existing proves there is one. Hanging it off EntityInitialize costs one guarded bool per Cart spawn instead of one per controller per tick
    [HarmonyPatch(typeof(ServerCart2Controller), nameof(ServerCart2Controller.EntityInitialize))]
    private static class Discovery {
        private static void Postfix() {
            CartCapacity.EnsureDiscovered(true);
        }
    }
}
