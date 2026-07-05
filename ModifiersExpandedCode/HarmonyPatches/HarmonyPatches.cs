using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class HarmonyPatches
{
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GoodModifiers), MethodType.Getter)]
    public static class GoodModifiersPatch
    {
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            MainFile.Logger.Info(MainFile.CreateLogMessage("Patching ModelDb.GoodModifiers"));
            var patched = new List<ModifierModel>(__result)
            {
                ModelDb.Modifier<ColorlessCards>(),
                ModelDb.Modifier<NeowsBlessing>(),
                ModelDb.Modifier<Enchanter>(),
                ModelDb.Modifier<BodyDouble>(),
                ModelDb.Modifier<PraiseSnecko>(),
                ModelDb.Modifier<Polymath>(),
                ModelDb.Modifier<Chimera>(),
            };
            __result = patched;
        }
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.BadModifiers), MethodType.Getter)]
    public static class BadModifiersPatch
    {
        public static void Postfix(ref IReadOnlyList<ModifierModel> __result)
        {
            MainFile.Logger.Info(MainFile.CreateLogMessage("Patching ModelDb.BadModifiers"));
            var patched = new List<ModifierModel>(__result)
            {
                ModelDb.Modifier<Phalanx>(),
                ModelDb.Modifier<Marathon>(),
                ModelDb.Modifier<Pauper>(),
                ModelDb.Modifier<LoneWolf>(),
                ModelDb.Modifier<Hubris>(),
                ModelDb.Modifier<Ephemeral>(),
                ModelDb.Modifier<RunicDome>(),
            };
            __result = patched;
        }
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.MutuallyExclusiveModifiers), MethodType.Getter)]
    public static class MutuallyExclusiveModifiersPatch
    {
        public static void Postfix(ref IReadOnlyList<IReadOnlySet<ModifierModel>> __result)
        {
            MainFile.Logger.Info(
                MainFile.CreateLogMessage("Patching ModelDb.MutuallyExclusiveModifiers")
            );
            var patched = new List<IReadOnlySet<ModifierModel>>(__result);
            var existingSet = new HashSet<ModifierModel>(patched[0])
            {
                ModelDb.Modifier<BodyDouble>(),
                ModelDb.Modifier<Chimera>(),
            };
            patched[0] = existingSet;
            __result = patched;
        }
    }

    // Nonupeipe already guards BeautifulBracelet with Swift.CanEnchant >= 4.
    // Glitter is in the fixed pool with no guard — patch it to match the same pattern.
    [HarmonyPatch(typeof(Nonupeipe), "OptionPool", MethodType.Getter)]
    public static class NonupeipeOptionPoolPatch
    {
        public static void Postfix(Nonupeipe __instance, ref IEnumerable<EventOption> __result)
        {
            var cards = __instance.Owner?.Deck?.Cards;
            if (cards == null)
                return;
            if (cards.Count(ModelDb.Enchantment<Glam>().CanEnchant) == 0)
                __result = __result.Where(o => o.Relic is not Glitter);
        }
    }

    // Orobas has no guard for ElectricShrymp (Imbued), which uses CardSelectCmd.FromDeckForEnchantment.
    // Offering it with 0 Imbued-enchantable cards would cause a UI soft-lock.
    [HarmonyPatch(typeof(Orobas), "OptionPool1", MethodType.Getter)]
    public static class OrobasOptionPool1Patch
    {
        public static void Postfix(Orobas __instance, ref IEnumerable<EventOption> __result)
        {
            var cards = __instance.Owner?.Deck?.Cards;
            if (cards == null)
                return;
            if (cards.Count(ModelDb.Enchantment<Imbued>().CanEnchant) == 0)
                __result = __result.Where(o => o.Relic is not ElectricShrymp);
        }
    }

    // Scale enemy block gains as if 2 players are present when LoneWolf is active.
    [HarmonyPatch(
        typeof(MultiplayerScalingModel),
        nameof(MultiplayerScalingModel.ModifyBlockMultiplicative)
    )]
    public static class LoneWolfBlockScalingPatch
    {
        public static void Postfix(Creature target, ValueProp props, ref decimal __result)
        {
            if (__result != 1m)
                return; // already multiplayer-scaled
            if (target == null || (!target.IsPrimaryEnemy && !target.IsSecondaryEnemy))
                return;
            if (!props.IsPoweredCardOrMonsterMoveBlock())
                return;
            var runState = target.CombatState?.RunState;
            if (runState == null || runState.Players.Count != 1)
                return;
            if (!runState.Modifiers.Any(m => m is LoneWolf))
                return;
            __result =
                2m
                * MultiplayerScalingModel.GetMultiplayerScaling(
                    target.CombatState?.Encounter,
                    runState.CurrentActIndex
                );
        }
    }

    // DrowningBeacon's Climb option costs max HP and awards FresnelLens (Nimble enchantment relic).
    // With Enchanter active, all card rewards are already enchanted — FresnelLens.CanEnchant returns
    // false for every card and the relic does nothing. Remove the Climb option so the player isn't
    // offered a worthless relic at the cost of max HP.
    [HarmonyPatch(typeof(DrowningBeacon), "GenerateInitialOptions")]
    public static class DrowningBeaconGenerateInitialOptionsPatch
    {
        public static void Postfix(
            DrowningBeacon __instance,
            ref IReadOnlyList<EventOption> __result
        )
        {
            var modifiers = __instance.Owner?.RunState?.Modifiers;
            if (modifiers == null || !modifiers.Any(m => m is Enchanter))
                return;
            __result = __result
                .Where(o => o.TextKey != "DROWNING_BEACON.pages.INITIAL.options.CLIMB")
                .ToList();
        }
    }

    [HarmonyPatch(typeof(CharacterCards), nameof(CharacterCards.ModifyMerchantCardPool))]
    public static class CharacterCardsMerchantPoolPatch
    {
        public static void Postfix(
            CharacterCards __instance,
            Player player,
            IEnumerable<CardModel> options,
            ref IEnumerable<CardModel> __result
        )
        {
            var resultList = __result.ToList();
            var cardPool = player.Character.CardPool;

            // Only add to character card slots. Colorless slots contain no player-pool cards.
            if (!resultList.Any(c => c.Pool == cardPool))
                return;

            var extra = ModelDb
                .GetById<CharacterModel>(__instance.CharacterModel)
                .CardPool.GetUnlockedCards(
                    player.UnlockState,
                    player.RunState.CardMultiplayerConstraint
                );
            __result = resultList.Concat(extra.Where(c => !resultList.Contains(c)));
        }
    }

    // Body double removes attacks from the card pool. The merchant has hardcoded Attack-type slots
    // that would throw if the pool contains no attacks. Redirect those slots to Skill instead.
    [HarmonyPatch(
        typeof(CardFactory),
        nameof(CardFactory.CreateForMerchant),
        new[] { typeof(Player), typeof(IEnumerable<CardModel>), typeof(CardType) }
    )]
    public static class BulwarkMerchantAttackSlotPatch
    {
        public static void Prefix(Player player, ref CardType type)
        {
            if (type != CardType.Attack)
                return;
            if (player.RunState.Modifiers.Any(m => m is BodyDouble))
                type = CardType.Skill;
        }
    }

    // Hubris sets max HP to 1. With WearyTraveler, Neow heals 80% of max HP (0.8), which
    // truncates to 0 via (int) cast in SetCurrentHpInternal. Ensure a positive heal from 0 HP
    // always results in at least 1 HP when Hubris is active.
    [HarmonyPatch(typeof(Creature), nameof(Creature.HealInternal))]
    public static class HubrisMinHpPatch
    {
        public static void Prefix(Creature __instance, ref decimal amount)
        {
            if (amount <= 0 || __instance.Player == null)
                return;
            if ((int)(__instance.CurrentHp + amount) != 0)
                return;
            if (__instance.Player.RunState.Modifiers.Any(m => m is Hubris))
                amount = 1m - __instance.CurrentHp;
        }
    }

    // // TESTING ONLY: cycle event rooms through DrowningBeacon → Orobas → Nonupeipe. Delete before shipping.
    // [HarmonyPatch(typeof(ActModel), nameof(ActModel.PullNextEvent))]
    // public static class ForceEventCyclePatch
    // {
    //     private static int _index = 0;
    //     private static readonly EventModel[] _events = new EventModel[]
    //     {
    //         ModelDb.Event<SapphireSeed>(),
    //         ModelDb.Event<AbyssalBaths>(),
    //     };

    //     public static void Postfix(ref EventModel __result)
    //     {
    //         __result = _events[_index % _events.Length];
    //         _index++;
    //     }
    // }

    // RunicDome relic: hide intent visuals after each visual update.
    [HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
    public static class RunicDomeHideIntentVisualsPatch
    {
        private static readonly FieldInfo _intentHolderField = AccessTools.Field(
            typeof(NIntent),
            "_intentHolder"
        );

        private static readonly FieldInfo _ownerField = AccessTools.Field(
            typeof(NIntent),
            "_owner"
        );

        public static void Postfix(NIntent __instance)
        {
            var owner = _ownerField.GetValue(__instance) as Creature;
            if (owner?.CombatState == null)
                return;

            var localPlayer = LocalContext.GetMe(owner.CombatState);
            if (localPlayer == null)
                return;

            if (!localPlayer.RunState.Modifiers.Any(m => m is RunicDome))
                return;

            if (_intentHolderField.GetValue(__instance) is CanvasItem intentHolder)
                intentHolder.Modulate = Colors.Transparent;
        }
    }

    // RunicDome relic: suppress hover tips on hidden intents.
    [HarmonyPatch(typeof(NIntent), "OnHovered")]
    public static class RunicDomeHideIntentTipPatch
    {
        private static readonly FieldInfo _ownerField = AccessTools.Field(
            typeof(NIntent),
            "_owner"
        );

        public static bool Prefix(NIntent __instance)
        {
            var owner = _ownerField.GetValue(__instance) as Creature;
            if (owner?.CombatState == null)
                return true;

            var localPlayer = LocalContext.GetMe(owner.CombatState);
            if (localPlayer == null)
                return true;

            // Return false (skip original) when RunicDome is active.
            return !localPlayer.RunState.Modifiers.Any(m => m is RunicDome);
        }
    }

    // RunicDome relic: strip intent tips from creature hover tips shown when hovering the enemy sprite.
    [HarmonyPatch(typeof(Creature), "get_HoverTips")]
    public static class RunicDomeHideCreatureIntentTipsPatch
    {
        public static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!__instance.IsMonster)
                return;

            var localPlayer = LocalContext.GetMe(__instance.CombatState);
            if (localPlayer == null || !localPlayer.RunState.Modifiers.Any(m => m is RunicDome))
                return;

            // Intent tips are prepended before power tips; count and skip them.
            int intentTipCount =
                __instance.Monster?.NextMove?.Intents?.Count(i => i.HasIntentTip) ?? 0;
            __result = __result.Skip(intentTipCount);
        }
    }

    // -------------------------------------------------------------------------
    // "Last Run" button — remembers the modifiers used on the previous custom run
    // and re-applies them with a single click.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Before a singleplayer custom run actually starts, snapshot the chosen modifiers.
    /// </summary>
    [HarmonyPatch(typeof(NGame), nameof(NGame.StartNewSingleplayerRun))]
    public static class CaptureCustomRunModifiersPatch
    {
        public static void Prefix(IReadOnlyList<ModifierModel> modifiers, GameMode gameMode)
        {
            if (gameMode != GameMode.Custom)
                return;
            PreviousRunModifiers.Modifiers = modifiers.ToList();
            MainFile.Logger.Info(
                MainFile.CreateLogMessage(
                    $"Stored {modifiers.Count} modifiers from custom run for Last Run button."
                )
            );
        }
    }

    /// <summary>
    /// After NCustomRunScreen._Ready, duplicate the randomize button and insert it
    /// immediately to its right as the "Last Run" button.
    /// </summary>
    [HarmonyPatch(typeof(NCustomRunScreen), "_Ready")]
    public static class CustomRunScreenReadyPatch
    {
        private static readonly FieldInfo _randomizeButtonField = AccessTools.Field(
            typeof(NCustomRunScreen),
            "_randomizeButton"
        );
        private static readonly FieldInfo _modifiersListField = AccessTools.Field(
            typeof(NCustomRunScreen),
            "_modifiersList"
        );

        // _shaderMaterial is cached in NCustomRunRandomizeButton._Ready and used by
        // OnFocus/OnUnfocus. After Duplicate() both buttons share the same ShaderMaterial
        // instance, so we must replace it with a copy and update the cached field.
        private static readonly FieldInfo _shaderMaterialField = AccessTools.Field(
            typeof(NCustomRunRandomizeButton),
            "_shaderMaterial"
        );

        public static void Postfix(NCustomRunScreen __instance)
        {
            var randomizeButton =
                _randomizeButtonField.GetValue(__instance) as NCustomRunRandomizeButton;
            var modifiersList = _modifiersListField.GetValue(__instance) as NCustomRunModifiersList;

            if (randomizeButton == null || modifiersList == null)
            {
                MainFile.Logger.Warn(
                    MainFile.CreateLogMessage(
                        "Could not inject Last Run button: randomizeButton or modifiersList is null."
                    )
                );
                return;
            }

            // Duplicate the randomize button (copies the entire sub-tree including
            // Background + Label children that _Ready depends on).
            var lastRunButton = (NCustomRunRandomizeButton)(
                (object)((Node)(object)randomizeButton).Duplicate()!
            );
            ((Node)(object)lastRunButton).Name = "LastRunModifiersButton";

            // AddSiblingSafely places the new node right after its sibling in the parent.
            // After this call _Ready has run on the duplicate (synchronous because the
            // parent is already ready), so _shaderMaterial has been cached.
            ((Node)(object)randomizeButton).AddSiblingSafely((Node?)(object)lastRunButton);

            // Duplicate() leaves the ShaderMaterial as a shared resource — both buttons
            // would otherwise react to each other's hover events.  Give the new button its
            // own material copy and refresh the field that _Ready already cached.
            var bgControl = ((Node)(object)lastRunButton).GetNode<Control>("Background");
            if (((CanvasItem)(object)bgControl).Material is ShaderMaterial sharedMat)
            {
                var uniqueMat = (ShaderMaterial)sharedMat.Duplicate();
                ((CanvasItem)(object)bgControl).Material = uniqueMat;
                _shaderMaterialField.SetValue(lastRunButton, uniqueMat);
            }

            // _Ready on the duplicate has now run (synchronous when parent is already
            // ready) and set the label to "RANDOMIZE". Override it.
            ((Node)(object)lastRunButton)
                .GetNode<MegaRichTextLabel>("Label")
                .SetTextAutoSize(
                    new LocString(
                        "main_menu_ui",
                        "CUSTOM_RUN_SCREEN.LAST_RUN_BUTTON"
                    ).GetFormattedText()
                );

            // Wire up the click handler.
            ((GodotObject)(object)lastRunButton).Connect(
                NClickableControl.SignalName.Released,
                Callable.From<NButton>(_ => OnLastRunPressed(modifiersList)),
                0u
            );

            PreviousRunModifiers.LastRunButton = lastRunButton;
            MainFile.Logger.Info(MainFile.CreateLogMessage("Last Run button injected."));
        }

        private static void OnLastRunPressed(NCustomRunModifiersList modifiersList)
        {
            if (PreviousRunModifiers.Modifiers == null || PreviousRunModifiers.Modifiers.Count == 0)
            {
                MainFile.Logger.Info(
                    MainFile.CreateLogMessage(
                        "Last Run button pressed but no previous run modifiers stored."
                    )
                );
                return;
            }

            try
            {
                modifiersList.SetTickedModifiers(PreviousRunModifiers.Modifiers);
            }
            catch (InvalidOperationException)
            {
                // In multiplayer-client or load mode the list is read-only; ignore.
            }
        }
    }

    /// <summary>Disable the Last Run button while the player is waiting for the run to begin.</summary>
    [HarmonyPatch(typeof(NCustomRunScreen), "OnEmbarkPressed")]
    public static class CustomRunEmbarkPatch
    {
        public static void Postfix()
        {
            if (
                PreviousRunModifiers.LastRunButton != null
                && GodotObject.IsInstanceValid(PreviousRunModifiers.LastRunButton)
            )
                PreviousRunModifiers.LastRunButton.Disable();
        }
    }

    /// <summary>Re-enable the Last Run button when the player un-readies (host/singleplayer only).</summary>
    [HarmonyPatch(typeof(NCustomRunScreen), "OnUnreadyPressed")]
    public static class CustomRunUnreadyPatch
    {
        public static void Postfix()
        {
            if (PreviousRunModifiers.IsClientMode)
                return;
            if (
                PreviousRunModifiers.LastRunButton != null
                && GodotObject.IsInstanceValid(PreviousRunModifiers.LastRunButton)
            )
                PreviousRunModifiers.LastRunButton.Enable();
        }
    }

    /// <summary>Disable the Last Run button in multiplayer-client mode (clients cannot change modifiers).</summary>
    [HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsClient))]
    public static class CustomRunClientInitPatch
    {
        public static void Postfix()
        {
            PreviousRunModifiers.IsClientMode = true;
            if (
                PreviousRunModifiers.LastRunButton != null
                && GodotObject.IsInstanceValid(PreviousRunModifiers.LastRunButton)
            )
                PreviousRunModifiers.LastRunButton.Disable();
        }
    }

    /// <summary>Clear the client-mode flag when the screen is used as singleplayer.</summary>
    [HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeSingleplayer))]
    public static class CustomRunSingleplayerInitPatch
    {
        public static void Postfix() => PreviousRunModifiers.IsClientMode = false;
    }

    /// <summary>Clear the client-mode flag when the screen is used as multiplayer host.</summary>
    [HarmonyPatch(typeof(NCustomRunScreen), nameof(NCustomRunScreen.InitializeMultiplayerAsHost))]
    public static class CustomRunHostInitPatch
    {
        public static void Postfix() => PreviousRunModifiers.IsClientMode = false;
    }
}
