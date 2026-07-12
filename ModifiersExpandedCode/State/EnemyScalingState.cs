namespace ModifiersExpanded.ModifiersExpandedCode.State;

public class EnemyScalingState
{
    public static EnemyScalingState Instance { get; } = new EnemyScalingState();

    public EnemyScalingState()
    {
        Health = 1.0f;
        Damage = 1.0f;
    }

    public float Health { get; set; }
    public float Damage { get; set; }
}
