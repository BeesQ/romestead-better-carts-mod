using System;
using Candide.GameModels;
using Candide.Graphics;
using CandideCreator.Shared.Helpers;
using CandideServer;
using CandideServer.Entities.Controllers;
using Microsoft.Xna.Framework;
using Shared.Entity;

namespace BetterCarts;

internal static class CartDisconnect {
    private const float StuckThreshold = 0.5f;
    private const float TextHeight = 40f;
    private const int MaxChainWalk = 32;
    private const string Message = "Cart disconnected";

    private static bool Announcing {
        get {
            if (ModConfig.Enabled == null || !ModConfig.Enabled.Value) {
                return false;
            }
            if (ModConfig.CartOverlaysEnabled == null || !ModConfig.CartOverlaysEnabled.Value) {
                return false;
            }
            return ModConfig.CartOverlayDisconnectMessage != null && ModConfig.CartOverlayDisconnectMessage.Value;
        }
    }

    // FollowTarget detaches in the same tick StuckTimer crosses the threshold and Detach resets it, so a timer at or above 0.5 can only mean the cart fell too far behind. Every other route here - the player releasing, a re-attach, a vanished target - arrives below it
    internal static void NoteDetach(ServerCart2Controller cart) {
        if (cart == null || !Announcing || cart.StuckTimer < StuckThreshold) {
            return;
        }
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return;
        }
        if (!PulledByLocalPlayer(cart)) {
            return;
        }
        if (!GameState.Entities.TryGetValue(cartEntity.Id, out EntityWrapper clientEntity) || clientEntity == null) {
            return;
        }
        FloatingTextSystem.AddNewFloatText(Message,
            clientEntity.Position.ToScreenSpace() + new Vector2(0f, -TextHeight),
            StyleHelper.TextWarningColor);
    }

    // the same structural walk Connect Range uses: follow the chain until it stops being a cart, and the entity it ends at is whoever is pulling
    private static bool PulledByLocalPlayer(ServerCart2Controller cart) {
        var localPlayer = GameState.LocalPlayer;
        if (localPlayer == null) {
            return false;
        }
        Guid? next = cart.FollowingId;
        for (int step = 0; step < MaxChainWalk && next.HasValue; step++) {
            if (!ServerGameState.Entities.TryGetValue(next.Value, out var model)) {
                return false;
            }
            EntityWrapper entity = model == null ? null : model.EntityWrapper;
            if (entity == null) {
                return false;
            }
            if (entity.Controller is ServerCart2Controller ahead) {
                next = ahead.FollowingId;
                continue;
            }
            return entity.Id == localPlayer.EntityId;
        }
        return false;
    }
}
