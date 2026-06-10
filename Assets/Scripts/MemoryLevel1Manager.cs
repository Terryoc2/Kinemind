using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryLevel1Manager : MonoBehaviour
{
    public MemoryGem[] availableGems;
    public int patternLength = 3;
    public bool allowRepeats = false;

    public AudioSource audioSource;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    private MemoryGem[] currentPattern;
    private int currentIndex = 0;
    private bool canTouch = false;

    void Start()
    {
        GenerateRandomPattern();
        StartCoroutine(ShowPattern());
    }

    void GenerateRandomPattern()
    {
        currentIndex = 0;
        currentPattern = new MemoryGem[patternLength];

        List<MemoryGem> pool = new List<MemoryGem>(availableGems);

        for (int i = 0; i < patternLength; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            currentPattern[i] = pool[randomIndex];

            if (!allowRepeats)
            {
                pool.RemoveAt(randomIndex);
            }
        }

        Debug.Log("Patron aleatorio generado");
    }

    IEnumerator ShowPattern()
    {
        canTouch = false;

        yield return new WaitForSeconds(1f);

        foreach (MemoryGem gem in currentPattern)
        {
            gem.Highlight(true);
            yield return new WaitForSeconds(0.8f);

            gem.Highlight(false);
            yield return new WaitForSeconds(0.3f);
        }

        canTouch = true;
    }

    public void TouchGem(MemoryGem touchedGem)
    {
        if (!canTouch) return;

        MemoryGem expectedGem = currentPattern[currentIndex];

        if (touchedGem == expectedGem)
        {
            Debug.Log("Correcto");

            if (audioSource != null && correctClip != null)
                audioSource.PlayOneShot(correctClip);

            touchedGem.HideGem();
            currentIndex++;

            if (currentIndex >= currentPattern.Length)
            {
                Debug.Log("Nivel 1 completado");
                canTouch = false;
            }
        }
        else
        {
            Debug.Log("Incorrecto");

            if (audioSource != null && wrongClip != null)
                audioSource.PlayOneShot(wrongClip);
        }
    }
}