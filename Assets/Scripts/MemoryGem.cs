using System;
using UnityEngine;

public class MemoryGem : MonoBehaviour
{
    [Header("Datos")]
    public string displayName;
    public Sprite referenceSprite;

    [Header("Nivel")]
    public MemoryLevel1Manager manager;
    public GameObject objectToHide;

    private Renderer gemRenderer;
    private Color originalColor;
    private Rigidbody gemRigidbody;
    private bool originalKinematic;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    public string NombreVisible
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            string rawName = gameObject.name.Replace("(Clone)", "").Trim();

            if (rawName.IndexOf("Star", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Estrella";
            }

            if (rawName.IndexOf("Sphere", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Esfera";
            }

            if (rawName.IndexOf("Diamond", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Diamante";
            }

            return rawName;
        }
    }

    private void Awake()
    {
        gemRenderer = GetComponent<Renderer>();
        gemRigidbody = GetComponent<Rigidbody>();

        if (gemRenderer != null)
        {
            originalColor = gemRenderer.material.color;
        }

        if (gemRigidbody != null)
        {
            originalKinematic = gemRigidbody.isKinematic;
        }

        if (objectToHide == null)
        {
            objectToHide = gameObject;
        }
    }

    public void PrepararOculta()
    {
        if (objectToHide == null)
        {
            objectToHide = gameObject;
        }

        objectToHide.SetActive(false);
    }

    public void ActivarEn(Vector3 position)
    {
        if (objectToHide == null)
        {
            objectToHide = gameObject;
        }

        objectToHide.SetActive(true);

        spawnPosition = position;
        spawnRotation = transform.rotation;

        DetenerFisica();
        transform.position = spawnPosition;

        if (gemRigidbody != null)
        {
            gemRigidbody.isKinematic = originalKinematic;
        }

        Highlight(false);
    }

    public void VolverAlInicio()
    {
        DetenerFisica();
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (gemRigidbody != null)
        {
            gemRigidbody.isKinematic = originalKinematic;
        }
    }

    public void FijarEn(Transform snapPoint)
    {
        if (snapPoint == null)
        {
            return;
        }

        DetenerFisica();

        if (gemRigidbody != null)
        {
            gemRigidbody.isKinematic = true;
        }

        transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
        Highlight(false);
    }

    public void Highlight(bool active)
    {
        if (gemRenderer == null) return;

        gemRenderer.material.color = active ? Color.yellow : originalColor;
    }

    private void DetenerFisica()
    {
        if (gemRigidbody == null)
        {
            return;
        }

        gemRigidbody.velocity = Vector3.zero;
        gemRigidbody.angularVelocity = Vector3.zero;
    }
}
