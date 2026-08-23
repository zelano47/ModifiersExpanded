using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;
using ModifiersExpanded.ModifiersExpandedCode.State;

namespace ModifiersExpanded.ModifiersExpandedCode.UI;

public static class ModifierList
{
    /// <summary>
    /// Refreshes every accordion section header (text + style). Call this after any
    /// operation that changes tickbox state from outside the per-tickbox signal path
    /// (e.g. Randomize or Last Run button clicks).
    /// </summary>
    public static Action? RefreshAllSectionHeaders { get; private set; }

    private static readonly (
        string LocKey,
        bool Exclusive,
        Func<ModifierModel, bool> Predicate,
        Func<ModifierModel, bool>? MutuallyExclusivePredicate
    )[] GroupSpecs =
    [
        ("REPLACE_STARTER_DECK", true, m => m.ClearsPlayerDeck, null),
        ("SPEEDRUN", true, m => m is SpeedrunBase, null),
        ("URGENCY", true, m => m is UrgencyBase, null),
        ("CARD_POOLS", false, m => m is CharacterCards or ColorlessCards, null),
        (
            "STARTING_RELICS",
            false,
            m => m is PraiseSnecko or Polymath or RelicSwap or NeowsBlessing or NeowsCondemnation,
            null
        ),
        ("STARTING_CARDS", false, m => m is AllStar or Specialized or HighRoller, null),
        (
            "MAP_MODIFIERS",
            false,
            m =>
                m is BigGameHunter or Marathon or DeadlyEvents or Flight or Sprint or DoubleTrouble,
            m => m is Marathon or Sprint
        ),
        (
            "REWARD_MODIFIERS",
            false,
            m => m is Pauper or Vintage or Enchanter or Hoarder or Midas,
            null
        ),
        (
            "CHALLENGES",
            false,
            m =>
                m
                    is Phalanx
                        or Ephemeral
                        or RunicDome
                        or LoneWolf
                        or CursedRun
                        or Hubris
                        or Murderous
                        or NightTerrors
                        or Terminal,
            null
        ),
        ("ENEMY_SCALING", false, m => m is EnemyScaling, null),
    ];

    private static List<ModifierGroup> BuildModifierGroups()
    {
        var exclusionGroups = ModelDb.MutuallyExclusiveModifiers;
        var allModifiers = ModelDb.GoodModifiers.Concat(ModelDb.BadModifiers).ToList();

        var classified = new HashSet<ModifierModel>();
        var groups = new List<ModifierGroup>();

        foreach (var (locKey, exclusive, predicate, mutuallyExclusivePredicate) in GroupSpecs)
        {
            var group = new ModifierGroup(
                new LocString("main_menu_ui", $"MODIFIER_GROUP.{locKey}.title"),
                exclusive
            );
            foreach (var m in allModifiers.Where(m => !classified.Contains(m) && predicate(m)))
            {
                group.Modifiers.Add(m);
                classified.Add(m);
                if (mutuallyExclusivePredicate != null && mutuallyExclusivePredicate(m))
                {
                    group.MutuallyExclusiveModifiers.Add(m.GetType());
                }
            }
            groups.Add(group);
        }

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
        EnemyScalingState.Instance.Reset();

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

        // ── Enemy-scaling tickboxes: hidden from layout, toggled by the slider ────────────
        var enemyScalingTickboxes = tickboxes.Where(t => t?.Modifier is EnemyScaling).ToList();
        foreach (var tb in enemyScalingTickboxes)
        {
            tb.IsTicked =
                EnemyScalingState.Instance.DamageMultiplier > 1f
                || EnemyScalingState.Instance.HpMultiplier > 1f
                || EnemyScalingState.Instance.NumAdditionalPlayers > 0;
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
            var section = new ModifierCheckboxSection(
                group,
                groupTickboxes,
                startExpanded: anyTicked
            );
            ((Node)(object)container).AddChildSafely(section.Root);
            sectionRefreshers.Add(section.Refresh);
        }

        RefreshAllSectionHeaders = () =>
        {
            foreach (var r in sectionRefreshers)
                r();
        };

        // ── 2. Enemy Scaling section ─────────────────────────────────────────
        var scalingTitle = new LocString(
            "main_menu_ui",
            "MODIFIER_GROUP.ENEMY_SCALING.title"
        ).GetFormattedText();
        var scalingSection = new EnemyScalingSection(scalingTitle, enemyScalingTickboxes);
        ((Node)(object)container).AddChildSafely(scalingSection.Root);
        sectionRefreshers.Add(scalingSection.Refresh);

        // ── 3. Standalone tickboxes (good before bad, alphabetical within each) ──────────
        var standalones = tickboxes
            .Where(t =>
                t != null && !tickboxToGroup.ContainsKey(t) && !(t.Modifier is EnemyScaling)
            )
            .OrderBy(t =>
                t!.Modifier != null && goodModifierTypes.Contains(t.Modifier.GetType()) ? 0 : 1
            )
            .ThenBy(t => ModifierGroupControls.ModifierDisplayName(t!.Modifier))
            .ToList();

        foreach (var tickbox in standalones)
        {
            ((Node)(object)container).AddChildSafely((Node)(object)tickbox);
        }
    }
}
