using UnityEngine;

public class Projectile_GrenadePunt : Projectile_Base
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

    protected override void OnHitEnvironment(Collider2D col)
    {
        Explode();
        DestroySelf();
    }
}
