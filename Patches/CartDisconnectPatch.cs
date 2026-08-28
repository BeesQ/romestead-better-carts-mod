using CandideServer.Entities.Controllers;
using HarmonyLib;

namespace BetterCarts.Patches;

internal static class CartDisconnectPatch {
    // a PREFIX because Detach clears FollowingId and zeroes StuckTimer, and both are needed to tell an automatic disconnect from a release the player pressed
    [HarmonyPatch(typeof(ServerCart2Controller), nameof(ServerCart2Controller.Detach), typeof(bool))]
    private static class Announce {
        private static void Prefix(ServerCart2Controller __instance) {
            CartDisconnect.NoteDetach(__instance);
        }
    }
}
