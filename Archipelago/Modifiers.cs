using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Handelabra.SpiritIsland.Engine.Model;

namespace Archipelago.Archipelago;

public static class ArchipelagoModifiers
{
    public static HashSet<string> BaseLockedCards { get; set; } = [];
    public static HashSet<string> BaseLockedSpirits { get; set; } = [];
    public static HashSet<string> BaseLockedAspects { get; set; } = [];
    public static Dictionary<string, int> GottenItems { get; set; } = [];
    public static ElementSet ElementsAdjustment { get; set; } = [];
    public static int spiritShards = 1;

    public static AdjustmentValue energyAdjustment = new();
    public static AdjustmentValue cardplaysAdjustment = new();
    public static AdjustmentValue blightAdjustment = new();

    public static bool prioritisedShuffle = true;

    public static void SetBaseAdjustment(int energy, int cardplays, int blight)
    {
        energyAdjustment.Base = energy;
        cardplaysAdjustment.Base = cardplays;
        blightAdjustment.Base = blight;
    }

    public static HashSet<string> LockedCards()
    {
        return BaseLockedCards.Except(GottenItems.Keys).ToHashSet();
    }

    public static HashSet<string> LockedSpirits()
    {
        return BaseLockedSpirits.Except(GottenItems.Where(kv => kv.Value >= spiritShards).Select(kv => kv.Key)).Select(s => RemoveSpecial(s.ToLower())).ToHashSet();
    }

    public static HashSet<string> LockedAspects()
    {
        return BaseLockedAspects.Except(GottenItems.Where(kv => kv.Value >= spiritShards).Select(kv => kv.Key)).Select(a => RemoveSpecial(a.ToLower())).ToHashSet();
    }

    private static readonly Regex sWhitespace = new(@"[^A-Za-z]");
    public static string RemoveSpecial(string input)
    {
        return sWhitespace.Replace(input, "");
    }
}

public struct AdjustmentValue(int baseValue = 0, int adjustment = 0)
{
    public int Base { get; set; } = baseValue;
    public int Adjustment { get; set; } = adjustment;

    public readonly int Value => Base + Adjustment;

    public static implicit operator int(AdjustmentValue value)
    {
        return value.Value;
    }

    public static AdjustmentValue operator ++(AdjustmentValue value)
    {
        value.Adjustment++;
        return value;
    }

    public static AdjustmentValue operator +(AdjustmentValue value, int amount)
    {
        value.Adjustment += amount;
        return value;
    }

    public static AdjustmentValue operator -(AdjustmentValue value, int amount)
    {
        value.Adjustment -= amount;
        return value;
    }

    public override readonly string ToString()
    {
        return Value.ToString();
    }
}