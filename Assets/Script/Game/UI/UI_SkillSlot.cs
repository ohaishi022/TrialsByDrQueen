using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_SkillSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;           // 스킬 아이콘 이미지
    public Image cooldownOverlay;     // 쿨타임 어두운 배경 (Fill Amount 사용)
    public TMP_Text cooldownText;     // 남은 쿨타임 초 (숫자)
    public TMP_Text stockText;        // 남은 탄창 수 (우측 하단 등)
    public GameObject lockIcon;       // (선택) 사용 불가/잠김 표시

    private Skill_Base targetSkill;

    public void Initialize(Skill_Base skill)
    {
        targetSkill = skill;

        if (targetSkill != null && targetSkill.icon != null)
        {
            iconImage.sprite = targetSkill.icon;
            iconImage.enabled = true;
        }
        else
        {
            // 아이콘이 없으면 투명하게 처리하거나 기본 이미지
            iconImage.enabled = false;
        }

        if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
        if (cooldownText != null) cooldownText.text = "";
        if (stockText != null) stockText.text = "";
    }

    public void UpdateSlot()
    {
        if (targetSkill == null) return;

        // 1. 쿨타임 처리
        if (targetSkill.IsOnCooldown())
        {
            float current = targetSkill.CurrentCooldown;
            float max = targetSkill.cooldownTime;

            // Fill Amount (0~1)
            if (cooldownOverlay != null)
                cooldownOverlay.fillAmount = Mathf.Clamp01(current / max);

            // Text 표시 (소수점 첫째자리까지, 0이면 숨김)
            if (cooldownText != null)
            {
                if (current > 0)
                    cooldownText.text = current.ToString("F1");
                else
                    cooldownText.text = "";
            }
        }
        else
        {
            // 쿨타임 아닐 때
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
            if (cooldownText != null) cooldownText.text = "";
        }

        // 2. 탄창(Stock) 텍스트 처리 (Raygun 등)
        if (stockText != null)
        {
            stockText.text = targetSkill.GetStockText();
        }

        // 3. (선택) 활성화 상태 표시 (isActive인 경우 테두리를 빛나게 하는 등)
        // if (targetSkill.isActive) { ... }
    }
}