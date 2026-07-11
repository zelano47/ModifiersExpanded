namespace ModifiersExpanded.ModifiersExpandedCode.State;

public class EnemyScalingState
{
    public static EnemyScalingState Instance { get; } = new EnemyScalingState();

    public EnemyScalingState()
    {
        HealthMultiplier = 1.0f;
        DamageMultiplier = 1.0f;
        NumPlayers = 1;
    }

    public float HealthMultiplier { get; set; }
    public float DamageMultiplier { get; set; }
    public int NumPlayers { get; set; }
}
