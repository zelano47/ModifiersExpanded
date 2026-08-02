using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ModifiersExpanded.ModifiersExpandedCode.Extensions;

namespace ModifiersExpanded.ModifiersExpandedCode.Modifiers;

public class Ephemeral : AzModifier
{
    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (card.Owner == null)
        {
            return false;
        }

        if (
            card.Type == CardType.Status
            || card.Type == CardType.Curse
            || card.Type == CardType.Quest
        )
        {
            return false;
        }

        return keywords.Add(CardKeyword.Ethereal);
    }
}
