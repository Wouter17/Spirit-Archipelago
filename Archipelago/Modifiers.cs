using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Archipelago.Archipelago;

public static class ArchipelagoModifiers
{
    public static HashSet<string> BaseLockedCards { get; set; } = [];
    public static HashSet<string> BaseLockedSpirits { get; set; } = [];
    public static HashSet<string> BaseLockedAspects { get; set; } = [];
    public static HashSet<string> GottenItems { get; set; } = [];
    public static int energyAdjustment = 0;
    public static int cardplaysAdjustment = 0;
    public static int blightAdjustment = 0;
    public static bool prioritisedShuffle = true;

    public static HashSet<string> LockedCards()
    {
        return BaseLockedCards.Except(GottenItems).ToHashSet();
    }

    public static HashSet<string> LockedSpirits()
    {
        return BaseLockedSpirits.Except(GottenItems).Select(s => RemoveSpecial(s.ToLower())).ToHashSet();
    }

    public static HashSet<string> LockedAspects()
    {
        return BaseLockedAspects.Except(GottenItems).Select(a => RemoveSpecial(a.ToLower())).ToHashSet();
    }

    private static readonly Regex sWhitespace = new(@"[^A-Za-z]");
    public static string RemoveSpecial(string input)
    {
        return sWhitespace.Replace(input, "");
    }
}
