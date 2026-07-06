using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Modifiers;
using ModifiersExpanded.ModifiersExpandedCode.Modifiers;

namespace ModifiersExpanded.ModifiersExpandedCode.HarmonyPatches;

public class MerchantPatches
{
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
    public static class BodyDoubleMerchantAttackSlotPatch
    {
        public static void Prefix(Player player, ref CardType type)
        {
            if (type != CardType.Attack)
                return;
            if (player.RunState.Modifiers.Any(m => m is BodyDouble))
                type = CardType.Skill;
        }
    }
}
