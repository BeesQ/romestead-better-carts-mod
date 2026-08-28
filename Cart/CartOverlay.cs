using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Candide;
using Candide.Entities.Controllers.Other;
using Candide.GameModels;
using Candide.Graphics;
using Candide.Graphics.Fonts;
using CandideCreator.Shared.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Entity;

namespace BetterCarts;

internal static class CartOverlay {
    private const float AnchorHeight = 20f;
    private const float ViewportCullFactor = 0.8f;

    private sealed class OverlayState {
        internal int Count = -1;
        internal string Text = string.Empty;
    }

    private static readonly ConditionalWeakTable<Cart2Controller, OverlayState> States =
        new ConditionalWeakTable<Cart2Controller, OverlayState>();

    private static readonly HashSet<Guid> ReuseIds = new HashSet<Guid>();
    private static readonly List<Guid> ReuseExtras = new List<Guid>();

    internal static bool Showing {
        get {
            if (ModConfig.Enabled == null || !ModConfig.Enabled.Value) {
                return false;
            }
            if (ModConfig.CartOverlaysEnabled == null || !ModConfig.CartOverlaysEnabled.Value) {
                return false;
            }
            return Flag(ModConfig.CartOverlayShowAboveVanilla)
                || Flag(ModConfig.CartOverlayShowVanilla)
                || Flag(ModConfig.CartOverlayShowEmpty);
        }
    }

    internal static void Track(Cart2Controller cart) {
        if (cart == null || !Showing) {
            return;
        }
        EntityWrapper cartEntity = cart.Entity;
        if (cartEntity == null || cartEntity.Removed) {
            return;
        }
        OverlayState state = States.GetOrCreateValue(cart);
        int count = CountCargo(cart, cartEntity);
        if (count != state.Count) {
            state.Count = count;
            state.Text = count.ToString(CultureInfo.InvariantCulture);
        }
    }

    // the batch is already open and carries no camera transform, exactly like PickupTextManager's own draw, so every position is transformed here and nothing calls Begin or End
    internal static void Draw(SpriteBatch batch, Matrix cameraMatrix) {
        if (batch == null || !Showing) {
            return;
        }
        Vector2 cameraCenter = Globals.Game.Camera.CurrentPositionCenter;
        float cull = batch.GraphicsDevice.Viewport.Width * ViewportCullFactor;
        cull *= cull;
        float scale = Globals.InterfaceScale;

        foreach (KeyValuePair<Cart2Controller, OverlayState> pair in States) {
            OverlayState state = pair.Value;
            if (state.Count < 0 || !Visible(state.Count)) {
                continue;
            }
            Cart2Controller cart = pair.Key;
            if (cart == null) {
                continue;
            }
            EntityWrapper cartEntity = cart.Entity;
            if (cartEntity == null || cartEntity.Removed || !GameState.Entities.ContainsKey(cartEntity.Id)) {
                continue;
            }
            Vector2 anchor = cartEntity.Position.ToScreenSpace() + new Vector2(0f, -AnchorHeight);
            if (Vector2.DistanceSquared(anchor, cameraCenter) > cull) {
                continue;
            }
            Vector2 position = Vector2.Transform(anchor, cameraMatrix);
            position.X = MathF.Round(position.X);
            position.Y = MathF.Round(position.Y);
            PixelSpriteFont.ArialPixel.DrawHorizontallyCentered(batch, state.Text,
                position + new Vector2(1f, 1f), scale, Color.Black);
            PixelSpriteFont.ArialPixel.DrawHorizontallyCentered(batch, state.Text,
                position, scale, StyleHelper.TextNormalColor);
        }
    }

    private static bool Visible(int count) {
        if (count == 0) {
            return Flag(ModConfig.CartOverlayShowEmpty);
        }
        if (count > CartCapacity.VanillaBlessed) {
            return Flag(ModConfig.CartOverlayShowAboveVanilla);
        }
        return Flag(ModConfig.CartOverlayShowVanilla);
    }

    // a cart can name the SAME item in several slot parameters, so counting entries reports five for a cart holding two. Distinct ids are the only correct count, and CarrierId is what separates cargo from the other Guids a cart stores
    private static int CountCargo(Cart2Controller cart, EntityWrapper cartEntity) {
        ReuseIds.Clear();
        var parameters = cart.Parameters;
        if (parameters == null) {
            return 0;
        }
        var dictionary = parameters.Dictionary;
        if (dictionary != null) {
            foreach (var pair in dictionary) {
                if (string.Equals(pair.Key, CartCargoSync.CargoKey, StringComparison.Ordinal)) {
                    continue;
                }
                if (Guid.TryParse(pair.Value, out Guid slotted)) {
                    AddIfCarried(cartEntity, slotted);
                }
            }
        }
        CartCargoSync.Unpack(parameters.GetString(CartCargoSync.CargoKey, string.Empty), ReuseExtras);
        foreach (Guid id in ReuseExtras) {
            AddIfCarried(cartEntity, id);
        }
        return ReuseIds.Count;
    }

    private static void AddIfCarried(EntityWrapper cartEntity, Guid id) {
        if (!GameState.Entities.TryGetValue(id, out EntityWrapper item)) {
            return;
        }
        if (item == null || item.Removed || !item.Carriable || item.CarrierId != cartEntity.Id) {
            return;
        }
        ReuseIds.Add(id);
    }

    private static bool Flag(BepInEx.Configuration.ConfigEntry<bool> entry) {
        return entry != null && entry.Value;
    }
}
