namespace Content.Shared.Chat;

public static class ChatChannelExtensions
{
    public static Color TextColor(this ChatChannel channel)
    {
        return channel switch
        {
            ChatChannel.Server => Color.FromHex("#D6A43A"),
            ChatChannel.Radio => Color.LimeGreen,
            ChatChannel.LOOC => Color.FromHex("#5F9587"),
            ChatChannel.OOC => Color.FromHex("#668DA1"),
            ChatChannel.Dead => Color.FromHex("#9C7AC7"),
            ChatChannel.Admin => Color.FromHex("#D84A3D"),
            ChatChannel.AdminAlert => Color.FromHex("#D84A3D"),
            ChatChannel.AdminChat => Color.FromHex("#D84A3D"),
            ChatChannel.Whisper => Color.DarkGray,
            _ => Color.LightGray
        };
    }
}
