using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

[Virtual]
public partial class ProfilePreviewSpriteView : SpriteView
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;

    /// <summary>
    /// Entity used for the profile editor preview
    /// </summary>
    public EntityUid PreviewDummy;

    private HumanoidCharacterProfile? _lastLoadedProfile;
    private string? _lastLoadedJob;
    private bool _lastLoadedShowClothes;

    public ProfilePreviewSpriteView()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Reloads the entire dummy entity for preview.
    /// </summary>
    /// <remarks>
    /// This is expensive so not recommended to run if you have a slider.
    /// </remarks>
    public void LoadPreview(HumanoidCharacterProfile profile, JobPrototype? jobOverride = null, bool showClothes = true)
    {
        // Pool updates re-run this for every card and slot with unchanged data; respawning
        // the dummy a dozen times in one frame shows up as a visible frame spike.
        var jobId = jobOverride?.ID;
        if (PreviewDummy.Valid &&
            _lastLoadedProfile is { } last &&
            profile.Equals(last) &&
            jobId == _lastLoadedJob &&
            showClothes == _lastLoadedShowClothes)
        {
            return;
        }

        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;

        LoadHumanoidEntity(profile, jobOverride, showClothes);

        SetEntity(PreviewDummy);
        SetName(profile.Name);
        _lastLoadedProfile = profile;
        _lastLoadedJob = jobId;
        _lastLoadedShowClothes = showClothes;
    }

    /// <summary>
    /// Sets the preview entity's name without reloading anything else.
    /// </summary>
    public void SetName(string newName)
    {
        EntMan.System<MetaDataSystem>().SetEntityName(PreviewDummy, newName);
    }

    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    public void ReloadProfilePreview(HumanoidCharacterProfile profile)
    {
        _lastLoadedProfile = null;
        ReloadHumanoidEntity(profile);
    }

    public void ClearPreview()
    {
        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;
        _lastLoadedProfile = null;
        _lastLoadedJob = null;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        ClearPreview();
    }
}
