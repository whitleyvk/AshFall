using System.Numerics;
using System.Linq;
using Ashfall.Client.Stylesheets;
using Content.Client.Message;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.Trauma.Knowledge;
using Content.Client.UserInterface.Controls;
using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Content.Trauma.Common.Knowledge;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Ashfall.CharacterGen.UI;

public sealed partial class AshfallPersonalFilesScreen : PanelContainer
{
    private AshfallSkillsDetailWindow? _skillsWindow;
    private AshfallCultureDetailWindow? _cultureWindow;

    /// <summary>
    ///     Accent color shared with the generator's education tags.
    /// </summary>
    private const string AccentColor = AshfallDossierSectionControl.AccentColor;

    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private JobRequirementsManager _requirements = default!;
    [Dependency] private IResourceCache _resCache = default!;
    private readonly SharedAudioSystem _audio;

    private GridContainer CandidatesGrid => this.FindControl<GridContainer>("CandidatesGrid");
    private GridContainer PrioritySlotsGrid => this.FindControl<GridContainer>("PrioritySlotsGrid");
    private Label PriorityHintLabel => this.FindControl<Label>("PriorityHintLabel");
    private BoxContainer DossierSkills => this.FindControl<BoxContainer>("DossierSkills");
    private BoxContainer DossierSections => this.FindControl<BoxContainer>("DossierSections");
    private GridContainer JobsGrid => this.FindControl<GridContainer>("JobsGrid");
    private Label AssignmentHeading => this.FindControl<Label>("AssignmentHeading");
    private PanelContainer AssignmentPanel => this.FindControl<PanelContainer>("AssignmentPanel");
    private PanelContainer DossierPanel => this.FindControl<PanelContainer>("DossierPanel");
    private ProfilePreviewSpriteView DossierPreview => this.FindControl<ProfilePreviewSpriteView>("DossierPreview");
    private Label DossierNameLabel => this.FindControl<Label>("DossierNameLabel");
    private Label DossierBioLabel => this.FindControl<Label>("DossierBioLabel");
    private RichTextLabel DossierCultureLabel => this.FindControl<RichTextLabel>("DossierCultureLabel");
    private Button CultureWikiButton => this.FindControl<Button>("CultureWikiButton");
    private Label DossierBirthplaceLabel => this.FindControl<Label>("DossierBirthplaceLabel");
    private Label DossierNumberLabel => this.FindControl<Label>("DossierNumberLabel");
    private Label DossierSelectionLabel => this.FindControl<Label>("DossierSelectionLabel");
    private ScrollContainer DossierScroll => this.FindControl<ScrollContainer>("DossierScroll");
    private Button RefreshButton => this.FindControl<Button>("RefreshButton");
    private Label CooldownLabel => this.FindControl<Label>("CooldownLabel");
    private Label HeaderStatusLabel => this.FindControl<Label>("HeaderStatusLabel");
    private Button BackButton => this.FindControl<Button>("BackButton");
    private TextureRect PortraitNoise => this.FindControl<TextureRect>("PortraitNoise");
    private TextureRect AmbientAnim => this.FindControl<TextureRect>("AmbientAnim");

    private readonly AshfallCharacterGenSystem _genSystem;
    private readonly List<AshfallCandidateCard> _cardControls = new();

    // Candidate index per card in row-major order (-1 = filler); never assume card i shows candidate i.
    private readonly List<int> _cardBindings = new();
    private readonly AshfallPrioritySlotCard?[] _slotCards =
        new AshfallPrioritySlotCard?[AshfallCharacterPoolConstants.PrioritySlotCount];
    private TimeSpan _cooldownEnd;
    private int _inspectedIndex = -1;

    // Slot whose pinned candidate is being inspected straight from the pin DTO; used when
    // the person survived a reroll and is no longer part of the current pool.
    private int? _inspectedPinSlot;
    private ProtoId<JobPrototype>? _draftJob;
    private bool _refreshPending;

    // Slot clicked while its role is still undecided; the pin fires when a role is picked.
    private int? _pendingSlot;

    // Slide + fade animation state.
    private const float AnimationDuration = 0.45f;
    private float _assignmentAnimationProgress = 0f;
    private bool _assignmentTargetOpen = false;

    private float _dossierAnimationProgress = 0f;
    private bool _dossierTargetOpen = false;

    // Cleared dossier content only after the close fade has finished, so the panel does not
    // visibly empty itself in one frame while it is still fading out.
    private bool _pendingDossierClear = false;

    // Retro CRT static cycling over the dossier portrait.
    private Texture[]? _noiseFrames;
    private bool _noiseBroken;
    private float _noiseClock;
    private int _noiseFrame;

    // Three-frame ambient sketch in the empty right column.
    private Texture[]? _ambientFrames;
    private bool _ambientBroken;
    private float _ambientClock;
    private int _ambientFrame;

    public event Action? BackToLobby;

    public AshfallPersonalFilesScreen()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);

        _genSystem = _entMan.System<AshfallCharacterGenSystem>();
        _audio = _entMan.System<SharedAudioSystem>();
        BackButton.OnPressed += _ =>
        {
            _audio.PlayGlobal("/Audio/Ashfall/UI/stampbar-close.ogg", Filter.Local(), false);
            BackToLobby?.Invoke();
        };
        RefreshButton.OnPressed += OnRefreshPressed;
        CultureWikiButton.OnPressed += OnCultureWikiPressed;

        LayoutContainer.SetAnchorPreset(AssignmentPanel, LayoutContainer.LayoutPreset.Wide);
        LayoutContainer.SetAnchorPreset(DossierPanel, LayoutContainer.LayoutPreset.Wide);

        // Assignment & Dossier start hidden with zero opacity.
        AssignmentPanel.Visible = false;
        AssignmentPanel.Modulate = new Color(1f, 1f, 1f, 0f);

        DossierPanel.Visible = false;
        DossierPanel.Modulate = new Color(1f, 1f, 1f, 0f);

        // Anchor helpers only take effect under a LayoutContainer parent: both the portrait
        // view and the noise overlay fill the same frame rect so the static covers it tightly.
        LayoutContainer.SetAnchorPreset(DossierPreview, LayoutContainer.LayoutPreset.Wide);
        LayoutContainer.SetAnchorPreset(PortraitNoise, LayoutContainer.LayoutPreset.Wide);
        PortraitNoise.Visible = false;

        // The ambient sketch sits at half its native size, a little above the middle of the
        // right column, heavily faded; the dossier panel simply covers it once open.
        LayoutContainer.SetAnchorLeft(AmbientAnim, 0.5f);
        LayoutContainer.SetAnchorRight(AmbientAnim, 0.5f);
        LayoutContainer.SetAnchorTop(AmbientAnim, 0.5f);
        LayoutContainer.SetAnchorBottom(AmbientAnim, 0.5f);
        LayoutContainer.SetMarginLeft(AmbientAnim, -89f);
        LayoutContainer.SetMarginRight(AmbientAnim, 89f);
        LayoutContainer.SetMarginTop(AmbientAnim, -115f);
        LayoutContainer.SetMarginBottom(AmbientAnim, 16f);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        // Subscribe here, not in the ctor: closing the archive only removes the screen from
        // the tree, the object survives, and ctor-time subscriptions would let closed ghost
        // screens react to pool updates (stacked pin sounds, wasted rebuilds).
        _genSystem.PoolUpdated += Populate;
        _requirements.Updated += PopulateDossier;
        _audio.PlayGlobal("/Audio/Ashfall/UI/booth-intro.ogg", Filter.Local(), false);
        _genSystem.RequestPool();
        Populate();
    }

    private void OnRefreshPressed(BaseButton.ButtonEventArgs args)
    {
        if (_timing.RealTime < _cooldownEnd)
            return;

        _refreshPending = true;
        _draftJob = null;
        _inspectedIndex = -1;
        _inspectedPinSlot = null;
        _pendingSlot = null;
        _pendingDossierClear = false;
        _assignmentTargetOpen = false;
        _dossierTargetOpen = false;
        _assignmentAnimationProgress = 0f;
        _dossierAnimationProgress = 0f;
        _genSystem.RequestPool(refresh: true);
    }

    // The inspected person either comes from the current pool (card click) or, after a
    // reroll, straight from the pinned slot DTO that survived it.
    private AshfallCharacterCandidate? ResolveInspectedCandidate()
    {
        if (_inspectedPinSlot is { } slot && _genSystem.GetPinnedSlot(slot) is { } pin)
            return pin.Candidate;

        if (_inspectedIndex >= 0 && _inspectedIndex < _genSystem.Candidates.Count)
            return _genSystem.Candidates[_inspectedIndex];

        return null;
    }

    private void OnCultureWikiPressed(BaseButton.ButtonEventArgs args)
    {
        if (ResolveInspectedCandidate() is not { } candidate)
            return;

        _cultureWindow ??= new AshfallCultureDetailWindow(_prototypes);
        _cultureWindow.Populate(candidate.Profile, candidate.Dossier);
        if (!_cultureWindow.IsOpen)
            _cultureWindow.OpenCentered();
    }

    private void Populate()
    {
        if (_genSystem.Candidates.Count == 0)
            return;

        if (_refreshPending)
        {
            _refreshPending = false;
            _inspectedIndex = -1;
            _inspectedPinSlot = null;
            _draftJob = null;
            _pendingSlot = null;
            _assignmentTargetOpen = false;
            _dossierTargetOpen = false;
            _assignmentAnimationProgress = 0f;
            _dossierAnimationProgress = 0f;
            CandidatesGrid.RemoveAllChildren();
            _cardControls.Clear();
        }

        PopulateCards();
        PopulatePrioritySlots();
        PopulateDossier();

        if (_genSystem.CooldownSecondsRemaining > 0)
            _cooldownEnd = _timing.RealTime + TimeSpan.FromSeconds(_genSystem.CooldownSecondsRemaining);
        UpdateCooldownUi();
    }

    private void PopulateCards()
    {
        // Columns split by species, not by pool index: the left column is humans only (the
        // server always generates at least four), the right one every other species. A fallback
        // human generated for a missing species prototype joins the left column too.
        var leftIndexes = new List<int>();
        var rightIndexes = new List<int>();
        for (var i = 0; i < _genSystem.Candidates.Count; i++)
        {
            if (_genSystem.Candidates[i].Profile.Species == "Human")
                leftIndexes.Add(i);
            else
                rightIndexes.Add(i);
        }

        var rows = Math.Max(leftIndexes.Count, rightIndexes.Count);
        var bindings = new List<int>();
        for (var row = 0; row < rows; row++)
        {
            bindings.Add(row < leftIndexes.Count ? leftIndexes[row] : -1);
            bindings.Add(row < rightIndexes.Count ? rightIndexes[row] : -1);
        }

        if (CandidatesGrid.ChildCount == 0 ||
            _cardControls.Count != bindings.Count ||
            !_cardBindings.SequenceEqual(bindings))
        {
            CandidatesGrid.RemoveAllChildren();
            _cardControls.Clear();
            _cardBindings.Clear();
            _cardBindings.AddRange(bindings);

            foreach (var candidateIndex in bindings)
            {
                if (candidateIndex == -1)
                {
                    CandidatesGrid.AddChild(new Control());
                    continue;
                }

                var card = new AshfallCandidateCard();
                _cardControls.Add(card);
                UpdateCard(candidateIndex, card);
                card.Inspected += inspectedIndex =>
                {
                    if (_inspectedIndex == inspectedIndex)
                    {
                        _inspectedIndex = -1;
                        _inspectedPinSlot = null;
                        _draftJob = null;
                        _pendingSlot = null;
                        _assignmentTargetOpen = false;
                        _dossierTargetOpen = false;
                        _pendingDossierClear = true;
                        UpdateAllCards();
                        PopulatePrioritySlots();
                        return;
                    }

                    _inspectedIndex = inspectedIndex;
                    _inspectedPinSlot = null;
                    _pendingSlot = null;
                    _pendingDossierClear = false;
                    var candidate = _genSystem.Candidates[inspectedIndex];
                    var pin = _genSystem.GetPinByCandidate(candidate.CandidateId);
                    _draftJob = pin?.Job ??
                        (inspectedIndex == _genSystem.SelectedIndex ? _genSystem.SelectedJob : null);
                    _assignmentTargetOpen = true;
                    _dossierTargetOpen = true;
                    UpdateAllCards();
                    PopulatePrioritySlots();
                    PopulateDossier();
                };
                CandidatesGrid.AddChild(card);
            }
        }
        else
        {
            UpdateAllCards();
        }
    }

    private void UpdateAllCards()
    {
        for (var i = 0; i < _cardControls.Count && i < _cardBindings.Count; i++)
        {
            if (_cardBindings[i] != -1)
                UpdateCard(_cardBindings[i], _cardControls[i]);
        }
    }

    private void UpdateInspectedCard()
    {
        var cardIndex = _cardBindings.IndexOf(_inspectedIndex);
        if (cardIndex >= 0 && cardIndex < _cardControls.Count)
            UpdateCard(_inspectedIndex, _cardControls[cardIndex]);
    }

    private void UpdateCard(int index, AshfallCandidateCard card)
    {
        if (index < 0 || index >= _genSystem.Candidates.Count)
            return;

        var candidate = _genSystem.Candidates[index];
        var isInspected = index == _inspectedIndex;
        var isConfirmed = index == _genSystem.SelectedIndex;
        var pin = _genSystem.GetPinByCandidate(candidate.CandidateId);
        var isPinned = pin != null;

        ProtoId<JobPrototype>? activeJobId = null;
        if (isInspected)
            activeJobId = _draftJob;
        else if (pin != null)
            activeJobId = pin.Job;
        else if (isConfirmed)
            activeJobId = _genSystem.SelectedJob;

        JobPrototype? activeJob = null;
        if (activeJobId is { } jobId && _prototypes.TryIndex(jobId, out JobPrototype? jp))
            activeJob = jp;

        card.SetCandidate(index, candidate, isInspected, isConfirmed, isPinned, activeJob);
    }

    private void PopulatePrioritySlots()
    {
        if (PrioritySlotsGrid.ChildCount != AshfallCharacterPoolConstants.PrioritySlotCount)
        {
            PrioritySlotsGrid.RemoveAllChildren();
            for (var i = 0; i < _slotCards.Length; i++)
            {
                _slotCards[i] = null;
            }

            for (var i = 0; i < AshfallCharacterPoolConstants.PrioritySlotCount; i++)
            {
                var card = new AshfallPrioritySlotCard(i);
                card.SlotPressed += OnSlotPressed;
                card.ClearPressed += OnSlotClearPressed;
                card.PinConfirmed += OnSlotPinConfirmed;
                card.SlotMovePressed += OnSlotMovePressed;
                _slotCards[i] = card;
                PrioritySlotsGrid.AddChild(card);
            }
        }

        for (var i = 0; i < AshfallCharacterPoolConstants.PrioritySlotCount; i++)
        {
            if (_slotCards[i] is { } card)
            {
                card.SetPin(_genSystem.GetPinnedSlot(i));
                card.SetPending(_pendingSlot == i);
                card.SetInspected(_inspectedPinSlot == i);
                if (_noiseFrames != null)
                    card.NoiseTexture = _noiseFrames[_noiseFrame];
            }
        }

        UpdatePriorityHint();
    }

    private void OnSlotPressed(int slotIndex)
    {
        var pinned = _genSystem.GetPinnedSlot(slotIndex);

        // Clicking an occupied slot: navigate to this candidate!
        if (pinned != null)
        {
            var targetIndex = _genSystem.Candidates.FindIndex(c => c.CandidateId == pinned.CandidateId);
            if (targetIndex >= 0)
            {
                _inspectedIndex = targetIndex;
                _inspectedPinSlot = null;
                _draftJob = pinned.Job;
                _pendingSlot = null;
                _pendingDossierClear = false;
                _assignmentTargetOpen = true;
                _dossierTargetOpen = true;
                UpdateAllCards();
                PopulatePrioritySlots();
                PopulateDossier();
                HidePriorityHint();
                return;
            }

            // Pinned survivor of an earlier pool: inspect straight from the slot DTO.
            if (_inspectedPinSlot == slotIndex)
            {
                _inspectedPinSlot = null;
                _draftJob = null;
                _assignmentTargetOpen = false;
                _dossierTargetOpen = false;
                _pendingDossierClear = true;
                PopulatePrioritySlots();
                return;
            }

            _inspectedIndex = -1;
            _inspectedPinSlot = slotIndex;
            _draftJob = pinned.Job;
            _pendingSlot = null;
            _pendingDossierClear = false;
            _assignmentTargetOpen = true;
            _dossierTargetOpen = true;
            UpdateAllCards();
            PopulatePrioritySlots();
            PopulateDossier();
            HidePriorityHint();
            return;
        }

        // Slot is empty:
        if (_inspectedIndex < 0 || _inspectedIndex >= _genSystem.Candidates.Count)
        {
            ShowPriorityHint(Loc.GetString("ashfall-personal-files-slot-no-candidate"));
            return;
        }

        var candidateId = _genSystem.Candidates[_inspectedIndex].CandidateId;

        if (_draftJob is not { } job)
        {
            ShowPriorityHint(Loc.GetString("ashfall-personal-files-slot-select-role"));
            return;
        }

        SetPendingSlot(null);
        HidePriorityHint();
        _genSystem.PinCandidate(slotIndex, candidateId, job);
    }

    private void OnSlotClearPressed(int slotIndex)
    {
        if (_pendingSlot == slotIndex)
            SetPendingSlot(null);

        HidePriorityHint();
        _genSystem.ClearPrioritySlot(slotIndex);
    }

    private void OnSlotPinConfirmed(int slotIndex)
    {
        // The slot card plays its own press flash; this is the quiet confirmation thud.
        _audio.PlayGlobal("/Audio/Ashfall/UI/button-drop.ogg", Filter.Local(), false,
            AudioParams.Default.WithVolume(-8f));
    }

    private void OnSlotMovePressed(int slotIndex, int direction)
    {
        var target = slotIndex + direction;
        if (target < 0 || target >= AshfallCharacterPoolConstants.PrioritySlotCount)
            return;

        // One atomic server-side reorder: move to an empty slot, swap with an occupied one.
        // Re-pinning from the client would momentarily overwrite the target's occupant.
        if (_genSystem.GetPinnedSlot(slotIndex) == null)
            return;

        _genSystem.MovePrioritySlot(slotIndex, target);
    }

    private void SetPendingSlot(int? slotIndex)
    {
        _pendingSlot = slotIndex;
        for (var i = 0; i < _slotCards.Length; i++)
        {
            _slotCards[i]?.SetPending(i == slotIndex);
        }
    }

    private void ShowPriorityHint(string text)
    {
        PriorityHintLabel.Text = text;
    }

    private void HidePriorityHint()
    {
        PriorityHintLabel.Text = string.Empty;
    }

    private void UpdatePriorityHint()
    {
        // Static hint while the inspected candidate is already pinned elsewhere.
        if (_inspectedIndex >= 0 && _inspectedIndex < _genSystem.Candidates.Count &&
            _genSystem.GetPinByCandidate(_genSystem.Candidates[_inspectedIndex].CandidateId) is { } pin)
        {
            ShowPriorityHint(Loc.GetString("ashfall-personal-files-slot-pinned", ("slot", pin.SlotIndex + 1)));
        }
        else
        {
            HidePriorityHint();
        }
    }

    private void PopulateDossier()
    {
        if (ResolveInspectedCandidate() is not { } candidate)
        {
            // A deferred clear is still fading out; wiping now would make the panel jump.
            if (!_pendingDossierClear)
                ClearDossier();
            return;
        }

        var profile = candidate.Profile;

        JobPrototype? currentJob = null;
        if (_draftJob is { } draftId && _prototypes.TryIndex(draftId, out JobPrototype? draftProto))
            currentJob = draftProto;
        else if (candidate.CompatibleJobs.Count > 0 && _prototypes.TryIndex(candidate.CompatibleJobs[0], out JobPrototype? primaryProto))
            currentJob = primaryProto;

        DossierPreview.LoadPreview(profile, currentJob, showClothes: true);
        DossierNameLabel.Text = profile.Name;
        var sex = profile.Sex switch
        {
            Sex.Male => Loc.GetString("ashfall-personal-files-sex-male"),
            Sex.Female => Loc.GetString("ashfall-personal-files-sex-female"),
            _ => Loc.GetString("ashfall-personal-files-sex-other"),
        };
        var species = _prototypes.TryIndex<SpeciesPrototype>(profile.Species, out var speciesProto)
            ? Loc.GetString(speciesProto.Name)
            : Loc.GetString("ashfall-personal-files-sex-other");
        DossierBioLabel.Text = Loc.GetString("ashfall-personal-files-dossier-bio", ("age", profile.Age), ("sex", sex), ("species", species));
        // RichTextLabel: muted color via markup, the line wraps instead of clipping.
        // Markup collapses runs of regular spaces, so the visual padding is non-breaking.
        var cultureGap = new string('\u00A0', 2);
        var cultureLine = !string.IsNullOrEmpty(candidate.Dossier.Morphology)
            ? $"МОРФОЛОГИЯ // {candidate.Dossier.Morphology}{cultureGap}•{cultureGap}КУЛЬТУРНАЯ ЛИНИЯ // {candidate.Dossier.CulturalOrigin}"
            : (!string.IsNullOrEmpty(candidate.Dossier.CulturalOrigin)
                ? $"КУЛЬТУРНАЯ ЛИНИЯ // {candidate.Dossier.CulturalOrigin}"
                : string.Empty);
        DossierCultureLabel.SetMarkup(string.IsNullOrEmpty(cultureLine)
            ? string.Empty
            : $"[color=#878C87]{cultureLine}[/color]");
        DossierBirthplaceLabel.Text = !string.IsNullOrEmpty(candidate.Dossier.Birthplace)
            ? $"МЕСТО РОЖДЕНИЯ // {candidate.Dossier.Birthplace}"
            : string.Empty;
        DossierNumberLabel.Text = Loc.GetString("ashfall-personal-files-number",
            ("number", _inspectedPinSlot is { } slot ? slot + 1 : _inspectedIndex + 1));
        var inspectedPin = _genSystem.GetPinByCandidate(candidate.CandidateId);
        DossierSelectionLabel.Text = inspectedPin != null
            ? Loc.GetString("ashfall-personal-files-pinned-label",
                ("slot", inspectedPin.SlotIndex + 1),
                ("job", GetJobName(inspectedPin.Job)),
                ("confirmed", inspectedPin.SlotIndex == _genSystem.ConfirmedSlotIndex))
            : string.Empty;

        DossierSections.RemoveAllChildren();

        var activeJobId = _draftJob ?? (_inspectedIndex == _genSystem.SelectedIndex ? _genSystem.SelectedJob : (candidate.CompatibleJobs.Count > 0 ? candidate.CompatibleJobs[0] : (ProtoId<JobPrototype>?) null));
        JobPrototype? activeJob = activeJobId != null && _prototypes.TryIndex(activeJobId, out JobPrototype? jobProto) ? jobProto : null;

        PopulateSkills(profile, activeJob);

        var previousGroup = -1;
        foreach (var (section, extraControl) in BuildDossierSections(candidate))
        {
            var group = SectionGroupRank(section.Kind);
            AddDossierSection(section, extraControl, groupStart: previousGroup != -1 && group != previousGroup);
            previousGroup = group;
        }

        if (_skillsWindow is { IsOpen: true })
            _skillsWindow.Populate(profile, activeJob);

        if (_cultureWindow is { IsOpen: true })
            _cultureWindow.Populate(profile, candidate.Dossier);

        JobsGrid.RemoveAllChildren();
        // A lone assignment reads better across the full row than half of it.
        JobsGrid.Columns = candidate.CompatibleJobs.Count == 1 ? 1 : 2;
        var anyAvailable = false;
        foreach (var jobId in candidate.CompatibleJobs)
        {
            if (!_prototypes.TryIndex(jobId, out JobPrototype? job))
                continue;

            var allowed = _requirements.IsAllowed(job, profile, out var reason);
            anyAvailable |= allowed;
            var selected = jobId == _draftJob;

            var button = new Button
            {
                Disabled = !allowed,
                HorizontalExpand = true,
                MinHeight = 32,
            };
            // Assignment rows behave like list items: quiet surface, selected = orange border.
            button.StyleClasses.Add(AshfallStylesheet.ListItemClass);
            if (selected)
                button.StyleClasses.Add(AshfallStylesheet.ListItemSelectedClass);

            var buttonContent = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            if (_prototypes.TryIndex<JobIconPrototype>(job.Icon, out var jobIcon))
            {
                buttonContent.AddChild(new TextureRect
                {
                    Texture = _entMan.System<SpriteSystem>().Frame0(jobIcon.Icon),
                    TextureScale = new Vector2(2f, 2f),
                    VerticalAlignment = VAlignment.Center,
                    Margin = new Thickness(6, 0, 4, 0),
                });
            }

            buttonContent.AddChild(new Label
            {
                Text = job.LocalizedName,
                VerticalAlignment = VAlignment.Center,
                HorizontalExpand = true,
                ClipText = true,
            });

            button.AddChild(buttonContent);

            if (!allowed && reason != null)
            {
                var tooltip = new Tooltip();
                tooltip.SetMessage(reason);
                button.TooltipSupplier = _ => tooltip;
            }
            if (allowed)
            {
                button.OnPressed += _ =>
                {
                    _draftJob = job.ID;

                    // A slot armed before the role pick: this choice completes the pin.
                    if (_pendingSlot is { } pendingSlot &&
                        _inspectedIndex >= 0 && _inspectedIndex < _genSystem.Candidates.Count)
                    {
                        var pendingCandidateId = _genSystem.Candidates[_inspectedIndex].CandidateId;
                        SetPendingSlot(null);
                        HidePriorityHint();
                        _genSystem.PinCandidate(pendingSlot, pendingCandidateId, job.ID);
                    }

                    if (_inspectedIndex >= 0 && _inspectedIndex < _cardControls.Count)
                        UpdateInspectedCard();

                    PopulateDossier();
                };
            }
            JobsGrid.AddChild(button);
        }

        if (!anyAvailable)
        {
            JobsGrid.AddChild(new Label
            {
                Text = Loc.GetString("ashfall-personal-files-no-jobs"),
                FontColorOverride = Color.FromHex("#B0574C"),
            });
        }

        AssignmentHeading.Text = _draftJob != null && _prototypes.TryIndex(_draftJob, out var draftJobProto)
            ? Loc.GetString("ashfall-personal-files-assignment-selected", ("job", draftJobProto.LocalizedName))
            : Loc.GetString("ashfall-personal-files-assignment-heading");
    }

    // No candidate picked yet: blank dossier and a dimmed assignment placeholder, so the
    // top-down flow starts at the candidate list.
    private void ClearDossier()
    {
        CloseSkillsWindow();
        DossierPreview.ClearPreview();
        DossierNameLabel.Text = string.Empty;
        DossierBioLabel.Text = string.Empty;
        DossierCultureLabel.SetMarkup(string.Empty);
        DossierBirthplaceLabel.Text = string.Empty;
        DossierNumberLabel.Text = string.Empty;
        DossierSelectionLabel.Text = string.Empty;
        DossierSkills.RemoveAllChildren();
        DossierSections.RemoveAllChildren();

        AssignmentHeading.Text = Loc.GetString("ashfall-personal-files-assignment-heading");
        JobsGrid.RemoveAllChildren();
    }

    // Presentation order only; the shared dossier DTO keeps its generation order.
    // origin -> education / qualification -> career -> personality / hook -> precryo / evaluation.
    private static int SectionGroupRank(string kind) => kind switch
    {
        "origin" => 0,
        "education" or "qualification" => 1,
        "career" => 2,
        "personality" or "hook" => 3,
        "precryo" or "evaluation" => 4,
        _ => 5,
    };

    private List<(AshfallDossierSection Section, Control? ExtraControl)> BuildDossierSections(
        AshfallCharacterCandidate candidate)
    {
        var result = new List<(AshfallDossierSection Section, Control? ExtraControl)>();

        // 1. Origin
        foreach (var section in candidate.Dossier.Sections)
        {
            if (section.Kind == "origin")
                result.Add((section, null));
        }

        // 2. Education
        foreach (var section in candidate.Dossier.Sections)
        {
            if (section.Kind == "education")
                result.Add((section, null));
        }

        // 3. Qualification background
        var qualLines = new List<string>();
        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in candidate.Dossier.Sections.Where(s => s.Kind == "qualification" && s.Lines.Count > 0))
        {
            var title = s.Title.Trim();
            if (seenTitles.Add(title))
            {
                qualLines.Add($"[color={AccentColor}]{title}[/color] — {s.Lines[0].Trim()}");
            }
        }

        var qualSection = new AshfallDossierSection
        {
            Kind = "qualification",
            Title = Loc.GetString("ashfall-lore-title-qualification"),
            Lines = qualLines,
        };
        result.Add((qualSection, null));

        // 4. Career
        foreach (var section in candidate.Dossier.Sections)
        {
            if (section.Kind == "career")
                result.Add((section, null));
        }

        // 5. Personality
        foreach (var section in candidate.Dossier.Sections)
        {
            if (section.Kind == "personality")
                result.Add((section, null));
        }

        // 6. Personal Hook
        foreach (var section in candidate.Dossier.Sections)
        {
            if (section.Kind == "hook")
                result.Add((section, null));
        }

        // 7. Cryosleep Circumstances
        foreach (var section in candidate.Dossier.Sections)
        {
            if (section.Kind == "precryo")
                result.Add((section, null));
        }

        // 8. Evaluation (if present)
        foreach (var section in candidate.Dossier.Sections)
        {
            if (section.Kind == "evaluation")
                result.Add((section, null));
        }

        // Server-rendered sections the presentation layer does not know yet are appended in
        // generation order instead of being silently dropped.
        var knownKinds = new HashSet<string>
        {
            "origin", "education", "qualification", "career", "personality", "hook", "precryo", "evaluation",
        };
        foreach (var section in candidate.Dossier.Sections)
        {
            if (!knownKinds.Contains(section.Kind))
                result.Add((section, null));
        }

        return result;
    }

    private void PopulateSkills(HumanoidCharacterProfile profile, JobPrototype? activeJob)
    {
        var knowledgeSystem = _entMan.System<KnowledgeSystem>();
        KnowledgeProfilePrototype? speciesKnowledge = null;
        if (_prototypes.TryIndex<SpeciesPrototype>(profile.Species, out var speciesProto) &&
            _prototypes.TryIndex(speciesProto.Knowledge, out KnowledgeProfilePrototype? spk))
        {
            speciesKnowledge = spk;
        }

        var activeSkills = new List<(string Name, int Mastery, string Roman)>();
        foreach (var skillId in knowledgeSystem.AllKnowledges.Keys)
        {
            var speciesBase = speciesKnowledge?.Profile.Mastery.GetValueOrDefault(skillId) ?? 0;
            var profileDiff = profile.Knowledge.Mastery.GetValueOrDefault(skillId);
            var jobFloor = activeJob?.Knowledge.GetValueOrDefault(skillId) ?? 0;
            var effective = Math.Max(jobFloor, speciesBase + profileDiff);
            if (effective > 0)
            {
                var skillName = AshfallSkillsDetailWindow.GetSkillName(skillId, _prototypes);
                var roman = AshfallSkillsDetailWindow.ToRoman(effective);
                activeSkills.Add((skillName, effective, roman));
            }
        }

        activeSkills = activeSkills.OrderByDescending(s => s.Mastery).ThenBy(s => s.Name).ToList();

        DossierSkills.RemoveAllChildren();
        var heading = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        heading.AddChild(new Label
        {
            Text = Loc.GetString("ashfall-dossier-skills-summary-heading"),
            FontColorOverride = Color.FromHex("#878C87"),
        });

        var detailsButton = new ContainerButton
        {
            HorizontalAlignment = HAlignment.Left,
            StyleBoxOverride = new StyleBoxFlat { BackgroundColor = Color.Transparent },
        };
        var detailsLabel = new Label
        {
            Text = Loc.GetString("ashfall-dossier-skills-details-button"),
            FontColorOverride = Color.FromHex("#A3A8A3"),
            Margin = new Thickness(4, 1),
        };
        detailsButton.AddChild(detailsLabel);
        detailsButton.OnMouseEntered += _ => detailsButton.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#343638"),
        };
        detailsButton.OnMouseExited += _ => detailsButton.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.Transparent,
        };
        detailsButton.OnPressed += _ =>
        {
            _skillsWindow ??= new AshfallSkillsDetailWindow(_prototypes, knowledgeSystem);
            _skillsWindow.Populate(profile, activeJob);
            if (!_skillsWindow.IsOpen)
                _skillsWindow.OpenCentered();
        };
        heading.AddChild(detailsButton);
        DossierSkills.AddChild(heading);

        var entries = activeSkills.Take(3).Select(skill => $"{skill.Name} {skill.Roman}").ToList();
        if (activeSkills.Count > 3)
            entries.Add(Loc.GetString("ashfall-dossier-skills-more", ("count", activeSkills.Count - 3)));

        var summary = new RichTextLabel { HorizontalExpand = true };
        summary.SetMessage(entries.Count > 0
            ? string.Join(" • ", entries)
            : Loc.GetString("ashfall-dossier-skills-none"), Color.FromHex("#A3A8A3"));
        DossierSkills.AddChild(summary);
    }

    private void AddDossierSection(AshfallDossierSection section, Control? extraControl = null, bool groupStart = false)
    {
        if (section.Lines.Count == 0 && extraControl == null)
            return;

        DossierSections.AddChild(new AshfallDossierSectionControl(section, groupStart, extraControl));
    }

    private string GetJobName(ProtoId<JobPrototype>? id)
    {
        return id is { } jobId && _prototypes.TryIndex(jobId, out JobPrototype? job)
            ? job.LocalizedName
            : Loc.GetString("ashfall-personal-files-record-unavailable");
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        UpdateCooldownUi();
        UpdatePortraitNoise(args);
        UpdateAmbientAnim(args);
        UpdateAssignmentAnimation(args);
        UpdateDossierAnimation(args);

        // The close animation has fully hidden both panels; only now is it safe to wipe
        // the dossier content without the emptying being visible.
        if (_pendingDossierClear &&
            _assignmentAnimationProgress <= 0.001f &&
            _dossierAnimationProgress <= 0.001f)
        {
            _pendingDossierClear = false;
            ClearDossier();
        }
    }

    private void UpdateAssignmentAnimation(FrameEventArgs args)
    {
        if (_assignmentTargetOpen)
        {
            if (_assignmentAnimationProgress < 1f)
            {
                _assignmentAnimationProgress = Math.Min(1f, _assignmentAnimationProgress + (float) (args.DeltaSeconds / AnimationDuration));
            }
        }
        else
        {
            if (_assignmentAnimationProgress > 0f)
            {
                _assignmentAnimationProgress = Math.Max(0f, _assignmentAnimationProgress - (float) (args.DeltaSeconds / AnimationDuration));
            }
        }

        if (_assignmentAnimationProgress <= 0.001f)
        {
            AssignmentPanel.Visible = false;
            AssignmentPanel.Modulate = Color.White;
            return;
        }

        AssignmentPanel.Visible = true;
        // Ease-in-out so the slide starts and ends gently instead of snapping.
        var t = _assignmentAnimationProgress;
        var ease = t < 0.5f
            ? 4f * t * t * t
            : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;

        // Modulate (not ModulateSelfOverride): the fade must cover the panel AND its
        // content, otherwise text and icons stay opaque until the final-frame snap.
        AssignmentPanel.Modulate = new Color(1f, 1f, 1f, ease);
        var travel = MathF.Max(AssignmentPanel.Height, 150f);
        var yOffset = (1f - ease) * -travel;
        LayoutContainer.SetPosition(AssignmentPanel, new Vector2(0f, yOffset));
    }

    private void UpdateDossierAnimation(FrameEventArgs args)
    {
        if (_dossierTargetOpen)
        {
            if (_dossierAnimationProgress < 1f)
            {
                _dossierAnimationProgress = Math.Min(1f, _dossierAnimationProgress + (float) (args.DeltaSeconds / AnimationDuration));
            }
        }
        else
        {
            if (_dossierAnimationProgress > 0f)
            {
                _dossierAnimationProgress = Math.Max(0f, _dossierAnimationProgress - (float) (args.DeltaSeconds / AnimationDuration));
            }
        }

        if (_dossierAnimationProgress <= 0.001f)
        {
            DossierPanel.Visible = false;
            DossierPanel.Modulate = Color.White;
            return;
        }

        DossierPanel.Visible = true;
        var t = _dossierAnimationProgress;
        var ease = t < 0.5f
            ? 4f * t * t * t
            : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;

        // Modulate (not ModulateSelfOverride): text, portrait and sections must fade out
        // together with the backdrop instead of snapping off on the last frame.
        DossierPanel.Modulate = new Color(1f, 1f, 1f, ease);
        // Slide accompanies the opening only; closing fades in place so the retained
        // dossier content never shifts or snaps mid-fade.
        if (_dossierTargetOpen)
            LayoutContainer.SetPosition(DossierPanel, new Vector2((1f - ease) * 60f, 0f));
    }

    private void UpdatePortraitNoise(FrameEventArgs args)
    {
        if (_noiseBroken)
            return;

        if (_noiseFrames == null)
        {
            // Purely decorative: if the frames are unavailable (e.g. a stale content pack),
            // degrade to no overlay instead of throwing from FrameUpdate every frame.
            var frames = new List<Texture>();
            for (var i = 0; i < 3; i++)
            {
                if (_resCache.TryGetResource<TextureResource>($"/Textures/Interface/Ashfall/noise-{i}.png", out var res))
                    frames.Add(res.Texture);
            }

            if (frames.Count == 0)
            {
                _noiseBroken = true;
                return;
            }

            _noiseFrames = frames.ToArray();
            PortraitNoise.Texture = _noiseFrames[0];
        }

        _noiseClock += args.DeltaSeconds;
        if (_noiseClock < 0.2f)
            return;

        _noiseClock = 0;
        _noiseFrame = (_noiseFrame + 1) % _noiseFrames.Length;
        var frame = _noiseFrames[_noiseFrame];

        // The dossier static hides with the dossier itself instead of flickering on
        // while the panel slides away; it must also persist through the close fade,
        // otherwise the portrait visibly loses its static in one frame.
        PortraitNoise.Visible = _dossierTargetOpen || _dossierAnimationProgress > 0.001f;
        PortraitNoise.Texture = frame;

        // One shared weak static cycle for all priority slot mini-portraits.
        for (var i = 0; i < _slotCards.Length; i++)
        {
            if (_slotCards[i] is { } card)
                card.NoiseTexture = frame;
        }
    }

    private void UpdateAmbientAnim(FrameEventArgs args)
    {
        if (_ambientBroken)
            return;

        if (_ambientFrames == null)
        {
            // Purely decorative: degrade to nothing instead of throwing if frames are absent.
            var frames = new List<Texture>();
            for (var i = 0; i < 3; i++)
            {
                if (_resCache.TryGetResource<TextureResource>($"/Textures/Ashfall/UI/archive-anim-{i}.png", out var res))
                    frames.Add(res.Texture);
            }

            if (frames.Count == 0)
            {
                _ambientBroken = true;
                AmbientAnim.Visible = false;
                return;
            }

            _ambientFrames = frames.ToArray();
            AmbientAnim.Texture = _ambientFrames[0];
        }

        _ambientClock += args.DeltaSeconds;
        if (_ambientClock < 0.3f)
            return;

        _ambientClock = 0;
        _ambientFrame = (_ambientFrame + 1) % _ambientFrames.Length;
        AmbientAnim.Texture = _ambientFrames[_ambientFrame];
    }

    private void UpdateCooldownUi()
    {
        var remaining = _cooldownEnd - _timing.RealTime;
        var exhausted = _genSystem.RemainingRefreshes == 0;
        if (remaining.TotalSeconds > 0 && !exhausted)
        {
            RefreshButton.Disabled = true;
            CooldownLabel.Text = Loc.GetString("ashfall-personal-files-cooldown", ("seconds", Math.Ceiling(remaining.TotalSeconds)));
            CooldownLabel.Visible = true;
        }
        else
        {
            RefreshButton.Disabled = exhausted;
            CooldownLabel.Visible = false;
        }

        HeaderStatusLabel.Text = _genSystem.RemainingRefreshes >= 0
            ? Loc.GetString("ashfall-personal-files-refreshes-left", ("count", _genSystem.RemainingRefreshes))
            : Loc.GetString("ashfall-personal-files-status");
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        _genSystem.PoolUpdated -= Populate;
        _requirements.Updated -= PopulateDossier;
        CloseSkillsWindow();
        DossierPreview.ClearPreview();
        CandidatesGrid.RemoveAllChildren();
        _cardControls.Clear();
        _cardBindings.Clear();
    }

    private void CloseSkillsWindow()
    {
        _skillsWindow?.Close();
        _skillsWindow?.Dispose();
        _skillsWindow = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CloseSkillsWindow();
            _genSystem.PoolUpdated -= Populate;
            _requirements.Updated -= PopulateDossier;
        }

        base.Dispose(disposing);
    }
}
