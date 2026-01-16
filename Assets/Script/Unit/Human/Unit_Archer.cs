using UnityEngine;

public class Unit_Archer : Unit_Base
{
    [Header("Prefabs")]
    public GameObject arrow;
    public GameObject backstepEffect;

    [Header("AI Settings")]
    [SerializeField] int frontTiles = 5;               // 전방 탐지 거리(타일)
    [SerializeField] int stopTiles = 4;                // (미사용이더라도 남겨둠: 접근 목표 계산 시 여유값에 활용 가능)
    [SerializeField] private float attackRange = 7f;   // 석궁 사거리(타일=1)
    [SerializeField] private float backstepThreatRange = 2f; // (AI에선 사용 안 함 / 플레이어만)

    private void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        base.Start();

        // 스킬 0: Crossbow
        var crossbow = AddSkill<Skill_Crossbow>();
        crossbow.projectilePrefab = arrow;

        // 스킬 1: Backstep (플레이어 전용)
        var backstep = AddSkill<Skill_Backstep>();
        backstep.skillEffectPrefab = backstepEffect;
        backstep.damage = 20f; //나중에 10으로 수정할 것
        backstep.dashTiles = 5; //나중에 3으로 수정할 것
    }

    private void Update()
    {
        base.Update();
    }

    // ===== 플레이어 조작 =====
    protected override void HandlePlayerInput()
    {
        base.HandlePlayerInput();
        if (unitState == UnitState.Player && Skills.Count > 1)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) ActivateSkill(0); // 석궁
            if (Input.GetKeyDown(KeyCode.Mouse1)) ActivateSkill(1); // 백스텝(플레이어만)
        }
    }

    // ===== AI =====
    protected override void HandleAIInput()
    {
        if (unitState != UnitState.AI) return;
        if (Skills.Count < 2) return; // [0]=Crossbow, [1]=Backstep

        // 타겟 갱신
        if (target == null || !IsValidEnemy(target))
        {
            var near = FindNearestEnemy(sight);
            target = near ? near.transform : null;
        }

        if (!target)
        {
            DeactivateSkill(0);
            HaltMovement();
            return;
        }

        var sCrossbow = Skills[0];
        var sBackstep = Skills[1];

        Unit_Base closeEnemy;
        bool enemyInFront2Tiles = SeeEnemyForward(2, out closeEnemy);

        // 전방 2타일 이내 → 백스탭
        if (!sBackstep.IsOnCooldown() && enemyInFront2Tiles)
        {
            HaltMovement();
            ActivateSkill(1);
            return;
        }

        // 전방 가시선(벽 차단) + 실제 앞에 보이는 적 참조
        Unit_Base seenEnemy;
        bool enemyInFront = SeeEnemyForward(frontTiles, out seenEnemy);

        if (enemyInFront && seenEnemy != null)
        {
            float dist = Vector2.Distance(transform.position, seenEnemy.transform.position);

            if (dist > attackRange)
            {
                // 보이지만 사거리 밖 → 사거리 경계까지 직진
                Vector2 dir = ((Vector2)seenEnemy.transform.position - (Vector2)transform.position).normalized;
                float desiredDist = Mathf.Max(attackRange - 0.5f, 1f); // 살짝 안쪽에 멈추도록
                Vector2 desired = (Vector2)seenEnemy.transform.position - dir * desiredDist;
                MoveToPosition(desired);
            }
            else
            {
                // 사거리 안 → 정지해서 사격
                HaltMovement();
                if (!sCrossbow.IsOnCooldown())
                    ActivateSkill(0);
            }
        }
        else
        {
            // 전방에 안 보이면 타겟 쪽으로 추적
            MoveToPosition(target.position);
        }
    }

    private void OnDrawGizmos()
    {
        // 시야
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sight);

        // 전방 코리도/레이(석궁 사거리)
        Vector2 d = DirectionVector;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
            d = new Vector2(Mathf.Sign(d.x == 0 ? 1 : d.x), 0f);
        else
            d = new Vector2(0f, Mathf.Sign(d.y == 0 ? 1 : d.y));

        if (d != Vector2.zero)
        {
            Gizmos.color = Color.green;
            Vector3 center = transform.position + (Vector3)d * (attackRange / 2f);
            Vector3 size = Mathf.Abs(d.x) > 0 ? new Vector3(attackRange, 1f, 1f)
                                              : new Vector3(1f, attackRange, 1f);
            Gizmos.DrawWireCube(center, size);
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)d * attackRange);
        }

        // 유지 거리 포인트(참고용)
        if (target)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, target.position);

            Vector2 dirToTgt = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude > 0
                ? ((Vector2)target.position - (Vector2)transform.position).normalized
                : Vector2.zero;

            if (dirToTgt != Vector2.zero)
            {
                Vector3 stopPoint = (Vector2)target.position - dirToTgt * stopTiles;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(stopPoint, 0.2f);
            }
        }
    }
}
