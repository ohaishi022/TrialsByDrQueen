using CS.AudioToolkit;
using UnityEngine;

public class Unit_StrawWingHacker : Unit_Base
{
    private float hackingRange = 0.5f; // 스위치와의 거리 판정 범위
    private Transform switchTarget;

    private void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        base.Start();

        AudioController.Play("SE_Unit_StrawWingHacker_Spawn", transform.position);
        Skill_StrawHacking hackingSkill = AddSkill<Skill_StrawHacking>();
    }

    void Update()
    {
        base.Update();

        if (gameSceneManager.CurrentSituation == GameSituation.CutScene && Skills.Count > 0 && Skills[0].isActive)
        {
            DeactivateSkill(0);
        }
    }

    protected override void HandlePlayerInput()
    {
        base.HandlePlayerInput();

        if (unitState == UnitState.Player)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) // 스트로윙 해킹 활성화
            {
                if (Skills[0].isActive == false)
                {
                    ActivateSkill(0); // 활성화
                }
                else if(Skills[0].isActive == true && Skills[0].canDeactivate == true)
                {
                    DeactivateSkill(0); // 비활성화
                }
            }
        }
    }

    protected override void HandleAIInput()
    {
        if (GameMode_Endless.Instance != null && GameMode_Endless.Instance.currentWaveState == WaveState.TutorialWave)
        {
            return; // AI 입력 무시
        }

        if (switchTarget == null)
        {
            FindClosestSwitch();
        }

        if (switchTarget != null)
        {
            float distance = Vector2.Distance(transform.position, switchTarget.position);

            if (distance <= hackingRange)
            {
                // 이미 해킹 스킬이 실행 중인지 확인
                if (Skills.Count > 0 && Skills[0] != null && !Skills[0].isActive)
                {
                    Debug.Log("스위치에 도달하여 해킹 시도.");
                    ActivateSkill(0); // Skill_StrawHacking
                }
            }
            else
            {
                // 스위치로 경로 이동
                FindPath(transform.position, switchTarget.position);
                MoveAlongPath();
            }
        }
    }

    private void FindClosestSwitch()
    {
        GameObject[] switches;
        try
        {
            switches = GameObject.FindGameObjectsWithTag("Switch");
        }
        catch (UnityException)
        {
            Debug.LogWarning("유니티 태그 'Switch'가 정의되어 있지 않습니다. 태그를 생성해 주세요.");
            return;
        }

        float closestDistance = float.MaxValue;
        Transform closestSwitch = null;

        foreach (var sw in switches)
        {
            float dist = Vector2.Distance(transform.position, sw.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestSwitch = sw.transform;
            }
        }

        if (closestSwitch != null)
        {
            switchTarget = closestSwitch;
        }
        else
        {
            Debug.LogWarning("맵에 감지 가능한 스위치가 없습니다.");
        }
    }

    public override void DestroyUnit(bool ignoreCutScene)
    {
        if (isDestroy) return; // base에서도 검사하긴 하지만 이펙트 중복 소환 방지를 위해 먼저 체크
        if (Skills[0].isActive == true && Skills[0].canDeactivate == true)
        {
            DeactivateSkill(0);
        }
        if (Skills[0].isActive)
        {
            Skills[0].GetComponent<Skill_StrawHacking>().StopAnimationOnDeath();
        }
        AudioController.Play("SE_Unit_StrawWingHacker_Destroy", transform.position);
        GameObject destroyEffect = Resources.Load<GameObject>("Prefab/Effect/StrawWingHacker/Effect_StrawWingHacker_Destroy");
        if (destroyEffect != null)
        {
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        }

        base.DestroyUnit(ignoreCutScene);
    }
}