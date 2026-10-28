using System.Collections;
using UnityEngine;
using TMPro;

public class ToastMessage : MonoBehaviour
{   // 토스트 메시지 UI를 관리하는 클래스 (단일 인스턴스)
    public static ToastMessage Instance { get; private set; }   
    // 클래스에 단 하나 존재하는 정적 변수. 읽기는 누구나 가능하지만, 쓰기는 이 클래스 내부에서만 가능한 싱글톤 패턴 구현

    [Header("References")]
    [SerializeField] CanvasGroup canvasGroup;   // 페이드 대상
    [SerializeField] TMP_Text messageText;      // 메시지 텍스트를 표시할 TMP_Text (글자) 컴포넌트

    [Header("Timing")]
    [SerializeField] float fadeDuration = 0.25f;    // 페이드 효과 지속 시간 (나타나고 사라지는 시간 전체)
    [SerializeField] float defaultDuration = 2f;   // 메시지가 화면에 표시되는 기본 지속 시간 (페이드 효과를 제외한 순수 메시지 표시 시간)

    Coroutine running;      // 현재 실행 중인 코루틴을 추적하기 위한 변수. 메시지가 표시되는 동안 다른 메시지가 들어오면 기존 코루틴을 중단하고 새 메시지를 표시

    void Awake()
    {
        Instance = this;            // 자기 자신을 전역 접근용으로 등록
        canvasGroup.alpha = 0f;     // 초기 상태에서 메시지가 보이지 않도록 알파 값을 0으로 설정
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(string message)
    {
        Show(message, defaultDuration);
    }

    public void Show(string message, float duration)
    {
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(ShowRoutine(message, duration));
    }

    IEnumerator ShowRoutine(string message, float duration)
    {
        messageText.text = message;
        yield return UIFade.Fade(canvasGroup, canvasGroup.alpha, 1f, fadeDuration);
        yield return new WaitForSecondsRealtime(duration);
        yield return UIFade.Fade(canvasGroup, 1f, 0f, fadeDuration);
        running = null;
    }
}