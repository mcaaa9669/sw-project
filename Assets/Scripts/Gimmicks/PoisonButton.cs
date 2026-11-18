using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public class PoisonButton : MonoBehaviour
{
    [Header("사라지게 할 독 오브젝트들")]
    public GameObject[] poisons;

    [Header("옵션")]
    public bool oneTime = true;
    public float fadeTime = 0.5f;
    public Sprite pressedSprite;

    [Header("눌렸을 때 실행할 것")]
    public UnityEvent onPressed;

    bool pressed = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        TryPress(other);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        TryPress(col.collider);
    }

    void TryPress(Collider2D other)
    {
        if (pressed && oneTime) return;

        Transform root = other.transform;
        if (other.attachedRigidbody != null)
            root = other.attachedRigidbody.transform;

        if (!(root.CompareTag("Player") || root.CompareTag("Shell"))) return;

        pressed = true;
        onPressed.Invoke();

        if (pressedSprite != null)
            GetComponent<SpriteRenderer>().sprite = pressedSprite;

        foreach (GameObject p in poisons)
        {
            if (p != null) StartCoroutine(FadeOut(p));
        }
    }

    IEnumerator FadeOut(GameObject target)
    {
        foreach (Collider2D c in target.GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        if (fadeTime > 0f)
        {
            SpriteRenderer[] srs = target.GetComponentsInChildren<SpriteRenderer>();
            Tilemap tm = target.GetComponent<Tilemap>();
            float t = 0f;

            while (t < fadeTime)
            {
                t += Time.deltaTime;
                float a = 1f - t / fadeTime;

                foreach (SpriteRenderer sr in srs)
                {
                    Color sc = sr.color;
                    sc.a = a;
                    sr.color = sc;
                }

                if (tm != null)
                {
                    Color tc = tm.color;
                    tc.a = a;
                    tm.color = tc;
                }

                yield return null;
            }
        }

        target.SetActive(false);
    }

    public void ResetButton()
    {
        pressed = false;

        foreach (GameObject p in poisons)
        {
            if (p == null) continue;

            p.SetActive(true);

            foreach (Collider2D c in p.GetComponentsInChildren<Collider2D>())
                c.enabled = true;

            foreach (SpriteRenderer sr in p.GetComponentsInChildren<SpriteRenderer>())
            {
                Color sc = sr.color;
                sc.a = 1f;
                sr.color = sc;
            }

            Tilemap tm = p.GetComponent<Tilemap>();
            if (tm != null)
            {
                Color tc = tm.color;
                tc.a = 1f;
                tm.color = tc;
            }
        }
    }
}