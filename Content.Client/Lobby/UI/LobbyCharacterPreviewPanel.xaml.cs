using Content.Client.Lobby.UI.ProfileEditorControls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Lobby.UI;

public sealed partial class LobbyCharacterPreviewPanel : Control
{
    public Button CharacterSetupButton => this.FindControl<Button>("CharacterSetup");
    public Button PersonalFilesButton => this.FindControl<Button>("PersonalFilesButton");
    public ProfilePreviewSpriteView ProfilePreviewSpriteView => this.FindControl<ProfilePreviewSpriteView>("ProfilePreviewSpriteView");
    public BoxContainer Loaded => this.FindControl<BoxContainer>("Loaded");
    public Label Unloaded => this.FindControl<Label>("Unloaded");
    public Label Summary => this.FindControl<Label>("Summary");

    public LobbyCharacterPreviewPanel()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void SetLoaded(bool value)
    {
        Loaded.Visible = value;
        Unloaded.Visible = !value;
    }

    public void SetSummaryText(string value)
    {
        Summary.Text = value;
    }
}
