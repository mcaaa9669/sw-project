
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text deathText;

    [Header("Soul Timer")]
    [SerializeField] private GameObject soulTimerGroup;
    [SerializeField] private Image timerFill;

    // 사망 횟수 갱신
    private int displayedMaxDeaths = 5;

    public void SetDeathCount(int count)
    {
        SetDeathCount(count, displayedMaxDeaths);
    }

    public void SetDeathCount(int count, int maxDeaths)
    {
        displayedMaxDeaths = Mathf.Max(1, maxDeaths);
        deathText.text = $"Deaths : {count} / {displayedMaxDeaths}";
    }

    // 남은 시간에 따라 게이지 갱신
    public void SetSoulTime(float remaining, float maxTime)
    {
        if (maxTime <= 0f)
        {
            timerFill.fillAmount = 0f;
            return;
        }

        timerFill.fillAmount =
            Mathf.Clamp01(remaining / maxTime);
    }

    // 영혼세계 타이머 표시 및 숨김
    public void ShowSoulTimer(bool show)
    {
        soulTimerGroup.SetActive(show);
    }

    private void Start()
    {
        
        SetSoulTime(5f, 5f);
        ShowSoulTimer(false);
    }
}
