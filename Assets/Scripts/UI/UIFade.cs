using System.Collections;       // 코루틴을 사용하기 위해 필요한 네임스페이스 (IEnumerator)
using UnityEngine;

public static class UIFade
{   // UI 페이드 관련 기능을 제공하는 정적 클래스 (공용)
    public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {           // 코루틴 (도중에 멈췄다가 다시 시작할 수 있는 함수)로 UI 페이드 효과를 수행
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);   // Mathf.Lerp: 선형 보간 함수, t/duration 비율에 따라 from과 to 사이의 값을 계산
            yield return null;      // yield return null: 다음 프레임까지 대기, 코루틴이 한 프레임 동안 멈춤
        }
        group.alpha = to;   // 최종적으로 to 값으로 설정하여 정확한 페이드 효과를 보장
    }
}