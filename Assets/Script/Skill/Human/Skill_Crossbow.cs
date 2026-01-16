using UnityEngine;
using System.Collections;

public class Skill_Crossbow : Skill_Base
{
    [Header("Projectile")]
    public GameObject projectilePrefab;

    public float projectileSpeed = 18.75f;
    public float projectileRange = 7f;
    public float projectileLifetime = 2f;
    public float projectileDamage = 20f;

    public int pierceCount = 0;

    private void Awake()
    {
        cooldownTime = 0.6f;
        canDeactivate = false;
    }

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        Unit_Base u = user ?? owner;
        if (u == null)
        {
            isActive = false;
            yield break;
        }

        FireProjectile(u);

        isActive = false;
        skillRoutine = null;
        yield return StartCooldown();
    }

    private void FireProjectile(Unit_Base u)
    {
        Vector3 spawnPos = u.SkillPosition;

        GameObject go =
            Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        Projectile_Base p = go.GetComponent<Projectile_Base>();
        if (p == null)
        {
            Debug.LogError("Skill_Crossbow: projectilePrefab에 Projectile_Base가 없음");
            Destroy(go);
            return;
        }

        // === 기본 세팅 ===
        p.shooter = u.gameObject;
        p.positiveUnitType = u.unitType;
        p.direction = u.DirectionVector.normalized;

        // === 스펙 전달 ===
        p.speed = projectileSpeed;
        p.range = projectileRange;
        p.lifetime = projectileLifetime;
        p.pierceCount = pierceCount;
        p.positiveUnitType = u.unitType;
        //p.damage = projectileDamage;
    }
}
