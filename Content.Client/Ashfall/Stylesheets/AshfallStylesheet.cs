using System.Linq;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Palette;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Ashfall.Client.Stylesheets;

public sealed class AshfallStylesheet : NanotrasenStylesheet
{
    public const string PanelClass = "AshfallPanel";
    public const string PanelDeepClass = "AshfallPanelDeep";
    public const string HeaderPanelClass = "AshfallHeaderPanel";
    public const string LobbyPanelClass = "AshfallLobbyPanel";
    public const string LobbyInsetClass = "AshfallLobbyInset";
    public const string LobbyHeaderClass = "AshfallLobbyHeader";
    public const string LobbyChatPanelClass = "AshfallLobbyChatPanel";
    public const string ChatLogPanelClass = "AshfallChatLogPanel";
    public const string ListItemClass = "AshfallListItem";
    public const string ListItemSelectedClass = "AshfallListItemSelected";
    public const string TerminalHeaderClass = "AshfallTerminalHeader";
    public const string SectionHeaderClass = "AshfallSectionHeader";
    public const string StatusClass = "AshfallStatus";
    public const string MutedClass = "AshfallMuted";
    public const string WarningClass = "AshfallWarning";
    public const string PrimaryActionClass = "AshfallPrimaryAction";
    public const string SecondaryActionClass = "AshfallSecondaryAction";
    public const string AccentActionClass = "AshfallAccentAction";
    public const string ReadyActionClass = "AshfallReadyAction";
    public const string NavigationActionClass = "AshfallNavigationAction";
    public const string UtilityActionClass = "AshfallUtilityAction";
    public const string DestructiveActionClass = "AshfallDestructiveAction";

    // Color calibration, "dead matrix" (2026-09-07): void background, cold cheap plastic
    // panels, recessed screen-inner surfaces, sickly phosphor text, LED amber accents.
    public static readonly Color Background = Color.FromHex("#151617");
    public static readonly Color Panel = Color.FromHex("#2E3033");
    public static readonly Color PanelDeep = Color.FromHex("#151617");
    public static readonly Color Border = Color.FromHex("#43464B");
    public static readonly Color Text = Color.FromHex("#A3A8A3");
    public static readonly Color MutedText = Color.FromHex("#878C87");
    public static readonly Color DisabledText = Color.FromHex("#545654");
    public static readonly Color SelectedSurface = Color.FromHex("#241E17");
    public static readonly Color PrimaryAmber = Color.FromHex("#D48944");
    public static readonly Color SelectedBorder = Color.FromHex("#D48944");
    public static readonly Color Teal = Color.FromHex("#5E8B82");
    public static readonly Color Orange = Color.FromHex("#D48944");
    public static readonly Color Error = Color.FromHex("#B0574C");

    public override string StylesheetName => "Ashfall";
    public new static readonly ResPath TextureRoot = new("/Textures/Interface/Ashfall");

    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [TextureRoot, NanotrasenStylesheet.TextureRoot] },
    };

    public override ColorPalette PrimaryPalette =>
        ColorPalette.FromHexBase("#3D3F42", element: Color.FromHex("#3D3F42"), background: Background, text: Text);
    public override ColorPalette SecondaryPalette =>
        ColorPalette.FromHexBase("#2A2C2E", element: Color.FromHex("#2A2C2E"), background: Panel, text: Text);
    public override ColorPalette PositivePalette =>
        ColorPalette.FromHexBase("#5E8B82", element: Teal, background: Color.FromHex("#18211F"), text: Teal);
    public override ColorPalette NegativePalette =>
        ColorPalette.FromHexBase("#B0574C", element: Error, background: Color.FromHex("#2B1918"), text: Error);
    public override ColorPalette HighlightPalette =>
        ColorPalette.FromHexBase("#D48944", element: Orange, background: Color.FromHex("#29211A"), text: Orange);

    public AshfallStylesheet(object config, StylesheetManager manager) : base(config, manager)
    {
        var rules = new[]
        {
            [
                Element()
                    .Prop(Label.StylePropertyFontColor, Text),
            ],
            GetAllSheetletRules<AshfallStylesheet, CommonSheetletAttribute>(manager),
        };

        Stylesheet = new Stylesheet(Stylesheet.Rules.Concat(rules.SelectMany(rule => rule)).ToArray());
    }
}
