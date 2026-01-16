using CS.AudioToolkit;
using System.Collections;
using UnityEngine;

public class SkillEffect : MonoBehaviour
{
    [Header("사운드")]
    public string[] SE_Start;

    [Header("지속 시간")]
    public float effectDuration = 1f;

    [Header("배속 적용 여부")]
    public bool followUnitSpeed = false;  // true면 owner.SpeedMultiplier 적용
    public Unit_Base owner;               // 자동 설정 가능

    private Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>(); // FX 안 Animator 자동 탐색
    }

    private void Start()
    {
        // 사운드
        if (SE_Start != null)
        {
            foreach (string clip in SE_Start)
            {
                if (!string.IsNullOrEmpty(clip))
                    AudioController.Play(clip, transform.position);
            }
        }

        // owner가 비어 있으면 부모에서 찾기
        if (followUnitSpeed && owner == null)
            owner = GetComponentInParent<Unit_Base>();

        StartCoroutine(Remove());
    }

    private void Update()
    {
        // 애니메이션 배속 적용
        if (followUnitSpeed && owner != null && anim != null)
        {
            anim.speed = Mathf.Max(0.01f, 1 * owner.SpeedMultiplier);
        }
    }

    private IEnumerator Remove()
    {
        // 배속 미적용 → 기존 방식
        if (!followUnitSpeed || owner == null)
        {
            yield return new WaitForSeconds(effectDuration);
            Destroy(gameObject);
            yield break;
        }

        // 배속 적용 버전
        float t = 0f;
        while (t < effectDuration)
        {
            float mul = Mathf.Max(0.01f, owner.SpeedMultiplier);
            t += Time.deltaTime * mul;
            yield return null;
        }

        Destroy(gameObject);
    }
}