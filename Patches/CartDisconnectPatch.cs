using System;
using Candide.Entities.Controllers.Other;
using CandideServer.Entities.Controllers;
using HarmonyLib;

namespace BetterCarts.Patches;

internal static class CartDisconnectPatch {
    // a PREFIX because Detach clears FollowingId and zeroes StuckTimer, and both are needed to tell an automatic disconnect from a release the player pressed. This half serves the host, which never receives the client callback below
    [HarmonyPatch(typeof(ServerCart2Controller), nameof(ServerCart2Controller.Detach), typeof(bool))]
    private static class Announce {
        private static void Prefix(ServerCart2Controller __instance) {
            CartDisconnect.NoteDetach(__instance);
        }
    }

    // OnServerSetState ends with a bare assignment of the synced "following" value, so capturing FollowingId before it runs and comparing after gives a clean non-null to null edge
    [HarmonyPatch(typeof(Cart2Controller), nameof(Cart2Controller.OnServerSetState), typeof(int))]
    private static class Sync {
        private static void Prefix(Cart2Controller __instance, out Guid? __state) {
            __state = __instance.FollowingId;
        }

        private static void Postfix(Cart2Controller __instance, Guid? __state) {
            CartDisconnect.NoteClientDetach(__instance, __state);
        }
    }
}
