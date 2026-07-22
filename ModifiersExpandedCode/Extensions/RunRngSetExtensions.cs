using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace ModifiersExpanded.ModifiersExpandedCode.Extensions;

public static class RunRngSetExtensions
{
    private static readonly PropertyInfo? _seedProperty = AccessTools.Property(
        typeof(RunRngSet),
        "Seed"
    );

    private static readonly ConstructorInfo? _rngCtorUint = AccessTools.Constructor(
        typeof(Rng),
        [typeof(uint), typeof(string)]
    );

    private static readonly ConstructorInfo? _rngCtorUlong = AccessTools.Constructor(
        typeof(Rng),
        [typeof(ulong), typeof(string)]
    );

    /// <summary>
    /// Reads RunRngSet.Seed in a branch-compatible way.
    /// Supports both sts2 main (uint) and sts2-beta (ulong).
    /// </summary>
    public static ulong GetSeedCompat(this RunRngSet runRngSet)
    {
        object? seedObj = _seedProperty?.GetValue(runRngSet);
        return seedObj switch
        {
            uint seed => seed,
            ulong seed => seed,
            _ => throw new InvalidOperationException(
                "Could not read a compatible RunRngSet.Seed value from sts2.dll."
            ),
        };
    }

    /// <summary>
    /// Creates a named Rng from RunRngSet.Seed in a branch-compatible way.
    /// Supports both sts2 main (Rng(uint, string)) and sts2-beta (Rng(ulong, string)).
    /// </summary>
    public static Rng CreateNamedRngCompat(this RunRngSet runRngSet, string name)
    {
        object? seedObj = _seedProperty?.GetValue(runRngSet);

        if (seedObj is uint seedUint && _rngCtorUint != null)
            return (Rng)_rngCtorUint.Invoke([seedUint, name]);

        if (seedObj is ulong seedUlong && _rngCtorUlong != null)
            return (Rng)_rngCtorUlong.Invoke([seedUlong, name]);

        throw new InvalidOperationException(
            "Could not find a compatible Rng(seed, name) constructor for the current sts2.dll."
        );
    }
}
