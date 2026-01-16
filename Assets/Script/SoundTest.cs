using CS.AudioToolkit;
using UnityEngine;
using System.Collections;

public class SoundTest : MonoBehaviour
{
    public float interval = 0.1f; // 오디오 재생 간격 (초)
    public string[] playOnce;
    public string[] playLoop;
    private Coroutine loopCoroutine; // 코루틴의 참조를 저장하기 위한 변수

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            // 배열에 있는 각 오디오 클립을 재생
            foreach (string clip in playOnce)
            {
                AudioController.Play(clip);
            }
        }

        if (playLoop != null)
        {
            if (Input.GetKeyDown(KeyCode.Alpha2)) // 특정 키(예: 2)가 처음 눌렸을 때
            {
                if (loopCoroutine == null) // 코루틴이 이미 실행중이지 않다면 시작
                {
                    loopCoroutine = StartCoroutine(PlaySoundRepeatedly());
                }
            }
            if (Input.GetKeyUp(KeyCode.Alpha2)) // 특정 키가 떼어졌을 때
            {
                if (loopCoroutine != null) // 코루틴이 실행중이라면 중지
                {
                    StopCoroutine(loopCoroutine);
                    loopCoroutine = null; // 참조 초기화
                }
            }
        }
    }

    private IEnumerator PlaySoundRepeatedly()
    {
        while (true) // 무한 루프
        {
            // 배열에 있는 각 오디오 클립을 재생
            foreach (string clip in playLoop)
            {
                AudioController.Play(clip);
            }
            yield return new WaitForSeconds(interval); // 설정된 간격만큼 대기
        }
    }
}
