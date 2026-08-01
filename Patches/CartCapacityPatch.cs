using CandideServer.Entities.Controllers;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;
using Shared.Entity.Base;

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
            CartCapacity.ObserveExact(cartEntity.BaseGuid, CartCapacity.Blessed, CartCargo.GetOccupied(cart));
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

    // the doodad database only exists once a world has been loaded, so discovery waits for a tick instead of patching the load itself
    [HarmonyPatch(typeof(AbstractController), nameof(AbstractController.Update), typeof(GameTime))]
    private static class Discovery {
        private static void Postfix() {
            CartCapacity.EnsureDiscovered();
        }
    }
}
