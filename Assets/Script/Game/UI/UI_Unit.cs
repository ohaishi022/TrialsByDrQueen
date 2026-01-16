using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UI_Unit : MonoBehaviour
{
    public Unit_Base unit;

    public CanvasGroup healthBarGroup;
    public Image healthBarFill;
    //public TMP_Text nameText;

    public Vector3 offset = new Vector3(0, 2.5f, 0);

    public float visibleTime = 2f;
    public float fadeTime = 4f;

    private float currentHP;
    private float maxHP;

    private Coroutine fadeRoutine;
    public Transform target;

    private void Awake()
    {
        UpdateHPBar();
        ShowHealthBar();
    }

    private void LateUpdate()
    {
        if (unit == null)
        {
            Destroy(gameObject);
            return;
        }

        // 2) 게임플레이카메라(MainCamera)가 없으면 좌표 업데이트 안 함
        if (Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position + offset);
            transform.position = screenPos;
        }

        else
        {
            healthBarGroup.alpha = 0f;
        }
    }

    //public void SetName(string unitName)
    //{
    //    if (nameText != null)
    //        nameText.text = unitName;
    //}

    public void InitializeHP(float maxHealth)
    {
        maxHP = maxHealth;
        currentHP = maxHealth;
        UpdateHPBar();
    }

    public void SetHPColor(UnitType type)
    {
        if (healthBarFill == null) return;

        switch (type)
        {
            case UnitType.Human:
                healthBarFill.color = new Color(0f, 1f, 0f); // 초록
                break;

            case UnitType.Zombie:
                healthBarFill.color = new Color(0.63f, 0.13f, 0.94f); // 보라 (A020F0 계열)
                break;

            case UnitType.None:
            default:
                healthBarFill.color = Color.white; // 하얀색(무색)
                break;
        }
    }

    public void OnDamaged(float newHP, float newMaxHP)
    {
        currentHP = newHP;
        maxHP = newMaxHP;

        UpdateHPBar();
        ShowHealthBar();
    }

    private void UpdateHPBar()
    {
        if (healthBarFill == null) return;

        healthBarFill.fillAmount = Mathf.Clamp01(currentHP / maxHP);
    }

    private void ShowHealthBar()
    {
        if (healthBarGroup == null) return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        healthBarGroup.alpha = 1f;
        fadeRoutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        yield return new WaitForSeconds(visibleTime);

        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            healthBarGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }

        healthBarGroup.alpha = 0f;
    }
}
