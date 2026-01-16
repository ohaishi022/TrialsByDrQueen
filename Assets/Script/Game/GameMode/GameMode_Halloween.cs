using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CS.AudioToolkit;

public class GameMode_Halloween : GameMode_Base
{
    public enum WaveState
    {
        NormalWave,
        BossWave,
        BreakWave
    }

    public WaveState currentWaveState { get; private set; }

    // 웨이브 테이블: WaveData_Halloween.json을 GameManager가 읽어 넣었다고 가정
    private EndlessWaveDataList waveTable;

    // 쉬는 시간 & 보스 웨이브 규칙 (Endless와 동일하게)
    private const float NormalWaveRestTime = 10f;
    private const float BossWaveRestTime = 20f;
    private static readonly HashSet<int> bossWaves = new HashSet<int> {1};

    // 그룹 완료 추적
    private readonly HashSet<string> clearedGroups = new HashSet<string>();

    public static GameMode_Halloween Instance { get; private set; }

    public override void Initialize(GameSceneManager sceneManager)
    {
        base.Initialize(sceneManager);

        // WaveData_Halloween 사용
        waveTable = GameManager.Instance.halloweenWaveDataList;
        if (waveTable == null || waveTable.waves == null)
        {
            Debug.LogError("HalloweenWaveDataList가 비어있습니다.");
        }
        Instance = this;
    }

    public override void StartGameMode()
    {
        sceneManager.StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        // 컷씬 상태에서 플레이어 소환
        sceneManager.SetGameSituation(GameSituation.CutScene);
        sceneManager.playerUnit = sceneManager.SpawnUnit(sceneManager.playerPrefabPath, sceneManager.playerSpawnPosition, UnitType.Human);

        if (sceneManager.playerUnit != null)
        {
            sceneManager.PlayBGM("BGM_Game_Halloween_TutorialWave", sceneManager.playerUnit.transform);
            sceneManager.playerUnit.SetUnitState(UnitState.Player);
            sceneManager.playerUnit.OnDestroyed += OnPlayerDestroyed;
        }

        sceneManager.PutPlayerAutoHeal();

        // 시작 연출 카메라 (기존 스타트 연출 재사용)
        if (sceneManager.cutsceneCameraAnimator != null)
        {
            sceneManager.cutsceneCameraAnimator.Play("Camera_Game_Endless_Start", 0, 0f);
        }

        yield return new WaitForSeconds(3f);

        // 플레이 상태 전환
        sceneManager.SetGameSituation(GameSituation.Playing);

        // 할로윈 시작 알림
        sceneManager.ShowAlertText("Game_Halloween_StartAlert_1", 5f);
        yield return new WaitForSeconds(5.5f);

        sceneManager.ShowAlertText("Game_Halloween_StartAlert_2", 5f);
        yield return new WaitForSeconds(5.5f);

        sceneManager.ShowAlertText("Game_Halloween_StartAlert_3", 5f);
        yield return new WaitForSeconds(5.5f);
        // 튜토리얼 적 없이 곧바로 웨이브 시작
        sceneManager.StartCoroutine(StartWaveRoutine(1));
    }

    private IEnumerator StartWaveRoutine(int waveIndex)
    {
        if (waveTable == null || waveTable.waves == null)
        {
            Debug.LogError("[Halloween] 웨이브 데이터가 비어있음");
            yield break;
        }

        int jsonWaveIndex = waveIndex - 1;
        if (jsonWaveIndex < 0 || jsonWaveIndex >= waveTable.waves.Count)
        {
            Debug.LogError($"[Halloween] 해당 웨이브 인덱스({waveIndex})의 데이터가 없음");
            yield break;
        }

        WaveData wave = waveTable.waves[jsonWaveIndex];
        Debug.Log($"[Halloween] 웨이브 {wave.wave} 시작");

        clearedGroups.Clear();

        bool isBossWave = bossWaves.Contains(wave.wave);
        currentWaveState = isBossWave ? WaveState.BossWave : WaveState.NormalWave;

        // 웨이브 BGM
        sceneManager.PlayBGM(isBossWave ? "BGM_Game_Halloween_BossWave" : "BGM_Game_Halloween_NormalWave",
                             sceneManager.playerUnit != null ? sceneManager.playerUnit.transform : sceneManager.transform);

        // 그룹 순차 소환
        for (int i = 0; i < wave.groups.Length; i++)
        {
            yield return sceneManager.StartCoroutine(HandleGroupSpawn(wave.groups[i]));
        }

        Debug.Log($"[Halloween] 웨이브 {wave.wave} 완료");

        bool hasNextWave = waveIndex < waveTable.waves.Count;
        if (hasNextWave)
        {
            // 웨이브 엔딩 BGM
            //sceneManager.PlayBGM(isBossWave ? "UI_Game_Endless_Wave_Complete" : "UI_Game_Endless_Wave_Complete",
            //                     sceneManager.playerUnit != null ? sceneManager.playerUnit.transform : sceneManager.transform);

            // 휴식 시간
            float breakTime = isBossWave ? BossWaveRestTime : NormalWaveRestTime;
            sceneManager.ShowAlertText(isBossWave ? "Game_BossWave_Rest" : "Game_Wave_Rest", breakTime);
            currentWaveState = WaveState.BreakWave;
            yield return new WaitForSeconds(breakTime);

            // 다음 웨이브
            sceneManager.StartCoroutine(StartWaveRoutine(waveIndex + 1));
        }
        else
        {
            AudioController.Play("UI_Game_Endless_Wave_Complete", sceneManager.playerUnit.transform);
            // 모든 웨이브 종료 → 게임오버(연출은 GameSceneManager 쪽 루틴에 맡김)
            GameOver();
            Debug.Log("[Halloween] 모든 웨이브 종료");
        }
    }

    private IEnumerator HandleGroupSpawn(WaveGroup group)
    {
        List<Unit_Base> spawnedEnemies = new List<Unit_Base>();

        // spawnTime 기준 정렬
        var enemies = new List<EnemySpawnData>(group.enemies);
        enemies.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

        foreach (var enemyData in enemies)
        {
            yield return sceneManager.StartCoroutine(SpawnEnemy(enemyData, spawnedEnemies));
        }

        // 해당 그룹의 적이 모두 제거될 때까지 대기
        while (spawnedEnemies.Count > 0)
            yield return null;

        clearedGroups.Add(group.groupId);
        Debug.Log($"[Halloween] 그룹 {group.groupId} 완료");
    }

    private IEnumerator SpawnEnemy(EnemySpawnData enemyData, List<Unit_Base> enemyList)
    {
        yield return new WaitForSeconds(enemyData.spawnTime);

        if (enemyData.spawnPosition == null || enemyData.spawnPosition.Count == 0)
        {
            Debug.LogError($"[Halloween.SpawnEnemy] {enemyData.unitPrefab} 의 spawnPosition이 null/빈 값");
            yield break;
        }

        Vector2 selectedPos = enemyData.spawnPosition[Random.Range(0, enemyData.spawnPosition.Count)];
        Vector3 pos = new Vector3(selectedPos.x, selectedPos.y, 0f);

        var enemy = sceneManager.SpawnUnit(enemyData.unitPrefab, pos, UnitType.Zombie);
        if (enemy != null)
        {
            // 제거 시 리스트에서 빠지도록
            enemy.SetOnDestroyCompleteCallback(() =>
            {
                if (enemyList.Contains(enemy))
                {
                    enemyList.Remove(enemy);
                    Debug.Log($"[Halloween Group] {enemy.unitName} 제거 → 남은 적: {enemyList.Count}");
                }
            });

            enemyList.Add(enemy);
        }
    }

    private void OnPlayerDestroyed()
    {
        if (sceneManager.CurrentSituation == GameSituation.Playing)
        {
            Debug.Log("플레이어 처치됨");
            GameOver();
        }
    }

    public void GameOver()
    {
        if (sceneManager.CurrentSituation == GameSituation.Playing)
        {
            StartCoroutine(GameOverRoutine());
        }
    }
    
    public override void EndGameMode()
    {
        Debug.Log("Halloween 모드 종료");
        if (Instance == this) Instance = null;
        sceneManager.SetGameSituation(GameSituation.CutScene);
    }

    public IEnumerator GameOverRoutine()
    {
        sceneManager.SetGameSituation(GameSituation.CutScene);

        if (sceneManager.cutsceneCameraAnimator != null)
        {
            if (sceneManager.playerUnit != null)
            {
                sceneManager.CutSceneCameraToPlayer();
            }
            yield return new WaitForSeconds(2.5f);
            sceneManager.cutsceneCameraAnimator.Play("Camera_Game_Endless_GameOver", 0, 0f);
        }

        yield return new WaitForSeconds(8f);

        Application.Quit();
    }
}
