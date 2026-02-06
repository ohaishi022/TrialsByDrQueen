using UnityEngine;

public class Projectile_SneakyRaygun : Projectile_Base
{
    private void Reset()
    {
        affectEnemies = true;
        affectAllies = false;

        pierceCount = 1;
    }

    protected override void OnHitEnemy(Unit_Base unit, Collider2D col)
    {
    }
}
