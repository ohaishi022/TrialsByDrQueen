using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class Skill_BatWarp : Skill_Base
{
    [Header("FX / Unit")]
    public GameObject warpEffect;
    public GameObject vumPetPrefab;

    [Header("Audio")]
    public string SE_Intro = "SE_Skill_BatWarp_Intro";
    public string SE_Loop = "SE_Skill_BatWarp_Loop";
    public string SE_End = "SE_Skill_BatWarp_End";

    [Header("Warp")]
    public int minWarpDist = 10;
    public int maxWarpDist = 20;
    public int warpSearchAttempts = 30;

    [Header("Timing")]
    public float introDelay = 0.5f;   // 무적 연출
    public float disappearTime = 4f;  // 박쥐 상태 유지
    public float outroDelay = 0.5f;   // 복귀 연출

    private void Awake()
    {
        cooldownTime = 12f;
        canDeactivate = false;

        if (!warpEffect)
            warpEffect = Resources.Load<GameObject>(
                "Prefab/Skill/Human/Vumpire/Skill_BatWarp");

        if (!vumPetPrefab)
            vumPetPrefab = Resources.Load<GameObject>(
                "Prefab/Unit/Human/Unit_Human_VumPet");
    }

    protected override IEnumerator SkillRoutine(Unit_Base user)
    {
        Unit_Base u = user ?? owner;
        if (!u) yield break;

        isActive = true;
        u.stopMoving = true;

        SpawnEffect(u);

        AudioController.Play(SE_Intro, u.transform.position);
        if (u.Status != null)
            u.Status.Add(new Buff_Invincible(introDelay));

        // 인트로 연출
        yield return WaitScaled(introDelay);

        TrySummonVumPet(u);

        // 박쥐 변신
        u.EnterDisappear();

        AudioController.Play(SE_Loop, u.transform.position);

        yield return WaitScaled(disappearTime);

        // 워프 위치 계산
        Vector2 warpPos = u.transform.position;
        if (TryFindRandomWarpLocation(
            u, minWarpDist, maxWarpDist, warpSearchAttempts, out Vector2 found))
        {
            warpPos = found;
        }

        u.TeleportUnit(warpPos);

        SpawnEffect(u);
        AudioController.Play(SE_End, u.transform.position);

        yield return WaitScaled(outroDelay);

        // 상태 복구
        u.ExitDisappear();
        u.stopMoving = false;

        Cleanup();
        isActive = false;

        yield return StartCooldown();
    }

    // ===================== FX =====================
    private void SpawnEffect(Unit_Base u)
    {
        if (!warpEffect) return;

        var fx = Instantiate(warpEffect, u.SkillPosition, Quaternion.identity);
        var se = fx.GetComponent<SkillEffect>();
        if (se) se.owner = u;
    }

    private void Cleanup()
    {
        AudioController.Stop(SE_Loop);
    }

    protected override void OnCanceled()
    {
        base.OnCanceled();

        Cleanup();

        if (owner)
        {
            owner.ExitDisappear();
            owner.stopMoving = false;
        }
    }

    // ===================== Warp Logic =====================
    private bool TryFindRandomWarpLocation(
        Unit_Base user,
        int minDist,
        int maxDist,
        int attempts,
        out Vector2 resultPos)
    {
        resultPos = user.transform.position;

        if (!user.gridManager || user.gridManager.grid == null)
            return false;

        Node startNode =
            user.gridManager.NodeFromWorldPoint(user.transform.position);
        if (startNode == null) return false;

        for (int i = 0; i < attempts; i++)
        {
            int dist = Random.Range(minDist, maxDist + 1);
            float ang = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            int cx = startNode.gridX + Mathf.RoundToInt(Mathf.Cos(ang) * dist);
            int cy = startNode.gridY + Mathf.RoundToInt(Mathf.Sin(ang) * dist);

            if (cx < 0 || cy < 0 ||
                cx >= user.gridManager.gridSizeX ||
                cy >= user.gridManager.gridSizeY)
                continue;

            Node cand = user.gridManager.grid[cx, cy];
            if (cand == null || !cand.isWalkable) continue;

            if (!IsReachableRough(user, startNode, cand)) continue;

            resultPos = cand.position;
            return true;
        }
        return false;
    }

    private bool IsReachableRough(
        Unit_Base user,
        Node startNode,
        Node destNode)
    {
        int dx = destNode.gridX - startNode.gridX;
        int dy = destNode.gridY - startNode.gridY;
        int steps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
        if (steps <= 0) steps = 1;

        Vector2 prev = startNode.position;

        for (int s = 1; s <= steps; s++)
        {
            float t = (float)s / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(startNode.gridX, destNode.gridX, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(startNode.gridY, destNode.gridY, t));

            if (x < 0 || y < 0 ||
                x >= user.gridManager.gridSizeX ||
                y >= user.gridManager.gridSizeY)
                return false;

            Node n = user.gridManager.grid[x, y];
            if (n == null || !n.isWalkable) return false;

            Vector2 cur = n.position;
            float dist = Vector2.Distance(prev, cur);

            if (dist > 0f)
            {
                var hit = Physics2D.Raycast(
                    prev, (cur - prev).normalized,
                    dist, user.wallMask | user.wallMask_AI);

                if (hit.collider) return false;
            }
            prev = cur;
        }
        return true;
    }

    // ===================== Summon =====================
    private void TrySummonVumPet(Unit_Base owner)
    {
        if (!owner || !vumPetPrefab) return;

        if (Random.value >= 0.5f) return;

        owner.SummonUnit(
            vumPetPrefab,
            owner.SkillPosition,
            null,
            true
        );
    }
}
