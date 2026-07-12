namespace ModifiersExpanded.ModifiersExpandedCode.State;

public class EnemyScalingState
{
    public static EnemyScalingState Instance { get; } = new EnemyScalingState();

    public EnemyScalingState()
    {
        DamageMultiplier = 1.0f;
        NumAdditionalPlayers = 0;
    }

    public float DamageMultiplier { get; set; }
    public int NumAdditionalPlayers { get; set; }

    public void Reset()
    {
        DamageMultiplier = 1.0f;
        NumAdditionalPlayers = 0;
    }
}
