using CS.AudioToolkit;
using UnityEngine;

public class Unit_QueenCrusher : Unit_Base
{
    private Skill_LoyalSlam skillLoyalSlam;
    private Skill_GrenadePunt skillGrenadePunt;

    public GameObject projectile;

    private UnitType enemyType;

    [Header("AI Settings")]
    [SerializeField] float visionRadius = 9999f;
    [SerializeField] int frontTiles_Hurt = 1;     // 전방 1칸

    private void Awake()
    {
        base.Awake();
        enemyType = (unitType == UnitType.Human) ? UnitType.Zombie : UnitType.Human;
    }

    private void Start()
    {
        base.Start();
        AudioController.Play("SE_Unit_StrawWingHacker_Spawn", transform.position);

        // 스킬 추가
        skillLoyalSlam = AddSkill<Skill_LoyalSlam>();
        skillGrenadePunt = AddSkill<Skill_GrenadePunt>();
        skillGrenadePunt.projectilePrefab = projectile;
        skillGrenadePunt.StartInitialCooldown();
    }

    private void Update()
    {
        base.Update();

        // 컷신 진입 시 스킬 강제 종료
        if (gameSceneManager.CurrentSituation == GameSituation.CutScene)
        {
            if (Skills.Count > 0 && Skills[0].isActive) DeactivateSkill(0);
            if (Skills.Count > 1 && Skills[1].isActive) DeactivateSkill(1);
        }
    }

    protected override void HandlePlayerInput()
    {
        base.HandlePlayerInput();
        if (unitState == UnitState.Player && Skills.Count > 2)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) ActivateSkill(0);
            if (Input.GetKeyDown(KeyCode.Mouse1)) ActivateSkill(1);
        }
    }

    protected override void HandleAIInput()
    {
        if (unitState != UnitState.AI) return;
        if (gameSceneManager.CurrentSituation == GameSituation.CutScene) return;
        if (isDestroy) return;

        if (target == null || !IsValidEnemy(target))
        {
            Unit_Base nearest = FindNearestEnemy(visionRadius);
            target = nearest ? nearest.transform : null;
        }

        if (!target)
        {
            HaltMovement();
            return;
        }

        if (!skillLoyalSlam.IsOnCooldown() && IsEnemyInFrontOrOverlap())
        {
            HaltMovement();
            ActivateSkill(0); // LoyalSlam
            return;
        }

        if (!skillGrenadePunt.IsOnCooldown() && IsEnemyInLongRange())
        {
            HaltMovement();
            ActivateSkill(1);  // Grenade Punt
            return;
        }

        MoveToPosition(target.position);
    }
    private bool IsEnemyInFrontOrOverlap()
    {
        Unit_Base dummy;

        // 1) 자기 칸(대충 1x1 박스)
        Vector2 selfCenter = transform.position;
        Vector2 selfSize = new Vector2(0.9f, 0.9f);
        if (CheckOverlapBoxForEnemy(selfCenter, selfSize, out dummy))
            return true;

        // 2) 자기칸 + 전방칸 2칸 범위
        Vector2 forward = DirectionVector;
        // 자기와 전방의 중간 지점이 중심
        Vector2 boxCenter = (Vector2)transform.position + forward * 0.5f;

        Vector2 boxSize;
        if (Mathf.Abs(forward.x) > 0f)      // 좌우 바라보는 중이면 2x1
            boxSize = new Vector2(2f, 1f);
        else                                // 상하 바라보는 중이면 1x2
            boxSize = new Vector2(1f, 2f);

        if (CheckOverlapBoxForEnemy(boxCenter, boxSize, out dummy))
            return true;

        return false;
    }

    private bool CheckOverlapBoxForEnemy(Vector2 center, Vector2 size, out Unit_Base found)
    {
        found = null;
        var cols = Physics2D.OverlapBoxAll(center, size, 0f);
        if (cols == null || cols.Length == 0) return false;

        UnitType opponent = GetOpponentType();

        foreach (var c in cols)
        {
            var ub = c.GetComponent<Unit_Base>();
            if (ub && ub != this &&
                ub.unitType == opponent &&
                !ub.HasSpeical(Speical.Disappear))
            {
                found = ub;
                return true;
            }
        }
        return false;
    }

    private bool IsEnemyInLongRange()
    {
        Vector2 dir = DirectionVector;
        // 바라보는 방향을 깔끔하게 정규화 (x 또는 y)
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            dir = new Vector2(Mathf.Sign(dir.x == 0 ? 1 : dir.x), 0f);
        else
            dir = new Vector2(0f, Mathf.Sign(dir.y == 0 ? 1 : dir.y));

        float maxDist = 12f;
        float minDist = 6f;

        var hits = Physics2D.RaycastAll((Vector2)transform.position + dir * 0.01f, dir, maxDist);
        UnitType opponent = GetOpponentType();

        foreach (var h in hits)
        {
            var ub = h.collider.GetComponent<Unit_Base>();
            if (ub && ub != this && ub.unitType == opponent && !ub.HasSpeical(Speical.Disappear))
            {
                float d = h.distance;
                if (d >= minDist && d <= maxDist)
                {
                    return true; // 전방 5~10칸 적 발견!
                }
            }

            // 벽 만나면 그 뒤는 무시
            if ((wallMask.value & (1 << h.collider.gameObject.layer)) != 0)
                return false;
        }

        return false;
    }

    public override void DestroyUnit(bool ignoreCutScene)
    {
        if (isDestroy) return;
        AudioController.Play("SE_Unit_VumPet_Destroy", transform.position);
        base.DestroyUnit(ignoreCutScene);
    }
}
