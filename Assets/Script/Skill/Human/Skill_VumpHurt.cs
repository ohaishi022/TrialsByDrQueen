using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class Skill_VumpHurt : Skill_Base
{
    public float damage = 30f;
    public float hitDelay = 0.1f;        // 시전 → 타격 지연
    public float healAmount = 15f;

    public Vector2 hitSizeLR = new Vector2(3f, 3f);
    public Vector2 hitSizeUD = new Vector2(3f, 3f);

    public GameObject effect;
    public GameObject healEffect;

    public string SE_Hit = "SE_Skill_Archer_BackStep_Hit";

    private void Awake()
    {
        cooldownTime = 1.2f;

        if (!effect)
            effect = Resources.Load<GameObject>(
                "Prefab/Skill/Human/Vumpire/Skill_VumpHurt");
        if (!healEffect)
            healEffect = Resources.Load<GameObject>(
                "Prefab/Skill/Human/Vumpire/Skill_VumpHurt_Heal");
    }

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        isActive = true;
        user.stopMoving = true;

        // ▶ 이펙트 + 사운드
        PlayEffect(user);
        if (!string.IsNullOrEmpty(SE_Hit))
            AudioController.Play(SE_Hit, user.transform.position);

        // ▶ 타격 타이밍
        yield return WaitScaled(hitDelay);

        ApplyDamageAndHeal(user);

        user.stopMoving = false;
        isActive = false;

        yield return StartCooldown();
    }

    private void PlayEffect(Unit_Base user)
    {
        if (!effect) return;

        float zRot = user.FacingDirection switch
        {
            Direction.Left => -90f,
            Direction.Up => 180f,
            Direction.Right => 90f,
            _ => 0f
        };

        Instantiate(
            effect,
            user.transform.position,
            Quaternion.Euler(0, 0, zRot),
            user.transform
        );
    }

    private void ApplyDamageAndHeal(Unit_Base user)
    {
        Vector2 forward = user.DirectionVector;
        Vector2 center = (Vector2)user.transform.position + forward * 0.5f;

        Vector2 size =
            Mathf.Abs(forward.x) > 0f ? hitSizeLR : hitSizeUD;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f);

        bool healed = false;

        foreach (var h in hits)
        {
            var enemy = h.GetComponent<Unit_Base>();
            if (!enemy || enemy.unitType == user.unitType)
                continue;

            enemy.TakeDamage(damage);

            bool enemyIsObject = enemy.CategoryHas(UnitCategory.Object);
            bool enemyHidden = enemy.HasSpeical(Speical.Disappear);
            bool enemyInvincible = enemy.HasBuff(BuffId.Invincible);

            if (!healed && !enemyIsObject && !enemyHidden && !enemyInvincible)
            {
                healed = true;
                user.TakeHeal(healAmount);

                if (healEffect)
                    Instantiate(
                        healEffect,
                        user.transform.position,
                        Quaternion.identity,
                        user.transform
                    );
            }
        }
    }
}
