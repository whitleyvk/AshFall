using Content.Client.ContextMenu.UI;
using Content.Client.Examine;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Content.Client.Stylesheets.Sheetlets;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Ashfall.Client.Stylesheets;

[CommonSheetlet]
public sealed class AshfallCoreSheetlet : Sheetlet<AshfallStylesheet>
{
    public override StyleRule[] GetRules(AshfallStylesheet sheet, object config)
    {
        var buttonNormal = Box(Color.FromHex("#252A2D"), AshfallStylesheet.Border, 8, 4);
        var buttonHover = Box(Color.FromHex("#332A22"), AshfallStylesheet.Orange, 8, 4);
        var buttonPressed = Box(Color.FromHex("#241D17"), AshfallStylesheet.Orange, 8, 4);
        var buttonDisabled = Box(AshfallStylesheet.PanelDeep, Color.FromHex("#2E3338"), 8, 4);

        var panel = Box(AshfallStylesheet.Panel, AshfallStylesheet.Border, 10, 8);
        var panelDeep = Box(AshfallStylesheet.PanelDeep, AshfallStylesheet.Border, 10, 8);
        var lineEdit = Box(AshfallStylesheet.PanelDeep, AshfallStylesheet.Border, 6, 3);
        var windowHeader = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#1D2023"),
            BorderColor = AshfallStylesheet.Border,
            BorderThickness = new Thickness(0, 0, 0, 1),
        };
        windowHeader.SetContentMarginOverride(StyleBox.Margin.Horizontal, 6);
        windowHeader.SetContentMarginOverride(StyleBox.Margin.Vertical, 4);

        var windowBackground = Box(AshfallStylesheet.Background, AshfallStylesheet.Border, 8, 8);
        var tooltipBox = Box(Color.FromHex("#131619"), AshfallStylesheet.Border, 8, 6);
        var contextMenuBox = Box(Color.FromHex("#14171A"), AshfallStylesheet.Border, 2, 2);
        var tabs = Box(AshfallStylesheet.PanelDeep, AshfallStylesheet.Border, 2, 2);
        var monoBold = ResCache.GetFont("/Fonts/RobotoMono/RobotoMono-Bold.ttf", 13);
        var monoSmall = ResCache.GetFont("/Fonts/RobotoMono/RobotoMono-Regular.ttf", 11);

        var rules = new List<StyleRule>
        {
            E<ContainerButton>().Class(ContainerButton.StyleClassButton)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonNormal),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).PseudoNormal()
                .Prop(ContainerButton.StylePropertyStyleBox, buttonNormal),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).PseudoHovered()
                .Prop(ContainerButton.StylePropertyStyleBox, buttonHover),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).PseudoPressed()
                .Prop(ContainerButton.StylePropertyStyleBox, buttonPressed),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).PseudoDisabled()
                .Prop(ContainerButton.StylePropertyStyleBox, buttonDisabled),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(StyleClass.ButtonOpenLeft)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonNormal),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(StyleClass.ButtonOpenRight)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonNormal),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(StyleClass.ButtonOpenBoth)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonNormal),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(StyleClass.ButtonSquare)
                .Prop(ContainerButton.StylePropertyStyleBox, buttonNormal),

            E<LineEdit>().Prop(LineEdit.StylePropertyStyleBox, lineEdit),

            E<PanelContainer>().Class(StyleClass.BackgroundPanel).Panel(panel),
            E<PanelContainer>().Class(StyleClass.BackgroundPanelDark).Panel(panelDeep),
            E<PanelContainer>().Class(AshfallStylesheet.PanelClass).Panel(panel),
            E<PanelContainer>().Class(AshfallStylesheet.PanelDeepClass).Panel(panelDeep),

            // Windows
            E().Class(DefaultWindow.StyleClassWindowPanel).Panel(windowBackground),
            E().Class(DefaultWindow.StyleClassWindowHeader).Panel(windowHeader),
            E<Label>().Class(DefaultWindow.StyleClassWindowTitle)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(13, FontKind.Bold))
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.Orange),
            E<Label>().Class("FancyWindowTitle")
                .Prop(Label.StylePropertyFont, monoBold)
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.Orange),
            E().Class(StyleClass.BorderedWindowPanel).Panel(windowBackground),

            // Tooltips & Examine Popup
            E<PanelContainer>().Class(StyleClass.TooltipPanel).Panel(tooltipBox),
            E<PanelContainer>().Class(ExamineSystem.StyleClassEntityTooltip).Panel(tooltipBox),
            E<Tooltip>().Prop(Tooltip.StylePropertyPanel, tooltipBox),
            E<RichTextLabel>().Class(StyleClass.TooltipTitle)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(13, FontKind.Bold))
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.Text),
            E<RichTextLabel>().Class(StyleClass.TooltipDesc)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(12))
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.MutedText),

            // Context Menu
            E<PanelContainer>().Class(ContextMenuPopup.StyleClassContextMenuPopup).Panel(contextMenuBox),

            E<TabContainer>()
                .Prop(TabContainer.StylePropertyPanelStyleBox, tabs)
                .Prop(TabContainer.StylePropertyTabStyleBox, Box(AshfallStylesheet.Orange, AshfallStylesheet.Orange, 8, 4))
                .Prop(TabContainer.StylePropertyTabStyleBoxInactive, Box(AshfallStylesheet.Panel, AshfallStylesheet.Border, 8, 4)),

            E<Label>().Class(AshfallStylesheet.TerminalHeaderClass)
                .Prop(Label.StylePropertyFont, monoBold)
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.Text),
            E<Label>().Class(AshfallStylesheet.SectionHeaderClass)
                .Prop(Label.StylePropertyFont, sheet.BaseFont.GetFont(14, FontKind.Bold))
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.Orange),
            E<Label>().Class(AshfallStylesheet.StatusClass)
                .Prop(Label.StylePropertyFont, monoSmall)
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.Orange),
            E<Label>().Class(AshfallStylesheet.MutedClass)
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.MutedText),
            E<Label>().Class(AshfallStylesheet.WarningClass)
                .Prop(Label.StylePropertyFont, monoSmall)
                .Prop(Label.StylePropertyFontColor, AshfallStylesheet.Orange),
        };

        AddButtonRules(rules, AshfallStylesheet.PrimaryActionClass,
            Color.FromHex("#5A3618"), Color.FromHex("#73441F"), Color.FromHex("#452A14"),
            AshfallStylesheet.Orange, sheet.BaseFont.GetFont(16, FontKind.Bold), AshfallStylesheet.Text);
        AddButtonRules(rules, AshfallStylesheet.SecondaryActionClass,
            Color.FromHex("#2B3034"), Color.FromHex("#353C40"), Color.FromHex("#24282B"),
            AshfallStylesheet.Border, sheet.BaseFont.GetFont(14, FontKind.Bold), AshfallStylesheet.Text);
        AddButtonRules(rules, AshfallStylesheet.NavigationActionClass,
            Color.FromHex("#282420"), Color.FromHex("#383028"), Color.FromHex("#201C18"),
            AshfallStylesheet.Orange, sheet.BaseFont.GetFont(12), AshfallStylesheet.Text);
        AddButtonRules(rules, AshfallStylesheet.UtilityActionClass,
            Color.FromHex("#252A2D"), Color.FromHex("#343A3E"), Color.FromHex("#202427"),
            AshfallStylesheet.Border, sheet.BaseFont.GetFont(12), AshfallStylesheet.Text);
        AddButtonRules(rules, AshfallStylesheet.DestructiveActionClass,
            Color.FromHex("#482625"), Color.FromHex("#63312F"), Color.FromHex("#351D1C"),
            AshfallStylesheet.Error, sheet.BaseFont.GetFont(12, FontKind.Bold), AshfallStylesheet.Text);

        return rules.ToArray();
    }

    private static void AddButtonRules(
        List<StyleRule> rules,
        string styleClass,
        Color normal,
        Color hovered,
        Color pressed,
        Color border,
        Font font,
        Color text)
    {
        rules.AddRange(new StyleRule[]
        {
            ButtonRule(styleClass).PseudoNormal()
                .Prop(ContainerButton.StylePropertyStyleBox, Box(normal, border, 12, 6)),
            ButtonRule(styleClass).PseudoHovered()
                .Prop(ContainerButton.StylePropertyStyleBox, Box(hovered, border, 12, 6)),
            ButtonRule(styleClass).PseudoPressed()
                .Prop(ContainerButton.StylePropertyStyleBox, Box(pressed, border, 12, 6)),
            ButtonRule(styleClass).PseudoDisabled()
                .Prop(ContainerButton.StylePropertyStyleBox, Box(AshfallStylesheet.PanelDeep, AshfallStylesheet.Border, 12, 6)),
            E<ContainerButton>().Class(ContainerButton.StyleClassButton).Class(styleClass)
                .ParentOf(E<Label>())
                .Prop(Label.StylePropertyFont, font)
                .Prop(Label.StylePropertyFontColor, text),
        });
    }

    private static MutableSelectorElement ButtonRule(string styleClass)
    {
        return E<ContainerButton>()
            .Class(ContainerButton.StyleClassButton)
            .Class(styleClass);
    }

    private static StyleBoxFlat Box(Color background, Color border, float horizontal, float vertical)
    {
        var box = new StyleBoxFlat
        {
            BackgroundColor = background,
            BorderColor = border,
            BorderThickness = new Thickness(1),
        };
        box.SetContentMarginOverride(StyleBox.Margin.Horizontal, horizontal);
        box.SetContentMarginOverride(StyleBox.Margin.Vertical, vertical);
        return box;
    }
}
