using System;
using System.Collections.Generic;
using UnityEngine;

public class MemoryGem : MonoBehaviour
{
    [Header("Datos")]
    public string displayName;
    public Sprite referenceSprite;

    [Header("Nivel")]
    public MemoryLevel1Manager manager;
    public GameObject objectToHide;
    public bool bloquearAgarreAlColocar = true;

    private Renderer gemRenderer;
    private Color originalColor;
    private Rigidbody gemRigidbody;
    private bool originalKinematic;
    private bool originalUseGravity;
    private Transform originalParent;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Behaviour[] grabBehaviours;
    private bool[] originalGrabEnabled;

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
            originalUseGravity = gemRigidbody.useGravity;
        }

        originalParent = transform.parent;
        GuardarComponentesDeAgarre();

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

        transform.SetParent(originalParent, true);
        RestaurarAgarre();
        DetenerFisica();
        transform.position = spawnPosition;

        if (gemRigidbody != null)
        {
            gemRigidbody.useGravity = originalUseGravity;
            gemRigidbody.isKinematic = originalKinematic;
        }

        Highlight(false);
    }

    public void VolverAlInicio()
    {
        transform.SetParent(originalParent, true);
        RestaurarAgarre();
        DetenerFisica();
        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (gemRigidbody != null)
        {
            gemRigidbody.useGravity = originalUseGravity;
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
            gemRigidbody.useGravity = false;
            gemRigidbody.isKinematic = true;
        }

        transform.SetPositionAndRotation(snapPoint.position, snapPoint.rotation);
        transform.SetParent(snapPoint, true);
        BloquearAgarre();
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

        if (gemRigidbody.isKinematic)
        {
            return;
        }

        gemRigidbody.velocity = Vector3.zero;
        gemRigidbody.angularVelocity = Vector3.zero;
    }

    private void GuardarComponentesDeAgarre()
    {
        Behaviour[] behaviours = GetComponents<Behaviour>();
        List<Behaviour> found = new List<Behaviour>();

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            if (typeName.IndexOf("Grab", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                found.Add(behaviour);
            }
        }

        grabBehaviours = found.ToArray();
        originalGrabEnabled = new bool[grabBehaviours.Length];

        for (int i = 0; i < grabBehaviours.Length; i++)
        {
            originalGrabEnabled[i] = grabBehaviours[i].enabled;
        }
    }

    private void BloquearAgarre()
    {
        if (!bloquearAgarreAlColocar || grabBehaviours == null)
        {
            return;
        }

        foreach (Behaviour behaviour in grabBehaviours)
        {
            if (behaviour != null)
            {
                behaviour.enabled = false;
            }
        }
    }

    private void RestaurarAgarre()
    {
        if (grabBehaviours == null || originalGrabEnabled == null)
        {
            return;
        }

        for (int i = 0; i < grabBehaviours.Length; i++)
        {
            if (grabBehaviours[i] != null)
            {
                grabBehaviours[i].enabled = originalGrabEnabled[i];
            }
        }
    }
}
