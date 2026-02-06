using UnityEngine;

public class Buff_SpeedBoost : Buff_Base
{
    public override string Id => "speedboost"; // 버프 고유 ID
    public override BuffType type => BuffType.Buff;

    // 동일한 배속 버프가 걸릴 경우 지속시간을 갱신하도록 설정
    public override BuffStackRule StackRule => BuffStackRule.RefreshDuration;

    private float speedMul;

    /// <param name="duration">지속 시간</param>
    /// <param name="multiplier">배속 값 (예: 1.5f)</param>
    public Buff_SpeedBoost(float duration, float multiplier) : base(duration)
    {
        speedMul = multiplier;
    }

    // Unit_Status에서 속도를 계산할 때 참조함
    public override float SpeedMultiplier => speedMul;
}