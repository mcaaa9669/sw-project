using System.Collections;
using UnityEngine;
using UnityEngine.Events;


public class DialoguePlayer : MonoBehaviour
{
    [SerializeField] DialogueBox dialogueBox;
    [SerializeField] TextAsset script;
    [SerializeField] bool playOnStart = false;
    [SerializeField] bool playOnce = true;
    [SerializeField] UnityEvent onFinished;
    bool isPlaying;
    bool played;

    public bool IsPlaying => isPlaying;

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (isPlaying) return;
        if (playOnce && played) return;
        if (dialogueBox.IsOpen) return;
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        isPlaying = true;
        played = true;

        DialogueLine[] lines = DialogueParser.Parse(script.text);
        yield return dialogueBox.Play(lines);

        isPlaying = false;
        onFinished.Invoke();
    }
}