using System.Linq;
using Content.Shared.Ashfall.CharacterGen;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Ashfall.CharacterGen.UI;

public sealed partial class AshfallDossierSectionControl : PanelContainer
{
    public const string AccentColor = "#D48944";

    private RichTextLabel SectionHeader => this.FindControl<RichTextLabel>("SectionHeader");
    private BoxContainer LinesContainer => this.FindControl<BoxContainer>("LinesContainer");

    public AshfallDossierSectionControl(AshfallDossierSection section, bool groupStart)
    {
        RobustXamlLoader.Load(this);

        if (groupStart)
            Margin = new Thickness(0, 10, 0, 0);

        var title = section.Title.Trim();
        if (section.Kind == "qualification")
        {
            SectionHeader.Visible = false;
            var label = new RichTextLabel
            {
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Top,
                Text = $"[color={AccentColor}]{title}[/color] — {section.Lines[0].Trim()}",
            };
            LinesContainer.AddChild(label);
            return;
        }

        SectionHeader.Text = $"[color={AccentColor}]{title}[/color]";

        var lines = section.Lines
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line => section.Kind == "career" ? "• " + line : line);

        foreach (var line in lines)
        {
            var label = new RichTextLabel
            {
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Top,
                Text = line,
            };
            LinesContainer.AddChild(label);
        }
    }
}
