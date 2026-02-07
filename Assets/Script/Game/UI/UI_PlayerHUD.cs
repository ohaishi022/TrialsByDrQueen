using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_PlayerHUD : MonoBehaviour
{
    [Header("Health UI")]
    public Slider hpSlider;          // 체력바 슬라이더
    public TMP_Text hpText;          // "100 / 100" 텍스트

    [Header("Skill UI")]
    public UI_SkillSlot skillSlot1;  // 좌클릭 스킬 (인덱스 0)
    public UI_SkillSlot skillSlot2;  // 우클릭 스킬 (인덱스 1)

    private GameSceneManager sceneManager;
    private Unit_Base playerUnit;

    void Start()
    {
        // 씬 매니저 찾기
        GameObject managerObj = GameObject.FindGameObjectWithTag("GameSceneManager");
        if (managerObj != null)
        {
            sceneManager = managerObj.GetComponent<GameSceneManager>();
        }
    }

    void Update()
    {
        // 플레이어 유닛이 변경되었거나(리스폰 등), 아직 할당 안 된 경우 갱신
        if (sceneManager != null && sceneManager.playerUnit != playerUnit)
        {
            RefreshPlayerLink();
        }

        if (playerUnit == null) return;

        UpdateHealth();
        UpdateSkills();
    }

    private void RefreshPlayerLink()
    {
        playerUnit = sceneManager.playerUnit;

        if (playerUnit != null)
        {
            // 스킬 슬롯 초기화
            if (playerUnit.Skills.Count > 0)
                skillSlot1.Initialize(playerUnit.Skills[0]);
            else
                skillSlot1.Initialize(null);

            if (playerUnit.Skills.Count > 1)
                skillSlot2.Initialize(playerUnit.Skills[1]);
            else
                skillSlot2.Initialize(null);
        }
    }

    private void UpdateHealth()
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = playerUnit.health;
            hpSlider.value = playerUnit.currentHealth;
        }

        if (hpText != null)
        {
            // 예: "80 / 100"
            hpText.text = $"{Mathf.CeilToInt(playerUnit.currentHealth)} / {Mathf.CeilToInt(playerUnit.health)}";
        }
    }

    private void UpdateSkills()
    {
        if (skillSlot1 != null) skillSlot1.UpdateSlot();
        if (skillSlot2 != null) skillSlot2.UpdateSlot();
    }
}