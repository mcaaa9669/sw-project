using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    public string speaker;
    [TextArea(2, 4)] public string body;
}