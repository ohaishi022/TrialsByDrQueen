using UnityEngine;

public class Projectile_Arrow_Freeze : Projectile_Base
{
    public float freezeDuration = 1f;

    protected override void OnHitEnemy(Unit_Base unit, Collider2D col)
    {
        if (unit.Status != null)
            unit.Status.Add(new Debuff_Freeze(freezeDuration));
    }
}
