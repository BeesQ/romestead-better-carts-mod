using System;
using Candide.Entities.Controllers.Other;
using Candide.GameModels;
using Candide.Graphics;
using CandideCreator.Shared.Helpers;
using Microsoft.Xna.Framework;
using Shared.Entity;

namespace BetterCarts;

internal static class CartDisconnect {
    private const float TextHeight = 40f;
    private const int MaxChainWalk = 32;
    private const string Message = "Cart disconnected!";

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

    // a manual release clears FollowingId locally BEFORE the server echoes the change back, so only an automatic detach still holds a target when this parameter lands. The non-null to null edge is the whole discriminator - no timer, nothing sent over the wire
    internal static void NoteClientDetach(Cart2Controller cart, Guid? previousTarget) {
        if (cart == null || !Announcing || !previousTarget.HasValue || cart.FollowingId.HasValue) {
            return;
        }
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return;
        }
        if (!PulledByLocalPlayer(previousTarget.Value)) {
            return;
        }
        FloatingTextSystem.AddNewFloatText(Message,
            cartEntity.Position.ToScreenSpace() + new Vector2(0f, -TextHeight),
            StyleHelper.TextWarningColor);
    }

    // the same structural walk Connect Range uses: follow the chain until it stops being a cart, and the entity it ends at is whoever was pulling
    private static bool PulledByLocalPlayer(Guid start) {
        var localPlayer = GameState.LocalPlayer;
        if (localPlayer == null) {
            return false;
        }
        Guid? next = start;
        for (int step = 0; step < MaxChainWalk && next.HasValue; step++) {
            if (!GameState.Entities.TryGetValue(next.Value, out EntityWrapper entity) || entity == null) {
                return false;
            }
            if (entity.Controller is Cart2Controller ahead) {
                next = ahead.FollowingId;
                continue;
            }
            return entity.Id == localPlayer.EntityId;
        }
        return false;
    }
}
