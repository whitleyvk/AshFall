using System.Linq;
using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Ashfall.CharacterGen.UI;

public sealed partial class AshfallCandidateCard : PanelContainer
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    // Normal: neutral cold dark metal border.
    // Selected: distinct warm amber border and background glow.
    private static readonly StyleBoxFlat NormalBox = new()
    {
        BackgroundColor = Color.FromHex("#1B1C1E"),
        BorderColor = Color.FromHex("#2E3033"),
        BorderThickness = new Thickness(1),
    };

    private static readonly StyleBoxFlat InspectedBox = new()
    {
        BackgroundColor = Color.FromHex("#2B2217"),
        BorderColor = Color.FromHex("#D48944"),
        BorderThickness = new Thickness(1),
    };

    private PanelContainer CardPanel => this.FindControl<PanelContainer>("CardPanel");
    private ProfileFullBodySpriteView PreviewSprite => this.FindControl<ProfileFullBodySpriteView>("PreviewSprite");
    private Label NameLabel => this.FindControl<Label>("NameLabel");
    private Label BioLineLabel => this.FindControl<Label>("BioLineLabel");
    private Label QualificationLabel => this.FindControl<Label>("QualificationLabel");
    private TextureRect ConfirmedMark => this.FindControl<TextureRect>("ConfirmedMark");
    private Button InspectButton => this.FindControl<Button>("InspectButton");

    private HumanoidCharacterProfile? _profile;

    public int CandidateIndex { get; private set; }
    public event Action<int>? Inspected;

    public AshfallCandidateCard()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        InspectButton.OnPressed += _ => Inspected?.Invoke(CandidateIndex);
    }

    public void SetCandidate(int index, AshfallCharacterCandidate candidate, bool isInspected, bool isConfirmed, bool isPinned = false, JobPrototype? activeJob = null)
    {
        CandidateIndex = index;
        _profile = candidate.Profile;
        NameLabel.Text = _profile.Name;
        NameLabel.FontColorOverride = isInspected ? Color.FromHex("#D48944") : Color.FromHex("#D8DDD8");
        var sex = _profile.Sex switch
        {
            Sex.Male => Loc.GetString("ashfall-personal-files-sex-male"),
            Sex.Female => Loc.GetString("ashfall-personal-files-sex-female"),
            _ => Loc.GetString("ashfall-personal-files-sex-other"),
        };
        BioLineLabel.Text = Loc.GetString("ashfall-personal-files-card-bio", ("age", _profile.Age), ("sex", sex));

        // Established competency title; fresh graduates show their professional sphere instead.
        var qualificationSection = candidate.Dossier.Sections
            .FirstOrDefault(s => s.Kind == "qualification");
        QualificationLabel.Text = qualificationSection != null
            ? qualificationSection.Title
            : Loc.GetString($"ashfall-domain-{candidate.PrimaryDomain.ToLowerInvariant()}");
        ConfirmedMark.Visible = isPinned || isConfirmed;
        ConfirmedMark.ToolTip = Loc.GetString(isPinned
            ? "ashfall-personal-files-pinned-marker"
            : "ashfall-personal-files-confirmed-marker");
        CardPanel.PanelOverride = isInspected ? InspectedBox : NormalBox;

        JobPrototype? jobToPreview = activeJob;
        if (jobToPreview == null && candidate.CompatibleJobs.Count > 0 && _prototypes.TryIndex(candidate.CompatibleJobs[0], out JobPrototype? job))
            jobToPreview = job;

        PreviewSprite.LoadPreview(_profile, jobToPreview, showClothes: true);
    }

    public void SetInspected(bool isInspected)
    {
        CardPanel.PanelOverride = isInspected ? InspectedBox : NormalBox;
    }

    public void UpdateJobPreview(JobPrototype? job)
    {
        if (_profile != null)
            PreviewSprite.LoadPreview(_profile, job, showClothes: true);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        PreviewSprite.ClearPreview();
    }
}
