using System.Numerics;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Ashfall.CharacterGen.UI;

/// <summary>
///     Profile preview framed as a bust "personnel file photograph": the sprite is zoomed so that
///     only the top <see cref="PortraitFraction"/> of the body (head, neck, shoulders) fills the
///     control. The aspect ratio is preserved, everything below is simply clipped.
///     The zoom is computed from the actual sprite bounds, so it works consistently across species.
/// </summary>
public sealed class ProfilePortraitSpriteView : ProfilePreviewSpriteView
{
    /// <summary>
    ///     How much of the sprite height (from the top) the control should frame.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float PortraitFraction { get; set; } = 0.5f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float VerticalOffset { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    public float HorizontalOffset { get; set; }

    private EntityUid? _boundsDummy;
    private Box2 _lastBounds;

    protected override void Draw(IRenderHandle renderHandle)
    {
        if (!EntMan.TryGetComponent(PreviewDummy, out SpriteComponent? sprite) ||
            !EntMan.TryGetComponent(PreviewDummy, out TransformComponent? xform))
            return;

        SpriteSystem ??= EntMan.System<SpriteSystem>();
        var transformSystem = EntMan.System<SharedTransformSystem>();

        // Ensure the sprite is animated despite possibly not being visible in any viewport.
        SpriteSystem.ForceUpdate(PreviewDummy);

        var bounds = sprite.CalculateRotatedBoundingBox(default, Angle.Zero, EyeRotation).CalcBoundingBox();
        if (bounds.Height <= 0f)
            return;

        // Layers (organ visuals, markings, job clothes) keep settling for a frame or two after
        // LoadPreview respawns the dummy; hold the previous crop until the bounds stabilize.
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

        // Keep the portrait crop fixed by height so every dossier uses the same close framing.
        var zoom = PixelSize.Y / (ppm * UIScale * bounds.Height * PortraitFraction);
        var scale = new Vector2(zoom, zoom);

        // Center the sprite's actual bounding box horizontally (its origin can be off-center for
        // some species) and pin its top to the control's top: everything below the framed area is
        // clipped by the control. Anchoring via the box top (not via height/2) keeps the head in
        // place even when gear shifts the bounding box.
        var position = new Vector2(
            PixelSize.X / 2f - bounds.Center.X * ppm * zoom * UIScale + HorizontalOffset * UIScale,
            VerticalOffset * UIScale + bounds.Top * ppm * zoom * UIScale);

        var world = renderHandle.DrawingHandleWorld;
        var oldModulate = world.Modulate;
        world.Modulate *= Modulate * ActualModulateSelf;

        renderHandle.DrawEntity(PreviewDummy, position, scale, null, EyeRotation, OverrideDirection, sprite, xform, transformSystem);
        world.Modulate = oldModulate;
    }
}
