public enum DamageType
{
    Normal,
    Fire,
    Ice,
    Poison,
    True
}

public readonly struct DamageInfo
{
    public readonly float Amount;
    public readonly DamageType Type;
    public readonly object Source;

    public DamageInfo(float amount, DamageType type = DamageType.Normal, object source = null)
    {
        Amount = amount;
        Type = type;
        Source = source;
    }
}
