using System.Linq;
using Content.Shared.Ashfall.CharacterGen;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Ashfall.CharacterGen.UI;

public sealed partial class AshfallDossierSectionControl : PanelContainer
{
    public const string AccentColor = "#D48944";

    private RichTextLabel SectionHeader => this.FindControl<RichTextLabel>("SectionHeader");
    private BoxContainer LinesContainer => this.FindControl<BoxContainer>("LinesContainer");

    public AshfallDossierSectionControl(AshfallDossierSection section, bool groupStart, Control? extraControl = null)
    {
        RobustXamlLoader.Load(this);

        if (groupStart)
            Margin = new Thickness(0, 10, 0, 0);

        var title = section.Title.Trim();
        SectionHeader.Text = $"[color={AccentColor}]{title}[/color]";

        if (section.Lines.Count > 0)
        {
            foreach (var rawLine in section.Lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                if (section.Kind == "career")
                    line = "• " + line;

                var label = new RichTextLabel
                {
                    HorizontalExpand = true,
                    Text = line,
                };
                LinesContainer.AddChild(label);
            }
        }

        if (extraControl != null)
        {
            LinesContainer.AddChild(extraControl);
        }
    }
}
