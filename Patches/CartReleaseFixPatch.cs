using System;
using Candide.Entities.Controllers.Other;
using Candide.GameModels;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Shared.Entity;
using Shared.Models.Interaction;

namespace BetterCarts.Patches;

internal static class CartReleaseFixPatch {
    private static Guid? _pulledCartId;

    [HarmonyPatch(typeof(Cart2Controller), nameof(Cart2Controller.Update), typeof(GameTime))]
    private static class TrackPulledCart {
        private static void Postfix(Cart2Controller __instance) {
            EntityWrapper cart = __instance.Entity;
            if (cart == null) {
                return;
            }
            if (__instance.FollowingId.HasValue && __instance.FollowingId.Value == GameState.LocalPlayer.EntityId) {
                _pulledCartId = cart.Id;
            }
            else if (_pulledCartId == cart.Id) {
                _pulledCartId = null;
            }
        }
    }

    [HarmonyPatch(typeof(Cart2Controller), nameof(Cart2Controller.GetInteraction), typeof(EntityWrapper))]
    private static class SkipOtherCartWhilePulling {
        private static bool Prefix(Cart2Controller __instance, EntityWrapper otherEntity, ref Interaction __result) {
            if (!ModConfig.Enabled.Value || !ModConfig.CartReleaseFixEnabled.Value) {
                return true;
            }
            if (!_pulledCartId.HasValue) {
                return true;
            }
            EntityWrapper cart = __instance.Entity;
            if (cart == null || cart.Id == _pulledCartId.Value) {
                return true;
            }
            if (otherEntity == null || otherEntity.Id != GameState.LocalPlayer.EntityId) {
                return true;
            }
            if (!GameState.Entities.TryGetValue(_pulledCartId.Value, out EntityWrapper pulledCart) || pulledCart.Removed
                || !(pulledCart.Controller is Cart2Controller pulledController) || !pulledController.FollowingId.HasValue
                || pulledController.FollowingId.Value != GameState.LocalPlayer.EntityId) {
                _pulledCartId = null;
                return true;
            }
            // returning null makes the interact scan skip this cart and pick the next best non-cart candidate
            __result = null;
            return false;
        }
    }
}
