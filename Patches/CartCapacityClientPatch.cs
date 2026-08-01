using Candide.Entities.Controllers.Other;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;

namespace BetterCarts.Patches;

// client-side only: it renders what the server already decided. A joining player without Better Carts installed still gets the host's capacity, but sees extra cargo lying on the ground instead of on the Cart
internal static class CartCapacityClientPatch {
    [HarmonyPatch(typeof(Cart2Controller), nameof(Cart2Controller.OnServerSetState))]
    private static class Sync {
        private static void Postfix(Cart2Controller __instance) {
            CartCargoClient.SyncSlots(__instance);
        }
    }

    [HarmonyPatch(typeof(Cart2Controller), nameof(Cart2Controller.Update), typeof(GameTime))]
    private static class Hold {
        private static void Postfix(Cart2Controller __instance) {
            CartCargoClient.UpdateSlots(__instance);
        }
    }

    [HarmonyPatch(typeof(Cart2Controller), nameof(Cart2Controller.OnRemove), typeof(EntityRemoveInfo))]
    private static class Release {
        private static void Postfix(Cart2Controller __instance) {
            CartCargoClient.ReleaseAll(__instance);
        }
    }

    // the client doodad database is the discovery fallback, so a Cart appearing here is the same "a world is loaded" proof the server patch uses
    [HarmonyPatch(typeof(Cart2Controller), nameof(Cart2Controller.EntityInitialize))]
    private static class Discovery {
        private static void Postfix() {
            CartCapacity.EnsureDiscovered();
        }
    }
}
