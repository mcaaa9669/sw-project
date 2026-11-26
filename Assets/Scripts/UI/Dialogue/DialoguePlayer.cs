using System.Collections;
using UnityEngine;
using UnityEngine.Events;


public class DialoguePlayer : MonoBehaviour
{
    const float MusicFadeDuration = 0.7f;

    [SerializeField] DialogueBox dialogueBox;
    [SerializeField] TextAsset script;
    [SerializeField] bool playOnStart = false;
    [SerializeField] bool playOnce = true;
    [SerializeField] UnityEvent onFinished;
    [SerializeField] AudioClip dialogueMusic;
    [SerializeField, Range(0f, 1f)] float dialogueMusicVolume = 0.2f;

    bool isPlaying;
    bool played;
    AudioSource musicSource;
    Coroutine musicFade;

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
        bool playMusic = lines.Length > 0 && dialogueMusic != null;
        if (playMusic)
            StartMusic();

        yield return dialogueBox.Play(lines);

        if (playMusic)
            musicFade = StartCoroutine(FadeOutMusic());

        isPlaying = false;
        onFinished.Invoke();
    }

    void StartMusic()
    {
        if (musicFade != null)
        {
            StopCoroutine(musicFade);
            musicFade = null;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }

        musicSource.clip = dialogueMusic;
        musicSource.volume = dialogueMusicVolume;
        musicSource.Play();
    }

    IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float startedAt = Time.unscaledTime;
        for (float elapsed = 0f; elapsed < MusicFadeDuration; elapsed = Time.unscaledTime - startedAt)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / MusicFadeDuration);
            yield return null;
        }

        musicSource.volume = 0f;
        musicSource.Stop();
        musicFade = null;
    }
}
