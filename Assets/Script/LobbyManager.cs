using CS.AudioToolkit;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public GameObject unitSelectPanel;
    private string selectedUnit;
    private string selectedUnitPrefabPath;

    public SelectedGameMode selectedGameMode = SelectedGameMode.None; // 현재 고정값, 필요하면 변경

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Push_PlayGameTest(); // 테스트 빌드 전용
        AudioController.Play("BGM_Lobby");
        //unitSelectPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 유닛 선택 버튼 클릭 시
    public void SelectUnit(string unitName)
    {
        selectedUnit = unitName;
        Debug.Log($"선택된 캐릭터: {selectedUnit}");

        // 게임매니저에 게임 모드와 캐릭터 전달
        GameManager.Instance.SetGameSettings(selectedGameMode, selectedUnit);

        // 게임 씬 로딩 시작
        GameManager.Instance.LoadGameScene();
    }

    public void Push_PlayGameTest()
    {
        selectedUnit = "Unit_Human_Archer"; //Unit_Human_EliteStalker
        selectedUnitPrefabPath = "Unit_Human_Archer"; // 나중에 수정하든 지우든 해야 됨
        selectedGameMode = SelectedGameMode.None;
        GameManager.Instance.SetGameSettings(selectedGameMode, selectedUnit);
        GameManager.Instance.LoadGameScene();
    }

    public void Push_TestEndless()
    {
        selectedUnit = "Unit_Human_EliteStalker"; //Unit_Human_EliteStalker
        selectedUnitPrefabPath = "Unit_Human_EliteStalker"; // 나중에 수정하든 지우든 해야 됨
        selectedGameMode = SelectedGameMode.Endless;
        GameManager.Instance.SetGameSettings(selectedGameMode, selectedUnit);
        GameManager.Instance.LoadGameScene();
    }
    public void Push_TestHalloween()
    {
        selectedUnit = "Unit_Human_Archer"; //Unit_Human_EliteStalker
        selectedUnitPrefabPath = "Unit_Human_Archer"; // 나중에 수정하든 지우든 해야 됨
        selectedGameMode = SelectedGameMode.Halloween;
        GameManager.Instance.SetGameSettings(selectedGameMode, selectedUnit);
        GameManager.Instance.LoadGameScene();
    }

    public void Push_Quit()
    {
        Debug.Log("게임 종료");
        Application.Quit();
    }
}
