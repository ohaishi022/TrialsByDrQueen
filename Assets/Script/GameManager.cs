using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
using TMPro;

public enum SelectedGameMode
{
    None,
    Endless,
    Lobby,
    Halloween
}


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public SelectedGameMode CurrentGameMode { get; private set; }

    public string SelectedUnit { get; private set; }

    public EndlessWaveDataList endlessWaveDataList { get; private set; }
    public EndlessWaveDataList halloweenWaveDataList { get; private set; }

    public TMP_Text loadingText; // 인스펙터에 연결

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        LoadFiles();
    }

    private void LoadFiles()
    {
        // 1) Endless
        endlessWaveDataList = TryLoadWaveData(Path.Combine(Application.streamingAssetsPath, "EndlessWaveData.json"),
                                              onFailKey: "Loading_Error_FileNotFound");

        // 2) Halloween
        halloweenWaveDataList = TryLoadWaveData(Path.Combine(Application.streamingAssetsPath, "WaveData_Halloween.json"),
                                                onFailKey: "Loading_Error_FileNotFound");

        // 데이터가 정상적으로 로드된 경우에만 씬 이동
        SceneManager.LoadScene("Scene_Lobby");
    }

    private EndlessWaveDataList TryLoadWaveData(string path, string onFailKey)
    {
        Debug.Log($"[WaveData] 파일 경로: {path}");
        if (!File.Exists(path))
        {
            Debug.LogError($"[WaveData] 파일이 존재하지 않음: {path}");
            ShowText(onFailKey);
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<EndlessWaveDataList>(json);
            if (data == null)
            {
                Debug.LogError("[WaveData] 파싱 실패");
                ShowText("Loading_Error_ParseNotFound");
                return null;
            }
            return data;
        }
        catch
        {
            Debug.LogError("[WaveData] 예외 발생");
            ShowText("Loading_Error_ParseNotFound");
            return null;
        }
    }

    private void ShowText(string localizedKey)
    {
        if (loadingText != null)
        {
            var localizedString = new LocalizedString("UI", localizedKey);
            localizedString.StringChanged += (localizedValue) =>
            {
                loadingText.text = localizedValue;
            };
            // 아래 줄 추가: 현재 언어로 번역된 텍스트를 즉시 할당
            loadingText.text = localizedString.GetLocalizedString();
        }
        Debug.LogError($"WaveTable 로딩 오류: {localizedKey}");
    }

    public void SetGameSettings(SelectedGameMode mode, string unitName)
    {
        CurrentGameMode = mode;
        SelectedUnit = unitName;
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("Scene_Game");
    }
}
