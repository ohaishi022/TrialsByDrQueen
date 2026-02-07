using UnityEngine;
using CS.AudioToolkit;
using System.Collections;
using System.Collections.Generic;

public abstract class Projectile_Base : MonoBehaviour
{
    [Header("Identity")]
    public GameObject shooter;
    public UnitType positiveUnitType;
    //protected UnitType negativeUnitType;
    public Animator animator;

    [Header("Kinetics")]
    public Vector2 direction;
    public float speed;
    public float range;
    public float lifetime;
    public float damage;
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
    bool isDestroyed;

    [Header("Audio")]
    public string[] SE_Start;     // 발사 시
    public string[] SE_Hit;       // 적중 시
    public string[] SE_Destroy;   // 소멸 시

    [Header("Effects")]
    public GameObject hitEffect;       // 적중 이펙트
    public GameObject destroyEffect;   // 소멸 이펙트

    public virtual void Init(GameObject shooterObj, Vector2 dir, float spd, UnitType shooterType)
    {
        shooter = shooterObj;
        positiveUnitType = shooterType;

        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        speed = spd;

        // 발사자와 충돌 무시 (자기/아군 히트박스에 걸리는 문제 확실히 차단)
        IgnoreShooterCollisions();
        UpdateAnimationDirection();
    }


    protected virtual void Awake()
    {
        startPosition = transform.position;

        remainingPierce = pierceCount;

        if (SE_Start != null)
            foreach (string clip in SE_Start) PlaySE(clip, transform.position);
    }

    protected virtual void Start()
    {
        UpdateAnimationDirection();
    }

    protected virtual void Update()
    {
        if (isHoming)
            UpdateHoming();

        Move();
        UpdateLifetime();
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

    protected UnitType GetNegative(UnitType positive)
    {
        return (positive == UnitType.Human) ? UnitType.Zombie : UnitType.Human;
    }

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if (isDestroyed) return;

        if (col.gameObject.layer == LayerMask.NameToLayer("Projectile"))
            return;
        if (col.TryGetComponent<Projectile_Base>(out _))
            return;

        Unit_Base unit = col.GetComponentInParent<Unit_Base>();

        if (unit == null)
        {
            OnHitEnvironment(col);
            return;
        }

        if (shooter != null)
        {
            Unit_Base shooterUnit = shooter.GetComponentInParent<Unit_Base>();
            if (shooterUnit != null && unit == shooterUnit)
                return;
        }

        GameObject targetRoot = unit.gameObject;
        if (hitList.Contains(targetRoot))
            return;

        if (SE_Hit != null)
            foreach (string clip in SE_Hit)
                PlaySE(clip, transform.position);

        UnitType negative = GetNegative(positiveUnitType);
        bool isEnemy = (unit.unitType == negative);
        bool isAlly = (unit.unitType == positiveUnitType);


        if (isEnemy && affectEnemies)
        {
            hitList.Add(targetRoot);

            if (damage > 0f)
                unit.TakeDamage(damage);

            OnHitEnemy(unit, col);
            SpawnHitEffect(col.ClosestPoint(transform.position));
            ConsumePierce();
        }
        else if (isAlly && affectAllies)
        {
            hitList.Add(targetRoot);

            // 필요하면 healAmount 따로 두는 걸 추천
            if (damage > 0f)
                unit.TakeHeal(damage);

            OnHitAlly(unit, col);
            SpawnHitEffect(col.ClosestPoint(transform.position));
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
            DestroySelf();
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

        Vector2 toTarget = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
        direction = Vector2.Lerp(direction, toTarget, Time.deltaTime * homingTurnSpeed).normalized;

        UpdateAnimationDirection();
    }

    protected virtual void SearchHomingTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, homingRadius, unitLayerMask);

        Transform best = null;
        float bestDist = float.MaxValue;


        foreach (var h in hits)
        {
            Unit_Base u = h.GetComponentInParent<Unit_Base>();
            if (u == null || u.currentHealth <= 0) continue;
            if (!IsValidHomingTarget(u)) continue;

            // 자기 자신 제외
            if (shooter != null)
            {
                var shooterUnit = shooter.GetComponentInParent<Unit_Base>();
                if (shooterUnit != null && u == shooterUnit) continue;
            }

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
        UnitType negative = GetNegative(positiveUnitType);

        if (affectEnemies && u.unitType == negative) return true;
        if (affectAllies && u.unitType == positiveUnitType) return true;
        return false;
    }
    protected virtual void Explode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, unitLayerMask);

        UnitType negative = GetNegative(positiveUnitType);

        foreach (var h in hits)
        {
            Unit_Base u = h.GetComponentInParent<Unit_Base>();
            if (u == null) continue;

            if (affectEnemies && u.unitType == negative)
                u.TakeDamage(explosionDamage);

            if (affectAllies && u.unitType == positiveUnitType)
                u.TakeHeal(explosionDamage);
        }
    }

    protected void IgnoreShooterCollisions()
    {
        if (!shooter) return;

        var myCols = GetComponentsInChildren<Collider2D>(true);
        var shooterCols = shooter.GetComponentsInChildren<Collider2D>(true);

        foreach (var a in myCols)
            foreach (var b in shooterCols)
                Physics2D.IgnoreCollision(a, b, true);
    }

    protected virtual void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;

        if (canExplode)
            Explode();
        if (SE_Destroy != null)
            foreach (string clip in SE_Destroy) AudioController.Play(clip, transform.position);
        if (destroyEffect)
            Instantiate(destroyEffect, transform.position, Quaternion.identity);

        HandleChildEffects();
        Destroy(gameObject);
    }

    void HandleChildEffects()
    {
        foreach (Transform child in transform)
        {
            var trail = child.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                child.parent = null;
                Destroy(child.gameObject, trail.time);
                continue;
            }

            var particle = child.GetComponent<ParticleSystem>();
            if (particle != null)
            {
                child.parent = null;
                particle.Stop();
                Destroy(child.gameObject, particle.main.duration + particle.main.startLifetime.constantMax);
            }
        }
    }

    protected void PlaySE(string seId, Vector3 worldPos)
    {
        if (string.IsNullOrEmpty(seId))
            return;

        worldPos.z = Camera.main.transform.position.z;
        AudioController.Play(seId, worldPos);
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
