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
    public const string TerminalHeaderClass = "AshfallTerminalHeader";
    public const string SectionHeaderClass = "AshfallSectionHeader";
    public const string StatusClass = "AshfallStatus";
    public const string MutedClass = "AshfallMuted";
    public const string WarningClass = "AshfallWarning";
    public const string PrimaryActionClass = "AshfallPrimaryAction";
    public const string SecondaryActionClass = "AshfallSecondaryAction";
    public const string NavigationActionClass = "AshfallNavigationAction";
    public const string UtilityActionClass = "AshfallUtilityAction";
    public const string DestructiveActionClass = "AshfallDestructiveAction";

    public static readonly Color Background = Color.FromHex("#151719");
    public static readonly Color Panel = Color.FromHex("#202427");
    public static readonly Color PanelDeep = Color.FromHex("#111315");
    public static readonly Color Border = Color.FromHex("#454B50");
    public static readonly Color Text = Color.FromHex("#D5D9DC");
    public static readonly Color MutedText = Color.FromHex("#8E969C");
    public static readonly Color Teal = Color.FromHex("#3F817B");
    public static readonly Color Orange = Color.FromHex("#C8782E");
    public static readonly Color Error = Color.FromHex("#A64D47");

    public override string StylesheetName => "Ashfall";
    public new static readonly ResPath TextureRoot = new("/Textures/Interface/Ashfall");

    public override Dictionary<Type, ResPath[]> Roots => new()
    {
        { typeof(TextureResource), [TextureRoot, NanotrasenStylesheet.TextureRoot] },
    };

    public override ColorPalette PrimaryPalette =>
        ColorPalette.FromHexBase("#343A3E", element: Color.FromHex("#343A3E"), background: Background, text: Text);
    public override ColorPalette SecondaryPalette =>
        ColorPalette.FromHexBase("#2B3034", element: Color.FromHex("#2B3034"), background: Panel, text: Text);
    public override ColorPalette PositivePalette =>
        ColorPalette.FromHexBase("#3F817B", element: Teal, background: Color.FromHex("#182725"), text: Teal);
    public override ColorPalette NegativePalette =>
        ColorPalette.FromHexBase("#A64D47", element: Error, background: Color.FromHex("#2B1918"), text: Error);
    public override ColorPalette HighlightPalette =>
        ColorPalette.FromHexBase("#C8782E", element: Orange, background: Color.FromHex("#2A2118"), text: Orange);

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
