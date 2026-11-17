
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameObject stageBadge;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text deathText;

    [Header("Soul Timer")]
    [SerializeField] private GameObject soulTimerGroup;
    [SerializeField] private Image timerFill;

    public void SetStage(string section, bool showDeaths)
    {
        if (stageBadge != null && stageText != null)
        {
            stageBadge.SetActive(!string.IsNullOrEmpty(section));
            stageText.text = string.IsNullOrEmpty(section) ? string.Empty : $"STAGE {section}";
        }

        if (deathText != null)
            deathText.gameObject.SetActive(showDeaths);
    }

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
        timerFill.fillAmount = maxTime > 0f ? Mathf.Clamp01(remaining / maxTime) : 0f;
    }

    // 영혼세계 타이머 표시 및 숨김
    public void ShowSoulTimer(bool show)
    {
        soulTimerGroup.SetActive(show);
    }

    private void Awake()
    {
        string sceneName = gameObject.scene.name;
        if (sceneName.StartsWith("Stage") && int.TryParse(sceneName.Substring(5), out int stageNumber))
            SetStage(stageNumber.ToString(), true);
        ShowSoulTimer(false);
    }
}
