using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Reflection;
using Path = System.IO.Path;

namespace LacyPvETweaks;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.lacyway.lpt";
    public override string Name { get; init; } = "Lacyway's PvE Tweaks";
    public override string Author { get; init; } = "Lacyway";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.7");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 70000)]
public class LacyPvETweaks(ISptLogger<LacyPvETweaks> logger,
    DatabaseService databaseService, DatabaseServer databaseServer,
    ConfigServer configServer, JsonUtil jsonUtil, ModHelper modHelper,
    IReadOnlyList<SptMod> sptMods) : IOnLoad
{
    public Task OnLoad()
    {
        string path;
        string configPath;
        TweaksConfig? config;

        try
        {
            path = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            configPath = Path.Combine(path, "config", "config.jsonc");
            config = jsonUtil.DeserializeFromFile<TweaksConfig>(configPath);
        }
        catch (Exception ex)
        {
            logger.Error("Exception caught when loading config, mod won't be loaded", ex);
            return Task.CompletedTask;
        }

        if (config == null)
        {
            logger.Error("Config was invalid after loading, mod won't be loaded");
            return Task.CompletedTask;
        }

        var friendlyRefLoaded = sptMods
            .Any(m => m.ModMetadata.ModGuid == "com.acidphantasm.reffriendlyquests");

        if (config.RefChanges)
        {
            if (!friendlyRefLoaded)
            {
                logger.Debug("Changing ref");
                EditRef();
            }
            else
            {
                logger.Warning("[Lacyway's PvE Tweaks] You are attempting to modify Ref quests while having Ref Friendly Quests by AcidPhantasm loaded, disabling option...");
                config.RefChanges = false;
            }
        }

        if (config.RemoveTransitQuests)
        {
            logger.Debug("Changing transits");
            EditTransits();
        }

        if (config.RemoveRecipes)
        {
            logger.Debug("Changing recipes");
            EditRecipes();
        }

        if (config.EnableLabyrinth)
        {
            logger.Debug("Changing labyrinth");
            EditLabyrinth();
        }

        if (config.QuestTweaks)
        {
            logger.Debug("Changing quests");
            EditQuests();
        }

        if (config.AddRecipes)
        {
            logger.Debug("Adding new recipes");
            AddProductions();
        }

        if (config.RemoveMapLimitations)
        {
            logger.Debug("Removing map limitations");
            RemoveMapLimitations();
        }

        if (config.TweakExtracts)
        {
            logger.Debug("Tweaking extracts");
            TweakExtracts();
        }

        logger.Success("[Lacyway's PvE Tweaks] Successfully loaded!" +
            $"\nRef: {config.RefChanges}, Transits: {config.RemoveTransitQuests}, Recipes: {config.RemoveRecipes}," +
            $" Labyrinth: {config.EnableLabyrinth}, QuestsTweaks: {config.QuestTweaks}, AddRecipes: {config.AddRecipes}," +
            $" Remove Map Limits: {config.RemoveMapLimitations}, TweakExtracts: {config.TweakExtracts}");

        return Task.CompletedTask;
    }

    private void TweakExtracts()
    {
        var location = databaseService.GetLocation("labyrinth");
        var extract = location?.Base.Exits
            .SingleOrDefault(e => e.Name == "labir_exit");
        if (extract != null)
        {
            logger.Debug("Found exfil");
            extract.MinTime = 300;
            extract.MinTimePVE = 300;
            extract.MaxTime = 0;
            extract.MaxTimePVE = 0;
            extract.ExfiltrationType = ExfiltrationType.Individual;
            return;
        }

        logger.Error("Could not find the Labyrinth extract. Did you modify your files?");
    }

    private void RemoveMapLimitations()
    {
        var quests = databaseService.GetQuests();
        var globals = databaseServer.GetTables().Locales.Global;

        MongoId[] questsToClean = [
            new("59ca2eb686f77445a80ed049"), // punisher pt. 6
            new("5c0bd01e86f7747cdd799e56"), // insomnia
            new("5d25bfd086f77442734d3007"), // surv path - zhivchik
            new("5d25e29d86f7740a22516326") // surv path - eagle-owl
        ];

        List<QuestCondition> toClean = [];
        var punisherPt6 = quests[questsToClean[0]];
        toClean.AddRange(punisherPt6.Conditions?.AvailableForFinish?
            .Where(c => c.Counter?.Conditions?.Any(qc => qc.ConditionType == "Location") == true));

        var insomnia = quests[questsToClean[1]];
        toClean.AddRange(insomnia.Conditions?.AvailableForFinish?
            .Where(c => c.Counter?.Conditions?.Any(qc => qc.ConditionType == "Location") == true));

        var zhivchik = quests[questsToClean[2]];
        toClean.AddRange(zhivchik.Conditions?.AvailableForFinish?
            .Where(c => c.Counter?.Conditions?.Any(qc => qc.ConditionType == "Location") == true));

        var eagleOwl = quests[questsToClean[3]];
        toClean.AddRange(eagleOwl.Conditions?.AvailableForFinish?
            .Where(c => c.Counter?.Conditions?.Any(qc => qc.ConditionType == "Location") == true));

        var removed = 0;
        foreach (var questCondition in toClean)
        {
            var result = questCondition.Counter?.Conditions?
                .RemoveAll(c => c.ConditionType == "Location");

            if (result != null)
            {
                removed++;
            }
        }

        logger.Debug($"Removed {removed} conditions, cleaning {toClean.Count} locales");

        foreach ((var locale, var lazyLoadedValue) in globals)
        {
            lazyLoadedValue.AddTransformer(localeData =>
            {
                if (localeData is null)
                {
                    return localeData;
                }

                foreach (var locale in toClean.Select(c => c.Id))
                {
                    var index = localeData[locale].LastIndexOf(" (");
                    if (index > -1)
                    {
                        localeData[locale] = localeData[locale][..index];
                    }
                }

                return localeData;
            });
        }
    }

    /// <summary>
    /// Adds some new production recipes to the hideout
    /// </summary>
    private void AddProductions()
    {
        var recipes = databaseService.GetHideout().Production.Recipes;
        recipes?.Add(new()
        {
            AreaType = HideoutAreas.IntelligenceCenter,
            Count = 1,
            EndProduct = new("679b9819a2f2dd4da9023512"),
            Id = new("68ff59b36e8d3b39f1b42378"),
            IsCodeProduction = false,
            IsEncoded = false,
            Locked = false,
            NeedFuelForAllProductionTime = false,
            ProductionLimitCount = 0,
            ProductionTime = 3600,
            Continuous = false,
            Requirements = [
                new Requirement() {
                    AreaType = 11,
                    RequiredLevel = 2,
                    Type = "Area"
                },
                new Requirement() {
                    TemplateId = new("5c94bbff86f7747ee735c08f"),
                    Count = 2,
                    Type = "Item"
                },
                new Requirement() {
                    TemplateId = new("5c12613b86f7743bbe2c3f76"),
                    Count = 2,
                    Type = "Item"
                },
                new Requirement() {
                    TemplateId = new("62a0a16d0b9d3c46de5b6e97"),
                    Type = "Item"
                },
                new Requirement() {
                    TemplateId = new("5c052fb986f7746b2101e909"),
                    Type = "Tool"
                }
            ]
        });
    }

    /// <summary>
    /// Edits some quests to be less grindy
    /// </summary>
    private void EditQuests()
    {
        var quests = databaseService.GetQuests();
        var testDrivePt2 = quests["63a5cf262964a7488f5243ce"];
        var testDrivePt2Cond = testDrivePt2.Conditions.AvailableForFinish?.FirstOrDefault();
        if (testDrivePt2Cond != null)
        {
            testDrivePt2Cond.Value = 5;
        }

        var testDrivePt3 = quests["64f5deac39e45b527a7c4232"];
        var testDrivePt3Cond = testDrivePt3.Conditions.AvailableForFinish?.FirstOrDefault();
        if (testDrivePt3Cond != null)
        {
            testDrivePt3Cond.Value = 5;
        }

        var hunter = quests["600302d73b897b11364cd161"];
        var hunterCond = hunter.Conditions.AvailableForFinish?.FirstOrDefault();
        if (hunterCond != null)
        {
            hunterCond.Value = 10;
        }
    }

    /// <summary>
    /// Removes all transit requirements and converts them into normal quests
    /// </summary>
    private void EditTransits()
    {
        var quests = databaseService.GetQuests();
        var globals = databaseServer.GetTables().Locales.Global;

        var transitQuests = quests
            .Where(q => q.Value?.Conditions?.AvailableForFinish?
                .Any(cond => cond?.Counter?.Conditions?
                .Any(y => (y?.Status?.Count ?? 0) == 1 && y!.Status!.Contains("Transit")) == true) == true
            ).ToArray();

        var localesToClean = new List<MongoId>();

        logger.Debug($"Found {transitQuests.Length} quests");
        for (var i = 0; i < transitQuests.Length; i++)
        {
            (var questId, var quest) = transitQuests[i];
            var condition = quest.Conditions?.AvailableForFinish?
                .FirstOrDefault(x => x.Counter?.Conditions?.Any(y => y.Status?.Count == 1 && y.Status.Contains("Transit")) == true);

            if (condition != null)
            {
                for (var j = 0; j < quest.Conditions?.AvailableForFinish?.Count; j++)
                {
                    var cond = quest.Conditions.AvailableForFinish[j];
                    if (cond.OneSessionOnly.GetValueOrDefault())
                    {
                        cond.OneSessionOnly = false;
                        localesToClean.Add(cond.Id);
                    }
                }

                logger.Debug("Found the condition");
                if (condition.OneSessionOnly.GetValueOrDefault())
                {
                    logger.Debug("Condition should be cleaned");
                    localesToClean.AddRange(quest.Conditions!.AvailableForFinish!.Select(x => x.Id));
                }
                quest.Conditions!.AvailableForFinish!.Remove(condition);
            }

            var childConditions = quest.Conditions?.AvailableForFinish?
                .Where(c => c.VisibilityConditions?.Count > 0)
                .ToList();

            if (childConditions != null)
            {
                foreach (var childCondition in childConditions)
                {
                    var toRemove = childCondition.VisibilityConditions?
                        .Where(vc => vc?.Target == condition?.Id)
                        .ToList();

                    if (toRemove != null)
                    {
                        foreach (var removeCondition in toRemove)
                        {
                            logger.Debug($"Removing visibility condition {condition?.Id}");
                            childCondition.VisibilityConditions?.Remove(removeCondition);
                        }
                    }
                }
            }
        }

        if (localesToClean.Count > 0)
        {
            logger.Debug($"Cleaning up {localesToClean.Count} locales.");
            foreach ((var locale, var lazyLoadedValue) in globals)
            {
                lazyLoadedValue.AddTransformer(localeData =>
                {
                    if (localeData is null)
                    {
                        return localeData;
                    }

                    foreach (var locale in localesToClean)
                    {
                        var index = localeData[locale].LastIndexOf(" (");
                        if (index > -1)
                        {
                            localeData[locale] = localeData[locale][..index];
                        }
                    }

                    return localeData;
                });
            }
        }
    }

    /// <summary>
    /// Enables Labyrinth on the map screen and moves the entry point
    /// </summary>
    private void EditLabyrinth()
    {
        var labyrinth = databaseService.GetLocations().Labyrinth;
        labyrinth.Base.Enabled = true;
        labyrinth.Base.IconY = 250f;
        labyrinth.Base.DisabledForScav = true;
        labyrinth.Base.ForceOnlineRaidInPVE = false;
    }

    /// <summary>
    /// Removes arena / event recipes
    /// </summary>
    private void EditRecipes()
    {
        var production = databaseService.GetHideout()
            .Production;

        MongoId[] idsToRemove = [
            new("67c9d54b017035dd060bff5e"),
            new("6666d7ea0b734650a91d0a42"),
            new("6666d899eb78191c502350b2"),
            new("66575197464c4b4ba4671004"),
            new("67caf5c5bfe0242ab1032966"),
            new("6666d829a8298779fc40e537"),
            new("67c9d447b53b0fcf1d0bb0ab"),
            new("67c9d4b251ce173bff01eec7"),
            new("67c9d5035be7fc94c806dee9"),
            new("66582be04de4820934746cea"),

            // non-arena crates
            new("67449c79268737ef6908d636"),
            new("6745925da9c9adf0450d5bca")
        ];

        production.Recipes?
            .RemoveAll(x => idsToRemove.Contains(x.Id));

        var config = configServer.GetConfig<HideoutConfig>();

        MongoId[] newCraftsToRemove = [
            new("67c5e55af344981d56050e7d"),
            new("67c5e56ef344981d56050e7e"),
            new("67c5eb0d533c65affb6732f7"),
            new("67c5eb17ec157da6c94dea6c"),
            new("67c5eb28a475f1532525477e"),
            new("67c5eb3036d41e7e85c62f06"),
            new("67cb4b25bb7852bce4a14364"),
            new("67cb4bb6dac4a5492d2e966a")
        ];

        config?.HideoutCraftsToAdd
            .RemoveAll(r => newCraftsToRemove.Contains(r.NewId));
        production.Recipes?
            .RemoveAll(x => newCraftsToRemove.Contains(x.Id));
    }

    /// <summary>
    /// Changes Ref quests to be PvE friendly
    /// </summary>
    private void EditRef()
    {
        MongoId[] refQuests = [
            new("68341eb25619c8e2a9031501"),
            new("68341f6fe2e7ef70a3060a0a"),
            new("6834202a186efa3c5b07f9a2"),
            new("683421515619c8e2a9031511"),
            new("68342265a8d674b5740b31f0"),
            new("68342446a8d674b5740b31fc")
        ];

        var quests = databaseService.GetQuests();
        var globals = databaseServer.GetTables().Locales.Global;

        var part1 = quests[refQuests[0]];
        part1.Conditions.AvailableForFinish!.Clear();
        part1.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("68341eb25619c8e2a9031504"),
            ConditionType = "CounterCreator",
            DynamicLocale = false,
            Type = "Elimination",
            Value = 20,
            Counter = new()
            {
                Conditions =
                [
                    new()
                    {
                        ConditionType = "Kills",
                        CompareMethod = ">=",
                        Distance = new()
                        {
                            CompareMethod = ">="
                        },
                        Id = new("67ee87fa75a5d18861fe8f88"),
                        Target = new(null, "Savage"),
                        Value = 1
                    }
                ]
            }
        });

        var part2 = quests[refQuests[1]];
        part2.Conditions.AvailableForFinish!.Clear();
        part2.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("68341f6fe2e7ef70a3060a0d"),
            ConditionType = "CounterCreator",
            DynamicLocale = false,
            Type = "Elimination",
            Value = 20,
            Counter = new()
            {
                Conditions =
                [
                    new()
                    {
                        ConditionType = "Kills",
                        CompareMethod = ">=",
                        Distance = new()
                        {
                            CompareMethod = ">="
                        },
                        Id = new("655e483da3ee7d4c56241e17"),
                        Target = new(null, "AnyPmc"),
                        Value = 1
                    }
                ]
            }
        });

        var part3 = quests[refQuests[2]];
        part3.Conditions.AvailableForFinish!.Clear();
        part3.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("662bb201589b7f21d7ab98fa"),
            ConditionType = "CounterCreator",
            DynamicLocale = false,
            Type = "Elimination",
            Value = 10,
            Counter = new()
            {
                Conditions =
                [
                    new()
                    {
                        ConditionType = "Kills",
                        CompareMethod = ">=",
                        Distance = new()
                        {
                            CompareMethod = ">="
                        },
                        Id = new("68ebbdf496bacde419279d25"),
                        Target = new(null, "Usec"),
                        Value = 1
                    }
                ]
            }
        });
        part3.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("67ebc19b79b4cd1899cb20f7"),
            ConditionType = "CounterCreator",
            DynamicLocale = false,
            Type = "Elimination",
            Value = 10,
            Counter = new()
            {
                Conditions =
                [
                    new()
                    {
                        ConditionType = "Kills",
                        CompareMethod = ">=",
                        Distance = new()
                        {
                            CompareMethod = ">="
                        },
                        Id = new("67ebc19b79b4cd1899cb20f7"),
                        Target = new(null, "Bear"),
                        Value = 1
                    }
                ]
            }
        });

        var part4 = quests[refQuests[3]];
        part4.Conditions.AvailableForFinish!.Clear();
        part4.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("683421515619c8e2a9031514"),
            ConditionType = "HandoverItem",
            DynamicLocale = false,
            Value = 15,
            DogtagLevel = 40,
            OnlyFoundInRaid = true,
            MaxDurability = 100,
            MinDurability = 0,
            Target = new(
            [
                ItemTpl.BARTER_DOGTAG_BEAR, ItemTpl.BARTER_DOGTAG_BEAR_EOD, ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_1,
                ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_2, ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_3, ItemTpl.BARTER_DOGTAG_BEAR_PRESTIGE_4,
                ItemTpl.BARTER_DOGTAG_BEAR_TUE
            ], null)
        });
        part4.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("683421515619c8e2a9031515"),
            ConditionType = "HandoverItem",
            DynamicLocale = false,
            Value = 15,
            DogtagLevel = 40,
            OnlyFoundInRaid = true,
            MaxDurability = 100,
            MinDurability = 0,
            Target = new(
            [
                ItemTpl.BARTER_DOGTAG_USEC, ItemTpl.BARTER_DOGTAG_USEC_EOD, ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_1,
                ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_2, ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_3, ItemTpl.BARTER_DOGTAG_USEC_PRESTIGE_4,
                ItemTpl.BARTER_DOGTAG_USEC_TUE
            ], null)
        });

        var part5 = quests[refQuests[4]];
        part5.Conditions.AvailableForFinish!.Clear();
        part5.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("662bb2c053b4c3d95e2e0753"),
            ConditionType = "CounterCreator",
            DynamicLocale = false,
            Type = "Elimination",
            Value = 1,
            Counter = new()
            {
                Conditions =
                [
                    new()
                    {
                        ConditionType = "Kills",
                        CompareMethod = ">=",
                        Distance = new()
                        {
                            CompareMethod = ">="
                        },
                        Id = new("68ebc1df1075f80e50e79cdb"),
                        SavageRole = ["bossKnight"],
                        Target = new(null, "Savage"),
                        Value = 1,
                        ResetOnSessionEnd = true
                    }
                ]
            }
        });
        part5.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("67e6b6e2727d4a1492fcfe1c"),
            ConditionType = "CounterCreator",
            DynamicLocale = false,
            Type = "Elimination",
            Value = 1,
            Counter = new()
            {
                Conditions =
                [
                    new()
                    {
                        ConditionType = "Kills",
                        CompareMethod = ">=",
                        Distance = new()
                        {
                            CompareMethod = ">="
                        },
                        Id = new("68ebc226ed17d544f9264fc8"),
                        SavageRole = ["followerBigPipe"],
                        Target = new(null, "Savage"),
                        Value = 1,
                        ResetOnSessionEnd = true
                    }
                ]
            }
        });
        part5.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("68ebc2460d29c043fcbd49b9"),
            ConditionType = "CounterCreator",
            DynamicLocale = false,
            Type = "Elimination",
            Value = 1,
            Counter = new()
            {
                Conditions =
                [
                    new()
                    {
                        ConditionType = "Kills",
                        CompareMethod = ">=",
                        Distance = new()
                        {
                            CompareMethod = ">="
                        },
                        Id = new("68ebc2492a2f82eace148352"),
                        SavageRole = ["followerBirdEye"],
                        Target = new(null, "Savage"),
                        Value = 1,
                        ResetOnSessionEnd = true
                    }
                ]
            }
        });

        var part6 = quests[refQuests[5]];
        part6.Conditions.AvailableForFinish!.Clear();
        part6.Conditions.AvailableForFinish.Add(new QuestCondition()
        {
            Id = new("68342446a8d674b5740b3200"),
            ConditionType = "CounterCreator",
            DynamicLocale = false,
            Type = "Elimination",
            Value = 1,
            Counter = new()
            {
                Conditions =
                [
                    new()
                    {
                        ConditionType = "Kills",
                        CompareMethod = ">=",
                        Distance = new()
                        {
                            CompareMethod = ">="
                        },
                        Id = new("663b9bde92a93ce2d8b0b835"),
                        SavageRole = ["bossPartisan"],
                        Target = new(null, "Savage"),
                        Value = 1,
                        ResetOnSessionEnd = true
                    }
                ]
            }
        });

        foreach ((var locale, var lazyLoadedValue) in globals)
        {
            lazyLoadedValue.AddTransformer(localeData =>
            {
                if (localeData is null)
                {
                    return localeData;
                }

                // part1
                localeData["68341eb25619c8e2a9031504"] = "Eliminate scavs on any location";
                // part2
                localeData["68341f6fe2e7ef70a3060a0d"] = "Eliminate PMCs on any location";
                // part3
                localeData["662bb201589b7f21d7ab98fa"] = "Eliminate USEC PMC operatives on any location";
                localeData["67ebc19b79b4cd1899cb20f7"] = "Eliminate BEAR PMC operatives on any location";
                // part4
                localeData["683421515619c8e2a9031514"] = "Hand over the found in raid BEAR PMC dogtag (Level 40+)";
                localeData["683421515619c8e2a9031515"] = "Hand over the found in raid USEC PMC dogtag (Level 40+)";
                // part5
                localeData["662bb2c053b4c3d95e2e0753"] = "Eliminate Knight in one raid";
                localeData["67e6b6e2727d4a1492fcfe1c"] = "Eliminate Big Pipe in one raid";
                localeData["68ebc2460d29c043fcbd49b9"] = "Eliminate Bird Eye in one raid";
                // part6
                localeData["68342446a8d674b5740b3200"] = "Eliminate Partisan in one raid without dying";

                return localeData;
            });
        }
    }
}
