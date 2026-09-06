using Content.Client.UserInterface.Screens;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ChatGameScreenSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var outputBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#101315"),
            BorderColor = Color.FromHex("#2A3138"),
            BorderThickness = new Thickness(1),
        };
        outputBox.SetContentMarginOverride(StyleBox.Margin.All, 4);

        return
        [
            E()
                .Class(SeparatedChatGameScreen.StyleClassChatContainer)
                .Panel(new StyleBoxFlat(Color.FromHex("#151719"))),
            E<OutputPanel>()
                .Class(SeparatedChatGameScreen.StyleClassChatOutput)
                .Panel(outputBox),
        ];
    }
}
