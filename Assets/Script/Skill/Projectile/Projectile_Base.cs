using UnityEngine;
using CS.AudioToolkit;
using System.Collections;
using System.Collections.Generic;

public abstract class Projectile_Base : MonoBehaviour
{
    [Header("Identity")]
    public GameObject shooter;
    public UnitType positiveUnitType;
    protected UnitType negativeUnitType;
    public Animator animator;

    [Header("Kinetics")]
    public Vector2 direction;
    public float speed = 18.75f;
    public float range = 7f;
    public float lifetime = 2f;
    public float damage = 20f;
    //public float healAmount;

    [Header("Targeting")]
    public bool affectEnemies = true;
    public bool affectAllies = false;

    [Header("Pierce")]
    [Tooltip("관통 가능 횟수 (0 = 무한 관통)")]
    public int pierceCount = 1;
    protected int remainingPierce;

    [Header("Homing")]
    public bool isHoming = false;
    public float homingRadius = 2.5f;
    public float homingTurnSpeed = 6f;
    public LayerMask unitLayerMask;
    protected Transform homingTarget;

    [Header("Explosion")]
    public bool canExplode = false;
    public float explosionRadius = 3f;
    public float explosionDamage = 0f;

    protected Vector2 startPosition;
    protected float elapsedTime;
    protected HashSet<GameObject> hitList = new();

    [Header("Audio")]
    public string[] SE_Start;     // 발사 시
    public string[] SE_Hit;       // 적중 시
    public string[] SE_Destroy;   // 소멸 시

    [Header("Effects")]
    public GameObject hitEffect;       // 적중 이펙트
    public GameObject destroyEffect;   // 소멸 이펙트

    protected virtual void Awake()
    {
        negativeUnitType =
            (positiveUnitType == UnitType.Human) ? UnitType.Zombie : UnitType.Human;

        startPosition = transform.position;

        remainingPierce = pierceCount;

        if (SE_Start != null)
            foreach (string clip in SE_Start) AudioController.Play(clip, transform.position);
    }

    protected virtual void Update()
    {
        if (isHoming)
            UpdateHoming();

        Move();
        UpdateLifetime();

        UpdateAnimationDirection();
    }

    protected virtual void UpdateAnimationDirection()
    {
        if (!animator) return;

        Vector2 dir = direction.normalized;

        animator.SetFloat("X", dir.x);
        animator.SetFloat("Y", dir.y);
    }

    protected virtual void Move()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        if (Vector2.Distance(startPosition, transform.position) >= range)
            DestroySelf();
    }

    protected virtual void UpdateLifetime()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= lifetime)
            DestroySelf();
    }

    protected virtual void SpawnHitEffect(Vector3 pos)
    {
        if (hitEffect)
            Instantiate(hitEffect, pos, Quaternion.identity);
    }

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        GameObject target = col.gameObject;

        if (col.gameObject.layer == LayerMask.NameToLayer("Projectile"))
            return;

        if (col.TryGetComponent<Projectile_Base>(out _))
            return;

        if (target == shooter || hitList.Contains(target))
            return;

        if (SE_Hit != null)
            foreach (string clip in SE_Hit) AudioController.Play(clip, transform.position);

        Unit_Base unit = target.GetComponent<Unit_Base>();
        if (unit == null)
        {
            OnHitEnvironment(col);
            return;
        }

        bool isEnemy = unit.unitType == negativeUnitType;
        bool isAlly = unit.unitType == positiveUnitType;

        if (isEnemy && affectEnemies)
        {
            hitList.Add(target);

            if (damage > 0f)
                unit.TakeDamage(damage);

            OnHitEnemy(unit, col);
            SpawnHitEffect(col.ClosestPoint(transform.position));
            ConsumePierce();
        }
        else if (isAlly && affectAllies)
        {
            hitList.Add(target);
            //if (damage > 0f)
            //    unit.TakeHeal(damage);
            OnHitAlly(unit, col);
            ConsumePierce();
        }
    }

    protected void ConsumePierce()
    {
        // 0 = 무한 관통
        if (pierceCount == 0)
            return;

        remainingPierce--;

        if (remainingPierce <= 0)
        {
            DestroySelf();
        }
    }

    protected abstract void OnHitEnemy(Unit_Base unit, Collider2D col);
    protected virtual void OnHitAlly(Unit_Base unit, Collider2D col) { }
    protected virtual void OnHitEnvironment(Collider2D col)
    {
        DestroySelf();
    }
    protected virtual void UpdateHoming()
    {
        if (homingTarget == null)
        {
            SearchHomingTarget();
            return;
        }

        Unit_Base u = homingTarget.GetComponent<Unit_Base>();
        if (u == null || u.currentHealth <= 0 || !IsValidHomingTarget(u))
        {
            homingTarget = null;
            return;
        }

        Vector2 toTarget =
            ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;

        direction =
            Vector2.Lerp(direction, toTarget,
                Time.deltaTime * homingTurnSpeed).normalized;
    }

    protected virtual void SearchHomingTarget()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(transform.position, homingRadius, unitLayerMask);

        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            Unit_Base u = h.GetComponent<Unit_Base>();
            if (u == null || u.currentHealth <= 0) continue;
            if (!IsValidHomingTarget(u)) continue;

            float d = Vector2.Distance(transform.position, u.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = u.transform;
            }
        }

        homingTarget = best;
    }

    protected virtual bool IsValidHomingTarget(Unit_Base u)
    {
        if (affectEnemies && u.unitType == negativeUnitType)
            return true;

        if (affectAllies && u.unitType == positiveUnitType)
            return true;

        return false;
    }
    protected virtual void Explode()
    {
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(transform.position, explosionRadius, unitLayerMask);

        foreach (var h in hits)
        {
            Unit_Base u = h.GetComponent<Unit_Base>();
            if (u == null) continue;

            if (affectEnemies && u.unitType == negativeUnitType)
            {
                u.TakeDamage(explosionDamage);
            }

            if (affectAllies && u.unitType == positiveUnitType)
            {
                u.TakeHeal(explosionDamage); // 필요 시 분리
            }
        }
    }

    bool isDestroyed;
    protected virtual void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (canExplode)
            Explode();
        if (SE_Destroy != null)
            foreach (string clip in SE_Destroy) AudioController.Play(clip, transform.position);
        if (destroyEffect)
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (isHoming)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, homingRadius);
        }

        if (canExplode)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
