using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Unit_Base))]
public class Unit_Status : MonoBehaviour
{
    private Unit_Base owner;
    private readonly Dictionary<string, Buff_Base> _buffs = new();

    // 집계 결과(매 프레임 계산)
    public bool CanMove { get; private set; } = true;
    public bool CanCast { get; private set; } = true;
    public float SpeedMul { get; private set; } = 1f;
    public float DamageTakenMul { get; private set; } = 1f;

    public IEnumerable<Buff_Base> All => _buffs.Values;

    public void Initialize(Unit_Base unit)
    {
        owner = unit;
    }

    public bool Has(string id) => _buffs.ContainsKey(id);

    public T Get<T>(string id) where T : Buff_Base
        => _buffs.TryGetValue(id, out var b) ? b as T : null;

    public void Add(Buff_Base buff)
    {
        if (buff == null || owner == null) return;

        if (_buffs.TryGetValue(buff.Id, out var existing))
        {
            ApplyStack(existing, buff);
            RebuildAggregate();
            return;
        }

        _buffs[buff.Id] = buff;
        buff.OnApply(owner);
        RebuildAggregate();
    }

    private void ApplyStack(Buff_Base existing, Buff_Base incoming)
    {
        switch (incoming.StackRule)
        {
            case BuffStackRule.IgnoreIfExists:
                return;

            case BuffStackRule.Replace:
                existing.OnRemove(owner);
                _buffs[incoming.Id] = incoming;
                incoming.OnApply(owner);
                return;

            case BuffStackRule.AddDuration:
                if (existing.IsInfinite) return;
                if (incoming.IsInfinite) { existing.ForceSetRemaining(-1f); return; }

                existing.ForceAddRemaining(incoming.Remaining);
                existing.ForceSetIntensity(Mathf.Max(existing.Intensity, incoming.Intensity));
                return;

            case BuffStackRule.AddIntensity:
                existing.ForceAddIntensity(incoming.Intensity);
                existing.MergeFrom(incoming);
                return;

            case BuffStackRule.RefreshDuration:
            default:
                existing.MergeFrom(incoming);
                existing.ForceSetIntensity(Mathf.Max(existing.Intensity, incoming.Intensity));
                return;
        }
    }


    public void Remove(string id)
    {
        if (!_buffs.TryGetValue(id, out var b)) return;
        b.OnRemove(owner);
        _buffs.Remove(id);
        RebuildAggregate();
    }

    public void NotifyDamaged(in DamageInfo info)
    {
        if (owner == null) return;
        foreach (var b in _buffs.Values)
            b.OnDamaged(owner, info);
    }

    private void Update()
    {
        if (owner == null) return;

        float dt = Time.deltaTime;

        bool anyRemoved = false;

        // Tick + 만료 제거
        var keys = ListPool<string>.Get();
        foreach (var kv in _buffs) keys.Add(kv.Key);

        foreach (var id in keys)
        {
            if (!_buffs.TryGetValue(id, out var b)) continue;

            b.Tick(owner, dt);

            if (b.IsExpired)
            {
                b.OnRemove(owner);
                _buffs.Remove(id);
                anyRemoved = true;
            }
        }
        ListPool<string>.Release(keys);

        // 매 프레임 집계(버프가 없어도 값이 변할 수 있어)
        if (anyRemoved) RebuildAggregate();
        else RebuildAggregate(); // 최적화하려면 dirty 플래그로 바꿔도 됨
    }

    private void RebuildAggregate()
    {
        bool canMove = true;
        bool canCast = true;

        float slowMul = 1f;   // 1에 가까울수록 정상, 0.5면 50% 슬로우
        float hasteMul = 1f;  // 1 이상만 의미 있게 (1.2면 20% 가속)
        float dmgMul = 1f;

        foreach (var b in _buffs.Values)
        {
            if (b.BlocksMove) canMove = false;
            if (b.BlocksCast) canCast = false;

            float sm = b.SpeedMultiplier;

            // 슬로우는 "가장 강한 것만" (최소 배율)
            if (sm < 1f) slowMul = Mathf.Min(slowMul, sm);
            // 헤이스트는 "가장 큰 것만" (최대 배율)
            else if (sm > 1f) hasteMul = Mathf.Max(hasteMul, sm);

            dmgMul *= b.DamageTakenMultiplier;
        }

        CanMove = canMove;
        CanCast = canCast;
        SpeedMul = Mathf.Clamp(slowMul * hasteMul, 0f, 99f);
        DamageTakenMul = Mathf.Clamp(dmgMul, 0f, 99f);
    }

    // 간단한 풀(매 프레임 foreach 할당 줄이기)
    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new();
        public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>(16);
        public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
    }
}
