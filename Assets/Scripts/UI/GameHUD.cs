
using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text deathText;

    // 사망 횟수를 화면에 표시하는 함수
    public void SetDeathCount(int count)
    {
        deathText.text = $"Deaths : {count}";
    }

    private void Start()
    {
        SetDeathCount(0);
    }
}

