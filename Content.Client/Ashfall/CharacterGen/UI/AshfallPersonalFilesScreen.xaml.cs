using System.Numerics;
using System.Linq;
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
    private const string AccentColor = "#C8782E";

    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private JobRequirementsManager _requirements = default!;

    private BoxContainer LeftCardsContainer => this.FindControl<BoxContainer>("LeftCardsContainer");
    private BoxContainer RightCardsContainer => this.FindControl<BoxContainer>("RightCardsContainer");
    private BoxContainer DossierSections => this.FindControl<BoxContainer>("DossierSections");
    private GridContainer JobsGrid => this.FindControl<GridContainer>("JobsGrid");
    private Label AssignmentHeading => this.FindControl<Label>("AssignmentHeading");
    private ProfilePreviewSpriteView DossierPreview => this.FindControl<ProfilePreviewSpriteView>("DossierPreview");
    private Label DossierNameLabel => this.FindControl<Label>("DossierNameLabel");
    private Label DossierBioLabel => this.FindControl<Label>("DossierBioLabel");
    private Label DossierCultureLabel => this.FindControl<Label>("DossierCultureLabel");
    private Label DossierBirthplaceLabel => this.FindControl<Label>("DossierBirthplaceLabel");
    private Label DossierNumberLabel => this.FindControl<Label>("DossierNumberLabel");
    private Label DossierSelectionLabel => this.FindControl<Label>("DossierSelectionLabel");
    private ScrollContainer DossierScroll => this.FindControl<ScrollContainer>("DossierScroll");
    private Button ConfirmButton => this.FindControl<Button>("ConfirmButton");
    private Button RefreshButton => this.FindControl<Button>("RefreshButton");
    private Label CooldownLabel => this.FindControl<Label>("CooldownLabel");
    private Label HeaderStatusLabel => this.FindControl<Label>("HeaderStatusLabel");
    private Button BackButton => this.FindControl<Button>("BackButton");

    private readonly AshfallCharacterGenSystem _genSystem;
    private TimeSpan _cooldownEnd;
    private int _inspectedIndex = -1;
    private ProtoId<JobPrototype>? _draftJob;
    private bool _refreshPending;

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
        LeftCardsContainer.RemoveAllChildren();
        RightCardsContainer.RemoveAllChildren();

        for (var i = 0; i < _genSystem.Candidates.Count; i++)
        {
            var card = new AshfallCandidateCard();
            card.SetCandidate(i, _genSystem.Candidates[i], i == _inspectedIndex, i == _genSystem.SelectedIndex);
            card.Inspected += index =>
            {
                _inspectedIndex = index;
                _draftJob = index == _genSystem.SelectedIndex ? _genSystem.SelectedJob : null;
                PopulateCards();
                PopulateDossier();
            };

            if (i < 4)
                LeftCardsContainer.AddChild(card);
            else
                RightCardsContainer.AddChild(card);
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
        DossierCultureLabel.Text = !string.IsNullOrEmpty(candidate.Dossier.Morphology)
            ? $"МОРФОЛОГИЯ // {candidate.Dossier.Morphology}  •  КУЛЬТУРНАЯ ЛИНИЯ // {candidate.Dossier.CulturalOrigin}"
            : (!string.IsNullOrEmpty(candidate.Dossier.CulturalOrigin)
                ? $"КУЛЬТУРНАЯ ЛИНИЯ // {candidate.Dossier.CulturalOrigin}"
                : string.Empty);
        DossierBirthplaceLabel.Text = !string.IsNullOrEmpty(candidate.Dossier.Birthplace)
            ? $"МЕСТО РОЖДЕНИЯ // {candidate.Dossier.Birthplace}"
            : string.Empty;
        DossierNumberLabel.Text = Loc.GetString("ashfall-personal-files-number", ("number", _inspectedIndex + 1));
        DossierSelectionLabel.Text = _inspectedIndex == _genSystem.SelectedIndex
            ? Loc.GetString("ashfall-personal-files-confirmed", ("job", GetJobName(_genSystem.SelectedJob)))
            : string.Empty;

        DossierSections.RemoveAllChildren();
        // Pin the wrap width to the section area's ACTUAL arranged width: the RichTextLabel's
        // measure width must match its arrange width exactly, or a soft-wrapped line outgrows
        // its plate (the clipped second line). The pool arrives after the screen is arranged,
        // so the section area already knows its real width.
        var sectionWidth = DossierSections.PixelSize.X / UIScale;
        var textWidth = sectionWidth > 120f ? sectionWidth - 16f : 800f;

        foreach (var section in candidate.Dossier.Sections)
            AddDossierSection(section, textWidth);

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
            button.StyleClasses.Add("AshfallSecondaryAction");
            if (selected)
                button.Modulate = Color.FromHex("#C8782E");

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
                FontColorOverride = Color.FromHex("#B35B52"),
            });
        }

        AssignmentHeading.Text = _draftJob != null && _prototypes.TryIndex(_draftJob, out var draftJobProto)
            ? Loc.GetString("ashfall-personal-files-assignment-selected", ("job", draftJobProto.LocalizedName))
            : Loc.GetString("ashfall-personal-files-assignment-heading");

        ConfirmButton.Disabled = _draftJob == null;
    }

    private void AddDossierSection(AshfallDossierSection section, float textWidth)
    {
        if (section.Lines.Count == 0)
            return;

        // Every section sits on its own subtle plate; qualifications are compact single rows.
        var plate = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#1A1F24"),
                ContentMarginTopOverride = 4,
                ContentMarginBottomOverride = 5,
            },
            HorizontalExpand = true,
        };

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 3,
            Margin = new Thickness(8, 0),
        };

        if (section.Kind == "qualification")
        {
            // Text setter parses markup; SetMessage(string) renders it literally.
            var row = new RichTextLabel { SetWidth = textWidth, HorizontalExpand = true };
            row.Text = $"[color={AccentColor}]{section.Title}[/color] — {section.Lines[0]}";
            box.AddChild(row);
        }
        else
        {
            var header = new RichTextLabel { HorizontalExpand = true };
            header.Text = $"[color={AccentColor}]▸[/color] [color=#C7CDD4]{section.Title}[/color]";
            box.AddChild(header);

            // Work history renders as a bullet list; other multi-line sections (education) as
            // plain stacked lines.
            var text = section.Lines.Count > 1
                ? string.Join("\n", section.Lines.Select(line => (section.Kind == "career" ? "• " : "") + line))
                : section.Lines[0];
            box.AddChild(new RichTextLabel { SetWidth = textWidth, Text = text, HorizontalExpand = true });
        }

        plate.AddChild(box);
        DossierSections.AddChild(plate);
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
        LeftCardsContainer.RemoveAllChildren();
        RightCardsContainer.RemoveAllChildren();
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
