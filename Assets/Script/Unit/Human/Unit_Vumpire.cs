using CS.AudioToolkit;
using System.Collections;
using System.Linq;
using UnityEngine;

public class Unit_Vumpire : Unit_Base
{
    [Header("FX")]
    public GameObject destroyEffect;

    [Header("AI Ranges (tiles)")]
    [SerializeField] int hurtRange = 1;     // 근접 휩쓸기
    [SerializeField] int leechRange = 6;    // 레이저
    [SerializeField] int escapeRange = 2;   // 회피 트리거

    // Skill index cache
    private Skill_VumpHurt skillHurt;
    private Skill_LeechLaser skillLeech;
    private Skill_BatWarp skillBatWarp;
    private Skill_VumpDodge skillDodge;

    private void Awake()
    {
        base.Awake();
        destroyEffect = Resources.Load<GameObject>(
            "Prefab/Skill/Human/Vumpire/Skill_BatWarp"
        );
    }

    private void Start()
    {
        base.Start();

        AudioController.Play("SE_Skill_BatWarp_End", transform.position);
        StartCoroutine(SpawnDestroyEffect(0f));

        // === Skills ===
        skillHurt = AddSkill<Skill_VumpHurt>();
        skillLeech = AddSkill<Skill_LeechLaser>();
        skillBatWarp = AddSkill<Skill_BatWarp>();
        skillDodge = AddSkill<Skill_VumpDodge>();

        skillLeech.laserEffectPrefab =
            Resources.Load<GameObject>("Prefab/Skill/Human/Vumpire/Skill_LeechLaser");
        skillLeech.blockMask = wallMask | wallMask_AI;
    }

    private void Update()
    {
        base.Update();
    }
    protected override void HandlePlayerInput()
    {
        base.HandlePlayerInput();
        if (unitState != UnitState.Player) return;

        // LMB : Hurt → Leech
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            if (!TryActivate(skillLeech))
                TryActivate(skillHurt);
        }
        // RMB : BatWarp → Dodge
        else if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (!TryActivate(skillBatWarp))
                TryActivate(skillDodge);
        }
    }

    protected override void HandleAIInput()
    {
        if (unitState != UnitState.AI) return;
        if (AnySkillActive()) return;

        UpdateTarget();
        if (!target)
        {
            HaltMovement();
            return;
        }

        // 1️ 근접 공격 가능
        if (CanUse(skillHurt) && EnemyInHurtZone())
        {
            HaltMovement();
            skillHurt.ActivateSkill(this);
            return;
        }

        // 2️ 전방 레이저
        if (SeeEnemyForward(leechRange, out var frontEnemy))
        {
            if (CanUse(skillLeech))
            {
                HaltMovement();
                skillLeech.ActivateSkill(this);
            }
            else
            {
                MoveToPosition(target.position);
            }
            return;
        }

        // 3️ 위기 회피
        if (skillHurt.IsOnCooldown() && EnemyNearby(escapeRange))
        {
            if (CanUse(skillBatWarp))
            {
                HaltMovement();
                skillBatWarp.ActivateSkill(this);
                return;
            }
            if (CanUse(skillDodge))
            {
                HaltMovement();
                skillDodge.ActivateSkill(this);
                return;
            }
        }

        // 4️ 기본 추적
        MoveToPosition(target.position);
    }

    private bool TryActivate(Skill_Base skill)
    {
        if (!CanUse(skill)) return false;
        skill.ActivateSkill(this);
        return true;
    }

    private bool CanUse(Skill_Base skill)
    {
        if (skill == null) return false;
        if (skill.isActive) return false;
        if (skill.IsOnCooldown()) return false;
        if (IsSkillRestricted(Skills.IndexOf(skill))) return false;
        return true;
    }

    private bool AnySkillActive()
    {
        return Skills.Any(s => s.isActive);
    }

    private void UpdateTarget()
    {
        if (target && IsValidEnemy(target)) return;

        var nearest = FindNearestEnemy(sight);
        target = nearest ? nearest.transform : null;
    }

    private bool EnemyNearby(float range)
    {
        return FindNearestEnemy(range + 0.01f) != null;
    }

    private bool EnemyInHurtZone()
    {
        bool horizontal = Mathf.Abs(DirectionVector.x) > 0f;
        Vector2 size =
            horizontal ? skillHurt.hitSizeLR : skillHurt.hitSizeUD;

        Vector2 centerForward =
            (Vector2)transform.position + DirectionVector;

        return Physics2D
            .OverlapBoxAll(centerForward, size, 0f)
            .Any(c =>
            {
                var u = c.GetComponent<Unit_Base>();
                return u &&
                       u.unitType == GetOpponentType() &&
                       !u.HasSpeical(Speical.Disappear);
            });
    }

    public override void DestroyUnit(bool ignoreCutScene)
    {
        if (isDestroy) return;

        foreach (var s in Skills)
            if (s.isActive)
                s.ForceCancel();

        AudioController.Play("SE_Unit_Vumpire_Destroy", transform.position);
        StartCoroutine(SpawnDestroyEffect(2f));

        base.DestroyUnit(ignoreCutScene);
    }

    private IEnumerator SpawnDestroyEffect(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (destroyEffect)
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
    }
}
