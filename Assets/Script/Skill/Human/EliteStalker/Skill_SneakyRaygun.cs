using UnityEngine;
using CS.AudioToolkit;
using System.Collections;

public class Skill_SneakyRaygun : Skill_Base
{
    [Header("Projectile")]
    public GameObject projectilePrefab;

    [Header("Energy")]
    public int maxEnergy = 40;
    private int currentEnergy;

    [Header("Fire")]
    public float fireInterval = 0.1f;
    private Coroutine fireRoutine;
    private float fireTimer = 0f;
    private bool activationLocked;
    private Coroutine lockRoutine;

    [Header("Reload")]
    public float reloadInterval = 0.1f;
    public float reloadDelay = 1f;
    private Coroutine reloadRoutine;

    [Header("Audio")]
    public string shootLoopSE = "SE_Skill_SneakyRaygun_Loop";
    public string shootEndSE = "SE_Skill_SneakyRaygun_End";
    private AudioObject loopAudioObj;

    private void Awake()
    {
        cooldownTime = 2f;
        canDeactivate = true;
        currentEnergy = maxEnergy;
    }

    public override string GetStockText()
    {
        return $"{currentEnergy} / {maxEnergy}"; // 또는 $"{currentEnergy}/{maxEnergy}"
    }

    public override void ActivateSkill(Unit_Base user)
    {
        if (activationLocked) return;
        if (isOnCooldown || currentEnergy <= 0) return;
        if (fireRoutine != null) return;

        base.ActivateSkill(user);
        //fireRoutine = StartCoroutine(SkillRoutine(user));
    }

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        Unit_Base u = user ?? owner;
        if (u == null)
        {
            Cleanup();
            yield break;
        }

        loopAudioObj = PlaySEAttached(shootLoopSE, u.transform);

        FireOnce(u);

        fireTimer = 0f;
        while (isActive && currentEnergy > 0)
        {
            if (u.SpeedMultiplier <= 0f) { yield return null; continue; }

            fireTimer += Time.deltaTime * u.SpeedMultiplier;

            if (fireTimer >= fireInterval)
            {
                fireTimer = 0;
                FireOnce(u);
            }

            yield return null;
        }

        Cleanup();

        if (currentEnergy > 0 && currentEnergy < maxEnergy && reloadRoutine == null)
        {
            reloadRoutine = StartCoroutine(RegenerateEnergy(u));
        }

        if (currentEnergy <= 0)
        {
            yield return StartCooldownScaled(u);

            // Freeze 중이면 대기
            while (u.SpeedMultiplier <= 0f)
                yield return null;

            currentEnergy = maxEnergy;
            //Debug.Log($"[SneakyRaygun] 쿨다운 종료 → 탄창 {currentEnergy}/{maxEnergy}");
        }
    }

    private void FireOnce(Unit_Base u)
    {
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            reloadRoutine = null;
        }

        if (currentEnergy <= 0)
            return;

        Vector3 spawnPos = u.SkillPosition;

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile_Base p = go.GetComponent<Projectile_Base>();

        if (p == null)
        {
            Destroy(go);
            return;
        }

        p.shooter = u.gameObject;
        p.positiveUnitType = u.unitType;
        p.direction = u.DirectionVector.normalized;

        currentEnergy--;
        //Debug.Log($"[SneakyRaygun] 탄창 {currentEnergy}/{maxEnergy}");
    }

    public override void DeactivateSkill()
    {
        if (!isActive)
            return;

        isActive = false;

        if (lockRoutine != null) StopCoroutine(lockRoutine);
        lockRoutine = StartCoroutine(ActivationLock(owner));
    }

    private IEnumerator ActivationLock(Unit_Base u)
    {
        activationLocked = true;
        float t = 0f;

        while (t < fireInterval)
        {
            // Freeze면 시간 안 흐르게
            if (u != null && u.SpeedMultiplier > 0f)
                t += Time.deltaTime * u.SpeedMultiplier;

            yield return null;
        }

        activationLocked = false;
        lockRoutine = null;
    }

    private void Cleanup()
    {
        isActive = false;

        if (loopAudioObj != null)
        {
            loopAudioObj.Stop();   // 툴킷 쪽 페이드/설정 반영되며 멈춤
            loopAudioObj = null;
        }
        AudioController.Play(shootEndSE, owner.transform);

        fireRoutine = null;
    }

    protected override void OnCanceled()
    {
        if (loopAudioObj != null)
        {
            loopAudioObj.Stop();   // 툴킷 쪽 페이드/설정 반영되며 멈춤
            loopAudioObj = null;
        }
    }

    private IEnumerator RegenerateEnergy(Unit_Base u)
    {
        float delayTimer = 0f;
        float tickTimer = 0f;

        // 재장전 딜레이
        while (delayTimer < reloadDelay)
        {
            if (u.SpeedMultiplier > 0f)
                delayTimer += Time.deltaTime * u.SpeedMultiplier;

            yield return null;
        }

        while (currentEnergy < maxEnergy)
        {
            if (u.SpeedMultiplier <= 0f)
            {
                yield return null;
                continue;
            }

            tickTimer += Time.deltaTime * u.SpeedMultiplier;

            if (tickTimer >= reloadInterval)
            {
                tickTimer -= reloadInterval;
                currentEnergy = Mathf.Min(currentEnergy + 1, maxEnergy);
                //Debug.Log($"[SneakyRaygun] 탄창 {currentEnergy}/{maxEnergy}");
            }

            yield return null;
        }

        reloadRoutine = null;
    }

    // 배속 적용 쿨다운
    private IEnumerator StartCooldownScaled(Unit_Base u)
    {
        isOnCooldown = true;
        float timer = 0f;

        while (timer < cooldownTime)
        {
            if (u.SpeedMultiplier > 0f)
                timer += Time.deltaTime * u.SpeedMultiplier;

            yield return null;
        }

        isOnCooldown = false;
    }
}
