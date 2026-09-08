// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Popups;
using Content.Client.UserInterface.Systems.Character.Windows;
using Content.Shared.Popups;
using Content.Client.Trauma.Knowledge.UI;
using Content.Trauma.Common.CCVar;
using Content.Trauma.Common.Knowledge;
using Content.Trauma.Common.Knowledge.Components;
using Content.Trauma.Common.Knowledge.Prototypes;
using Content.Trauma.Shared.Knowledge.Systems;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Trauma.Knowledge;

public sealed partial class KnowledgeSystem : SharedKnowledgeSystem
{
    [Dependency] private PopupSystem _popup = default!;

    private WeakReference<CharacterWindow>? _activeWindow;
    private bool _showPopups;
    private TimeSpan _nextPopup;
    private TimeSpan _popupCooldown = TimeSpan.FromSeconds(3);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KnowledgeHolderComponent, UpdateExperienceEvent>(OnUpdateExperienceEvent);
        Subs.CVar(_cfg, TraumaCVars.SkillPopups, x => _showPopups = x, true);
        SubscribeAllEvent<SkillPopupEvent>(OnSkillPopup);

        CharacterWindow.OnOpened += EnsureKnowledgeTab;
        LobbyUIController.OnProfileEditorCreated += AddProfileEditorTab;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CharacterWindow.OnOpened -= EnsureKnowledgeTab;
        LobbyUIController.OnProfileEditorCreated -= AddProfileEditorTab;
    }

    private void EnsureKnowledgeTab(CharacterWindow window)
    {
        _activeWindow = new WeakReference<CharacterWindow>(window);

        KnowledgeTab? knowledgeTab = null;
        foreach (var child in window.Tabs.Children)
        {
            if (child is KnowledgeTab tab)
            {
                knowledgeTab = tab;
                break;
            }
        }

        TabContainer.SetTabTitle(window.CharacterTab, Loc.GetString("trauma-character-title"));

        if (knowledgeTab == null)
        {
            knowledgeTab = new KnowledgeTab();
            window.Tabs.AddChild(knowledgeTab);
        }

        if (_player.LocalEntity is {} player)
            knowledgeTab.UpdateKnowledgeTab(player);
    }

    private void AddProfileEditorTab(HumanoidProfileEditor editor)
    {
        var above = editor.MarkingsTab;
        var index = above.GetPositionInParent();

        var tab = new KnowledgeProfileEditor(ProtoMan, this);
        tab.OnSave += knowledge =>
        {
            editor.Profile = editor.Profile?.WithKnowledge(knowledge);
            editor.IsDirty = true;
        };

        editor.OnSetProfile += profile =>
        {
            if (profile is not null)
                tab.SetProfile(profile.Species, profile.Knowledge);
        };
        editor.TabContainer.AddChild(tab);
        tab.SetPositionInParent(index);
        TabContainer.SetTabTitle(tab, Loc.GetString("knowledge-editor-tab"));
    }

    public List<(ProtoId<KnowledgeCategoryPrototype> Category, KnowledgeInfo Info)>? GrabAllKnowledge(EntityUid target)
    {
        var knowledgeList = TryGetAllKnowledgeUnits(target);

        if (knowledgeList is not { } || knowledgeList.Count == 0)
            return null;

        return knowledgeList
            .Select(ent => GetKnowledgeInfo(ent))
            .OrderBy(data => data.Category)
            .ThenBy(data => data.Info.Name)
            .ToList();
    }

    public void OnUpdateExperienceEvent(Entity<KnowledgeHolderComponent> ent, ref UpdateExperienceEvent args)
    {
        var localPlayer = _player.LocalEntity;
        if (localPlayer != ent.Owner)
            return;

        if (_activeWindow is not { } || !_activeWindow.TryGetTarget(out var window))
            return;

        EnsureKnowledgeTab(window);
    }

    private void OnSkillPopup(SkillPopupEvent args)
    {
        if (!_showPopups)
            return;

        var now = _timing.CurTime;
        if (now < _nextPopup)
            return;

        _nextPopup = now + _popupCooldown;
        if (_player.LocalEntity is { } player)
            _popup.PopupEntity(args.Popup, player, player, PopupType.Small);
    }
}
