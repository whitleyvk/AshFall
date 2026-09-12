using System.Numerics;
using Content.Client.Ashfall.CharacterGen.UI;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Lobby.UI;

public sealed partial class LobbyCharacterPreviewPanel : Control
{
    public Button CharacterSetupButton => this.FindControl<Button>("CharacterSetup");
    public Button PersonalFilesButton => this.FindControl<Button>("PersonalFilesButton");
    public ProfilePortraitSpriteView ProfilePreview => this.FindControl<ProfilePortraitSpriteView>("ProfilePreview");
    public BoxContainer Loaded => this.FindControl<BoxContainer>("Loaded");
    public Label Unloaded => this.FindControl<Label>("Unloaded");
    public Label Summary => this.FindControl<Label>("Summary");
    public Control PortraitFrame => this.FindControl<PanelContainer>("PortraitFrame");

    // The empty portrait frame reads as a broken render; hide it while no candidate is pinned
    // and let a centered muted hint line carry the state instead.
    public void SetCandidateLoaded(bool value)
    {
        PortraitFrame.Visible = value;
        if (value)
        {
            Summary.Align = Label.AlignMode.Left;
            Summary.MinSize = Vector2.Zero;
            Summary.Margin = new Thickness(0);
            Summary.FontColorOverride = null;
        }
        else
        {
            Summary.Align = Label.AlignMode.Center;
            // Breathing room above and below the hint while the portrait is hidden.
            Summary.MinSize = new Vector2(0, 52);
            Summary.Margin = new Thickness(0, 10);
            Summary.FontColorOverride = Color.FromHex("#6E736E");
        }
    }

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
