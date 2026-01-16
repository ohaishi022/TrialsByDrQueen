using UnityEngine;

public class Projectile_Arrow_Chill : Projectile_Base
{
    public float damage = 20f;

    [Header("Chill")]
    public float chillDuration = 5f;
    [Range(0.1f, 0.9f)]
    public float slowMultiplier = 0.5f;

    protected override void OnHitEnemy(Unit_Base unit, Collider2D col)
    {
        unit.TakeDamage(damage);

        if (unit.Status != null)
        {
            unit.Status.Add(new Debuff_Chill(chillDuration, slowMultiplier));
        }
    }
}
