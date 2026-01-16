using CS.AudioToolkit;
using UnityEngine;

public class Debuff_Chill : Buff_Base
{
    public override string Id => "chill";
    public override BuffType type => BuffType.Debuff;

    // 동일 Chill 다시 걸리면 지속시간 갱신
    public override BuffStackRule StackRule => BuffStackRule.RefreshDuration;

    private float slowMul;

    /// <param name="duration">지속시간 (초, <=0 이면 무한)</param>
    /// <param name="slowMultiplier">속도 배율 (0.5 = 50% 슬로우)</param>
    public Debuff_Chill(float duration, float slowMultiplier = 0.5f)
        : base(duration)
    {
        slowMul = Mathf.Clamp(slowMultiplier, 0.05f, 1f);
    }

    public override float SpeedMultiplier => slowMul;

    public override void OnApply(Unit_Base unit)
    {
        base.OnApply(unit);

        AudioController.Play("SE_Debuff_IceBlock", unit.transform.position);//Chill
        // 필요하면 여기서 이펙트 / 아이콘 / 사운드
        // ex) unit.PlayStatusFX("Chill");
    }

    public override void OnRemove(Unit_Base unit)
    {
        base.OnRemove(unit);

        // 해제 연출
    }
}
