using Content.Server.Chat.Managers;
using Content.Shared.Ashfall;
using Content.Shared.Ashfall.Examine;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Ashfall.Examine;

public sealed partial class AshfallExamineChatSystem : EntitySystem
{
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private INetConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MetaDataComponent, ExamineCompletedEvent>(OnExamineCompleted);
    }

    private void OnExamineCompleted(EntityUid uid, MetaDataComponent component, ref ExamineCompletedEvent args)
    {
        if (!TryComp<ActorComponent>(args.Examiner, out var actor))
            return;

        var channel = actor.PlayerSession.Channel;
        if (!_cfg.GetClientCVar(channel, AshfallCCVars.ChatLogInChat))
            return;

        var markup = args.Message.ToMarkup();
        if (string.IsNullOrWhiteSpace(markup))
            return;

        var rawMessage = FormattedMessage.RemoveMarkupPermissive(markup);

        _chatManager.ChatMessageToOne(
            ChatChannel.Emotes,
            rawMessage,
            markup,
            uid,
            hideChat: false,
            client: channel);
    }
}
