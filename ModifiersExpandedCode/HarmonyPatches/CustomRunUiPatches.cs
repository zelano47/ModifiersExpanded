using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

// -------------------------------------------------------------------------
// "Last Run" button — remembers the modifiers used on the previous custom run
// and re-applies them with a single click.
// -------------------------------------------------------------------------
public class CustomRunUiPatches
{
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
            if (PreviousRunModifiers.Modifiers == null || PreviousRunModifiers.Modifiers.Count == 0)
                lastRunButton.Disable();
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
                && PreviousRunModifiers.Modifiers != null
                && PreviousRunModifiers.Modifiers.Count > 0
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
