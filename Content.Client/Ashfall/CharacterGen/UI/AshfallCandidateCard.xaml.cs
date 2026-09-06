using System.Numerics;
using Content.Client.Lobby.UI.ProfileEditorControls;
using Content.Shared.Ashfall.CharacterGen;
using Content.Shared.Ashfall.CharacterGen.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Ashfall.CharacterGen.UI;

public sealed partial class AshfallCandidateCard : PanelContainer
{
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IEntityManager _entMan = default!;

    private static readonly StyleBoxFlat NormalBox = new()
    {
        BackgroundColor = Color.FromHex("#181B1E"),
        BorderColor = Color.FromHex("#353D45"),
        BorderThickness = new Thickness(1),
    };

    private static readonly StyleBoxFlat InspectedBox = new()
    {
        BackgroundColor = Color.FromHex("#22241E"),
        BorderColor = Color.FromHex("#C8782E"),
        BorderThickness = new Thickness(2),
    };

    private PanelContainer CardPanel => this.FindControl<PanelContainer>("CardPanel");
    private ProfilePreviewSpriteView PreviewSprite => this.FindControl<ProfilePreviewSpriteView>("PreviewSprite");
    private Label NameLabel => this.FindControl<Label>("NameLabel");
    private Label BioLineLabel => this.FindControl<Label>("BioLineLabel");
    private Label QualificationLabel => this.FindControl<Label>("QualificationLabel");
    private Label ConfirmedLabel => this.FindControl<Label>("ConfirmedLabel");
    private Button InspectButton => this.FindControl<Button>("InspectButton");

    public int CandidateIndex { get; private set; }
    public event Action<int>? Inspected;

    public AshfallCandidateCard()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
        InspectButton.OnPressed += _ => Inspected?.Invoke(CandidateIndex);
    }

    public void SetCandidate(int index, AshfallCharacterCandidate candidate, bool isInspected, bool isConfirmed)
    {
        CandidateIndex = index;
        var profile = candidate.Profile;
        NameLabel.Text = profile.Name;
        var sex = profile.Sex switch
        {
            Sex.Male => Loc.GetString("ashfall-personal-files-sex-male"),
            Sex.Female => Loc.GetString("ashfall-personal-files-sex-female"),
            _ => Loc.GetString("ashfall-personal-files-sex-other"),
        };
        BioLineLabel.Text = Loc.GetString("ashfall-personal-files-card-bio", ("age", profile.Age), ("sex", sex));

        var qualification = candidate.Dossier.Qualifications.Count > 0 &&
                            _prototypes.TryIndex(candidate.Dossier.Qualifications[0], out AshfallCharacterLoreFragmentPrototype? fragment)
            ? Loc.GetString(fragment.Title)
            : Loc.GetString("ashfall-personal-files-record-unavailable");
        QualificationLabel.Text = qualification;
        ConfirmedLabel.Visible = isConfirmed;
        ConfirmedLabel.ToolTip = Loc.GetString("ashfall-personal-files-confirmed-marker");
        CardPanel.PanelOverride = isInspected ? InspectedBox : NormalBox;

        JobPrototype? primaryJob = null;
        if (candidate.CompatibleJobs.Count > 0 && _prototypes.TryIndex(candidate.CompatibleJobs[0], out JobPrototype? job))
            primaryJob = job;

        PreviewSprite.LoadPreview(profile, primaryJob, showClothes: true);
        FitPreviewScale();
    }

    /// <summary>
    ///     Normalizes the card preview: the full body is fit into the thumbnail area with an even
    ///     margin, using one uniform scale for every species so cards look consistent.
    /// </summary>
    private void FitPreviewScale()
    {
        if (!_entMan.TryGetComponent(PreviewSprite.PreviewDummy, out SpriteComponent? sprite))
            return;

        var bounds = sprite.CalculateRotatedBoundingBox(default, Angle.Zero, Angle.Zero).CalcBoundingBox();
        if (bounds.Height <= 0 || bounds.Width <= 0)
            return;

        const float margin = 14f;
        var ppm = EyeManager.PixelsPerMeter;
        var target = PreviewSprite.PixelSize;
        if (target.Y <= margin || target.X <= margin)
            return;

        var scale = MathF.Min((target.Y - margin) / (bounds.Height * ppm),
            (target.X - margin) / (bounds.Width * ppm));
        scale = Math.Clamp(scale, 1f, 2.2f);

        PreviewSprite.Scale = new Vector2(scale, scale);
        PreviewSprite.Stretch = SpriteView.StretchMode.None;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        PreviewSprite.ClearPreview();
    }
}
