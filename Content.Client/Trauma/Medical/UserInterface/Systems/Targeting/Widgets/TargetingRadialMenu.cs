// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Medical.Common.Targeting;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;

namespace Content.Medical.Client.UserInterface.Systems.Targeting.Widgets;

public sealed partial class TargetingRadialMenu : BaseWindow
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IResourceCache _resourceCache = default!;

    public event Action<TargetBodyPart>? OnTargetSelected;

    private readonly LayoutContainer _container;
    private readonly RadialMenuOuterAreaButton _outerButton;
    private readonly TargetingCentralCircleButton _chestButton;
    private readonly Dictionary<TargetBodyPart, RadialMenuButtonWithSector> _sectorButtons = new();

    private const float WindowDimension = 240f;
    private static readonly Vector2 WindowSize = new(WindowDimension, WindowDimension);
    private static readonly Vector2 Center = WindowSize * 0.5f;

    private const float ChestRadius = 34f;
    private const float SectorInnerRadius = 44f;
    private const float SectorOuterRadius = 106f;
    private const float OuterCloseRadius = 112f;
    private const float SectorAngularGap = 0.06f;

    private static readonly Color BaseBackground = Color.FromHex("#282b3dc0");
    private static readonly Color HoverBackground = Color.FromHex("#3f4563d0");
    private static readonly Color BaseBorder = Color.FromHex("#525c7ec0");
    private static readonly Color HoverBorder = Color.FromHex("#8899c7e0");

    private static readonly Color SelectedBackground = Color.FromHex("#1d522cc0");
    private static readonly Color SelectedBorder = Color.FromHex("#48d168e0");

    public TargetingRadialMenu()
    {
        IoCManager.InjectDependencies(this);

        MinSize = WindowSize;
        SetSize = WindowSize;

        _container = new LayoutContainer
        {
            MinSize = WindowSize,
            SetSize = WindowSize,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        AddChild(_container);

        // Outside click detector
        _outerButton = new RadialMenuOuterAreaButton
        {
            ParentCenter = Center,
            OuterRadius = OuterCloseRadius,
        };
        _outerButton.OnButtonUp += _ => Close();
        _container.AddChild(_outerButton);

        // Central button: Chest
        _chestButton = new TargetingCentralCircleButton
        {
            Radius = ChestRadius,
            ParentCenter = Center,
            SetSize = new Vector2(ChestRadius * 2f, ChestRadius * 2f),
            BackgroundColor = BaseBackground,
            HoverBackgroundColor = HoverBackground,
            BorderColor = BaseBorder,
            HoverBorderColor = HoverBorder,
            SelectedBackgroundColor = SelectedBackground,
            SelectedBorderColor = SelectedBorder,
            ToolTip = Loc.GetString("targeting-menu-chest"),
        };
        _chestButton.OnPressed += _ =>
        {
            OnTargetSelected?.Invoke(TargetBodyPart.Chest);
            Close();
        };

        var chestTexture = _resourceCache.GetTexture("/Textures/_Trauma/Interface/Targeting/Doll/torso.png");
        var chestIcon = new TextureRect
        {
            Texture = chestTexture,
            TextureScale = new Vector2(2.5f, 2.5f),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
        };
        _chestButton.AddChild(chestIcon);

        LayoutContainer.SetPosition(_chestButton, Center - new Vector2(ChestRadius, ChestRadius));
        _container.AddChild(_chestButton);

        // 6 Peripheral Sectors (arranged around the center)
        // 0: LeftArm (upper-right, 0° to 60°)
        // 1: Head (top, 60° to 120°)
        // 2: RightArm (upper-left, 120° to 180°)
        // 3: RightLeg (lower-left, 180° to 240°)
        // 4: Groin (bottom, 240° to 300°)
        // 5: LeftLeg (lower-right, 300° to 360°)
        var sectorConfigs = new (TargetBodyPart Part, float From, float To, string TexturePath, string LocKey)[]
        {
            (TargetBodyPart.LeftArm, 0f, MathF.PI / 3f, "/Textures/_Trauma/Interface/Targeting/Doll/leftarm.png", "targeting-menu-left-arm"),
            (TargetBodyPart.Head, MathF.PI / 3f, 2f * MathF.PI / 3f, "/Textures/_Trauma/Interface/Targeting/Doll/head.png", "targeting-menu-head"),
            (TargetBodyPart.RightArm, 2f * MathF.PI / 3f, MathF.PI, "/Textures/_Trauma/Interface/Targeting/Doll/rightarm.png", "targeting-menu-right-arm"),
            (TargetBodyPart.RightLeg, MathF.PI, 4f * MathF.PI / 3f, "/Textures/_Trauma/Interface/Targeting/Doll/rightleg.png", "targeting-menu-right-leg"),
            (TargetBodyPart.Groin, 4f * MathF.PI / 3f, 5f * MathF.PI / 3f, "/Textures/_Trauma/Interface/Targeting/Doll/groin.png", "targeting-menu-groin"),
            (TargetBodyPart.LeftLeg, 5f * MathF.PI / 3f, MathF.Tau, "/Textures/_Trauma/Interface/Targeting/Doll/leftleg.png", "targeting-menu-left-leg"),
        };

        const float midRadius = (SectorInnerRadius + SectorOuterRadius) * 0.5f;
        var buttonButtonSize = new Vector2(40f, 40f);

        foreach (var (part, fromAngle, toAngle, texturePath, locKey) in sectorConfigs)
        {
            var sectorBtn = new RadialMenuButtonWithSector
            {
                SetSize = buttonButtonSize,
                InnerRadius = SectorInnerRadius,
                OuterRadius = SectorOuterRadius,
                AngularGap = SectorAngularGap,
                AngleSectorFrom = fromAngle,
                AngleSectorTo = toAngle,
                AngleOffset = 0f,
                ParentCenter = Center,
                DrawBackground = true,
                DrawBorder = true,
                BackgroundColor = BaseBackground,
                HoverBackgroundColor = HoverBackground,
                BorderColor = BaseBorder,
                HoverBorderColor = HoverBorder,
                SelectedBackgroundColor = SelectedBackground,
                SelectedBorderColor = SelectedBorder,
                SeparatorColor = BaseBorder,
                ToolTip = Loc.GetString(locKey),
            };

            var partLocal = part;
            sectorBtn.OnPressed += _ =>
            {
                OnTargetSelected?.Invoke(partLocal);
                Close();
            };

            var tex = _resourceCache.GetTexture(texturePath);
            var icon = new TextureRect
            {
                Texture = tex,
                TextureScale = new Vector2(2.5f, 2.5f),
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                MouseFilter = MouseFilterMode.Ignore,
            };
            sectorBtn.AddChild(icon);

            var midAngle = (fromAngle + toAngle) * 0.5f;
            var iconCenter = Center + new Vector2(MathF.Cos(midAngle) * midRadius, -MathF.Sin(midAngle) * midRadius);
            LayoutContainer.SetPosition(sectorBtn, iconCenter - buttonButtonSize * 0.5f);

            _container.AddChild(sectorBtn);
            _sectorButtons[part] = sectorBtn;
        }
    }

    public void SetCurrentTarget(TargetBodyPart currentTarget)
    {
        _chestButton.IsSelected = (currentTarget & TargetBodyPart.Chest) != 0;

        foreach (var (part, button) in _sectorButtons)
        {
            button.IsSelected = (currentTarget & part) != 0;
        }
    }

    public void OpenOverMouseScreenPosition()
    {
        var vpSize = _clyde.ScreenSize;
        if (vpSize.X <= 0 || vpSize.Y <= 0)
            return;

        OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }
}

public sealed class TargetingCentralCircleButton : RadialMenuButtonBase
{
    private static Vector2[]? _fillPoints;
    private static Vector2[]? _borderPoints;

    public float Radius { get; set; } = 34f;
    public Vector2 ParentCenter { get; set; }

    public bool DrawBackground { get; set; } = true;
    public bool DrawBorder { get; set; } = true;

    public Color BackgroundColor { get; set; }
    public Color HoverBackgroundColor { get; set; }
    public Color BorderColor { get; set; }
    public Color HoverBorderColor { get; set; }

    public bool IsSelected { get; set; }
    public Color? SelectedBackgroundColor { get; set; }
    public Color? SelectedBorderColor { get; set; }

    protected override bool HasPoint(Vector2 point)
    {
        var distSq = (point + Position - ParentCenter).LengthSquared();
        return distSq <= Radius * Radius;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var center = (ParentCenter - Position) * UIScale;
        var radius = Radius * UIScale;
        const int segments = 32;

        if (DrawBackground)
        {
            var bgColor = DrawMode == DrawModeEnum.Hover
                ? HoverBackgroundColor
                : (IsSelected && SelectedBackgroundColor.HasValue ? SelectedBackgroundColor.Value : BackgroundColor);

            var srgbBg = Color.ToSrgb(bgColor);
            var bufferSize = (segments + 1) * 2;
            if (_fillPoints == null || _fillPoints.Length != bufferSize)
                _fillPoints = new Vector2[bufferSize];

            for (var i = 0; i <= segments; i++)
            {
                var angle = (float)i / segments * MathF.Tau;
                var unit = new Vector2(MathF.Cos(angle), -MathF.Sin(angle));
                _fillPoints[i * 2] = center + unit * radius;
                _fillPoints[i * 2 + 1] = center;
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, _fillPoints, srgbBg);
        }

        var shouldDrawBorder = DrawBorder || (IsSelected && SelectedBorderColor.HasValue);
        if (shouldDrawBorder)
        {
            var bdColor = DrawMode == DrawModeEnum.Hover
                ? HoverBorderColor
                : (IsSelected && SelectedBorderColor.HasValue ? SelectedBorderColor.Value : BorderColor);

            var srgbBorder = Color.ToSrgb(bdColor);
            var borderSize = segments + 1;
            if (_borderPoints == null || _borderPoints.Length != borderSize)
                _borderPoints = new Vector2[borderSize];

            for (var i = 0; i <= segments; i++)
            {
                var angle = (float)i / segments * MathF.Tau;
                _borderPoints[i] = center + new Vector2(MathF.Cos(angle), -MathF.Sin(angle)) * radius;
            }

            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, _borderPoints, srgbBorder);
        }
    }
}
