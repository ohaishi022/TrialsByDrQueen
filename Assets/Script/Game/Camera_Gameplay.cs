using UnityEngine;

public class Camera_Gameplay : MonoBehaviour
{
    public float followSpeed = 6.4f; // 카메라 이동 속도 (부드럽게 조정)
    private GameSceneManager sceneManager;
    private Transform playerTransform;

    [Header("HP 기반 뭉개짐(로우패스) 설정")]
    [Range(0.1f, 1f)] public float muffleThreshold = 0.5f; // HP 비율이 이 값 미만일 때부터 뭉개짐 시작
    [Min(10f)] public float minCutoff = 220f;              // HP가 0%일 때 근접하는 컷오프(더 낮게=더 뭉개짐)
    [Min(1000f)] public float maxCutoff = 22000f;          // 정상 상태 컷오프
    [Range(0.01f, 1f)] public float cutoffSmoothTime = 0.1f; // 컷오프 스무딩 시간(짧을수록 즉각적)

    [Header("데미지 인디케이터 UI")]
    public CanvasGroup damageIndicator; // ← CanvasGroup 추천 (Image 써도 가능)
    public AudioSource lowHealthAudio;

    private AudioLowPassFilter lp;
    private float cutoffVel; // SmoothDamp용
    private float alphaVel; // 알파용 스무딩
    private float volVel;

    void Start()
    {
        GameObject managerObj = GameObject.FindGameObjectWithTag("GameSceneManager");
        if (managerObj != null)
            sceneManager = managerObj.GetComponent<GameSceneManager>();

        lp = GetComponent<AudioLowPassFilter>();
        if (lp == null) lp = gameObject.AddComponent<AudioLowPassFilter>();
        lp.enabled = true;
        lp.lowpassResonanceQ = 1.0f;     // 너무 울리지 않게 기본값
        lp.cutoffFrequency = maxCutoff;  // 시작은 정상 청취

        if (damageIndicator != null)
        {
            damageIndicator.alpha = 0f; // 시작은 완전 투명
            damageIndicator.gameObject.SetActive(false);
        }

        if (lowHealthAudio != null)
        {
            if (!lowHealthAudio.isPlaying)
                lowHealthAudio.Play();
            lowHealthAudio.volume = 0f;
        }
    }

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
            transform.position.z // 기존 카메라 z 위치 유지
        );
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);

        float targetCutoff = maxCutoff;
        float targetAlpha = 0f;

        if (sceneManager.CurrentSituation == GameSituation.Playing)
        {
            var player = sceneManager.playerUnit;

            float maxHP = Mathf.Max(0.0001f, player.health); // 0 방지
            float hpRatio = Mathf.Clamp01(player.currentHealth / maxHP); // 0~1

            float dangerT;
            if (hpRatio >= muffleThreshold)
            {
                dangerT = 0f;
            }
            else if (hpRatio <= 0.1f)
            {
                dangerT = 1f;
            }
            else
            {
                dangerT = (muffleThreshold - hpRatio) / (muffleThreshold - 0.1f);
            }
            dangerT = Mathf.Clamp01(dangerT);

            targetCutoff = Mathf.Lerp(maxCutoff, minCutoff, dangerT);

            targetAlpha = Mathf.Lerp(0f, 1f, dangerT);
        }

        lp.cutoffFrequency = Mathf.SmoothDamp(
            lp.cutoffFrequency,
            targetCutoff,
            ref cutoffVel,
            cutoffSmoothTime
        );

        if (damageIndicator != null)
        {
            if (!damageIndicator.gameObject.activeSelf && targetAlpha > 0f)
                damageIndicator.gameObject.SetActive(true);

            float newAlpha = Mathf.SmoothDamp(
                damageIndicator.alpha,
                targetAlpha,
                ref alphaVel,
                cutoffSmoothTime
            );
            damageIndicator.alpha = newAlpha;

            if (newAlpha <= 0.001f)
            {
                damageIndicator.gameObject.SetActive(false);
            }
        }

        if (lowHealthAudio != null)
        {
            float newVol = Mathf.SmoothDamp(
                lowHealthAudio.volume,
                targetAlpha,
                ref volVel,
                cutoffSmoothTime
            );
            lowHealthAudio.volume = Mathf.Clamp01(newVol);
        }
    }
}
