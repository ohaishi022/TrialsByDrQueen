using CS.AudioToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Human,
    Zombie,
    None
}

public enum UnitState
{
    Player,
    AI
}

public enum Direction // 유닛이 바라보고 있는 방향
{
    Down,
    Left,
    Up,
    Right
}

[Flags]
public enum Speical
{
    None = 0,
    Float = 1 << 0,        // 비행 상태
    Disappear = 1 << 1    // 사라짐(무적 + 비가시성)
}

[Flags]
public enum UnitCategory
{
    None = 0,
    Object = 1 << 0,   // 오브젝트 여부
    Imp = 1 << 1,      // 소형 유닛
    Giant = 1 << 2,    // 대형 유닛
    Boss = 1 << 3,     // 보스 유닛
}

public enum UnitSpawnState
{
    Active,
    Spawning
}

public enum AIMoveStyle
{
    AStar,
    StraightThenDiagonal,     // 대부분 직선 → 끝 지그재그
    DiagonalThenStraight    // 초반 지그재그 → 이후 직선
}

[System.Serializable]
public class LevelStats
{
    public int level;        // 유닛 레벨
    public float health;     // 해당 레벨에서의 체력
    public float speed;      // 해당 레벨에서의 속도
}

public class Unit_Base : MonoBehaviour
{
    public AIMoveStyle aiMoveStyle = AIMoveStyle.StraightThenDiagonal;

    [Header("Managers")]
    public GameSceneManager gameSceneManager;
    public GridManager gridManager;

    [Header("Movement (Separated)")]
    public Unit_Movement movement;

    // Movement Proxy (중계 API)
    public Vector3 SkillPosition
        => movement != null ? movement.skillPosition : transform.position;
    public Vector3 StartPosition
        => movement != null ? movement.startPosition : transform.position;
    public Vector3 EndPosition
        => movement != null ? movement.endPosition : transform.position;
    public Vector2 DirectionVector
        => movement != null ? movement.directionVector : Vector2.down;
    public Direction FacingDirection
        => movement != null ? movement.direction : Direction.Down;
    public bool isMoving
        => movement != null && movement.IsMoving;
    public bool stopMoving
    {
        get => movement != null && movement.StopMoving;
        set
        {
            if (movement != null) movement.SetStopMoving(value);
        }
    }
    public void ChangeDirection(Vector2 dir)
    {
        if (movement != null) movement.ChangeDirection(dir);
    }
    public void HaltMovement()
    {
        if (movement != null) movement.HaltMovement();
    }
    public void MoveToPosition(Vector2 dest)
    {
        if (movement != null) movement.MoveTo(dest);
    }
    public void FindPath(Vector2 startPos, Vector2 targetPos)
    {
        if (movement != null) movement.FindPath(startPos, targetPos);
    }
    public void MoveAlongPath()
    {
        if (movement != null) movement.MoveAlongPath_Public();
    }

    [Header("Status/Buffs")]
    public Unit_Status Status { get; private set; }

    [Header("Identity")]
    public string unitName;

    [Header("Team / State")]
    public UnitType unitType;
    public UnitState unitState = UnitState.AI;

    public Speical speicals = Speical.None;
    public UnitCategory categories = UnitCategory.None;

    [Header("UI")]
    public UI_Unit unitUI;

    [Header("Spawn")]
    public float spawnTime = 0f;
    public UnitSpawnState spawnState;

    [Header("Skills")]
    public List<Skill_Base> Skills = new List<Skill_Base>();
    private HashSet<int> restrictedSkills = new HashSet<int>();

    [Header("AI")]
    public Transform target;
    public float sight = 9999f;

    [Header("Stats")]
    public float health;
    public float currentHealth;

    public float speed;
    public float currentSpeed;

    [Header("Speed Multiplier")]
    public float SpeedMultiplier
    {
        get
        {
            if (Status == null) return 1f;
            return Mathf.Clamp(Status.SpeedMul, 0f, 99f);
        }
    }

    public int level;
    public LevelStats[] levelStats;

    [Header("Components")]
    public BoxCollider2D boxCollider;
    public Animator animator;
    public SpriteRenderer sprite;
    public GameObject characherShadow;

    [Header("Masks")]
    public LayerMask wallMask;
    public LayerMask wallMask_AI;

    // ===== Destroy =====
    public bool isDestroy = false;
    public bool isDestroyGroup = false;
    public float destroyTime = 0.3f;
    public bool hasDestroyEvent;
    private Action onDestroyComplete;

    public event Action OnDestroyed;

    // ===== Summon =====
    [Header("Summon")]
    [SerializeField] protected int maxSummonedUnits = 5;
    protected readonly List<Unit_Base> summonedUnits = new();
    public int SummonedCount => summonedUnits.Count;
    public bool CanSummon => summonedUnits.Count < maxSummonedUnits;
    protected Unit_Base summoner;

    public void SetSummoner(Unit_Base owner) => summoner = owner;

    public Unit_Base SummonUnit(GameObject prefab, Vector2 position, Transform parent = null, bool inheritTeam = true)
    {
        if (!prefab || !CanSummon) return null;

        GameObject go = Instantiate(prefab, position, Quaternion.identity, parent);
        Unit_Base unit = go.GetComponent<Unit_Base>();
        if (!unit) { Destroy(go); return null; }

        if (!TryRegisterSummon(unit)) { Destroy(go); return null; }

        if (inheritTeam) unit.unitType = unitType;
        return unit;
    }

    public bool TryRegisterSummon(Unit_Base unit)
    {
        if (unit == null) return false;
        if (summonedUnits.Count >= maxSummonedUnits) return false;

        summonedUnits.Add(unit);
        unit.SetSummoner(this);
        return true;
    }

    public void UnregisterSummon(Unit_Base unit)
    {
        if (unit == null) return;
        summonedUnits.Remove(unit);
    }

    // Unity
    public void Awake()
    {
        // Stats init
        currentSpeed = speed;
        currentHealth = health;

        // Cache components
        boxCollider = GetComponent<BoxCollider2D>();

        // Snap position (y-based z)
        transform.position = new Vector3((int)transform.position.x, (int)transform.position.y, transform.position.y * 0.01f);

        // Managers (※ movement.Initialize 전에 반드시 잡아야 함)
        gridManager = GameObject.FindGameObjectWithTag("Wall").GetComponent<GridManager>();
        gameSceneManager = GameObject.FindGameObjectWithTag("GameSceneManager").GetComponent<GameSceneManager>();

        // Movement
        movement = GetComponent<Unit_Movement>();
        if (movement == null) movement = gameObject.AddComponent<Unit_Movement>();

        movement.Initialize(this, gridManager, wallMask, wallMask_AI);

        // (선택) 3칸마다 타겟 재탐색 같은 거 movement가 요구하면 여기서 연결
        movement.retargetResolver = () => FindNearestEnemy(sight);

        Status = GetComponent<Unit_Status>();
        if (Status == null) Status = gameObject.AddComponent<Unit_Status>();
        Status.Initialize(this);

        // Spawn
        spawnState = UnitSpawnState.Spawning;
        OnSpawnAnimationEnd();
    }

    public void Start()
    {
        SetupUnitUI();
    }

    public void Update()
    {
        if (gameSceneManager.CurrentSituation == GameSituation.CutScene) return;
        if (spawnState != UnitSpawnState.Active) return;

        if (unitState == UnitState.Player) HandlePlayerInput();
        else HandleAIInput();

        // Animator speed scaling
        if (animator != null)
        {
            bool isDestroyState = animator.GetCurrentAnimatorStateInfo(0).IsName("Destroy");
            animator.speed = isDestroyState ? 1f : Mathf.Max(0.01f, SpeedMultiplier);

            if (movement != null && !movement.IsMoving)
                animator.SetBool("isWalk", false);
        }
    }

    // UI
    private void SetupUnitUI()
    {
        Transform canvas = GameObject.Find("Canvas_GameUI").transform;
        GameObject uiPrefab = Resources.Load<GameObject>("Prefab/UI/UI_Unit");
        GameObject uiObj = Instantiate(uiPrefab, canvas);

        unitUI = uiObj.GetComponent<UI_Unit>();
        unitUI.target = transform;
        unitUI.unit = this;

        unitUI.InitializeHP(health);
        unitUI.SetHPColor(unitType);
    }

    // Spawn
    public void OnSpawnAnimationEnd()
    {
        if (spawnTime <= 0f) spawnState = UnitSpawnState.Active;
        else StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        float t = 0f;
        while (t < spawnTime)
        {
            t += Time.deltaTime * Mathf.Max(0.01f, SpeedMultiplier);
            yield return null;
        }
        spawnState = UnitSpawnState.Active;
    }

    // Input / AI
    protected virtual void HandlePlayerInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (movement == null) return;

        if (h != 0)
            movement.Get_Move(new Vector2(h, 0));
        else if (v != 0)
            movement.Get_Move(new Vector2(0, v));
    }

    protected virtual void HandleAIInput()
    {
        if (movement == null) return;

        // 타겟 없으면 탐색
        if (target == null)
        {
            Unit_Base nearest = FindNearestEnemy(sight);
            if (nearest != null) target = nearest.transform;
        }

        if (target != null)
        {
            movement.MoveTo(target.position);
        }
        else
        {
            movement.HaltMovement();
        }
    }

    // Speed / Buff
    public bool HasSpeical(Speical speical) => (speicals & speical) == speical;

    // Category
    public void CategoryOn(UnitCategory category) => categories |= category;
    public void CategoryOff(UnitCategory category) => categories &= ~category;
    public void CategoryToggle(UnitCategory category) => categories ^= category;
    public bool CategoryHas(UnitCategory category) => (categories & category) == category;

    // Combat
    public void TakeDamage(float damage)
    {
        TakeDamage(new DamageInfo(damage, DamageType.Normal, null));
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (gameSceneManager != null &&
            gameSceneManager.CurrentSituation == GameSituation.CutScene) return;

        // Disappear는 계속 “피격 무시”로 둘지 선택
        if (HasSpeical(Speical.Disappear)) return;

        // Invincible은 버프
        if (Status != null && Status.Has("invincible")) return;

        // 버프에게 피해 이벤트 전달 (Chill/Freeze가 Fire면 여기서 풀림)
        Status?.NotifyDamaged(info);

        currentHealth -= info.Amount;
        unitUI?.OnDamaged(currentHealth, health);

        if (currentHealth <= 0f)
            DestroyUnit(false);
    }

    public event System.Action<float> OnHealed;

    public float TakeHeal(float amount, bool ignoreCutScene = true)
    {
        if (amount <= 0f) return 0f;

        if (!ignoreCutScene && gameSceneManager != null &&
            gameSceneManager.CurrentSituation == GameSituation.CutScene)
            return 0f;

        float before = currentHealth;
        currentHealth = Mathf.Min(health, before + amount);
        float healed = Mathf.Max(0f, currentHealth - before);

        if (healed > 0f)
        {
            OnHealed?.Invoke(healed);
            unitUI?.OnDamaged(currentHealth, health);
        }

        return healed;
    }

    // Skills
    public void ActivateSkill(int index)
    {
        if (gameSceneManager.CurrentSituation == GameSituation.CutScene) return;
        if (spawnState != UnitSpawnState.Active) return;
        if (Status != null && !Status.CanCast) return;
        if (SpeedMultiplier <= 0f) return;
        if (IsSkillRestricted(index)) return;

        if (index >= 0 && index < Skills.Count)
            Skills[index].ActivateSkill(this);
    }

    public void DeactivateSkill(int index)
    {
        if (index >= 0 && index < Skills.Count)
            Skills[index].DeactivateSkill();
    }

    public void ForceCancelSkill(int index, bool startCooldownOnCancel = false)
    {
        if (index < 0 || index >= Skills.Count) return;
        Skills[index].ForceCancel(startCooldownOnCancel);
    }

    public void ForceCancelAllSkills(bool startCooldownOnCancel = false)
    {
        foreach (var s in Skills)
            s.ForceCancel(startCooldownOnCancel);
    }

    public T AddSkill<T>() where T : Skill_Base
    {
        T skill = gameObject.AddComponent<T>();
        Skills.Add(skill);
        return skill;
    }

    public void RestrictSkillUsage(int skillIndex) => restrictedSkills.Add(skillIndex);
    public void AllowSkillUsage(int skillIndex) => restrictedSkills.Remove(skillIndex);
    public bool IsSkillRestricted(int skillIndex) => restrictedSkills.Contains(skillIndex);

    public void SetUnitState(UnitState state)
    {
        unitState = state;
    }

    public virtual void UpdateStatsByLevel()
    {
        foreach (var stats in levelStats)
        {
            if (stats.level == level)
            {
                health = stats.health;
                speed = stats.speed;
                currentHealth = stats.health;
                currentSpeed = stats.speed;
                break;
            }
        }
    }

    // Enemy helpers (기존 유지)
    public UnitType GetOpponentType()
    {
        return unitType == UnitType.Human ? UnitType.Zombie
             : unitType == UnitType.Zombie ? UnitType.Human
             : UnitType.None;
    }

    public bool IsValidEnemy(Transform t)
    {
        var enemyType = GetOpponentType();
        var ub = t ? t.GetComponent<Unit_Base>() : null;

        if (!ub || ub == this || ub.unitType != enemyType) return false;
        if (ub.HasSpeical(Speical.Disappear)) return false;

        return true;
    }

    public Unit_Base FindNearestEnemy(float radius)
    {
        var enemyType = GetOpponentType();
        if (enemyType == UnitType.None) return null;

        Unit_Base best = null;
        float bestSqr = float.MaxValue;

        foreach (var c in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            var ub = c.GetComponent<Unit_Base>();
            if (!ub || ub == this || ub.unitType != enemyType) continue;
            if (ub.HasSpeical(Speical.Disappear)) continue;

            float s = (ub.transform.position - transform.position).sqrMagnitude;
            if (s < bestSqr) { bestSqr = s; best = ub; }
        }

        return best;
    }

    // Teleport / Disappear / Invincible (이동은 movement에 위임)
    public void TeleportUnit(Vector2 pos)
    {
        if (movement != null) movement.Teleport(pos);
        else transform.position = new Vector3(pos.x, pos.y, pos.y * 0.01f);
    }

    public void EnterDisappear()
    {
        speicals |= Speical.Disappear;

        if (sprite != null) sprite.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;
        if (characherShadow != null) characherShadow.SetActive(false);

        // 이동 정지
        //movement?.StopMovingFor(9999f); // 무한정 멈추고 싶다면 이런 식으로
    }

    public void ExitDisappear()
    {
        speicals &= ~Speical.Disappear;

        if (sprite != null) sprite.enabled = true;
        if (boxCollider != null) boxCollider.enabled = true;
        if (characherShadow != null) characherShadow.SetActive(true);

        // 다시 움직이게 하고 싶으면 HaltMovement 후 AI가 다시 MoveTo 걸면 됨
        movement?.HaltMovement();
    }

    public bool HasBuff(string id)
    {
        return Status != null && Status.Has(id);
    }

    // Destroy
    public void SetOnDestroyCompleteCallback(Action callback) => onDestroyComplete = callback;

    public virtual void DestroyUnit(bool ignoreCutScene)
    {
        ForceCancelAllSkills();

        if (!ignoreCutScene && gameSceneManager.CurrentSituation == GameSituation.CutScene) return;

        if (!hasDestroyEvent) DestroyUnitGroup();

        StartCoroutine(AutoDestroy());
        if (isDestroy) return;

        isDestroy = true;

        if (unitUI != null) Destroy(unitUI.gameObject);
        if (boxCollider != null) boxCollider.enabled = false;

        if (summoner != null)
        {
            summoner.UnregisterSummon(this);
            summoner = null;
        }

        if (animator != null && animator.HasState(0, Animator.StringToHash("Destroy")))
            animator.Play("Destroy");
        else
        {
            DestroyUnitGroup();
            HandleChildEffects();
            Destroy(gameObject);
        }
    }

    public IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(destroyTime);
        DestroyUnitGroup();
        HandleChildEffects();
        Destroy(gameObject);
    }

    public void DestroyUnitGroup()
    {
        if (isDestroyGroup) return;

        isDestroyGroup = true;
        OnDestroyed?.Invoke();
        onDestroyComplete?.Invoke();
        onDestroyComplete = null;
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

    public bool SeeEnemyForward(int frontTiles, out Unit_Base seen)
    {
        seen = null;

        var enemyType = GetOpponentType();
        if (enemyType == UnitType.None) return false;

        Vector2 origin = transform.position;

        // 0) 같은 칸(겹침) 우선 체크
        foreach (var col in Physics2D.OverlapPointAll(origin))
        {
            var ub0 = col.GetComponent<Unit_Base>();
            if (ub0 && ub0 != this && ub0.unitType == enemyType && !ub0.HasSpeical(Speical.Disappear))
            {
                seen = ub0;
                return true;
            }
        }

        // 1) 전방 방향(4방 스냅)
        Vector2 fwd = DirectionVector; // movement 프록시 사용
        Vector2 dir = Mathf.Abs(fwd.x) >= Mathf.Abs(fwd.y)
            ? new Vector2(Mathf.Sign(fwd.x == 0 ? 1 : fwd.x), 0f)
            : new Vector2(0f, Mathf.Sign(fwd.y == 0 ? 1 : fwd.y));

        float maxDist = Mathf.Max(1, frontTiles);

        // 2) 레이캐스트
        var hits = Physics2D.RaycastAll(origin + dir * 0.01f, dir, maxDist);

        LayerMask block = wallMask | wallMask_AI; // Unit_Base에 남아있는 마스크
        foreach (var h in hits)
        {
            var col = h.collider;
            if (!col) continue;

            // 벽 먼저 맞으면 그 뒤는 차단
            if ((block.value & (1 << col.gameObject.layer)) != 0)
                return false;

            var ub = col.GetComponent<Unit_Base>();
            if (ub && ub != this && ub.unitType == enemyType && !ub.HasSpeical(Speical.Disappear))
            {
                seen = ub;
                return true;
            }
        }
        return false;
    }

    public int TileDistance(Vector2 a, Vector2 b)
    {
        Vector2Int d = Vector2Int.RoundToInt(b - a);
        return Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y));
    }

    protected void PlaySE(string seId, Vector3 worldPos)
    {
        if (string.IsNullOrEmpty(seId))
            return;

        // AudioListener(= 카메라) Z 기준으로 보정
        if (Camera.main != null)
        {
            worldPos.z = Camera.main.transform.position.z;
        }

        AudioController.Play(seId, worldPos);
    }

    public void Get_StopMoving(float duration)
    {
        if (movement != null) movement.StopMovingFor(duration);
    }

    public void Get_Move(Vector2 dir)
    {
        if (movement != null) movement.Get_Move(dir);
    }

    public void Get_Dash(Vector2 dashDir, int tiles, float dashTotalDuration, float stopMoveDuration)
    {
        if (movement != null) movement.Get_Dash(dashDir, tiles, dashTotalDuration, stopMoveDuration);
    }
}
