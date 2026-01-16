using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAutoHeal : MonoBehaviour
{
    [Header("Regen Settings")]
    [Tooltip("마지막 피해 후 회복 시작까지 대기 시간(초)")]
    public float idleDelay = 8f;

    private Unit_Base unit;
    private float lastDamageTime;
    private float prevHealth;

    private void Awake()
    {
        unit = GetComponent<Unit_Base>();
        if (unit == null)
        {
            enabled = false;
            Debug.LogWarning("[AutoRegen_Player] Unit_Base가 없어 비활성화됨.");
            return;
        }
        prevHealth = unit.currentHealth;
        lastDamageTime = Time.time;
    }

    private void Update()
    {
        if (unit == null) return;

        // 이미 풀피면 끝
        if (unit.currentHealth >= unit.health)
        {
            prevHealth = unit.currentHealth;
            return;
        }

        // 피해 감지
        if (unit.currentHealth < prevHealth)
        {
            lastDamageTime = Time.time;
        }

        // 최근 피해 이후 idleDelay가 지났다면 회복 시작
        if (Time.time - lastDamageTime >= idleDelay)
        {
            // 체력비율(0~1): 낮을수록 빠르게 회복
            float ratio = Mathf.Clamp01(unit.currentHealth / Mathf.Max(1f, unit.health));

            // ★ 전체 체력의 퍼센트로 회복량 계산
            float maxRegenPerSec = unit.health * 0.10f; // 10%
            float minRegenPerSec = unit.health * 0.01f; // 1%

            // 체력이 낮을수록 max에 가깝도록 보간
            float regenPerSec = Mathf.Lerp(maxRegenPerSec, minRegenPerSec, ratio);

            float healThisFrame = regenPerSec * Time.deltaTime;
            if (healThisFrame > 0f)
            {
                unit.TakeHeal(healThisFrame, ignoreCutScene: true);
            }
        }

        prevHealth = unit.currentHealth;
    }
}
