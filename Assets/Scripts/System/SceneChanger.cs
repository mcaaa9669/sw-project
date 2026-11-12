using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void Load(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"씬을 불러올 수 없습니다. {sceneName}", this);
            return;
        }

        try
        {
            SaveSession.SaveProgress(sceneName);
        }
        catch (Exception error) when (SaveSession.IsFileError(error))
        {
            Debug.LogError($"스테이지 진행을 저장할 수 없습니다. {error.Message}", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
