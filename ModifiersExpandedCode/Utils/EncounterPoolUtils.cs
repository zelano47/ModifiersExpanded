using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ModifiersExpanded.ModifiersExpandedCode.Utils;

public static class EncounterPoolUtils
{
    /// <summary>
    /// Returns true when this combat room is a hallway encounter from the act's easy pool.
    /// In base acts this maps to the first 3 hallway fights in Act 1 and first 2 in Acts 2/3.
    /// </summary>
    public static bool IsEasyPoolEncounter(CombatRoom? combatRoom)
    {
        return IsEasyPoolEncounter(combatRoom?.Encounter);
    }

    /// <summary>
    /// Returns true when the encounter model is a hallway encounter from the act's easy pool.
    /// The game marks these encounters via EncounterModel.IsWeak.
    /// </summary>
    public static bool IsEasyPoolEncounter(EncounterModel? encounter)
    {
        return encounter is { RoomType: RoomType.Monster, IsWeak: true };
    }
}
