using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Skill_Base : MonoBehaviour
{
    /* ───────────── Owner / State ───────────── */
    protected Unit_Base owner;                 // 스킬 소유 유닛
    protected Coroutine skillRoutine;          // 실행 중인 코루틴 핸들

    public bool isActive;                      // 스킬 활성 여부
    public bool canDeactivate = false;         // 입력으로 해제 가능한가

    /* ───────────── Cooldown ───────────── */
    public float cooldownTime = 1f;
    public bool isOnCooldown;

    /* ───────────── Effects / Events ───────────── */
    public GameObject activeEffect;            // 유지형 이펙트
    public event Action OnSkillCanceled;       // 강제 취소 이벤트

    /* ───────────── Summon 관리 ───────────── */
    protected List<Unit_Base> summonedUnits = new List<Unit_Base>();
    public IReadOnlyList<Unit_Base> SummonedUnits => summonedUnits;

    /* ───────────── Public API ───────────── */

    public virtual void ActivateSkill(Unit_Base user)
    {
        if (isActive || isOnCooldown)
            return;

        owner = user;
        isActive = true;

        skillRoutine = StartCoroutine(SkillRoutine(user));
    }

    /// <summary>
    /// 입력 해제 등으로 "자연 종료" 요청
    /// (토글 / 홀드형 스킬)
    /// </summary>
    public virtual void DeactivateSkill()
    {
        if (!canDeactivate)
            return;

        isActive = false;
    }

    /// <summary>
    /// 강제 취소 (사망 / 상태이상 / 컷신 등)
    /// </summary>
    public virtual void ForceCancel(bool startCooldown = false)
    {
        if (!isActive && skillRoutine == null)
            return;

        isActive = false;

        if (skillRoutine != null)
        {
            StopCoroutine(skillRoutine);
            skillRoutine = null;
        }

        OnCanceled();

        if (startCooldown && !isOnCooldown)
            StartCoroutine(StartCooldown());

        OnSkillCanceled?.Invoke();
    }

    /* ───────────── Override Hooks ───────────── */

    /// <summary>
    /// 강제 취소 시 정리 로직
    /// (이펙트 제거, 사운드 중지 등)
    /// </summary>
    protected virtual void OnCanceled()
    {
        if (activeEffect != null)
            activeEffect.SetActive(false);
    }

    /// <summary>
    /// 실제 스킬 동작 코루틴
    /// (즉발이면 yield return null 1회로 끝내도 됨)
    /// </summary>
    protected abstract IEnumerator SkillRoutine(Unit_Base user);

    /* ───────────── Cooldown ───────────── */

    protected virtual IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        yield return WaitScaled(cooldownTime);
        isOnCooldown = false;
    }

    /* ───────────── Time (배속 / Freeze 대응) ───────────── */

    /// <summary>
    /// 유닛 배속이 반영된 deltaTime
    /// Freeze(SpeedMultiplier = 0)면 0 반환
    /// </summary>
    protected float GetSkillDeltaTime()
    {
        if (owner == null)
            return Time.deltaTime;

        return Time.deltaTime * Mathf.Max(0f, owner.SpeedMultiplier);
    }

    /// <summary>
    /// 배속 대응 대기
    /// Freeze 상태면 정지
    /// </summary>
    protected IEnumerator WaitScaled(float duration)
    {
        if (duration <= 0f)
            yield break;

        float t = 0f;

        while (t < duration)
        {
            float dt = GetSkillDeltaTime();

            // ❄️ Freeze → 멈춤
            if (dt > 0f)
                t += dt;

            yield return null;
        }
    }

    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }

    public void StartInitialCooldown()
    {
        if (!isOnCooldown)
            StartCoroutine(StartCooldown());
    }

    /* ───────────── Summon Helpers ───────────── */

    protected Unit_Base SummonUnit(
        GameObject prefab,
        Vector3 position,
        Transform parent = null,
        bool inheritUnitType = true)
    {
        if (prefab == null)
            return null;

        GameObject go = Instantiate(prefab, position, Quaternion.identity, parent);
        Unit_Base summoned = go.GetComponent<Unit_Base>();
        if (summoned == null)
            return null;

        if (inheritUnitType && owner != null)
            summoned.unitType = owner.unitType;

        summonedUnits.Add(summoned);

        // 자동 제거
        summoned.OnDestroyed += () =>
        {
            summonedUnits.Remove(summoned);
        };

        return summoned;
    }

    protected void DespawnAllSummons()
    {
        foreach (var u in summonedUnits)
        {
            if (u != null)
                u.DestroyUnit(true);
        }
        summonedUnits.Clear();
    }
}
