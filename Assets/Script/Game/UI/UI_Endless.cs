using UnityEngine;
using UnityEngine.UI;

public class UI_Endless : UI_Base
{
    [SerializeField] private Image switchHealthFillImage;

    public override void Initialize()
    {

    }

    public void UpdateSwitchHealthBar()
    {
        if (GameMode_Endless.Instance == null) return;
        if (switchHealthFillImage == null) return;

        float ratio =
            Mathf.Clamp01(GameMode_Endless.Instance.SwitchCurrentHealth /
                          GameMode_Endless.Instance.SwitchMaxHealth);

        switchHealthFillImage.fillAmount = ratio;
    }
}