using System.Numerics;
using System.Linq;
using Ashfall.Client.Stylesheets;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Content.Client.Players.PlayTimeTracking;
using Content.Client.UserInterface.Controls;
using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Ashfall.CharacterGen.UI;

public sealed partial class AshfallPersonalFilesScreen : PanelContainer
{
    /// <summary>
    ///     Accent color shared with the generator's education tags.
    /// </summary>
    private const string AccentColor = AshfallDossierSectionControl.AccentColor;

    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private JobRequirementsManager _requirements = default!;
    [Dependency] private IResourceCache _resCache = default!;

    private GridContainer CandidatesGrid => this.FindControl<GridContainer>("CandidatesGrid");
    private BoxContainer DossierSections => this.FindControl<BoxContainer>("DossierSections");
    private GridContainer JobsGrid => this.FindControl<GridContainer>("JobsGrid");
    private Label AssignmentHeading => this.FindControl<Label>("AssignmentHeading");
    private ProfilePreviewSpriteView DossierPreview => this.FindControl<ProfilePreviewSpriteView>("DossierPreview");
    private Label DossierNameLabel => this.FindControl<Label>("DossierNameLabel");
    private Label DossierBioLabel => this.FindControl<Label>("DossierBioLabel");
    private RichTextLabel DossierCultureLabel => this.FindControl<RichTextLabel>("DossierCultureLabel");
    private Label DossierBirthplaceLabel => this.FindControl<Label>("DossierBirthplaceLabel");
    private Label DossierNumberLabel => this.FindControl<Label>("DossierNumberLabel");
    private Label DossierSelectionLabel => this.FindControl<Label>("DossierSelectionLabel");
    private ScrollContainer DossierScroll => this.FindControl<ScrollContainer>("DossierScroll");
    private Button ConfirmButton => this.FindControl<Button>("ConfirmButton");
    private Button RefreshButton => this.FindControl<Button>("RefreshButton");
    private Label CooldownLabel => this.FindControl<Label>("CooldownLabel");
    private Label HeaderStatusLabel => this.FindControl<Label>("HeaderStatusLabel");
    private Button BackButton => this.FindControl<Button>("BackButton");
    private TextureRect PortraitNoise => this.FindControl<TextureRect>("PortraitNoise");

    private readonly AshfallCharacterGenSystem _genSystem;
    private TimeSpan _cooldownEnd;
    private int _inspectedIndex = -1;
    private ProtoId<JobPrototype>? _draftJob;
    private bool _refreshPending;

    // Retro CRT static cycling over the dossier portrait.
    private Texture[]? _noiseFrames;
    private bool _noiseBroken;
    private float _noiseClock;
    private int _noiseFrame;

    public event Action? BackToLobby;
    public event Action<int>? CandidateSelected;

    public AshfallPersonalFilesScreen()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        LayoutContainer.SetAnchorPreset(this, LayoutContainer.LayoutPreset.Wide);

        _genSystem = _entMan.System<AshfallCharacterGenSystem>();
        BackButton.OnPressed += _ => BackToLobby?.Invoke();
        RefreshButton.OnPressed += OnRefreshPressed;
        ConfirmButton.OnPressed += OnConfirmPressed;
        _genSystem.PoolUpdated += Populate;
        _requirements.Updated += PopulateDossier;

        // Anchor helpers only take effect under a LayoutContainer parent: both the portrait
        // view and the noise overlay fill the same frame rect so the static covers it tightly.
        LayoutContainer.SetAnchorPreset(DossierPreview, LayoutContainer.LayoutPreset.Wide);
        LayoutContainer.SetAnchorPreset(PortraitNoise, LayoutContainer.LayoutPreset.Wide);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _genSystem.RequestPool();
        Populate();
    }

    private void OnRefreshPressed(BaseButton.ButtonEventArgs args)
    {
        if (_timing.RealTime < _cooldownEnd)
            return;

        _refreshPending = true;
        _draftJob = null;
        _inspectedIndex = 0;
        _genSystem.RequestPool(refresh: true);
    }

    private void OnConfirmPressed(BaseButton.ButtonEventArgs args)
    {
        if (_draftJob is not { } job || _inspectedIndex < 0 || _inspectedIndex >= _genSystem.Candidates.Count)
            return;

        _genSystem.SelectCandidate(_inspectedIndex, job);
        CandidateSelected?.Invoke(_inspectedIndex);
    }

    private void Populate()
    {
        if (_genSystem.Candidates.Count == 0)
            return;

        if (_refreshPending)
        {
            _refreshPending = false;
            _inspectedIndex = 0;
            _draftJob = null;
        }
        else if (_inspectedIndex < 0 || _inspectedIndex >= _genSystem.Candidates.Count)
        {
            _inspectedIndex = _genSystem.SelectedIndex >= 0 ? _genSystem.SelectedIndex : 0;
        }

        PopulateCards();
        PopulateDossier();

        if (_genSystem.CooldownSecondsRemaining > 0)
            _cooldownEnd = _timing.RealTime + TimeSpan.FromSeconds(_genSystem.CooldownSecondsRemaining);
        UpdateCooldownUi();
    }

    private void PopulateCards()
    {
        CandidatesGrid.RemoveAllChildren();

        // Candidates 0..3 are human and 4..7 are non-human. Interleave the two groups in a
        // grid so matching rows share their height while preserving the established columns.
        const int columnSplit = 4;
        var leftCount = Math.Min(columnSplit, _genSystem.Candidates.Count);
        var rightCount = Math.Max(0, _genSystem.Candidates.Count - columnSplit);
        var rows = Math.Max(leftCount, rightCount);

        for (var row = 0; row < rows; row++)
        {
            AddCandidateCard(row < leftCount ? row : null);
            AddCandidateCard(row < rightCount ? columnSplit + row : null);
        }

        void AddCandidateCard(int? index)
        {
            if (index is not { } candidateIndex)
            {
                CandidatesGrid.AddChild(new Control());
                return;
            }

            var card = new AshfallCandidateCard();
            card.SetCandidate(candidateIndex,
                _genSystem.Candidates[candidateIndex],
                candidateIndex == _inspectedIndex,
                candidateIndex == _genSystem.SelectedIndex);
            card.Inspected += inspectedIndex =>
            {
                _inspectedIndex = inspectedIndex;
                _draftJob = inspectedIndex == _genSystem.SelectedIndex ? _genSystem.SelectedJob : null;
                PopulateCards();
                PopulateDossier();
            };
            CandidatesGrid.AddChild(card);
        }
    }

    private void PopulateDossier()
    {
        if (_inspectedIndex < 0 || _inspectedIndex >= _genSystem.Candidates.Count)
            return;

        var candidate = _genSystem.Candidates[_inspectedIndex];
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
        DossierCultureLabel.Text = string.IsNullOrEmpty(cultureLine)
            ? string.Empty
            : $"[color=#878C87]{cultureLine}[/color]";
        DossierBirthplaceLabel.Text = !string.IsNullOrEmpty(candidate.Dossier.Birthplace)
            ? $"МЕСТО РОЖДЕНИЯ // {candidate.Dossier.Birthplace}"
            : string.Empty;
        DossierNumberLabel.Text = Loc.GetString("ashfall-personal-files-number", ("number", _inspectedIndex + 1));
        DossierSelectionLabel.Text = _inspectedIndex == _genSystem.SelectedIndex
            ? Loc.GetString("ashfall-personal-files-confirmed", ("job", GetJobName(_genSystem.SelectedJob)))
            : string.Empty;

        DossierSections.RemoveAllChildren();

        var previousGroup = -1;
        foreach (var section in MergeQualificationsIntoEducation(OrderSectionsForReading(candidate.Dossier.Sections)))
        {
            if (section.Kind is "evaluation" or "precryo" or "cryo")
                continue;

            // Extra vertical air before each new semantic group (person / training / archive)
            // must be REAL layout margin: ExpandMargin draws the plate outside its layout slot
            // and the next plate's text overlaps it (the measure/arrange race).
            var group = SectionGroupRank(section.Kind);
            AddDossierSection(section, groupStart: previousGroup != -1 && group != previousGroup);
            previousGroup = group;
        }

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

        ConfirmButton.Disabled = _draftJob == null;
    }

    // Presentation order only; the shared dossier DTO keeps its generation order.
    // person (origin, character, open question) -> training (education, experience, review) -> archive (cryo).
    private static int SectionGroupRank(string kind) => kind switch
    {
        "origin" or "personality" or "hook" => 0,
        "education" or "qualification" or "career" or "evaluation" => 1,
        _ => 2,
    };

    private static IEnumerable<AshfallDossierSection> OrderSectionsForReading(IEnumerable<AshfallDossierSection> sections)
    {
        return sections
            .Select((section, index) => (section, index))
            .OrderBy(pair => SectionGroupRank(pair.section.Kind))
            .ThenBy(pair => pair.index)
            .Select(pair => pair.section);
    }

    // «Образование» reads as one plate: qualification rows fold into it as extra lines with
    // the same amber-title dash style. Presentation only — the shared dossier DTO keeps its
    // own separate sections, and a dossier without an education section falls back to plates.
    private static List<AshfallDossierSection> MergeQualificationsIntoEducation(IEnumerable<AshfallDossierSection> sections)
    {
        var result = new List<AshfallDossierSection>();
        var folded = new List<string>();
        var seenTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var educationIndex = -1;

        foreach (var section in sections)
        {
            if (section.Kind == "education")
            {
                educationIndex = result.Count;
                result.Add(section);
            }
            else if (section.Kind == "qualification" && section.Lines.Count > 0)
            {
                var title = section.Title.Trim();
                if (seenTitles.Add(title))
                {
                    folded.Add($"[color={AccentColor}]{title}[/color] — {section.Lines[0].Trim()}");
                }
            }
            else
            {
                result.Add(section);
            }
        }

        if (educationIndex >= 0 && folded.Count > 0)
        {
            var education = result[educationIndex];
            var lines = new List<string>(education.Lines);
            lines.AddRange(folded);
            result[educationIndex] = new AshfallDossierSection
            {
                Kind = education.Kind,
                Title = education.Title,
                Lines = lines,
            };
        }

        return result;
    }

    private void AddDossierSection(AshfallDossierSection section, bool groupStart = false)
    {
        if (section.Lines.Count == 0)
            return;

        DossierSections.AddChild(new AshfallDossierSectionControl(section, groupStart));
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
        if (_noiseClock < 0.13f)
            return;

        _noiseClock = 0;
        _noiseFrame = (_noiseFrame + 1) % _noiseFrames.Length;
        PortraitNoise.Texture = _noiseFrames[_noiseFrame];
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
        DossierPreview.ClearPreview();
        CandidatesGrid.RemoveAllChildren();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _genSystem.PoolUpdated -= Populate;
            _requirements.Updated -= PopulateDossier;
        }

        base.Dispose(disposing);
    }
}
