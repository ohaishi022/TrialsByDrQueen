public class Buff_Invincible : Buff_Base
{
    public override string Id => "invincible";
    public override BuffType type => BuffType.Buff;
    public override BuffStackRule StackRule => BuffStackRule.RefreshDuration;

    public Buff_Invincible(float duration) : base(duration) { }
}