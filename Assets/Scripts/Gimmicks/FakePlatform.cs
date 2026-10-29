using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Collider2D))]          // Collider2D가 없으면 Unity가 자동으로 추가하고, 콜라이더를 실수로 지우지 못하게 막기. 안전 장치
public class FakePlatform : MonoBehaviour
{
    [SerializeField] float delayBeforeDialogue = 2f;
    [SerializeField] DialoguePlayer dialoguePlayer;
    [SerializeField] float fadeDuration = 0.3f;

    // 미리 선언해두면, Update()에서 매번 GetComponent를 호출하지 않아도 됨. 성능 향상
    Collider2D col;
    Tilemap tilemap;
    bool triggered;         // 발동했는지 검사하는 변수. 발동되면 true, 발동 안되면 false

    void Awake()
    {
        col = GetComponent<Collider2D>();
        tilemap = GetComponent<Tilemap>();
    }

    void OnCollisionEnter2D(Collision2D collision)                              // 충돌 감지
    {
        if (triggered) return;                                                  // 밟았으면 return; 밟지 않았으면 밑으로 내려감
        if (!collision.collider.TryGetComponent(out PlayerController _)) return; // 플레이어인 경우에만 발동

        foreach (ContactPoint2D contact in collision.contacts)                  // 충돌 지점이 여러 개일 수 있으므로, foreach로 모든 충돌 지점을 확인
        {
            if (contact.normal.y < -0.5f)                                       // 충돌 지점의 법선 벡터가 아래를 향하고 있는지(0,-1) 확인. 밟았는지 확인하는 방법. 0.5는 45도 정도의 범위까지 허용. 비스듬히 밟아도 인정
            {
                triggered = true;
                StartCoroutine(CollapseRoutine());                              // 코루틴 시작
                return;
            }
        }
    }

    IEnumerator CollapseRoutine()                                   
    {
        yield return new WaitForSeconds(delayBeforeDialogue);       // 대사 재생 전 대기 시간

        if (dialoguePlayer != null)                                 // 대사 재생이 지정되어 있으면, 대사 재생
        {
            dialoguePlayer.Play();                                  // 대사 재생 시작
            while (dialoguePlayer.IsPlaying) yield return null;     // 대사 재생이 끝날 때까지 기다림
        }

        col.enabled = false;                                        // 콜라이더 비활성화. 플레이어가 밟을 수 없게 됨

        float t = 0f;                                               // 투명화 진행 시간. 0~fadeDuration까지 증가
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(1f - t / fadeDuration);                        // 투명화 진행. 1~0까지 감소
            yield return null;
        }
        SetAlpha(0f);                                               
    }

    void SetAlpha(float a)                                          // 투명도 설정
    {
        Color c = tilemap.color;
        c.a = a;
        tilemap.color = c;
    }
}