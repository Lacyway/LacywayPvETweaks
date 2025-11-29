using System.Text.Json.Serialization;

namespace LacyPvETweaks;

public record TweaksConfig
{
    /// <summary>
    /// Should ref quests be modified to be more PvE friendly
    /// </summary>
    [JsonPropertyName("refChanges")]
    public bool RefChanges { get; set; }

    /// <summary>
    /// Should transits quests be turned into normal quests
    /// </summary>
    [JsonPropertyName("removeTransitQuests")]
    public bool RemoveTransitQuests { get; set; }

    /// <summary>
    /// Removes redundant recipes for e.g. arena crates
    /// </summary>
    [JsonPropertyName("removeRecipes")]
    public bool RemoveRecipes { get; set; }

    /// <summary>
    /// Enables Labyrinth on the map screen so that you can queue into it
    /// </summary>
    [JsonPropertyName("enableLabyrinth")]
    public bool EnableLabyrinth { get; set; }

    /// <summary>
    /// Makes several vanilla quests less grindy/obnoxious
    /// </summary>
    [JsonPropertyName("questTweaks")]
    public bool QuestTweaks { get; set; }

    /// <summary>
    /// Adds new recipes to the hideout
    /// </summary>
    [JsonPropertyName("addRecipes")]
    public bool AddRecipes { get; set; }

    /// <summary>
    /// Removes map limitations from certain quests
    /// </summary>
    [JsonPropertyName("removeMapLimitations")]
    public bool RemoveMapLimitations { get; set; }

    /// <summary>
    /// Makes certain extracts less restricted
    /// </summary>
    [JsonPropertyName("tweakExtracts")]
    public bool TweakExtracts { get; set; }

    /// <summary>
    /// Allows you to unlock otherwise unobtainable customizations for the hideout
    /// </summary>
    [JsonPropertyName("hideoutCustomizations")]
    public bool HideoutCustomizations { get; set; }

    /// <summary>
    /// Tweaks the AS VAL to not be a molten volcano
    /// </summary>
    [JsonPropertyName("tweakAsVal")]
    public bool TweakASVal { get; set; }
}
