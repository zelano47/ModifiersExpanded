using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

public static class ModifierList
{
    /// <summary>
    /// Refreshes every accordion section header (text + style). Call this after any
    /// operation that changes tickbox state from outside the per-tickbox signal path
    /// (e.g. Randomize or Last Run button clicks).
    /// </summary>
    public static Action? RefreshAllSectionHeaders { get; private set; }

    private static List<ModifierGroup> BuildModifierGroups()
    {
        var exclusionGroups = ModelDb.MutuallyExclusiveModifiers;
        var allModifiers = ModelDb.GoodModifiers.Concat(ModelDb.BadModifiers).ToList();

        var replaceStarterDeckGroup = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.REPLACE_STARTER_DECK.title")
        );
        var speedrunGroup = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.SPEEDRUN.title")
        );
        var urgencyGroup = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.URGENCY.title")
        );
        var cardPoolsGroup = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.CARD_POOLS.title"),
            false
        );
        var startingRelics = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.STARTING_RELICS.title"),
            false
        );
        var startingCards = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.STARTING_CARDS.title"),
            false
        );
        var mapModifiers = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.MAP_MODIFIERS.title"),
            false
        );
        var rewardModifiers = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.REWARD_MODIFIERS.title"),
            false
        );
        var challenges = new ModifierGroup(
            new LocString("main_menu_ui", "MODIFIER_GROUP.CHALLENGES.title"),
            false
        );

        foreach (var modifier in allModifiers)
        {
            if (modifier.ClearsPlayerDeck)
                replaceStarterDeckGroup.Modifiers.Add(modifier);
            else if (modifier is SpeedrunBase)
                speedrunGroup.Modifiers.Add(modifier);
            else if (modifier is UrgencyBase)
                urgencyGroup.Modifiers.Add(modifier);
            else if (modifier is CharacterCards || modifier is ColorlessCards)
                cardPoolsGroup.Modifiers.Add(modifier);
            else if (
                modifier is PraiseSnecko
                || modifier is Polymath
                || modifier is RelicSwap
                || modifier is NeowsBlessing
            )
                startingRelics.Modifiers.Add(modifier);
            else if (modifier is AllStar || modifier is Chimera || modifier is Specialized)
                startingCards.Modifiers.Add(modifier);
            else if (
                modifier is BigGameHunter
                || modifier is Marathon
                || modifier is DeadlyEvents
                || modifier is Flight
            )
                mapModifiers.Modifiers.Add(modifier);
            else if (
                modifier is Pauper
                || modifier is Vintage
                || modifier is Enchanter
                || modifier is Hoarder
                || modifier is Midas
            )
                rewardModifiers.Modifiers.Add(modifier);
            else if (
                modifier is Phalanx
                || modifier is Ephemeral
                || modifier is RunicDome
                || modifier is LoneWolf
                || modifier is CursedRun
                || modifier is Hubris
                || modifier is Murderous
                || modifier is NightTerrors
                || modifier is Terminal
            )
                challenges.Modifiers.Add(modifier);
        }

        var groups = new List<ModifierGroup>
        {
            replaceStarterDeckGroup,
            speedrunGroup,
            urgencyGroup,
            cardPoolsGroup,
            startingRelics,
            startingCards,
            mapModifiers,
            rewardModifiers,
            challenges,
        };

        // Generate one section per external mutual-exclusion set for any modifier not already
        // classified above. Iterating each set separately preserves distinct groupings from
        // different mods rather than collapsing them all into a single fallback section.
        var alreadyGroupedTypes = groups
            .SelectMany(g => g.Modifiers)
            .Select(m => m.GetType())
            .ToHashSet();

        foreach (var exclusionSet in exclusionGroups)
        {
            var unclassified = allModifiers
                .Where(m =>
                    !alreadyGroupedTypes.Contains(m.GetType())
                    && exclusionSet.Any(e => e.GetType() == m.GetType())
                )
                .ToList();

            if (unclassified.Count > 1)
            {
                // No GroupName: the header will fall back to listing member names.
                var externalGroup = new ModifierGroup();
                foreach (var m in unclassified)
                    externalGroup.Modifiers.Add(m);
                groups.Add(externalGroup);
            }
        }

        // Drop groups with fewer than two members — no meaningful choice to present.
        // Their modifier(s) will fall through to standalone tickboxes in the layout.
        return groups.Where(g => g.Modifiers.Count > 1).OrderBy(g => g.GroupName).ToList();
    }

    // ── Layout builder ───────────────────────────────────────────────────────

    public static void RebuildWithAccordionGroups(
        Control container,
        List<NRunModifierTickbox> tickboxes
    )
    {
        var groups = BuildModifierGroups();
        var goodModifierTypes = ModelDb.GoodModifiers.Select(m => m.GetType()).ToHashSet();

        // Map each non-null tickbox to its ModifierGroup.
        var tickboxToGroup = new Dictionary<NRunModifierTickbox, ModifierGroup>();
        foreach (var tickbox in tickboxes)
        {
            if (tickbox?.Modifier == null)
                continue;
            var match = groups.FirstOrDefault(g =>
                g.Modifiers.Any(m => m.GetType() == tickbox.Modifier.GetType())
            );
            if (match != null)
                tickboxToGroup[tickbox] = match;
        }

        // Detach every tickbox from the container without freeing it. Their existing signal
        // connections (to NCustomRunModifiersList.AfterModifiersChanged) are preserved on
        // the GodotObject — they survive the reparent.
        foreach (var tickbox in tickboxes)
        {
            if (tickbox == null)
                continue;
            ((Node)(object)tickbox).GetParent()?.RemoveChild((Node)(object)tickbox);
        }

        // ── 1. Accordion sections (groups in definition order, tickboxes alphabetical) ──
        var sectionRefreshers = new List<Action>();
        foreach (var group in groups)
        {
            var groupTickboxes = tickboxes
                .Where(t => t != null && tickboxToGroup.TryGetValue(t, out var g) && g == group)
                .OrderBy(t =>
                    t!.Modifier != null && goodModifierTypes.Contains(t.Modifier.GetType()) ? 0 : 1
                )
                .ThenBy(t => ModifierGroupControls.ModifierDisplayName(t!.Modifier))
                .ToList();

            if (groupTickboxes.Count == 0)
                continue;

            bool anyTicked = groupTickboxes.Any(t => t.IsTicked);
            var (section, refresh) = ModifierGroupControls.BuildAccordionSection(
                group,
                groupTickboxes,
                startExpanded: anyTicked
            );
            ((Node)(object)container).AddChildSafely((Node)(object)section);
            sectionRefreshers.Add(refresh);
        }

        RefreshAllSectionHeaders = () =>
        {
            foreach (var r in sectionRefreshers)
                r();
        };

        // ── 2. Standalone tickboxes (good before bad, alphabetical within each) ──────────
        var standalones = tickboxes
            .Where(t => t != null && !tickboxToGroup.ContainsKey(t))
            .OrderBy(t =>
                t!.Modifier != null && goodModifierTypes.Contains(t.Modifier.GetType()) ? 0 : 1
            )
            .ThenBy(t => ModifierGroupControls.ModifierDisplayName(t!.Modifier))
            .ToList();

        foreach (var tickbox in standalones)
            ((Node)(object)container).AddChildSafely((Node)(object)tickbox);
    }
}
