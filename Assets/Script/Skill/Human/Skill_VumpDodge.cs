using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class Skill_VumpDodge : Skill_Base
{
    [Header("FX / Unit")]
    public GameObject dodgeEffect;
    public GameObject vumPetPrefab;

    [Header("Audio")]
    public string SE = "SE_Skill_VumpDodge";

    [Header("Warp")]
    public int minWarpDist = 2;
    public int maxWarpDist = 5;
    public int warpSearchAttempts = 10;

    private void Awake()
    {
        cooldownTime = 2f;
        canDeactivate = false;

        if (!dodgeEffect)
            dodgeEffect = Resources.Load<GameObject>(
                "Prefab/Skill/Human/Vumpire/Skill_VumpDodge");

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

        // ¢º ½ÃÀÛ ÀÌÆåÆ®
        SpawnEffect(u);

        // ¢º »ç¿îµå
        AudioController.Play(SE, u.transform.position);

        // ¢º Æê ¼ÒÈ¯ (È®·ü)
        TrySummonVumPet(u);

        // ¢º ¿öÇÁ À§Ä¡ Å½»ö
        Vector2 warpPos = u.transform.position;
        if (TryFindRandomWarpLocation(
            u, minWarpDist, maxWarpDist, warpSearchAttempts, out Vector2 found))
        {
            warpPos = found;
        }

        // ¢º ¿öÇÁ
        u.TeleportUnit(warpPos);

        // ¢º µµÂø ÀÌÆåÆ®
        SpawnEffect(u);

        // ¢º »óÅÂ º¹±¸
        u.stopMoving = false;
        isActive = false;

        yield return StartCooldown();
    }

    // ===================== FX =====================

    private void SpawnEffect(Unit_Base u)
    {
        if (!dodgeEffect) return;
        Instantiate(dodgeEffect, u.SkillPosition, Quaternion.identity);
    }

    protected override void OnCanceled()
    {
        base.OnCanceled();

        if (owner)
            owner.stopMoving = false;
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
