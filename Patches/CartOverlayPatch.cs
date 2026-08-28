using Candide.Entities.Controllers.Other;
using Candide.LegacyUI;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BetterCarts.Patches;

// client-side only: it draws what the server already decided, and a player without Better Carts simply sees no number
internal static class CartOverlayPatch {
    [HarmonyPatch(typeof(Cart2Controller), nameof(Cart2Controller.Update), typeof(GameTime))]
    private static class Track {
        private static void Postfix(Cart2Controller __instance) {
            CartOverlay.Track(__instance);
        }
    }

    // PickupTextManager, not FloatingTextSystem: this block samples PointClamp, which is what the pixel font needs. Drawing the same text in the float-text block filters it and looks soft
    [HarmonyPatch(typeof(PickupTextManager), nameof(PickupTextManager.Draw))]
    private static class Draw {
        private static void Postfix(SpriteBatch batch, Matrix cameraMatrix) {
            CartOverlay.Draw(batch, cameraMatrix);
        }
    }
}
