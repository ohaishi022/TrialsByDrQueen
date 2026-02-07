using System.Collections;
using UnityEngine;
using CS.AudioToolkit;
using System.Linq;

public class Skill_LeechLaser : Skill_Base
{
    [Header("Laser")]
    public float maxRangeTiles = 10f;
    public float tickInterval = 0.2f;
    public float tickDamage = 10f;
    public float chargeTime = 1f;
    public float fireDuration = 4f;
    public LayerMask blockMask;

    [Header("FX")]
    public GameObject laserEffectPrefab;
    public GameObject laserEndEffectPrefab;
    public GameObject laserHealEffect;

    [Header("Audio")]
    public string SE_Start = "SE_Skill_LeechLaser";
    public string SE_Beam = "SE_Skill_LeechLaser_Beam";

    private LineRenderer lr;
    private GameObject beamFX;
    private GameObject endFX;

    private void Awake()
    {
        cooldownTime = 14f;

        if (!laserEndEffectPrefab)
            laserEndEffectPrefab =
                Resources.Load<GameObject>("Prefab/Skill/Human/Vumpire/Skill_LeechLaser_End");

        if (!laserHealEffect)
            laserHealEffect =
                Resources.Load<GameObject>("Prefab/Skill/Human/Vumpire/Skill_LeechLaser_Heal");
    }

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        Unit_Base u = user ?? owner;
        if (!u) yield break;

        isActive = true;
        u.stopMoving = true;

        // 시전 시작
        PlaySE(SE_Start, u.transform.position);

        SpawnBeamFX(u);

        // 차징
        yield return WaitScaled(chargeTime);

        u.stopMoving = false;

        // 발사 시작
        PlaySE(SE_Start, u.transform.position);

        SpawnEndFX(u);

        float fireTime = 0f;
        float tickTimer = 0f;
        bool healedThisTick = false;

        while (fireTime < fireDuration)
        {
            UpdateLaser(u, out Unit_Base hitEnemy, out bool blocked);

            float dt = GetSkillDeltaTime();
            fireTime += dt;
            tickTimer += dt;

            if (tickTimer >= tickInterval)
            {
                tickTimer -= tickInterval;
                healedThisTick = false;

                if (hitEnemy != null && !blocked)
                {
                    hitEnemy.TakeDamage(tickDamage);

                    bool canHeal =
                        !hitEnemy.CategoryHas(UnitCategory.Object) &&
                        !hitEnemy.HasBuff(BuffId.Invincible) &&
                        !hitEnemy.HasSpeical(Speical.Disappear);

                    if (canHeal && !healedThisTick)
                    {
                        healedThisTick = true;
                        u.TakeHeal(5f);

                        if (laserHealEffect)
                            Instantiate(laserHealEffect, u.transform.position,
                                Quaternion.identity, u.transform);
                    }
                }
            }

            yield return null;
        }

        Cleanup();
        isActive = false;
        yield return StartCooldown();
    }

    // ===================== FX =====================

    private void SpawnBeamFX(Unit_Base u)
    {
        if (!laserEffectPrefab) return;

        beamFX = Instantiate(laserEffectPrefab, u.transform);

        var se = beamFX.GetComponent<SkillEffect>();
        if (se) se.owner = u;

        lr = beamFX.GetComponent<LineRenderer>();
        if (lr)
        {
            lr.enabled = false;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
        }
    }

    private void SpawnEndFX(Unit_Base u)
    {
        if (!laserEndEffectPrefab) return;

        endFX = Instantiate(laserEndEffectPrefab);
        var se = endFX.GetComponent<SkillEffect>();
        if (se) se.owner = u;

        if (lr) lr.enabled = true;
    }

    private void UpdateLaser(Unit_Base u, out Unit_Base enemyHit, out bool blocked)
    {
        enemyHit = null;
        blocked = false;
        if (!lr || !u) return;

        Vector2 dir =
            Mathf.Abs(u.DirectionVector.x) >= Mathf.Abs(u.DirectionVector.y)
            ? new Vector2(Mathf.Sign(u.DirectionVector.x == 0 ? 1 : u.DirectionVector.x), 0f)
            : new Vector2(0f, Mathf.Sign(u.DirectionVector.y == 0 ? 1 : u.  DirectionVector.y));

        Vector2 origin = u.transform.position;
        float maxDist = maxRangeTiles;

        RaycastHit2D[] hits =
            Physics2D.RaycastAll(origin + dir * 0.01f, dir, maxDist);

        float wallDist = float.MaxValue;
        float enemyDist = float.MaxValue;

        foreach (var h in hits)
        {
            if ((blockMask.value & (1 << h.collider.gameObject.layer)) != 0)
                wallDist = Mathf.Min(wallDist, h.distance);

            var ub = h.collider.GetComponent<Unit_Base>();
            if (ub && ub != u && ub.unitType != u.unitType)
                enemyDist = Mathf.Min(enemyDist, h.distance);
        }

        Vector2 end = origin + dir * maxDist;

        if (enemyDist < wallDist)
        {
            enemyHit = hits
                .Select(h => h.collider.GetComponent<Unit_Base>())
                .FirstOrDefault(ub => ub && ub.unitType != u.unitType);

            end = origin + dir * enemyDist;
        }
        else if (wallDist < float.MaxValue)
        {
            blocked = true;
            end = origin + dir * wallDist;
        }

        lr.SetPosition(0, new Vector3(origin.x, origin.y + 0.5f, origin.y * 0.01f));
        lr.SetPosition(1, new Vector3(end.x, end.y, end.y * 0.01f));

        if (endFX)
            endFX.transform.position =
                new Vector3(end.x, end.y, end.y * 0.01f);
    }

    private void Cleanup()
    {
        AudioController.Stop(SE_Beam);

        if (lr) lr.enabled = false;
        if (beamFX) Destroy(beamFX);
        if (endFX) Destroy(endFX);

        lr = null;
        beamFX = null;
        endFX = null;
    }

    protected override void OnCanceled()
    {
        base.OnCanceled();
        Cleanup();

        if (owner)
            owner.stopMoving = false;
    }
}
