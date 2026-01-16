using UnityEngine;
using UnityEngine.UI;

public class UI_Gameplay : MonoBehaviour
{
    public static UI_Gameplay Instance { get; private set; }
    [SerializeField] private Image switchHealthFillImage;

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("UI_Gameplay 인스턴스가 중복되어 파괴됨.");
            Destroy(gameObject); // 중복 방지 (선택 사항)
        }

        if (switchHealthFillImage == null)
        {
            Debug.LogWarning("switchHealthFillImage가 인스펙터에서 할당되지 않음.");
        }
    }

    public void UpdateSwitchHealthBar()
    {
        if (GameMode_Endless.Instance != null && switchHealthFillImage != null)
        {
            float ratio = Mathf.Clamp01(GameMode_Endless.Instance.SwitchCurrentHealth / GameMode_Endless.Instance.SwitchMaxHealth);
            switchHealthFillImage.fillAmount = ratio;
        }
    }
}
