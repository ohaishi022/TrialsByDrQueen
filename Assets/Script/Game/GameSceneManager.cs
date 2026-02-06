using UnityEngine;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using CS.AudioToolkit;
using UnityEngine.SceneManagement;

public enum GameMode
{
    None,
    Endless,
    Lobby,
    Halloween,
    Trial
}

public enum GameSituation
{
    Playing,
    CutScene
}

public class GameSceneManager : MonoBehaviour
{
    public GameObject testesc; // 테스트 빌드 전용

    [Header("UI Prefabs")]
    public UI_Base uiEndlessPrefab;
    public UI_Base uiHalloweenPrefab;
    public UI_Base uiLobbyPrefab;

    public UI_Base currentUI;

    public GameMode currentGameMode = GameMode.Halloween;
    public Animator fadeAnimator;
    public Camera gameplayCamera;
    public Canvas gameplyCanvas;
    public Camera cutsceneCamera;
    public GameObject cutsceneCameraObject;
    public Animator cutsceneCameraAnimator;

    private string selectedUnitName;
    public string playerPrefabPath;//프빗

    public Vector2 playerSpawnPosition = new Vector2(0, 0);

    public GameSituation CurrentSituation = GameSituation.CutScene; // { get; private; set; }

    private GameMode_Base activeGameMode;
    public GameMode_Base CurrentGameMode => activeGameMode;

    public Unit_Base playerUnit;//프빗

    private AudioObject currentBGM;

    private GameObject currentMap; // 인스턴스 저장용

    [Header("중앙 공지 텍스트")]
    public GameObject alertTextPanel;
    public LocalizeStringEvent alertText_Text; // Localization 지원되는 TextMeshPro
    public Animator alertTextAnimator;
    private Coroutine alertTextRoutine;

    private void Awake()
    {
        selectedUnitName = GameManager.Instance.SelectedUnit;
        playerPrefabPath = $"Prefab/Unit/Human/{selectedUnitName}";

        currentGameMode = (GameMode)GameManager.Instance.CurrentGameMode;

        MapGenerator(currentGameMode);
    }

    private void Start()
    {
        SetupGameMode(currentGameMode);
    }

    private void Update()
    {
        // 테스트 빌드 전용
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("로비로 이동");
            SceneManager.LoadScene("Scene_Lobby");
        }
    }

    private void SetupGameplayUI(GameMode mode)
    {
        if (currentUI != null) return;

        UI_Base prefab = null;

        switch (mode)
        {
            case GameMode.Endless:
                prefab = uiEndlessPrefab;
                break;

            case GameMode.Halloween:
                //prefab = uiHalloweenPrefab;
                break;

            case GameMode.Lobby:
                //prefab = uiLobbyPrefab;
                break;
            case GameMode.Trial:
                prefab = uiEndlessPrefab;
                break;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"해당 모드에 UI 프리팹 없음 : {mode}");
            return;
        }

        currentUI = Instantiate(prefab, gameplyCanvas.transform);
        currentUI.Initialize();
    }

    void SetupGameMode(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Endless:
                activeGameMode = gameObject.AddComponent<GameMode_Endless>();
                Debug.Log("엔드리스 모드");
                break;
            case GameMode.Trial:
                activeGameMode = gameObject.AddComponent<GameMode_Trial>();
                Debug.Log("시련 모드");
                break;
            case GameMode.Lobby:
                //activeGameMode = gameObject.AddComponent<GameMode_Lobby>();
                activeGameMode = gameObject.AddComponent<GameMode_Endless>();
                break;
            case GameMode.Halloween:
                activeGameMode = gameObject.AddComponent<GameMode_Halloween>();
                Debug.Log("할로윈 모드");
                break;
            case GameMode.None:
                Debug.LogError("지원되지 않는 게임 모드");
                return;
        }
        SetupGameplayUI(mode);
        activeGameMode.Initialize(this);
        StartCoroutine(StartGameModeRoutine());
    }

    private IEnumerator StartGameModeRoutine()
    {
        if (fadeAnimator != null)
        {
            fadeAnimator.SetTrigger("FadeIn");
        }
        activeGameMode.StartGameMode();
        yield return null;
    }

    public void SetGameSituation(GameSituation situation)
    {
        CurrentSituation = situation;

        gameplayCamera.gameObject.SetActive(situation == GameSituation.Playing);
        cutsceneCamera.gameObject.SetActive(situation == GameSituation.CutScene);
    }

    private void MapGenerator(GameMode mode)
    {
        if (currentMap != null) return;

        // 모드별 리소스 경로 결정(추후 삭제할 수도)
        string mapPath = GetMapPrefabPath(mode);

        if (string.IsNullOrEmpty(mapPath))
        {
            Debug.LogWarning($"맵 프리팹 경로를 찾을 수 없음. 게임 모드 : {mode}");
            return;
        }

        GameObject mapPrefab = Resources.Load<GameObject>(mapPath);
        if (mapPrefab == null)
        {
            Debug.LogError($"맵 프리팹을 Resources에서 찾지 못 함 : {mapPath}");
            return;
        }

        currentMap = Instantiate(mapPrefab, Vector3.zero, Quaternion.identity);

        Debug.Log($"맵 스폰 완료 : {mapPath}");
    }

    // *** 모드별 맵 경로를 돌려주는 함수
    // 실제 프리팹 경로에 맞게 수정해
    private string GetMapPrefabPath(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Endless:
                return "Prefab/Map/Map_Endless";
            case GameMode.Halloween:
                return "Prefab/Map/Map_Halloween_2025";
            case GameMode.Lobby:
                return "Prefab/Map/Map_Endless"; //return "Prefab/Map/Map_Lobby";
            case GameMode.Trial:
                return "Prefab/Map/Map_Endless";
            default:
                return null;
        }
    }

    private bool _gameOverInProgress = false;
    public void EndCurrentGameMode()
    {
        activeGameMode?.EndGameMode();
    }

    public void GameOver()
    {
        if (_gameOverInProgress) return;
        _gameOverInProgress = true;

        // ★ 모드 종료를 여기서 보장 → "Halloween 모드 종료" 로그 뜸
        EndCurrentGameMode();

        StartCoroutine(HandleGameOver());
    }

    private IEnumerator HandleGameOver()
    {
        Debug.Log("핸들 게임오버");
        CurrentSituation = GameSituation.CutScene;
        yield return new WaitForSeconds(2f); //할로윈 말고는 2로 설정할 것
        if (fadeAnimator != null)
        {
            fadeAnimator.SetTrigger("FadeIn");
            yield return new WaitForSeconds(1f); // 페이드 인 시간 대기
        }

        _gameOverInProgress = false;
    }

    public Unit_Base SpawnUnit(string unitPrefabPath, Vector2 spawnPosition, UnitType unitType)
    {
        // Resources 폴더에서 프리팹 로드
        GameObject unitPrefab = Resources.Load<GameObject>(unitPrefabPath);

        if (unitPrefab == null)
        {
            Debug.LogError($"유닛 프리팹을 찾을 수 없음: {unitPrefabPath}");
            return null;
        }

        // 유닛 생성
        GameObject newUnit = Instantiate(unitPrefab, new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.y * 0.01f), Quaternion.identity);
        Unit_Base unitBase = newUnit.GetComponent<Unit_Base>();

        if (unitBase != null)
        {
            // 소환된 유닛의 타입 설정
            unitBase.unitType = unitType;

            Debug.Log($"유닛 소환 완료: {unitBase.unitName} ({spawnPosition.x}, {spawnPosition.y}) - 타입: {unitType}");
            return unitBase;
        }
        else
        {
            Debug.LogError($"{unitPrefabPath}에 Unit_Base 컴포넌트가 없음");
            Destroy(newUnit); // 컴포넌트가 없으면 생성된 오브젝트 삭제
            return null;
        }
    }

    public void PutPlayerAutoHeal()
    {
        if (playerUnit == null) return;

        var autoHeal = playerUnit.GetComponent<PlayerAutoHeal>();
        if (autoHeal == null)
        {
            autoHeal = playerUnit.gameObject.AddComponent<PlayerAutoHeal>();
        }
    }

    public void RespawnPlayer(Vector2 respawnPosition, string respawnPrefabPath)
    {
        // 기존 플레이어 제거
        if (playerUnit != null)
        {
            playerUnit.DestroyUnit(true);
            playerUnit = null;
        }

        // 새 유닛 소환
        playerUnit = SpawnUnit(respawnPrefabPath, respawnPosition, UnitType.Human);
        if (playerUnit != null)
        {
            playerUnit.SetUnitState(UnitState.Player);
            PutPlayerAutoHeal();
            Debug.Log($"플레이어 리스폰 완료 : {respawnPosition}");
        }
    }

    /// <summary>
    /// 중앙에 공지 텍스트를 표시.
    /// </summary>
    /// <param name="localizedKey">Localization Table Key</param>
    /// <param name="displayDuration">화면에 표시될 시간</param>
    public void ShowAlertText(string localizedKey, float displayDuration = 2.5f)
    {
        if (alertTextRoutine != null)
        {
            StopCoroutine(alertTextRoutine);
            alertTextRoutine = null;
        }

        alertTextRoutine = StartCoroutine(ShowAlertTextRoutine(localizedKey, displayDuration));
    }

    private IEnumerator ShowAlertTextRoutine(string localizedKey, float displayDuration)
    {
        // 텍스트 설정
        alertText_Text.StringReference = new LocalizedString { TableReference = "UI", TableEntryReference = localizedKey };

        // 패널 활성화 및 애니메이션 트리거
        alertTextPanel.SetActive(true);

        alertTextAnimator.ResetTrigger("FadeOut");
        alertTextAnimator.SetTrigger("FadeIn");

        yield return new WaitForSeconds(displayDuration);

        alertTextAnimator.ResetTrigger("FadeIn");
        alertTextAnimator.SetTrigger("FadeOut");

        yield return new WaitForSeconds(0.5f); // FadeOut 애니메이션 길이에 맞춤
        alertTextPanel.SetActive(false);

        alertTextRoutine = null;
    }

    public void CutSceneCameraToPlayer()
    {
        Debug.Log($"컷씬카메라 이동 전, 플레이어 위치 : {playerUnit.transform.position}");
        Debug.Log($"기존 카메라 위치: {cutsceneCameraObject.transform.position}");
        cutsceneCameraObject.transform.position = new Vector3(
            playerUnit.transform.position.x,
            playerUnit.transform.position.y,
            -10f
            ); // z값 -10f 또는 기존 z값
        Debug.Log("컷씬카메라 실제 위치 : " + cutsceneCameraObject.transform.position);
    }

    public void PlayBGM(string bgmName, Transform tr)
    {
        // 기존 BGM이 있으면 정지
        if (currentBGM != null)
        {
            currentBGM.Stop();
            currentBGM = null;
        }

        // BGM을 재생
        var bgmSource = AudioController.Play(bgmName, tr);
        currentBGM = bgmSource;

        if (bgmSource != null)
        {
            AudioSource src = bgmSource.GetComponent<AudioSource>();
            if (src != null)
            {
                src.dopplerLevel = 0f;
                src.spatialBlend = 0f;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 20f;
                src.maxDistance = 40f;

            }
        }
        else
        {
            Debug.LogWarning("BGM AudioSource를 찾을 수 없음");
        }
    }

    public void StopBGM()
    {
        if (currentBGM != null)
        {
            currentBGM.Stop();
            currentBGM = null;
        }
    }
}