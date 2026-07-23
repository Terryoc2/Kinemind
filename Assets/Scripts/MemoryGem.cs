using System;
using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
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

    [Header("Ajuste al colocar")]
    public Vector3 offsetLocalAlColocar = Vector3.zero;
    public Vector3 rotacionLocalAlColocar = Vector3.zero;
    public bool centrarVisualAlColocar = false;
    public bool centrarVisualAutomaticoDiamante = true;
    public bool congelarFisicaAlColocar = true;

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
        AsegurarAgarreConManos();

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

        BloquearAgarre();
        DetenerFisica();

        if (gemRigidbody != null)
        {
            gemRigidbody.useGravity = false;
            gemRigidbody.isKinematic = true;
        }

        Vector3 posicionFinal = snapPoint.TransformPoint(offsetLocalAlColocar);
        Quaternion rotacionFinal = snapPoint.rotation * Quaternion.Euler(rotacionLocalAlColocar);

        transform.SetPositionAndRotation(posicionFinal, rotacionFinal);

        if (centrarVisualAlColocar || (centrarVisualAutomaticoDiamante && EsDiamante()))
        {
            CentrarVisualEn(posicionFinal);
        }

        transform.SetParent(snapPoint, true);

        if (congelarFisicaAlColocar)
        {
            CongelarFisicaHijos();
        }

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

    private void CongelarFisicaHijos()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>();

        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb == null)
            {
                continue;
            }

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    private bool EsDiamante()
    {
        string nombre = !string.IsNullOrWhiteSpace(displayName) ? displayName : gameObject.name;

        return nombre.IndexOf("Diamond", StringComparison.OrdinalIgnoreCase) >= 0
            || nombre.IndexOf("Diamante", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void CentrarVisualEn(Vector3 posicionObjetivo)
    {
        if (!TryGetRendererBounds(out Bounds bounds))
        {
            return;
        }

        transform.position += posicionObjetivo - bounds.center;
    }

    private bool TryGetRendererBounds(out Bounds bounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer rendererActual in renderers)
        {
            if (rendererActual == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = rendererActual.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(rendererActual.bounds);
            }
        }

        return hasBounds;
    }

    #pragma warning disable 0618
    private void AsegurarAgarreConManos()
    {
        Rigidbody rigidbodyActual = GetComponent<Rigidbody>();
        if (rigidbodyActual == null)
        {
            rigidbodyActual = gameObject.AddComponent<Rigidbody>();
            rigidbodyActual.useGravity = false;
            rigidbodyActual.isKinematic = false;
        }

        if (GetComponent<Collider>() == null)
        {
            SphereCollider colliderFallback = gameObject.AddComponent<SphereCollider>();
            colliderFallback.radius = 0.5f;
        }

        Grabbable grabbable = GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = gameObject.AddComponent<Grabbable>();
        }

        grabbable.InjectOptionalRigidbody(rigidbodyActual);
        grabbable.InjectOptionalTargetTransform(transform);
        grabbable.InjectOptionalKinematicWhileSelected(true);
        grabbable.InjectOptionalThrowWhenUnselected(true);

        PhysicsGrabbable physicsGrabbable = GetComponent<PhysicsGrabbable>();
        if (physicsGrabbable == null)
        {
            physicsGrabbable = gameObject.AddComponent<PhysicsGrabbable>();
        }

        physicsGrabbable.InjectPointable(grabbable);
        physicsGrabbable.InjectRigidbody(rigidbodyActual);

        HandGrabInteractable handGrabInteractable = GetComponent<HandGrabInteractable>();
        if (handGrabInteractable == null)
        {
            handGrabInteractable = gameObject.AddComponent<HandGrabInteractable>();
        }

        handGrabInteractable.InjectRigidbody(rigidbodyActual);
        handGrabInteractable.InjectOptionalPointableElement(grabbable);
        handGrabInteractable.InjectOptionalPhysicsGrabbable(physicsGrabbable);
    }
    #pragma warning restore 0618
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
