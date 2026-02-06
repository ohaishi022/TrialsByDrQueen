using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class Skill_StrawHacking : Skill_Base
{
    [Header("Damage")]
    public float unitDamage = 50f;
    public float switchDamage = 1f;

    [Header("Timing")]
    public float startDelay = 0.1f;     // 시전 후 해킹 시작까지
    public float tickInterval = 1f;

    [Header("Area")]
    public float unitDamageArea = 2f;
    public Vector2 switchDamageArea = new Vector2(1f, 1f);

    private Unit_StrawWingHacker strawWingHacker;

    private void Awake()
    {
        cooldownTime = 0.5f;
        canDeactivate = false;
    }

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        strawWingHacker = user as Unit_StrawWingHacker;
        if (strawWingHacker == null)
        {
            isActive = false;
            yield break;
        }

        isActive = true;
        user.stopMoving = true;

        // 시작 애니메이션
        user.animator.Play("Skill_StrawHacking_Start");

        // ⏱ 시전 딜레이 (배속/Freeze 반영)
        yield return WaitScaled(startDelay);

        // 도중 취소되었으면 종료
        if (!isActive || strawWingHacker.isDestroy)
        {
            EndSkill();
            yield break;
        }

        // 해킹 시작
        yield return HackingLoop();

        EndSkill();
        yield return StartCooldown();
    }

    /// <summary>
    /// 해킹 시작 + 유지 루프
    /// </summary>
    private IEnumerator HackingLoop()
    {
        canDeactivate = true;

        Instantiate(
            Resources.Load("Prefab/Effect/StrawHacking/Effect_StrawHacking_Start"),
            strawWingHacker.SkillPosition,
            Quaternion.identity
        );

        // === 초기 주변 유닛 피해 ===
        Collider2D[] hitEnemies =
            Physics2D.OverlapCircleAll(strawWingHacker.SkillPosition, unitDamageArea);

        foreach (var hit in hitEnemies)
        {
            Unit_Base enemy = hit.GetComponent<Unit_Base>();
            if (enemy != null && enemy.unitType != strawWingHacker.unitType)
            {
                enemy.TakeDamage(unitDamage);
            }
        }

        AudioController.Play(
            "SE_Skill_StrawHacking_Detonation",
            strawWingHacker.transform.position
        );

        float tickTimer = 0f;

        // === Switch 지속 피해 ===
        while (isActive && !strawWingHacker.isDestroy)
        {
            bool hasSwitch = false;

            Collider2D[] hits =
                Physics2D.OverlapBoxAll(
                    strawWingHacker.SkillPosition,
                    switchDamageArea,
                    0f
                );

            foreach (var h in hits)
            {
                if (h.CompareTag("Switch"))
                {
                    hasSwitch = true;

                    if (GameMode_Endless.Instance != null)
                        GameMode_Endless.Instance.DamageSwitch(switchDamage);
                    else
                    {
                        isActive = false;
                        yield break;
                    }

                    break;
                }
            }

            if (!hasSwitch)
            {
                isActive = false;
                yield break;
            }

            // ⏱ 틱 대기 (배속/Freeze 반영)
            tickTimer = 0f;
            while (tickTimer < tickInterval)
            {
                float dt = GetSkillDeltaTime();
                if (dt > 0f)
                    tickTimer += dt;

                if (!isActive)
                    yield break;

                yield return null;
            }
        }
    }

    public override void DeactivateSkill()
    {
        if (!isActive)
            return;

        isActive = false;
    }

    protected override void OnCanceled()
    {
        EndSkill();
    }

    private void EndSkill()
    {
        if (strawWingHacker != null && !strawWingHacker.isDestroy)
        {
            strawWingHacker.animator.Play("Idle");
            strawWingHacker.stopMoving = false;
        }
    }

    public void StopAnimationOnDeath()
    {
        if (strawWingHacker != null)
            strawWingHacker.animator.Play("Destroy");

        ForceCancel();
    }
}
