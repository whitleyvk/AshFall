using System.Text.RegularExpressions;
using Content.Shared.Ashfall.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;

namespace Content.Shared.Ashfall.Speech.EntitySystems;

public sealed partial class GrowlingAccentSystem : RelayAccentSystem<GrowlingAccentComponent>
{
    [Dependency] private IRobustRandom _random = default!;

    private static readonly Regex RegexLowerR = new("r+", RegexOptions.Compiled);
    private static readonly Regex RegexUpperR = new("R+", RegexOptions.Compiled);
    private static readonly Regex RegexLowerRp = new("р+", RegexOptions.Compiled);
    private static readonly Regex RegexUpperRp = new("Р+", RegexOptions.Compiled);

    private static readonly string[] ReplacementsR = { "rr", "rrr" };
    private static readonly string[] ReplacementsRUpper = { "RR", "RRR" };
    private static readonly string[] ReplacementsRp = { "рр", "ррр" };
    private static readonly string[] ReplacementsRpUpper = { "РР", "РРР" };

    public override string Accentuate(string message, Entity<GrowlingAccentComponent>? ent = null)
    {
        // rr!

        message = RegexLowerR.Replace(message, _random.Pick(ReplacementsR));
        // rRR!
        message = RegexUpperR.Replace(message, _random.Pick(ReplacementsRUpper));
        // рр!
        message = RegexLowerRp.Replace(message, _random.Pick(ReplacementsRp));
        // рРР!
        message = RegexUpperRp.Replace(message, _random.Pick(ReplacementsRpUpper));

        return message;
    }
}
