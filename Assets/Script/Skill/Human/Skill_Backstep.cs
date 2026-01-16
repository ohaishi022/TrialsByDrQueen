using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class Skill_Backstep : Skill_Base
{
    public float damage = 20f;
    public int dashTiles = 5;
    public Vector2 attackVector = new Vector2(2, 1);

    public GameObject skillEffectPrefab; // 스킬 이펙트 프리팹

    public string SE_Hit = "SE_Skill_Archer_BackStep_Hit";

    private void Awake()
    {
        cooldownTime = 4f; // 재충전 시간
        canDeactivate = false;
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

        if (skillEffectPrefab != null)
        {
            float zRot = 0f;
            switch (u.FacingDirection)
            {
                case Direction.Down: zRot = 90f; break;
                case Direction.Left: zRot = 0f; break;
                case Direction.Up: zRot = -90f; break;
                case Direction.Right: zRot = 180f; break;
            }
            var fx = Instantiate(skillEffectPrefab, u.transform.position, Quaternion.Euler(0, 0, zRot), u.transform);
            var se = fx.GetComponent<SkillEffect>();
            if (se != null)
            {
                se.owner = u;
            }
        }

        Vector2 attackStart = (Vector2)u.transform.position + u.DirectionVector;
        Collider2D[] hits = Physics2D.OverlapBoxAll(attackStart, attackVector, 0f);
        foreach (var h in hits)
        {
            var enemy = h.GetComponent<Unit_Base>();
            if (enemy != null && enemy.unitType != u.unitType)
            {
                enemy.TakeDamage(damage);
                AudioController.Play(SE_Hit, enemy.transform.position);
            }
        }

        Vector2 backDir = -u.DirectionVector;
        u.Get_Dash(backDir, dashTiles, 0.1f, 0.2f);
        yield return WaitScaled(0.15f);
        u.ChangeDirection(-backDir);

        isActive = false;
        skillRoutine = null;
        yield return StartCooldown();
    }

    protected override void OnCanceled()
    {
        base.OnCanceled();

        if (owner != null)
        {
            owner.stopMoving = false;
        }
    }
}
