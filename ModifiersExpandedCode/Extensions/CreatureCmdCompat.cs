using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ModifiersExpanded.ModifiersExpandedCode.Extensions;

public static class CreatureCmdCompat
{
    private static Func<
        PlayerChoiceContext,
        Creature,
        decimal,
        ValueProp,
        Task<IEnumerable<DamageResult>>
    >? _damageNoSourceDelegate;

    /// <summary>
    /// Calls CreatureCmd.Damage with no dealer and no card source, compatible with both
    /// the main branch (Creature?, CardModel?) and beta branch (Creature?, CardModel?, CardPlay?)
    /// overload sets.
    /// </summary>
    public static Task<IEnumerable<DamageResult>> DamageNoSource(
        PlayerChoiceContext context,
        Creature target,
        decimal amount,
        ValueProp props
    )
    {
        _damageNoSourceDelegate ??= BuildDamageDelegate();
        return _damageNoSourceDelegate(context, target, amount, props);
    }

    private static Func<
        PlayerChoiceContext,
        Creature,
        decimal,
        ValueProp,
        Task<IEnumerable<DamageResult>>
    > BuildDamageDelegate()
    {
        var type = typeof(CreatureCmd);

        // Beta branch: Damage(PlayerChoiceContext, Creature, decimal, ValueProp, Creature?, CardModel?, CardPlay?)
        var betaMethod = type.GetMethod(
            "Damage",
            [
                typeof(PlayerChoiceContext),
                typeof(Creature),
                typeof(decimal),
                typeof(ValueProp),
                typeof(Creature),
                typeof(CardModel),
                typeof(CardPlay),
            ]
        );

        if (betaMethod != null)
        {
            return (ctx, target, amount, props) =>
                (Task<IEnumerable<DamageResult>>)
                    betaMethod.Invoke(null, [ctx, target, amount, props, null, null, null])!;
        }

        // Main branch: Damage(PlayerChoiceContext, Creature, decimal, ValueProp, Creature?, CardModel?)
        var mainMethod = type.GetMethod(
            "Damage",
            [
                typeof(PlayerChoiceContext),
                typeof(Creature),
                typeof(decimal),
                typeof(ValueProp),
                typeof(Creature),
                typeof(CardModel),
            ]
        );

        if (mainMethod != null)
        {
            return (ctx, target, amount, props) =>
                (Task<IEnumerable<DamageResult>>)
                    mainMethod.Invoke(null, [ctx, target, amount, props, null, null])!;
        }

        throw new InvalidOperationException(
            "No compatible CreatureCmd.Damage overload found in sts2.dll!"
        );
    }
}
