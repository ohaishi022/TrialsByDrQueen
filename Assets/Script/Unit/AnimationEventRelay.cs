using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    public Unit_Base unitBase;

    private void Awake()
    {
        if (unitBase == null)
            unitBase = GetComponentInParent<Unit_Base>();
    }

    public void OnSpawnAnimationEnd()
    {
        if (unitBase != null)
        {
            unitBase.OnSpawnAnimationEnd();
        }
    }

    public void OnDestroyUnit()
    {
        if (unitBase != null)
        {
            unitBase.DestroyUnitGroup();
        }
    }
}
