using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Candide;
using Candide.Entities.Controllers.Other;
using Candide.GameModels;
using Candide.Graphics;
using Candide.Graphics.Fonts;
using CandideCreator.Shared.Graphics;
using CandideCreator.Shared.Helpers;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared.Entity;

namespace BetterCarts;

internal static class CartOverlay {
    // Font
    // ArialPixel, Adventurer, Arial, Calibri, Courier
    private static readonly StaticSpriteFont Font = PixelSpriteFont.ArialPixel;

    // Size
    // multiplies Globals.InterfaceScale, which is 2 by default. Whole numbers keep the glyph pixels even
    private const float TextScale = 1f;

    // Height
    // world units above the cart, applied before the camera transform, so the gap scales with zoom
    private const float AnchorHeight = 16f;

    // Color
    // any StyleHelper constant: TextNormalColor, TextYellowColor, TextGreenColor, TextWarningColor
    private static readonly Color TextColor = StyleHelper.TextNormalColor;

    // Shadow (4-sided)
    private static readonly bool DrawShadow = true;
    private static readonly float ShadowOffset = 1f;
    private static readonly Color ShadowColor = Color.Black;

    // Backing plate
    // vanilla's own plate is DimGray at 0.3 alpha with 4 x 2 padding
    private static readonly bool DrawPlate = false;
    private const float PlatePaddingX = 4f;
    private const float PlatePaddingY = 2f;
    private static readonly Color PlateColor = new Color(Color.DimGray, 0.3f);

    // Zoom cull
    // vanilla hides distant entities while zoomed out; at normal zoom this costs one bool test and hides nothing
    private static readonly bool CullWhenZoomedOut = true;

    // Viewport cull
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
        float scale = Globals.InterfaceScale * TextScale;
        Vector2 scaleVector = new Vector2(scale, scale);
        Vector2 anchorOffset = new Vector2(0f, -AnchorHeight);

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
            Vector2 anchor = cartEntity.Position.ToScreenSpace() + anchorOffset;
            if (Vector2.DistanceSquared(anchor, cameraCenter) > cull) {
                continue;
            }
            if (CullWhenZoomedOut && DeferredRenderer.IsOutsideFoW(cartEntity)) {
                continue;
            }
            Vector2 position = Vector2.Transform(anchor, cameraMatrix);
            position.X = MathF.Round(position.X);
            position.Y = MathF.Round(position.Y);
            if (DrawPlate) {
                // X2 and Y2 are width and height ONLY because the measured position is Vector2.Zero - pass a real position and they become far-edge coordinates instead
                Bounds bounds = Font.TextBounds(state.Text, Vector2.Zero, scaleVector);
                float plateWidth = bounds.X2 + PlatePaddingX;
                float plateHeight = bounds.Y2 + PlatePaddingY;
                batch.DrawRect(position - new Vector2(plateWidth / 2f, 1f), plateWidth, plateHeight, PlateColor);
            }
            if (DrawShadow) {
                Font.DrawHorizontallyCentered(batch, state.Text, position + new Vector2(ShadowOffset, ShadowOffset), scale, ShadowColor);
                Font.DrawHorizontallyCentered(batch, state.Text, position + new Vector2(ShadowOffset, -ShadowOffset), scale, ShadowColor);
                Font.DrawHorizontallyCentered(batch, state.Text, position + new Vector2(-ShadowOffset, ShadowOffset), scale, ShadowColor);
                Font.DrawHorizontallyCentered(batch, state.Text, position + new Vector2(-ShadowOffset, -ShadowOffset), scale, ShadowColor);
            }
            Font.DrawHorizontallyCentered(batch, state.Text, position, scale, TextColor);
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