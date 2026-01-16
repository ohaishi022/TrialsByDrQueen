using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class Skill_MedicalBurst : Skill_Base
{
    public GameObject projectilePrefab;
    public int holdEnergy = 40; // 홀드에너지 최대치
    private int currentHoldEnergy; // 현재 홀드에너지
    private bool isOnCooldown = false; // 쿨다운 중인지
    private bool isUsing = false; // 스킬 사용 중인지
    private Coroutine regenerateCoroutine; // 재충전 코루틴 참조

    void Awake()
    {
        cooldownTime = 2f; // 과부하 쿨다운 시간
        currentHoldEnergy = holdEnergy; // 초기 홀드 에너지 설정
    }

    public override void ActivateSkill(Unit_Base user)
    {
        if (isOnCooldown)
        {
            Debug.Log($"{user.unitName}의 스킬이 재충전 중입니다.");
            return;
        }
        if (!isUsing && currentHoldEnergy > 0)
        {
            if (regenerateCoroutine != null) // 재충전 중이면 중지
            {
                StopCoroutine(regenerateCoroutine);
                regenerateCoroutine = null;
            }
            isUsing = true;
            StartCoroutine(SkillRoutine(user));
        }
    }

    public override void DeactivateSkill()
    {
        if (isUsing)
        {
            isUsing = false;
            StopCoroutine("SkillRoutine"); // 특정 코루틴만 중지
            regenerateCoroutine = StartCoroutine(RegenerateEnergy()); // 재충전 시작
        }
    }

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        while (currentHoldEnergy > 0 && isUsing)
        {
            GameObject projectile = Instantiate(projectilePrefab, user.transform.position, Quaternion.identity);
            Projectile projectileScript = projectile.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.direction = user.DirectionVector;
                projectileScript.positiveUnitType = user.unitType;
                projectileScript.shooter = user.gameObject;
            }

            currentHoldEnergy -= 1;
            Debug.Log($"Current Hold Energy: {currentHoldEnergy}");
            yield return new WaitForSeconds(0.128f);
        }

        if (currentHoldEnergy <= 0)
        {
            StartCoroutine(StartCooldown());
        }
        isUsing = false;
    }

    private IEnumerator StartCooldown()
    {
        isOnCooldown = true;
        GameObject effect = Instantiate(Resources.Load("Prefab/Effect/MedicalBurst/Effect_MedicalBurst_Reload"), gameObject.transform.position, Quaternion.identity) as GameObject;
        effect.transform.parent = gameObject.transform;
        AudioController.Play("SE_Skill_MedicalBurst_Reload", gameObject.transform);
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
        currentHoldEnergy = holdEnergy;
    }

    private IEnumerator RegenerateEnergy()
    {
        yield return new WaitForSeconds(1.0f); // 스킬 사용 종료 후 1초 대기
        while (currentHoldEnergy < holdEnergy)
        {
            yield return new WaitForSeconds(0.1f);
            currentHoldEnergy++;
            Debug.Log($"Recharging Energy: {currentHoldEnergy}/{holdEnergy}");
        }
    }
}
