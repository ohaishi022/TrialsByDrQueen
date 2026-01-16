using CS.AudioToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Identity")]
    public GameObject shooter;                // 발사자
    public UnitType positiveUnitType;         // 발사한 유닛의 타입
    private UnitType negativeUnitType;        // 발사한 유닛의 반대 타입

    [Header("Kinetics")]
    public Vector2 direction;                 // 이동 방향(정규화 권장)
    public float speed = 18.75f;              // 투사체 속도
    public float range = 5f;                  // 이동 가능한 최대 거리
    public float lifetime = 5f;               // 투사체 생존 시간
    public bool canPierce = false;            // 관통 여부

    [Header("Effects / Audio")]
    public string[] SE_Start;
    public string[] SE_Hit;
    public string[] SE_Destroy;
    public GameObject hitEffect;              // 적중 시 생성
    public GameObject killEfftct;             // 처치 시 생성
    public Animator animator;

    [Header("Damage / Heal")]
    public float projectileDamage = 10f;      // 피해량(적 대상)
    public float healingAmount = 10f;         // 회복량(아군 대상)

    [Header("Explosion")]
    public bool canExplode = false;
    public float explosionRadius = 3f;        // 폭발 범위
    public float explosionDamage;             // 폭발 피해(적 대상)

    [Header("Targeting")]
    public bool affectEnemies = true;         // 적에게 영향
    public bool affectAllies = false;        // 아군에게 영향(힐/버프용)
    public bool destroyOnAllyHit = false;     // 아군 적중 시 파괴 여부(요청 사항: 기본 false)

    [Header("Homing")]
    public bool isHoming = false;             // 유도 여부
    public Transform homingTarget;            // 유도 타겟
    public float homingTurnSpeed = 6f;        // 조향 속도
    public float homingRadius = 2.5f;         // 유도 탐지 반경
    public LayerMask unitLayerMask;
    [SerializeField] private string unitLayerName = "Unit";

    private Vector2 startPosition;
    private float elapsedTime;
    private HashSet<GameObject> hitList = new HashSet<GameObject>(); // 이미 적중한 대상

    void Start()
    {
        startPosition = transform.position;
        negativeUnitType = (positiveUnitType == UnitType.Human) ? UnitType.Zombie : UnitType.Human;

        if (SE_Start != null)
            foreach (string clip in SE_Start) AudioController.Play(clip, transform.position);

        if (isHoming)
            StartCoroutine(HomingSearchRoutine());

        UpdateAnimationDirection();
    }

    void Update()
    {
        if (isHoming && homingTarget != null)
            UpdateHoming();

        MoveStraight();
        CheckLifetime();
    }

    void UpdateAnimationDirection()
    {
        if (!animator) return;
        animator.SetFloat("X", direction.x);
        animator.SetFloat("Y", direction.y);
    }

    void MoveStraight()
    {
        transform.Translate(direction * speed * Time.deltaTime);
        if (Vector2.Distance(startPosition, transform.position) >= range)
            DestroyProjectile();
    }

    void CheckLifetime()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= lifetime)
            DestroyProjectile();
    }

    void DestroyProjectile()
    {
        if (canExplode)
            Explode();

        if (SE_Destroy != null)
            foreach (string clip in SE_Destroy) AudioController.Play(clip, transform.position);

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

    void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject target = collision.gameObject;

        // 자기 자신/다른 투사체/이미 적중한 대상은 무시
        if (target == shooter || target.CompareTag("Projectile") || hitList.Contains(target))
            return;

        // 유닛인지 판단
        var unit = target.GetComponent<Unit_Base>();
        if (unit == null)
        {
            // 유닛이 아니면(벽/오브젝트 등) 기존 동작 유지: 관통 아니면 파괴
            if (!canPierce) DestroyProjectile();
            return;
        }

        // 팀 판정
        bool isEnemy = unit.unitType == negativeUnitType;
        bool isAlly = unit.unitType == positiveUnitType;

        // === 적 처리 ===
        if (isEnemy && affectEnemies)
        {
            hitList.Add(target); // 중복 적용 방지

            // 실제 타격 이펙트/사운드
            if (hitEffect) Instantiate(hitEffect, transform.position, Quaternion.identity);
            if (SE_Hit != null)
                foreach (string clip in SE_Hit) AudioController.Play(clip, transform.position);

            float prevHP = unit.currentHealth;
            unit.TakeDamage(projectileDamage);
            Debug.Log($"{unit.unitName}에게 피해: {projectileDamage}");

            // 처치 시 이펙트
            if (killEfftct && prevHP > 0 && unit.currentHealth <= 0)
                Instantiate(killEfftct, unit.SkillPosition, Quaternion.identity);

            if (!canPierce) DestroyProjectile();
            return;
        }

        // === 아군 처리 ===
        if (isAlly)
        {
            // 요청사항: 같은 편 맞춰도 파괴되지 않게
            // heal/buff 투사체(affectAllies=true)는 효과 적용, 그 외는 완전 무시
            if (affectAllies)
            {
                // (예시) 힐 적용 — 필요 시 버프 로직도 여기에 추가
                hitList.Add(target); // 같은 아군에 중복 힐 방지
                unit.currentHealth = Mathf.Min(unit.health, unit.currentHealth + healingAmount);
                Debug.Log($"{unit.unitName} 치유: {healingAmount}");

                // 히트 이펙트/사운드: 힐 전용 이펙트를 쓰고 싶다면 별도 필드로 분리 가능
                if (hitEffect) Instantiate(hitEffect, transform.position, Quaternion.identity);
                if (SE_Hit != null)
                    foreach (var clip in SE_Hit) AudioController.Play(clip, transform.position);

                // 아군 적중 시는 절대 파괴하지 않음(관통 유지)
                // if (destroyOnAllyHit) DestroyProjectile(); // 필요할 때만 사용
            }
            // affectAllies == false → 완전 무시(파괴 X)
            return;
        }

        // 여기까지 왔다는 건 유닛이지만 타게팅 대상이 아님 → 무시(파괴 X)
    }

    IEnumerator HomingSearchRoutine()
    {
        while (isHoming)
        {
            if (homingTarget == null) // 타겟이 없을 때만 검색
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, homingRadius, unitLayerMask);
                Transform best = null;
                float bestDist = float.PositiveInfinity;

                foreach (var col in hits)
                {
                    Unit_Base u = col.GetComponent<Unit_Base>();
                    if (u == null || u.currentHealth <= 0f) continue;

                    // 유도 대상은 타게팅 규칙에 맞는 유닛만
                    if (!IsValidTargetForHoming(u)) continue;

                    float d = Vector2.Distance(transform.position, u.transform.position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = u.transform;
                    }
                }

                homingTarget = best;
            }

            yield return new WaitForSeconds(1f / Mathf.Max(1f, speed));
        }
    }

    bool IsValidTargetForHoming(Unit_Base u)
    {
        // 적만 영향 → 적만 추적
        if (affectEnemies && !affectAllies)
            return u.unitType == negativeUnitType;
        // 아군만 영향(힐/버프) → 아군만 추적
        if (!affectEnemies && affectAllies)
            return u.unitType == positiveUnitType;
        // 둘 다 영향 → 우선 적, 없으면 아군 (원하면 우선순위 바꿔도 됨)
        if (affectEnemies && affectAllies)
            return u.unitType == negativeUnitType || u.unitType == positiveUnitType;

        // 아무도 영향 주지 않는 탄은 추적 대상 없음
        return false;
    }

    void UpdateHoming()
    {
        var unit = homingTarget ? homingTarget.GetComponent<Unit_Base>() : null;

        // 유효성 재검사(사망/팀 변경/타게팅 규칙 불만족)
        if (unit == null || unit.currentHealth <= 0f || !IsValidTargetForHoming(unit))
        {
            homingTarget = null;
            return;
        }

        Vector2 toTarget = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
        direction = Vector2.Lerp(direction, toTarget, Time.deltaTime * homingTurnSpeed).normalized;
    }

    void Explode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, unitLayerMask);
        foreach (var h in hits)
        {
            var u = h.GetComponent<Unit_Base>();
            if (u == null) continue;

            // 폭발은 적에게만 피해(아군 보호)
            if (affectEnemies && u.unitType == negativeUnitType)
                u.TakeDamage(explosionDamage);

            // 필요하면 아군 힐 폭발도 지원 가능:
            // if (affectAllies && u.unitType == positiveUnitType)
            //     u.currentHealth = Mathf.Min(u.health, u.currentHealth + healingAmount);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (isHoming)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, homingRadius);
        }
    }
}
