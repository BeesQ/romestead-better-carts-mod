using System;
using Candide.Entities.Controllers.Other;
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

    // FollowTarget detaches in the same tick StuckTimer crosses the threshold and Detach resets it, so a timer at or above 0.5 can only mean the cart fell too far behind. Every other route here - the player releasing, a re-attach, a vanished target - arrives below it
    internal static void NoteDetach(ServerCart2Controller cart) {
        if (cart == null || !Announcing || cart.StuckTimer < StuckThreshold) {
            return;
        }
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return;
        }
        if (!PulledByLocalPlayer(cart.FollowingId)) {
            return;
        }
        if (!GameState.Entities.TryGetValue(cartEntity.Id, out EntityWrapper clientEntity)) {
            return;
        }
        Announce(clientEntity);
    }

    // the client half, for a player who is NOT hosting. A manual release clears FollowingId locally BEFORE the server echoes it back, so only an automatic detach still holds a target when this parameter lands - which makes the non-null to null edge the whole discriminator, with no timer and nothing sent over the wire. The host never receives this callback at all, so the two halves can never both fire
    internal static void NoteClientDetach(Cart2Controller cart, Guid? previousTarget) {
        if (cart == null || !Announcing || !previousTarget.HasValue || cart.FollowingId.HasValue) {
            return;
        }
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return;
        }
        if (!PulledByLocalPlayerClient(previousTarget.Value)) {
            return;
        }
        Announce(cartEntity);
    }

    private static void Announce(EntityWrapper cartEntity) {
        FloatingTextSystem.AddNewFloatText(Message,
            cartEntity.Position.ToScreenSpace() + new Vector2(0f, -TextHeight),
            StyleHelper.TextWarningColor);
    }

    // the same structural walk Connect Range uses: follow the chain until it stops being a cart, and the entity it ends at is whoever is pulling
    private static bool PulledByLocalPlayer(Guid? start) {
        var localPlayer = GameState.LocalPlayer;
        if (localPlayer == null) {
            return false;
        }
        Guid? next = start;
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

    private static bool PulledByLocalPlayerClient(Guid start) {
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
