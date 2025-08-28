using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private int slowMotionCounter = 0; // 슬로우 모션 요청 카운터

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RequestSlowMotion(float duration, float timeScale)
    {
        // 첫번째 슬로우 모션 요청일 때만 시간을 느리게 함
        if (slowMotionCounter == 0)
        {
            Time.timeScale = timeScale;
        }

        // 카운터 증가 및 복구 코루틴 시작
        slowMotionCounter++;
        StartCoroutine(RestoreTimeScaleCoroutine(duration));
    }

    // 지젖ㅇ된 시간(실제 시간 기준) 후에 카운터를 줄이고 시간을 복구하는 코루틴
    private IEnumerator RestoreTimeScaleCoroutine(float duration)
    {
        // Time.timeScale에 영향을 받지 않는 실제 시간 기준으로 기다림
        yield return new WaitForSecondsRealtime(duration);

        slowMotionCounter--;

        // 모든 슬로우 모션 요청이 끝났을 때만 시간을 원래대로 복구
        if (slowMotionCounter == 0)
        {
            Time.timeScale = 1f; // 원래 시간으로 복구
        }
    }
}
