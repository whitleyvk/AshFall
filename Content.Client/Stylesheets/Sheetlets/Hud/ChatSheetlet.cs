using Content.Client.Stylesheets.SheetletConfigs;
using Content.Client.Stylesheets.Stylesheets;
using Content.Client.UserInterface.Systems.Chat.Controls;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ChatSheetlet<T> : Sheetlet<T> where T: PalettedStylesheet, IButtonConfig
{
    public override StyleRule[] GetRules(T sheet, object config)
    {
        IButtonConfig btnCfg = sheet;

        var chatInputBg = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#1A1F24"),
            BorderColor = Color.FromHex("#353E47"),
            BorderThickness = new Thickness(1),
        };
        chatInputBg.SetContentMarginOverride(StyleBox.Margin.All, 4);

        var channelBox = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#252C33"),
            BorderColor = Color.FromHex("#3D4853"),
            BorderThickness = new Thickness(1),
        };
        channelBox.SetContentMarginOverride(StyleBox.Margin.Horizontal, 6);
        channelBox.SetContentMarginOverride(StyleBox.Margin.Vertical, 2);

        return
        [
            E<PanelContainer>()
                .Class(ChatInputBox.StyleClassChatPanel)
                .Panel(chatInputBg),
            E<LineEdit>()
                .Class(ChatInputBox.StyleClassChatLineEdit)
                .Prop(LineEdit.StylePropertyStyleBox, new StyleBoxEmpty()),
            E<Button>().Class(ChatInputBox.StyleClassChatFilterOptionButton).Box(channelBox),
            E<ContainerButton>().Class(ChatInputBox.StyleClassChatFilterOptionButton).Box(channelBox),
        ];
    }
}
