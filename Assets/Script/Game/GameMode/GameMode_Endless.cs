using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CS.AudioToolkit;

public enum WaveState
{
    TutorialWave,
    NormalWave,
    BossWave,
    BreakWave
}

public class GameMode_Endless : GameMode_Base
{
    public WaveState currentWaveState { get; private set; }

    public float SwitchCurrentHealth = 100f;
    public float SwitchMaxHealth = 100f;

    // 웨이브 진행 데이터
    private EndlessWaveDataList waveTable; // 모든 웨이브 정보 (GameManager에서 받아옴)
    private HashSet<string> clearedGroups = new HashSet<string>(); // 이번 웨이브 클리어 그룹

    // 쉬는 시간 세팅
    private const float NormalWaveRestTime = 15f;
    private const float BossWaveRestTime = 20f;

    // 보스웨이브 구간
    private static readonly HashSet<int> bossWaves = new HashSet<int> { 5, 10, 15, 20, 25 };

    public static GameMode_Endless Instance { get; private set; }

    public void ValidateWaveData(EndlessWaveDataList waveDataList)
    {
        for (int i = 0; i < waveDataList.waves.Count; i++)
        {
            var wave = waveDataList.waves[i];
            foreach (var group in wave.groups)
            {
                foreach (var enemyData in group.enemies)
                {
                    if (enemyData.spawnPosition == null || enemyData.spawnPosition.Count == 0)
                    {
                        Debug.LogError($"[WaveData 오류] Wave {wave.wave} Group {group.groupId} 유닛 {enemyData.unitPrefab} 의 spawnPosition이 비어있습니다.");
                    }
                }
            }
        }
    }

    public override void Initialize(GameSceneManager sceneManager)
    {
        base.Initialize(sceneManager);
        SwitchCurrentHealth = SwitchMaxHealth;

        // GameManager에서 waveTable 받아오기 (반드시 GameManager에서 로드되어 있어야 함)
        waveTable = GameManager.Instance.endlessWaveDataList;
        if (waveTable == null || waveTable.waves == null)
        {
            Debug.LogError("EndlessWaveDataList가 비어있습니다.");
        }

        Instance = this; // 현재 인스턴스를 등록
        ValidateWaveData(waveTable);
    }

    public override void StartGameMode()
    {
        sceneManager.StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        currentWaveState = WaveState.TutorialWave;
        sceneManager.SetGameSituation(GameSituation.CutScene);
        sceneManager.playerUnit = sceneManager.SpawnUnit(sceneManager.playerPrefabPath, sceneManager.playerSpawnPosition, UnitType.Human);

        if (sceneManager.playerUnit != null)
        {
            sceneManager.PlayBGM("BGM_Game_Start", sceneManager.playerUnit.transform);
            sceneManager.playerUnit.SetUnitState(UnitState.Player);
            // 플레이어 유닛이 처치될 경우 게임오버 처리
            sceneManager.playerUnit.OnDestroyed += OnPlayerDestroyed;
        }
        sceneManager.PutPlayerAutoHeal();
        sceneManager.cutsceneCameraAnimator.Play("Camera_Game_Endless_Start", 0, 0f);
        yield return new WaitForSeconds(3f);
        sceneManager.SetGameSituation(GameSituation.Playing);
        sceneManager.ShowAlertText("Game_Endless_StartAlert_1", 5f);
        yield return new WaitForSeconds(5.5f);
        sceneManager.ShowAlertText("Game_Endless_StartAlert_2", 5f);
        yield return new WaitForSeconds(5.5f);
        var tut = sceneManager.SpawnUnit("Prefab/Unit/Zombie/Unit_Zombie_StrawWingHacker", new Vector3(0, -9, -0.09f), UnitType.Zombie);
        if (tut != null)
        {
            // 적 처치 이벤트 등록
            tut.OnDestroyed += () =>
            {
                // 코루틴 실행은 MonoBehaviour에서만 가능하므로
                sceneManager.StartCoroutine(OnTutorialEnemyDefeated());
            };
        }
        sceneManager.ShowAlertText("Game_Endless_StartAlert_3", 10f);

        // 모든 웨이브 완료 시 엔딩 등 추가
        //sceneManager.ShowAlertText("Game_Endless_AllClear", 5f);
        //Debug.Log("모든 웨이브 종료");
    }

    // 튜토리얼 적이 죽은 뒤 연출, 본격 웨이브 스타트
    private IEnumerator OnTutorialEnemyDefeated()
    {
        currentWaveState = WaveState.BreakWave;
        // 음악 교체
        sceneManager.PlayBGM("BGM_Game_Wave_Start", sceneManager.playerUnit.transform);
        //AudioController.Play("BGM_Game_Wave_Start", sceneManager.playerUnit.transform);

        // 컷씬 및 연출
        sceneManager.SetGameSituation(GameSituation.CutScene);
        sceneManager.CutSceneCameraToPlayer();
        sceneManager.cutsceneCameraAnimator.Play("Camera_Game_Endless_Wave_Start", 0, 0f);
        yield return new WaitForSeconds(14.5f);
        sceneManager.playerUnit.TeleportUnit(new Vector2(0, 0));
        sceneManager.CutSceneCameraToPlayer();
        yield return new WaitForSeconds(5.5f);
        // 웨이브 시작
        sceneManager.SetGameSituation(GameSituation.Playing);
        yield return new WaitForSeconds(5f);
        StartCoroutine(StartWaveRoutine(1)); // 1웨이브 시작 (1을 wave index로 넘겨줌)
    }

    // 웨이브 시작 코루틴
    private IEnumerator StartWaveRoutine(int waveIndex)
    {
        if (GameManager.Instance.endlessWaveDataList == null || GameManager.Instance.endlessWaveDataList.waves == null)
        {
            Debug.LogError("웨이브 데이터가 비어있음");
            yield break;
        }

        int jsonWaveIndex = waveIndex - 1;
        if (jsonWaveIndex < 0 || jsonWaveIndex >= GameManager.Instance.endlessWaveDataList.waves.Count)
        {
            Debug.LogError($"해당 웨이브 인덱스({waveIndex})의 데이터가 없음");
            yield break;
        }

        WaveData wave = GameManager.Instance.endlessWaveDataList.waves[jsonWaveIndex];
        Debug.Log($"[웨이브] {wave.wave} 시작");

        clearedGroups.Clear();

        // 1. BGM 전환
        bool isBossWave = bossWaves.Contains(wave.wave);
        currentWaveState = isBossWave ? WaveState.BossWave : WaveState.NormalWave;
        sceneManager.PlayBGM(isBossWave ? "BGM_Game_Wave_Boss" : "BGM_Game_Wave_Normal", sceneManager.playerUnit.transform);

        // 그룹들을 순차적으로 소환
        for (int i = 0; i < wave.groups.Length; i++)
        {
            yield return StartCoroutine(HandleGroupSpawn(wave.groups[i]));
        }

        AudioController.Play("UI_Game_Endless_Wave_Complete", sceneManager.playerUnit.transform);
        Debug.Log($"[웨이브] {wave.wave} 완료!");

        // 다음 웨이브 여부 판단
        bool hasNextWave = waveIndex < GameManager.Instance.endlessWaveDataList.waves.Count;

        if (hasNextWave)
        {
            // 웨이브 엔딩 BGM 전환
            sceneManager.PlayBGM(isBossWave ? "BGM_Game_Wave_Boss_End" : "BGM_Game_Wave_Normal_End", sceneManager.playerUnit.transform);

            // 쉬는 시간
            float breakTime = bossWaves.Contains(wave.wave) ? BossWaveRestTime : NormalWaveRestTime;
            sceneManager.ShowAlertText(bossWaves.Contains(wave.wave) ? "Game_BossWave_Rest" : "Game_Wave_Rest", breakTime);
            currentWaveState = WaveState.BreakWave;
            yield return new WaitForSeconds(breakTime);

            // 다음 웨이브 시작
            StartCoroutine(StartWaveRoutine(waveIndex + 1));
        }
        else
        {
            GameOver(); // 나중에 수정할 것
            Debug.Log("[웨이브] 모든 웨이브 종료!");
        }
    }

    // 그룹 내 모든 적이 죽으면 리턴
    private IEnumerator HandleGroupSpawn(WaveGroup group)
    {
        List<Unit_Base> spawnedEnemies = new List<Unit_Base>();

        // spawnTime 순으로 정렬
        var enemies = new List<EnemySpawnData>(group.enemies);
        enemies.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));

        foreach (var enemyData in enemies)
        {
            yield return StartCoroutine(SpawnEnemy(enemyData, spawnedEnemies));
        }

        // 그룹 내 적이 모두 사라질 때까지 대기
        while (spawnedEnemies.Count > 0)
            yield return null;

        clearedGroups.Add(group.groupId);
        Debug.Log($"[웨이브] 그룹 {group.groupId} 완료");
    }

    private IEnumerator SpawnEnemy(EnemySpawnData enemyData, List<Unit_Base> enemyList)
    {
        yield return new WaitForSeconds(enemyData.spawnTime);

        if (enemyData.spawnPosition == null || enemyData.spawnPosition.Count == 0)
        {
            Debug.LogError($"[SpawnEnemy] {enemyData.unitPrefab} 의 spawnPosition이 null 또는 비어있습니다.");
            yield break;
        }

        Vector2 selectedPos = enemyData.spawnPosition[Random.Range(0, enemyData.spawnPosition.Count)];
        Vector3 pos = new Vector3(selectedPos.x, selectedPos.y, 0f);

        var enemy = sceneManager.SpawnUnit(enemyData.unitPrefab, pos, UnitType.Zombie);
        if (enemy != null)
        {
            // 그룹에서 제거되도록 콜백 등록
            enemy.SetOnDestroyCompleteCallback(() =>
            {
                if (enemyList.Contains(enemy))
                {
                    enemyList.Remove(enemy);
                    Debug.Log($"[Group] {enemy.unitName} 제거됨 → 현재 남은 적: {enemyList.Count}");
                }
            });

            enemyList.Add(enemy);
        }
    }

    public void DamageSwitch(float amount)
    {
        SwitchCurrentHealth -= amount;
        Debug.Log($"스위치 체력 감소: {SwitchCurrentHealth}/{SwitchMaxHealth}");

        if (sceneManager.currentUI is UI_Endless endlessUI)
        {
            endlessUI.UpdateSwitchHealthBar();
        }

        if (SwitchCurrentHealth <= 0)
        {
            GameOver();
        }
    }

    private void OnPlayerDestroyed()
    {
        if (sceneManager.CurrentSituation == GameSituation.Playing)
        {
            GameOver();
        }
    }

    public override void EndGameMode()
    {
        Debug.Log("Endless 모드 종료");
        if (Instance == this) Instance = null;
        sceneManager.SetGameSituation(GameSituation.CutScene);
    }

    public void GameOver()
    {
        if (sceneManager.CurrentSituation == GameSituation.Playing)
        {
            StartCoroutine(GameOverRoutine());
        }
    }

    public IEnumerator GameOverRoutine()
    {
        sceneManager.SetGameSituation(GameSituation.CutScene);

        sceneManager.StopBGM();
        sceneManager.PlayBGM("BGM_Game_Wave_GameOver", sceneManager.cutsceneCamera.transform);

        if (sceneManager.cutsceneCameraAnimator != null)
        {
            if (sceneManager.playerUnit != null)
            {
                sceneManager.CutSceneCameraToPlayer();
            }
            sceneManager.cutsceneCameraAnimator.Play("Camera_Game_Endless_GameOver", 0, 0f);
        }

        yield return new WaitForSeconds(12f);

        sceneManager.RespawnPlayer(new Vector2(0, 35), sceneManager.playerPrefabPath);
        sceneManager.PlayBGM("BGM_Game_Result", sceneManager.playerUnit.transform);
        if (sceneManager.cutsceneCameraAnimator != null)
        {
            sceneManager.CutSceneCameraToPlayer();
            sceneManager.cutsceneCameraAnimator.Play("Camera_Game_Endless_Result", 0, 0f);
        }
        sceneManager.testesc.SetActive(true); // 테스트 빌드 전용
    }
}