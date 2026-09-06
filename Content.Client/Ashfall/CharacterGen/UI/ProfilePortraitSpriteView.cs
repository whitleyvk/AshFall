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

        var ppm = EyeManager.PixelsPerMeter;

        // Zoom so that the top PortraitFraction of the body fills the control height.
        var zoom = PixelSize.Y / (ppm * UIScale * bounds.Height * PortraitFraction);
        var scale = new Vector2(zoom, zoom);

        // Center the sprite's actual bounding box (its origin can be off-center for some species),
        // and top-anchor it: everything below the framed area is clipped by the control.
        var position = new Vector2(
            PixelSize.X / 2f - bounds.Center.X * ppm * zoom * UIScale,
            bounds.Height / 2f * ppm * zoom * UIScale);

        var world = renderHandle.DrawingHandleWorld;
        var oldModulate = world.Modulate;
        world.Modulate *= Modulate * ActualModulateSelf;

        renderHandle.DrawEntity(PreviewDummy, position, scale, null, EyeRotation, OverrideDirection, sprite, xform, transformSystem);
        world.Modulate = oldModulate;
    }
}
