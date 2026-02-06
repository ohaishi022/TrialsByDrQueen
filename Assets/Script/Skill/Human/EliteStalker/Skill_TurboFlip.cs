using System.Collections;
using UnityEngine;
using CS.AudioToolkit;

public class Skill_TurboFlip : Skill_Base
{
    [Header("Skill Settings")]
    public float invincibleDuration = 1f;
    public float buffDuration = 7f; // 본인용 지속시간
    public float speedMultiplier = 1.5f;
    public float buffRadius = 5f;

    [Header("FX / Audio")]
    public GameObject flipEffect;
    public string SE_Flip = "";

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        Unit_Base u = user ?? owner;
        if (u == null) yield break;

        isActive = true;

        if (!string.IsNullOrEmpty(SE_Flip))
            AudioController.Play(SE_Flip, u.transform.position);

        if (flipEffect != null)
            Instantiate(flipEffect, u.transform.position, Quaternion.identity, u.transform);

        // 1. 자신에게 무적 부여
        if (u.Status != null)
            u.Status.Add(new Buff_Invincible(invincibleDuration));

        // 2. 주변 아군 및 자신에게 배속 버프 부여
        ApplySpeedBuffToAllies(u);

        isActive = false;
        yield return StartCooldown(); // 쿨타임 시작
    }

    private void ApplySpeedBuffToAllies(Unit_Base u)
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(u.transform.position, buffRadius);

        foreach (var col in targets)
        {
            Unit_Base targetUnit = col.GetComponent<Unit_Base>();

            // 팀 확인 (동일한 UnitType인지 체크)
            if (targetUnit != null && targetUnit.unitType == u.unitType)
            {
                if (targetUnit.Status != null)
                {
                    // 본인과 타인의 지속시간 차등 적용
                    float duration = (targetUnit == u) ? buffDuration : (buffDuration * 0.5f);

                    // Buff_SpeedBuff 추가
                    targetUnit.Status.Add(new Buff_SpeedBoost(duration, speedMultiplier));
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = (owner != null) ? owner.transform.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, buffRadius);
    }
}