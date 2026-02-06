public class Debuff_Freeze : Buff_Base
{
    public override string Id => "freeze";
    public override BuffType type => BuffType.Debuff;
    public override BuffStackRule StackRule => BuffStackRule.RefreshDuration;
    public Debuff_Freeze(float duration) : base(duration) { }
    public override bool BlocksMove => true;
    public override bool BlocksCast => true;
    public override float SpeedMultiplier => 0f;
}