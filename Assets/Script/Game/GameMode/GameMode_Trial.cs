using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMode_Trial : GameMode_Base
{
    private TrialWaveDataList trialWaveDataList;
    private Dictionary<string, ZombieCatalogEntry> zombieCatalog;

    private Coroutine waveRoutine;
    private bool isEnding = false;

    public override void Initialize(GameSceneManager sceneManager)
    {
        base.Initialize(sceneManager);

        trialWaveDataList = GameManager.Instance.trialWaveDataList;

        if (trialWaveDataList == null || trialWaveDataList.waves == null)
        {
            Debug.LogError("TrialWaveDataList가 비어있습니다.");
            return;
        }

        zombieCatalog = new Dictionary<string, ZombieCatalogEntry>();
        if (trialWaveDataList.zombieCatalog != null)
        {
            foreach (var z in trialWaveDataList.zombieCatalog)
            {
                if (z == null || string.IsNullOrEmpty(z.id)) continue;
                zombieCatalog[z.id] = z;
            }
        }
    }

    public override void StartGameMode()
    {
        // 여기서 시작!
        if (trialWaveDataList == null || trialWaveDataList.waves == null)
        {
            Debug.LogError("TrialWaveDataList가 로드되지 않아 Trial을 시작할 수 없음");
            return;
        }

        isEnding = false;

        // 1) 플레이어 스폰
        if (sceneManager.playerUnit == null)
        {
            sceneManager.playerUnit = sceneManager.SpawnUnit(
                sceneManager.playerPrefabPath,
                sceneManager.playerSpawnPosition,
                UnitType.Human
            );

            if (sceneManager.playerUnit == null)
            {
                Debug.LogError($"플레이어 스폰 실패: {sceneManager.playerPrefabPath}");
                return;
            }

            sceneManager.playerUnit.SetUnitState(UnitState.Player);
            sceneManager.PutPlayerAutoHeal();
        }

        // 2) 게임 상황을 플레이로 전환(원하는 연출 있으면 CutScene로 시작해도 됨)
        sceneManager.SetGameSituation(GameSituation.Playing);

        // 3) 웨이브 시작
        if (waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = StartCoroutine(StartWaveRoutine(1));
    }

    public override void EndGameMode()
    {
        isEnding = true;

        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        Debug.Log("Trial 모드 종료");
    }

    private IEnumerator StartWaveRoutine(int waveIndex)
    {
        if (isEnding) yield break;

        int jsonWaveIndex = waveIndex - 1;
        if (jsonWaveIndex < 0 || jsonWaveIndex >= trialWaveDataList.waves.Length)
            yield break;

        var wave = trialWaveDataList.waves[jsonWaveIndex];
        Debug.Log($"[Trial 웨이브] {wave.wave} 시작");

        for (int i = 0; i < wave.groups.Length; i++)
        {
            if (isEnding) yield break;
            yield return StartCoroutine(HandleGroupSpawn(wave.groups[i]));
        }

        Debug.Log($"[Trial 웨이브] {wave.wave} 완료!");

        // 다음 웨이브 자동 진행
        if (!isEnding && waveIndex < trialWaveDataList.waves.Length)
            yield return StartCoroutine(StartWaveRoutine(waveIndex + 1));
    }

    private IEnumerator HandleGroupSpawn(TrialWaveGroup group)
    {
        List<Unit_Base> alive = new List<Unit_Base>();

        List<FixedSpawn> fixedList = new List<FixedSpawn>();
        if (group.fixedSpawns != null) fixedList.AddRange(group.fixedSpawns);
        fixedList.Sort((a, b) => a.time.CompareTo(b.time));

        float duration = (group.budget != null) ? Mathf.Max(0f, group.budget.duration) : 0f;

        int wavePoint = group.budget != null ? group.budget.startPoint : 0;
        int maxPoint = group.budget != null ? group.budget.maxPoint : 999999;
        float gainPerSec = group.budget != null ? group.budget.gainPerSecond : 0f;

        float cooldown = group.spawnRule != null ? group.spawnRule.cooldown : 0.5f;
        int maxAlive = group.spawnRule != null ? group.spawnRule.maxAlive : 999;
        int spawnLimit = group.spawnRule != null ? group.spawnRule.spawnCountLimit : 999;

        int spawnedCount = 0;
        float t = 0f;
        float spawnTimer = 0f;
        int fixedIndex = 0;

        while (!isEnding && t < duration)
        {
            float dt = Time.deltaTime;
            t += dt;

            // 포인트 수급
            if (gainPerSec > 0f)
            {
                wavePoint += Mathf.FloorToInt(gainPerSec * dt);
                if (wavePoint > maxPoint) wavePoint = maxPoint;
            }

            // 고정 스폰
            while (fixedIndex < fixedList.Count && fixedList[fixedIndex].time <= t)
            {
                var fs = fixedList[fixedIndex++];
                TrySpawnByZombieId(fs.zombieId, new Vector2(fs.position.x, fs.position.y), alive);
            }

            // 포인트 스폰
            spawnTimer += dt;
            if (spawnTimer >= cooldown)
            {
                spawnTimer = 0f;

                if (alive.Count < maxAlive && spawnedCount < spawnLimit)
                {
                    if (TryPickZombieToBuy(group, wavePoint, out string pickedZombieId, out int cost))
                    {
                        wavePoint -= cost;
                        spawnedCount++;

                        Vector2 pos = PickSpawnPoint(group);
                        TrySpawnByZombieId(pickedZombieId, pos, alive);
                    }
                }
            }

            yield return null;
        }

        // duration 끝난 뒤에도 fixed spawn 남아있으면 처리
        while (!isEnding && fixedIndex < fixedList.Count)
        {
            var fs = fixedList[fixedIndex++];
            TrySpawnByZombieId(fs.zombieId, new Vector2(fs.position.x, fs.position.y), alive);
        }

        if (!isEnding)
            yield return StartCoroutine(WaitGroupComplete(group, wavePoint, alive));

        Debug.Log($"[Trial 웨이브] 그룹 {group.groupId} 완료");
    }

    private IEnumerator WaitGroupComplete(TrialWaveGroup group, int wavePoint, List<Unit_Base> alive)
    {
        string type = (group.completeCondition != null && !string.IsNullOrEmpty(group.completeCondition.type))
            ? group.completeCondition.type
            : "TimeUpAndAllDead";

        if (type == "AllDead" || type == "TimeUpAndAllDead")
        {
            while (!isEnding && alive.Count > 0) yield return null;
            yield break;
        }

        if (type == "BudgetSpentAndAllDead")
        {
            while (!isEnding)
            {
                if (alive.Count == 0 && !CanBuyAnything(group, wavePoint))
                    break;
                yield return null;
            }
            yield break;
        }

        while (!isEnding && alive.Count > 0) yield return null;
    }

    private bool TryPickZombieToBuy(TrialWaveGroup group, int wavePoint, out string zombieId, out int cost)
    {
        zombieId = null;
        cost = 0;

        if (group.allowedEnemies == null || group.allowedEnemies.Length == 0)
            return false;

        List<(AllowedEnemy e, int cost)> candidates = new List<(AllowedEnemy, int)>();

        foreach (var ae in group.allowedEnemies)
        {
            if (ae == null || string.IsNullOrEmpty(ae.zombieId)) continue;

            if (!zombieCatalog.TryGetValue(ae.zombieId, out var info))
                continue;

            if (wavePoint < info.cost) continue;
            if (wavePoint < ae.minWavePointToUse) continue;

            candidates.Add((ae, info.cost));
        }

        if (candidates.Count == 0)
            return false;

        int sum = 0;
        for (int i = 0; i < candidates.Count; i++)
            sum += Mathf.Max(1, candidates[i].e.weight);

        int r = Random.Range(0, sum);
        for (int i = 0; i < candidates.Count; i++)
        {
            int w = Mathf.Max(1, candidates[i].e.weight);
            if (r < w)
            {
                zombieId = candidates[i].e.zombieId;
                cost = candidates[i].cost;
                return true;
            }
            r -= w;
        }

        zombieId = candidates[0].e.zombieId;
        cost = candidates[0].cost;
        return true;
    }

    private bool CanBuyAnything(TrialWaveGroup group, int wavePoint)
    {
        if (group.allowedEnemies == null) return false;

        foreach (var ae in group.allowedEnemies)
        {
            if (ae == null) continue;
            if (!zombieCatalog.TryGetValue(ae.zombieId, out var info)) continue;

            if (wavePoint >= info.cost && wavePoint >= ae.minWavePointToUse)
                return true;
        }

        return false;
    }

    private Vector2 PickSpawnPoint(TrialWaveGroup group)
    {
        if (group.spawnPoints == null || group.spawnPoints.Length == 0)
            return Vector2.zero;

        var p = group.spawnPoints[Random.Range(0, group.spawnPoints.Length)];
        return new Vector2(p.x, p.y);
    }

    private bool TrySpawnByZombieId(string zombieId, Vector2 pos, List<Unit_Base> aliveList)
    {
        if (!zombieCatalog.TryGetValue(zombieId, out var info))
        {
            Debug.LogError($"[Spawn] zombieId({zombieId}) 가 zombieCatalog에 없음");
            return false;
        }

        // 현재는 전부 Zombie로 스폰됨. (Vumpire가 Human이면 여기 예외처리 필요)
        var enemy = sceneManager.SpawnUnit(info.prefab, pos, UnitType.Zombie);
        if (enemy == null) return false;

        enemy.SetOnDestroyCompleteCallback(() =>
        {
            if (aliveList.Contains(enemy))
                aliveList.Remove(enemy);
        });

        aliveList.Add(enemy);
        return true;
    }
}
