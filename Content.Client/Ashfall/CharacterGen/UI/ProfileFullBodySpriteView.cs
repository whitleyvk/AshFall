using System.Numerics;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Ashfall.CharacterGen.UI;

/// <summary>
///     Profile preview with the whole body fit inside the control: the sprite is zoomed as one
///     piece so its full bounding box (wings, gear, everything) stays within the frame, like an
///     animated ID thumbnail. The zoom is recomputed every frame from the actual sprite bounds,
///     so late-settling layers cannot leave the preview cropped or overflowing.
/// </summary>
public sealed class ProfileFullBodySpriteView : ProfilePreviewSpriteView
{
    // Matches the old one-shot card cap so small sprites do not blow up next to the text.
    private const float MaxZoom = 2.2f;

    /// <summary>Empty space kept between the sprite bounds and the control edges, in pixels.</summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float FitMargin { get; set; } = 4f;

    private EntityUid? _boundsDummy;
    private Box2 _lastBounds;

    protected override void Draw(IRenderHandle renderHandle)
    {
        if (!EntMan.TryGetComponent(PreviewDummy, out SpriteComponent? sprite) ||
            !EntMan.TryGetComponent(PreviewDummy, out TransformComponent? xform))
            return;

        SpriteSystem ??= EntMan.System<SpriteSystem>();

        // Ensure the sprite is animated despite possibly not being visible in any viewport.
        SpriteSystem.ForceUpdate(PreviewDummy);

        var bounds = sprite.CalculateRotatedBoundingBox(default, Angle.Zero, EyeRotation).CalcBoundingBox();
        if (bounds.Height <= 0f || bounds.Width <= 0f)
            return;

        // Layers (organ visuals, markings, job clothes) keep settling for a frame or two after
        // LoadPreview respawns the dummy; hold the previous fit until the bounds stabilize.
        if (_boundsDummy != PreviewDummy)
        {
            _boundsDummy = PreviewDummy;
        }
        else if (!_lastBounds.EqualsApprox(bounds, 0.5f))
        {
            bounds = _lastBounds;
        }
        _lastBounds = bounds;

        var ppm = EyeManager.PixelsPerMeter;

        var usable = new Vector2(PixelSize.X - FitMargin * 2f * UIScale, PixelSize.Y - FitMargin * 2f * UIScale);
        if (usable.X <= 1f || usable.Y <= 1f)
            return;

        var zoom = MathF.Min(usable.X / (bounds.Width * ppm * UIScale),
            usable.Y / (bounds.Height * ppm * UIScale));
        zoom = MathF.Min(zoom, MaxZoom);
        var scale = new Vector2(zoom, zoom);

        // Center the sprite's actual bounding box in the control (its origin can be off-center
        // for some species), so the body never drifts out of the frame.
        var position = new Vector2(
            PixelSize.X / 2f - bounds.Center.X * ppm * zoom * UIScale,
            PixelSize.Y / 2f - bounds.Center.Y * ppm * zoom * UIScale);

        var world = renderHandle.DrawingHandleWorld;
        var oldModulate = world.Modulate;
        // Same as the bust view: take the tint from the screen handle, which already carries
        // the accumulated tree modulate (ancestor fades), not from own values.
        var tint = renderHandle.DrawingHandleScreen.Modulate;
        // Dim-fade: darken RGB by the fade alpha and keep the sprite opaque, so every layer
        // dims as one silhouette instead of clothes/hair revealing the body behind them.
        // The dim bottoms out at the panel tone, not pure black, to blend into the dossier.
        const float FloorR = 0x14 / 255f, FloorG = 0x15 / 255f, FloorB = 0x17 / 255f;
        var fade = tint.A;
        tint.R = tint.R * fade + FloorR * (1f - fade);
        tint.G = tint.G * fade + FloorG * (1f - fade);
        tint.B = tint.B * fade + FloorB * (1f - fade);
        tint.A = 1;
        world.Modulate *= tint;

        renderHandle.DrawEntity(PreviewDummy, position, scale, null, EyeRotation, OverrideDirection, sprite, xform, EntMan.System<SharedTransformSystem>());
        world.Modulate = oldModulate;
    }
}
