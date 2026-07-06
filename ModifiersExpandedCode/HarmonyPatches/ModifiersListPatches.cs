using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class ModifierGroup
{
    public ModifierGroup(bool isMutuallyExclusive = true)
    {
        Modifiers = new HashSet<ModifierModel>();
        IsMutuallyExclusive = isMutuallyExclusive;
    }

    public ModifierGroup(LocString groupName, bool isMutuallyExclusive = true)
    {
        Modifiers = new HashSet<ModifierModel>();
        GroupName = groupName;
        IsMutuallyExclusive = isMutuallyExclusive;
    }

    public HashSet<ModifierModel> Modifiers { get; }
    public LocString? GroupName { get; set; }

    /// <summary>
    /// When true, ticking one modifier in this group automatically unticks all others.
    /// Enforced directly from the group's own tickbox connections so the mod no longer
    /// depends on <see cref="ModelDb.MutuallyExclusiveModifiers"/> at display time.
    /// </summary>
    public bool IsMutuallyExclusive { get; init; }
};

/// <summary>
/// Replaces the flat <see cref="NCustomRunModifiersList"/> layout with a grouped accordion
/// UI. Modifiers that belong to a mutual-exclusion set are collapsed into an expandable
/// section; standalone modifiers are displayed directly as before.
///
/// The patch is a Postfix on _Ready: the original method still creates all
/// <see cref="NRunModifierTickbox"/> instances and registers its signal connections. We
/// only reorganise how those tickboxes are parented in the scene tree — all existing
/// mutual-exclusion logic and the ModifiersChanged signal remain untouched.
/// </summary>
public class ModifiersListPatches
{
    private static readonly FieldInfo? _containerField = AccessTools.Field(
        typeof(NCustomRunModifiersList),
        "_container"
    );

    private static readonly FieldInfo? _modifierTickboxesField = AccessTools.Field(
        typeof(NCustomRunModifiersList),
        "_modifierTickboxes"
    );

    // ── Patch ────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(NCustomRunModifiersList), "_Ready")]
    public static class NCustomRunModifiersListReadyPatch
    {
        public static void Postfix(NCustomRunModifiersList __instance)
        {
            if (_containerField?.GetValue(__instance) is not Control container)
                return;
            if (
                _modifierTickboxesField?.GetValue(__instance)
                is not List<NRunModifierTickbox> tickboxes
            )
                return;

            RebuildWithAccordionGroups(container, tickboxes);
        }
    }

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
        }

        var groups = new List<ModifierGroup>
        {
            replaceStarterDeckGroup,
            speedrunGroup,
            urgencyGroup,
            cardPoolsGroup,
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
        return groups.Where(g => g.Modifiers.Count > 1).ToList();
    }

    // ── Layout builder ───────────────────────────────────────────────────────

    private static void RebuildWithAccordionGroups(
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
        foreach (var group in groups)
        {
            var groupTickboxes = tickboxes
                .Where(t => t != null && tickboxToGroup.TryGetValue(t, out var g) && g == group)
                .OrderBy(t => ModifierDisplayName(t!.Modifier))
                .ToList();

            if (groupTickboxes.Count == 0)
                continue;

            bool anyTicked = groupTickboxes.Any(t => t.IsTicked);
            var section = BuildAccordionSection(group, groupTickboxes, startExpanded: anyTicked);
            ((Node)(object)container).AddChildSafely((Node)(object)section);
        }

        // ── 2. Standalone tickboxes (good before bad, alphabetical within each) ──────────
        var standalones = tickboxes
            .Where(t => t != null && !tickboxToGroup.ContainsKey(t))
            .OrderBy(t =>
                t!.Modifier != null && goodModifierTypes.Contains(t.Modifier.GetType()) ? 0 : 1
            )
            .ThenBy(t => ModifierDisplayName(t!.Modifier))
            .ToList();

        foreach (var tickbox in standalones)
            ((Node)(object)container).AddChildSafely((Node)(object)tickbox);
    }

    // ── Accordion section ────────────────────────────────────────────────────

    private static VBoxContainer BuildAccordionSection(
        ModifierGroup group,
        List<NRunModifierTickbox> groupTickboxes,
        bool startExpanded
    )
    {
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 0);

        var (headerPanel, headerLabel) = CreateHeaderPanel();
        var body = new VBoxContainer();
        body.AddThemeConstantOverride("separation", 0);
        body.Visible = startExpanded;

        foreach (var tickbox in groupTickboxes)
            body.AddChild((Node)(object)tickbox);

        RefreshHeader(headerLabel, body.Visible, group, groupTickboxes);

        // Toggle on header click.
        headerPanel.GuiInput += evt =>
        {
            if (
                evt is InputEventMouseButton btn
                && btn.ButtonIndex == MouseButton.Left
                && btn.Pressed
            )
            {
                body.Visible = !body.Visible;
                RefreshHeader(headerLabel, body.Visible, group, groupTickboxes);
            }
        };

        // Single Toggled handler per tickbox: refreshes the header AND enforces mutual
        // exclusion if the group requires it. Guarding on toggled.IsTicked == true prevents
        // cascades when we programmatically untick sibling tickboxes below.
        foreach (var tickbox in groupTickboxes)
        {
            ((GodotObject)(object)tickbox).Connect(
                NTickbox.SignalName.Toggled,
                Callable.From<NRunModifierTickbox>(toggled =>
                {
                    RefreshHeader(headerLabel, body.Visible, group, groupTickboxes);
                    if (group.IsMutuallyExclusive && toggled.IsTicked)
                        foreach (var other in groupTickboxes)
                            if (other != toggled)
                                other.IsTicked = false;
                }),
                0u
            );
        }

        root.AddChild(headerPanel);
        root.AddChild(body);
        return root;
    }

    // ── Header panel ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a styled clickable header using a <see cref="PanelContainer"/> with a
    /// native <see cref="Label"/> child. <c>Label</c> inherits fonts through Godot's
    /// theme tree, picking up the project's custom font automatically at runtime.
    /// <c>NButton</c> is scene-based; <c>new MegaLabel()</c> creates a bare instance
    /// without the font resources embedded in scene-loaded versions.
    /// </summary>
    private static (PanelContainer panel, Label label) CreateHeaderPanel()
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.CustomMinimumSize = new Vector2(0, 40);
        // Stop propagates mouse events to this node so GuiInput fires.
        panel.MouseFilter = Control.MouseFilterEnum.Stop;

        // Dark brownish section header — visually distinct from the tickboxes below.
        var normal = new StyleBoxFlat();
        normal.BgColor = new Color(0.13f, 0.10f, 0.07f, 0.95f);
        normal.BorderWidthLeft = 1;
        normal.BorderWidthTop = 1;
        normal.BorderWidthRight = 1;
        normal.BorderWidthBottom = 1;
        normal.BorderColor = new Color(0.45f, 0.33f, 0.12f, 0.80f);
        normal.CornerRadiusTopLeft = 3;
        normal.CornerRadiusTopRight = 3;
        normal.CornerRadiusBottomLeft = 3;
        normal.CornerRadiusBottomRight = 3;
        normal.ContentMarginLeft = 10;
        normal.ContentMarginTop = 4;
        normal.ContentMarginBottom = 4;

        // Slightly lighter on hover.
        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(0.21f, 0.17f, 0.10f, 0.95f);

        panel.AddThemeStyleboxOverride("panel", normal);
        panel.MouseEntered += () => panel.AddThemeStyleboxOverride("panel", hover);
        panel.MouseExited += () => panel.AddThemeStyleboxOverride("panel", normal);

        var label = new Label();
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        panel.AddChild(label);

        return (panel, label);
    }

    private static void RefreshHeader(
        Label label,
        bool expanded,
        ModifierGroup group,
        List<NRunModifierTickbox> tickboxes
    )
    {
        string arrow = expanded ? "▼  " : "▶  ";

        // Collapsed with a selection: show the selected modifier's name.
        if (!expanded)
        {
            NRunModifierTickbox[] selected = tickboxes.Where(t => t.IsTicked).ToArray();
            if (selected.Length > 0)
            {
                label.Text = arrow + ModifierGroupCollapsedText(selected);
                return;
            }
        }

        // Expanded, or collapsed with nothing selected:
        // prefer the group's localized name; fall back to listing member names.
        string text =
            group.GroupName != null
                ? group.GroupName.GetFormattedText()
                : ModifierGroupCollapsedText(tickboxes.ToArray());
        label.Text = arrow + text;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private const int maxCollapsedTextLength = 75;
    private const int numEllipsisChars = 3;

    private static string ModifierGroupCollapsedText(NRunModifierTickbox[] tickboxes)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(string.Join(", ", tickboxes.Select(t => ModifierDisplayName(t.Modifier))));
        if (sb.Length > maxCollapsedTextLength)
        {
            sb.Remove(
                maxCollapsedTextLength - numEllipsisChars,
                sb.Length - (maxCollapsedTextLength - numEllipsisChars)
            );
            sb.Append(new string('.', numEllipsisChars));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts a PascalCase modifier ID to a readable display name.
    /// "SealedDeck" → "Sealed Deck"
    /// </summary>
    private static string ModifierDisplayName(ModifierModel? modifier)
    {
        if (modifier == null)
            return "?";
        return modifier.Title.GetFormattedText();
    }
}
