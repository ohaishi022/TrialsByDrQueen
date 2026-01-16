using UnityEngine;

public class Unit_JackOLantern : Unit_Base
{

    private new void Awake()
    {
        base.Awake();
    }

    // Update ¼û±â±â + base.Update È£Ãâ
    private new void Update()
    {
        base.Update();

        if (isMoving)
        {
            speicals |= Speical.Float;
        }
        else if (!isMoving)
        {
            speicals &= ~Speical.Float;
        }
    }
}
