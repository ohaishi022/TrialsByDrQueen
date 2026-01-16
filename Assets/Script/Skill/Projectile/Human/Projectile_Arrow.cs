using UnityEngine;

public class Projectile_Arrow : Projectile_Base
{
    public float damage = 20f;

    private void Reset()
    {
        speed = 18.75f;
        range = 7f;
        lifetime = 2f;

        affectEnemies = true;
        affectAllies = false;

        pierceCount = 0; // 무한 관통
    }

    protected override void OnHitEnemy(Unit_Base unit, Collider2D col)
    {
        unit.TakeDamage(damage);
    }
}
