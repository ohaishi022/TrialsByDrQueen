using UnityEngine;

public class Unit_EliteStalker : Unit_Base
{
    [Header("Skill/FX")]
    public GameObject projectile;
    public GameObject effect;

    [Header("AI Settings")]
    [SerializeField] float visionRadius = 9999f;
    [SerializeField] int frontTiles = 7;   // 전방 탐지 거리(타일)
    [SerializeField] int stopTiles = 5;    // 유지 거리(타일)

    private void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        base.Start();

        var sneakyRaygun = AddSkill<Skill_SneakyRaygun>();
        sneakyRaygun.projectilePrefab = projectile;
    }

    private void Update()
    {
        base.Update();

        // 컷신 중이면 스킬 강제 해제
        if (gameSceneManager.CurrentSituation == GameSituation.CutScene && Skills.Count > 0 && Skills[0].isActive)
        {
            DeactivateSkill(0);
        }
    }

    protected override void HandlePlayerInput()
    {
        base.HandlePlayerInput();
        if (unitState == UnitState.Player && Skills.Count > 0)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) ActivateSkill(0);
            else if (Input.GetKeyUp(KeyCode.Mouse0)) DeactivateSkill(0);
        }
    }

    protected override void HandleAIInput()
    {
        if (unitState != UnitState.AI) return;
        //if (Skills.Count < 1) return;

        // 타겟 갱신
        if (target == null || !IsValidEnemy(target))
        {
            var near = FindNearestEnemy(visionRadius);   // ← 반경 전달
            target = near ? near.transform : null;
        }

        if (!target) { DeactivateSkill(0); HaltMovement(); return; }

        var skill = Skills[0];
        int distTiles = TileDistance(transform.position, target.position);

        // 전방 레이로 적 감지(겹침 포함)
        bool enemyInFront = SeeEnemyForward(frontTiles, out var seenEnemy);  // ← 전방 타일수 전달

        if (enemyInFront)
        {
            if (distTiles > stopTiles)
            {
                Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
                Vector2 dest = (Vector2)target.position - dir * stopTiles; // 5칸 위치로 접근
                MoveToPosition(dest);
            }
            else
            {
                HaltMovement(); // 5칸 유지
            }

            if (!skill.IsOnCooldown()) ActivateSkill(0); // 보이면 발사
        }
        else
        {
            DeactivateSkill(0);           // 전방에 없으면 스킬 해제
            MoveToPosition(target.position);      // 추적
        }
    }

    private void OnDrawGizmos()
    {
        // 시야 반경
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // 전방 감지 코리도(폭 1, 길이 frontTiles)
        Vector2 d = DirectionVector;
        if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
            d = new Vector2(Mathf.Sign(d.x == 0 ? 1 : d.x), 0f);
        else
            d = new Vector2(0f, Mathf.Sign(d.y == 0 ? 1 : d.y));

        if (d != Vector2.zero)
        {
            Gizmos.color = Color.green;
            Vector3 center = transform.position + (Vector3)d * (frontTiles / 2f);
            Vector3 size = Mathf.Abs(d.x) > 0 ? new Vector3(frontTiles, 1f, 1f)
                                              : new Vector3(1f, frontTiles, 1f);
            Gizmos.DrawWireCube(center, size);

            // 레이 자체
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)d * frontTiles);

            // 플레이 중이면 실제 히트 포인트 시각화
            if (Application.isPlaying)
            {
                LayerMask blockMask = wallMask | wallMask_AI;
                var hits = Physics2D.RaycastAll((Vector2)transform.position + d * 0.01f, d, frontTiles);
                foreach (var h in hits)
                {
                    var col = h.collider;
                    if (!col) continue;

                    if ((blockMask.value & (1 << col.gameObject.layer)) != 0)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawSphere(h.point, 0.15f);
                        break;
                    }

                    var ub = col.GetComponent<Unit_Base>();
                    if (ub && ub != this && ub.unitType == GetOpponentType())
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawSphere(h.point, 0.15f);
                        break;
                    }
                }
            }
        }

        // 타겟 및 정지 지점 표시
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
