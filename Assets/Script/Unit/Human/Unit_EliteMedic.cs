using UnityEngine;

public class Unit_EliteMedic : Unit_Base
{
    public GameObject projectile;

    private void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        // MedicalBurst 스킬 추가
        Skill_MedicalBurst medicalBurst = AddSkill<Skill_MedicalBurst>();
        medicalBurst.projectilePrefab = projectile;
    }

    void Update()
    {
        base.Update();

        if (unitState == UnitState.Player)
        {
            HandlePlayerInput(); // 플레이어 입력 처리
        }
    }

    private void HandlePlayerInput()
    {
        if (unitState == UnitState.Player)
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) // 스킬 활성화
            {
                ActivateSkill(0); // MedicalBurst 스킬 활성화
            }

            else if (Input.GetKeyUp(KeyCode.Mouse0)) // 스킬 비활성화
            {
                DeactivateSkill(0); // Skill_MedicalBurst 비활성화
            }
        }
    }
}