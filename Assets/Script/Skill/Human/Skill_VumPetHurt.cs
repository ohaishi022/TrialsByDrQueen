using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class Skill_VumPetHurt : Skill_Base
{
    public float damage = 20f;
    public float healPerTarget = 5f;

    public Vector2 attackVector = new Vector2(2, 1);

    public GameObject effect;     // 공격 이펙트
    public GameObject healEffect; // 회복 이펙트

    public string SE_Hit = "SE_Skill_Archer_BackStep_Hit";

    private void Awake()
    {
        cooldownTime = 1.5f;   // 요구한 쿨타임
        canDeactivate = false;

        if (effect == null)
            effect = Resources.Load<GameObject>("Prefab/Skill/Human/Vumpire/Skill_VumpHurt");

        if (healEffect == null)
            healEffect = Resources.Load<GameObject>("Prefab/Skill/Human/Vumpire/Skill_VumpHurt_Heal");
    }

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        Unit_Base u = user ?? owner;
        if (u == null)
        {
            isActive = false;
            yield break;
        }

        isActive = true;

        AudioController.Play(SE_Hit, u.transform.position);

        // ===== 공격 이펙트 생성 =====
        if (effect != null)
        {
            float zRotation = 0f;
            switch (u.FacingDirection)
            {
                case Direction.Down: zRotation = 0f; break;
                case Direction.Left: zRotation = -90f; break;
                case Direction.Up: zRotation = 180f; break;
                case Direction.Right: zRotation = 90f; break;
            }

            Instantiate(effect, user.transform.position, Quaternion.Euler(0, 0, zRotation), user.transform);
        }

        Vector2 forward = u.DirectionVector;
        Vector2 boxCenter = (Vector2)u.transform.position + forward * 0.5f;

        Vector2 boxSize;
        if (Mathf.Abs(forward.x) > 0f)      // 좌/우
            boxSize = new Vector2(2f, 1f);
        else                                // 상/하
            boxSize = new Vector2(1f, 2f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);

        foreach (var h in hits)
        {
            var enemy = h.GetComponent<Unit_Base>();
            if (enemy != null && enemy.unitType != u.unitType)
            {
                enemy.TakeDamage(damage);
                bool canHeal =
                    !enemy.CategoryHas(UnitCategory.Object) &&
                    !enemy.HasBuff(BuffId.Invincible) &&
                    !enemy.HasSpeical(Speical.Disappear);

                if (canHeal)
                {
                    u.TakeHeal(healPerTarget);

                    if (healEffect != null)
                        Instantiate(healEffect, u.transform.position, Quaternion.identity, u.transform);
                }

                AudioController.Play(SE_Hit, enemy.transform.position);
            }
        }

        isActive = false;
        skillRoutine = null;
        yield return StartCooldown();   // 쿨타임(배속 적용)
    }
}
