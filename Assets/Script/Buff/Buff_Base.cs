using System;
using UnityEngine;

public enum BuffType
{
    Buff,
    Debuff
}

public enum BuffStackRule
{
    RefreshDuration,     // 동일 버프 재적용 시 남은시간 갱신(더 긴 쪽/새 값으로)
    AddDuration,         // 지속시간 누적
    AddIntensity,        // 강도 누적(중첩)
    Replace,             // 완전 교체
    IgnoreIfExists,      // 이미 있으면 무시
}

public abstract class Buff_Base
{
    public abstract string Id { get; }             // 고유 ID (예: "stun", "poison")
    public abstract BuffType type { get; }
    public virtual BuffStackRule StackRule => BuffStackRule.RefreshDuration;

    /// <summary>남은 시간. duration < 0 이면 무한 지속</summary>
    public float Remaining { get; protected set; }

    /// <summary>강도/수치 (예: 독 DPS, 화상 DPS, 슬로우 배율 등)</summary>
    public float Intensity { get; protected set; }

    public bool IsInfinite => Remaining < 0f;
    public bool IsExpired => !IsInfinite && Remaining <= 0f;

    protected Unit_Base owner;

    protected Buff_Base(float duration, float intensity = 0f)
    {
        Remaining = (duration > 0f) ? duration : -1f;
        Intensity = intensity;
    }

    /// <summary>처음 적용될 때 1회</summary>
    public virtual void OnApply(Unit_Base unit)
    {
        owner = unit;
    }

    /// <summary>해제될 때 1회</summary>
    public virtual void OnRemove(Unit_Base unit) { }

    /// <summary>매 프레임</summary>
    public virtual void Tick(Unit_Base unit, float dt)
    {
        if (!IsInfinite) Remaining -= dt;
    }

    /// <summary>재적용(중첩/갱신 처리)</summary>
    public virtual void MergeFrom(Buff_Base other)
    {
        // 기본 규칙: duration 더 긴 걸로 갱신
        if (IsInfinite) return;
        if (other.IsInfinite) { Remaining = -1f; return; }

        Remaining = Mathf.Max(Remaining, other.Remaining);
        // intensity는 기본 유지(원하면 자식에서 override)
    }

    // === 상태 집계용 훅 ===
    public virtual bool BlocksMove => false;     // 기절/빙결 같은 CC
    public virtual bool BlocksCast => false;     // 침묵 같은 거
    public virtual float SpeedMultiplier => 1f;  // 슬로우/헤이스트
    public virtual float DamageTakenMultiplier => 1f; // 받피증/감

    // ===== Internal mutators (for Unit_Status stacking) =====
    public void ForceSetRemaining(float remaining)
    {
        Remaining = remaining;
    }

    public void ForceAddRemaining(float delta)
    {
        if (IsInfinite) return;
        if (delta <= 0f) return;
        Remaining += delta;
    }

    public void ForceSetIntensity(float intensity)
    {
        Intensity = intensity;
    }

    public void ForceAddIntensity(float delta)
    {
        Intensity += delta;
    }

    public virtual void OnDamaged(Unit_Base unit, in DamageInfo info) { }
}
