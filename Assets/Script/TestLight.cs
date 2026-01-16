using UnityEngine;

public class TestLight : MonoBehaviour
{
    private GameSceneManager sceneManager;
    private Transform playerTransform;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject managerObj = GameObject.FindGameObjectWithTag("GameSceneManager");
        if (managerObj != null)
            sceneManager = managerObj.GetComponent<GameSceneManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (sceneManager == null)
            return;

        if (sceneManager.playerUnit == null)
            return;

        if (playerTransform == null || playerTransform != sceneManager.playerUnit.transform)
            playerTransform = sceneManager.playerUnit.transform;

        Vector3 targetPos = new Vector3(
    playerTransform.position.x,
    playerTransform.position.y,
    playerTransform.position.z // 기존 카메라 z 위치 유지
);

        transform.position = targetPos;
    }
}
