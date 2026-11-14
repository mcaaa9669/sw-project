using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class GameSave
{
    public string id;
    public string stage;
    public string section;
    public string updatedAt;
}

public static class SaveSession
{
    public const string FirstStage = "Stage01";
    public static GameSave Current { get; private set; }
    static GameSave pendingResume;
    static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Current = null;
        pendingResume = null;
    }

    public static bool IsSaveStage(string sceneName)
    {
        return sceneName == FirstStage || sceneName == "Stage02" ||
            sceneName == "Stage03" || sceneName == "Ending";
    }

    public static GameSave Create()
    {
        return Write(Guid.NewGuid().ToString("N"), FirstStage);
    }

    public static GameSave[] List()
    {
        Directory.CreateDirectory(SaveDirectory);
        var saves = new List<GameSave>();
        foreach (string file in Directory.EnumerateFiles(SaveDirectory, "*.json"))
        {
            try
            {
                saves.Add(Read(Path.GetFileNameWithoutExtension(file)));
            }
            catch (Exception error) when (IsFileError(error))
            {
                Debug.LogWarning($"저장 파일을 건너뛰었습니다. {file} ({error.Message})");
            }
        }

        saves.Sort((left, right) => string.CompareOrdinal(right.updatedAt, left.updatedAt));
        return saves.ToArray();
    }

    public static GameSave Read(string id)
    {
        string json = File.ReadAllText(GetPath(id));
        var save = JsonUtility.FromJson<GameSave>(json);
        if (save == null || save.id != id || !IsSaveStage(save.stage) ||
            !DateTimeOffset.TryParse(save.updatedAt, out _))
        {
            throw new InvalidDataException("저장 파일 형식이 올바르지 않습니다.");
        }

        return save;
    }

    public static void Select(GameSave save)
    {
        Current = save;
    }

    public static void PrepareResume(GameSave save)
    {
        pendingResume = save;
    }

    public static string ConsumeResumeSection(string stage)
    {
        if (pendingResume == null || pendingResume.stage != stage)
            return null;

        string section = pendingResume.section;
        pendingResume = null;
        return section;
    }

    // Save the scene and its entry section, without recording live gameplay state.
    public static void SaveAndExit(string stage, string section)
    {
        Current = Write(Current?.id ?? Guid.NewGuid().ToString("N"), stage, section);
    }

    public static void SaveProgress(string stage)
    {
        if (Current != null && IsSaveStage(stage))
            Current = Write(Current.id, stage);
    }

    public static bool IsFileError(Exception error)
    {
        return error is IOException || error is UnauthorizedAccessException || error is ArgumentException;
    }

    static GameSave Write(string id, string stage, string section = null)
    {
        Directory.CreateDirectory(SaveDirectory);
        string path = GetPath(id);
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var save = new GameSave { id = id, stage = stage, section = section, updatedAt = DateTime.UtcNow.ToString("O") };
        try
        {
            File.WriteAllText(temp, JsonUtility.ToJson(save, true));
            if (File.Exists(path))
                File.Replace(temp, path, null);
            else
                File.Move(temp, path);
        }
        finally
        {
            File.Delete(temp);
        }

        return save;
    }

    static string GetPath(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _))
            throw new InvalidDataException("저장 슬롯 번호가 올바르지 않습니다.");

        return Path.Combine(SaveDirectory, id + ".json");
    }
}
