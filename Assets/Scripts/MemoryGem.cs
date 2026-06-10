using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MemoryGem : MonoBehaviour
{
    public MemoryLevel1Manager manager;
    public GameObject objectToHide;

    private Renderer gemRenderer;
    private Color originalColor;

    void Awake()
    {
        gemRenderer = GetComponent<Renderer>();

        if (gemRenderer != null)
        {
            originalColor = gemRenderer.material.color;
        }

        if (objectToHide == null)
        {
            objectToHide = gameObject;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerHand"))
        {
            manager.TouchGem(this);
        }
    }

    public void HideGem()
    {
        objectToHide.SetActive(false);
    }

    public void Highlight(bool active)
    {
        if (gemRenderer == null) return;

        gemRenderer.material.color = active ? Color.yellow : originalColor;
    }
}