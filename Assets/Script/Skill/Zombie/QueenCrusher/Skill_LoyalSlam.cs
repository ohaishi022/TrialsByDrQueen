using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class Skill_LoyalSlam : Skill_Base
{
    public float damage = 30f;
    public float hitDelay = 0.5f;
    public Vector2 attackVector = new Vector2(2, 1);

    public Vector2 hitSize = new Vector2(3f, 3f);

    public GameObject effect;     // 공격 이펙트

    public string SE_Hit = "SE_Skill_Archer_BackStep_Hit";

    private void Awake()
    {
        cooldownTime = 1.8f;   // 요구한 쿨타임
        canDeactivate = false;

        if (effect == null)
            effect = Resources.Load<GameObject>("Prefab/Skill/Human/Vumpire/Skill_VumpHurt");
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
        user.stopMoving = true;
        yield return WaitScaled(hitDelay);
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

        ApplyDamage(user);

        user.stopMoving = false;
        isActive = false;

        yield return StartCooldown();
    }

    private void ApplyDamage(Unit_Base user)
    {
        Vector2 forward = user.DirectionVector;
        Vector2 center = (Vector2)user.transform.position + forward * 0.5f;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitSize, 0f);

        foreach (var h in hits)
        {
            var enemy = h.GetComponent<Unit_Base>();
            if (!enemy || enemy.unitType == user.unitType)
                continue;

            enemy.TakeDamage(damage);

            bool enemyIsObject = enemy.CategoryHas(UnitCategory.Object);
            bool enemyHidden = enemy.HasSpeical(Speical.Disappear);
            bool enemyInvincible = enemy.HasBuff(BuffId.Invincible);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 플레이 중이면 owner 기준, 아니면 자기 오브젝트 기준
        Unit_Base u = owner != null ? owner : GetComponentInParent<Unit_Base>();
        Transform t = u != null ? u.transform : transform;

        Vector2 forward = (u != null) ? u.DirectionVector : Vector2.zero;

        Vector2 center = (Vector2)t.position + forward * 0.5f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, hitSize);
    }
}
