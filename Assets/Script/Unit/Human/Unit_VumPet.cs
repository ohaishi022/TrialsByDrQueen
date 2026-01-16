using CS.AudioToolkit;
using UnityEngine;

public class Unit_VumPet : Unit_Base
{
    [Header("AI Settings")]
    [SerializeField] int frontTiles_Hurt = 1;     // 전방 1칸

    private Skill_VumPetHurt vumPetHurt;

    private void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        base.Start();

        AudioController.Play("SE_Unit_VumPet_Spawn", transform.position);
        vumPetHurt = AddSkill<Skill_VumPetHurt>();
    }

    private void Update()
    {
        base.Update();
    }


    protected override void HandlePlayerInput()
    {
        base.HandlePlayerInput();

        if (unitState != UnitState.Player) return;
        if (gameSceneManager.CurrentSituation == GameSituation.CutScene) return;
        if (Skills.Count < 1) return;
        if (isDestroy == true) return;

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            ActivateSkill(0);  // 첫 번째 스킬
        }
    }

    protected override void HandleAIInput()
    {
        if (unitState != UnitState.AI) return;
        if (gameSceneManager.CurrentSituation == GameSituation.CutScene) return;
        if (Skills.Count < 1 || vumPetHurt == null) return;
        if (isDestroy == true) return;

        if (target == null || !IsValidEnemy(target))
        {
            Unit_Base nearest = FindNearestEnemy(sight);
            target = nearest ? nearest.transform : null;
        }

        if (target == null)
        {
            HaltMovement();
            return;
        }

        if (!vumPetHurt.IsOnCooldown() && IsEnemyInFrontOrOverlap())
        {
            HaltMovement();
            ActivateSkill(0);
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

    public override void DestroyUnit(bool ignoreCutScene)
    {
        if (isDestroy) return;
        AudioController.Play("SE_Unit_VumPet_Destroy", transform.position);
        base.DestroyUnit(ignoreCutScene);
    }
}