using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryBoxTarget : MonoBehaviour
{
    [Header("Referencia visual")]
    public GameObject referenceRoot;
    public SpriteRenderer referenceSpriteRenderer;
    public Image referenceImage;
    public TMP_Text referenceText;

    [Header("Colocacion")]
    public Transform snapPoint;
    public Renderer feedbackRenderer;

    [Header("Deteccion")]
    public bool asegurarTriggerDeteccion = true;
    public Vector3 triggerSize = new Vector3(0.55f, 0.35f, 0.55f);
    public Vector3 triggerCenter = new Vector3(0f, 0.15f, 0f);
    public bool usarColliderExistenteComoTrigger = true;
    public bool centrarSnapAutomaticoEnCollider = true;
    public bool filtrarCercaniaSiSnapEsAutomatico = true;
    public float distanciaMaximaAlColocar = 0.28f;
    public Vector3 snapAutomaticoLocal = new Vector3(0f, 0.15f, 0f);

    [Header("Colores")]
    public Color normalColor = Color.white;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public float wrongFlashTime = 0.35f;

    private MemoryLevel1Manager manager;
    private MemoryGem expectedGem;
    private bool solved;
    private Material feedbackMaterial;
    private Coroutine wrongRoutine;
    private float nextCheckTime;
    private bool snapAutomatico;
    private const float RepeatCheckDelay = 0.25f;

    private void Awake()
    {
        if (snapPoint == null)
        {
            CrearSnapAutomatico();
        }

        if (feedbackRenderer != null)
        {
            feedbackMaterial = feedbackRenderer.material;
            normalColor = feedbackMaterial.color;
        }

        if (asegurarTriggerDeteccion)
        {
            AsegurarTriggerDeteccion();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        RevisarEntrada(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        RevisarEntrada(other);
    }

    private void RevisarEntrada(Collider other)
    {
        if (manager == null)
        {
            return;
        }

        MemoryGem gem = other.GetComponentInParent<MemoryGem>();

        if (gem == null)
        {
            return;
        }

        if (solved)
        {
            if (!EsCorrecta(gem))
            {
                gem.VolverAlInicio();
            }

            return;
        }

        if (!PuedeColocarseAqui(gem))
        {
            return;
        }

        nextCheckTime = Time.time + RepeatCheckDelay;
        manager.IntentarColocar(this, gem);
    }

    public void Configurar(MemoryLevel1Manager levelManager, MemoryGem gem, int orderNumber)
    {
        manager = levelManager;
        expectedGem = gem;
        solved = false;

        if (snapPoint == null)
        {
            CrearSnapAutomatico();
        }

        if (asegurarTriggerDeteccion)
        {
            AsegurarTriggerDeteccion();
        }

        SetReferenceVisible(true);

        if (referenceSpriteRenderer != null)
        {
            referenceSpriteRenderer.sprite = gem != null ? gem.referenceSprite : null;
            referenceSpriteRenderer.enabled = referenceSpriteRenderer.sprite != null;
        }

        if (referenceImage != null)
        {
            referenceImage.sprite = gem != null ? gem.referenceSprite : null;
            referenceImage.enabled = referenceImage.sprite != null;
        }

        if (referenceText != null)
        {
            string name = gem != null ? gem.NombreVisible : string.Empty;
            referenceText.text = orderNumber + ". " + name;
        }

        SetFeedbackColor(normalColor);
    }

    public void OcultarReferencia()
    {
        SetReferenceVisible(false);
        solved = false;
        expectedGem = null;
        SetFeedbackColor(normalColor);
    }

    public bool EsCorrecta(MemoryGem gem)
    {
        if (expectedGem == null || gem == null)
        {
            return false;
        }

        if (expectedGem == gem)
        {
            return true;
        }

        return string.Equals(expectedGem.NombreVisible, gem.NombreVisible, StringComparison.OrdinalIgnoreCase);
    }

    public string NombreEsperado
    {
        get { return expectedGem != null ? expectedGem.NombreVisible : "sin figura"; }
    }

    public void MarcarCorrecta()
    {
        solved = true;
        SetFeedbackColor(correctColor);
    }

    public void MarcarIncorrecta()
    {
        if (wrongRoutine != null)
        {
            StopCoroutine(wrongRoutine);
        }

        wrongRoutine = StartCoroutine(FlashWrong());
    }

    private IEnumerator FlashWrong()
    {
        SetFeedbackColor(wrongColor);
        yield return new WaitForSeconds(wrongFlashTime);
        SetFeedbackColor(normalColor);
        wrongRoutine = null;
    }

    private void AsegurarTriggerDeteccion()
    {
        Collider[] colliders = GetComponents<Collider>();
        BoxCollider primerBoxCollider = null;

        foreach (Collider collider in colliders)
        {
            if (collider != null && collider.isTrigger)
            {
                collider.enabled = true;
                return;
            }

            if (primerBoxCollider == null)
            {
                primerBoxCollider = collider as BoxCollider;
            }
        }

        if (usarColliderExistenteComoTrigger && primerBoxCollider != null)
        {
            primerBoxCollider.isTrigger = true;
            primerBoxCollider.enabled = true;
            return;
        }

        BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = triggerSize;
        trigger.center = triggerCenter;
        trigger.enabled = true;
    }

    private void CrearSnapAutomatico()
    {
        snapAutomatico = true;

        Transform existente = transform.Find("SnapPoint_Auto");

        if (existente != null)
        {
            snapPoint = existente;
            return;
        }

        GameObject autoSnap = new GameObject("SnapPoint_Auto");
        autoSnap.transform.SetParent(transform, false);
        autoSnap.transform.localPosition = ObtenerPosicionSnapAutomatico();
        autoSnap.transform.localRotation = Quaternion.identity;
        snapPoint = autoSnap.transform;
    }

    private Vector3 ObtenerPosicionSnapAutomatico()
    {
        if (!centrarSnapAutomaticoEnCollider)
        {
            return snapAutomaticoLocal;
        }

        Collider collider = GetComponent<Collider>();

        if (collider is BoxCollider boxCollider)
        {
            return boxCollider.center;
        }

        return snapAutomaticoLocal;
    }

    private bool PuedeColocarseAqui(MemoryGem gem)
    {
        if (!filtrarCercaniaSiSnapEsAutomatico || !snapAutomatico || gem == null || snapPoint == null)
        {
            return true;
        }

        float limite = Mathf.Max(0.01f, distanciaMaximaAlColocar);
        return EstaCercaDelSnap(gem, limite);
    }

    private bool EstaCercaDelSnap(MemoryGem gem, float limite)
    {
        Collider[] colliders = gem.GetComponentsInChildren<Collider>(true);
        bool tieneCollider = false;

        foreach (Collider colliderFigura in colliders)
        {
            if (colliderFigura == null || !colliderFigura.enabled)
            {
                continue;
            }

            tieneCollider = true;

            Vector3 puntoMasCercano = colliderFigura.ClosestPoint(snapPoint.position);

            if (Vector3.Distance(puntoMasCercano, snapPoint.position) <= limite)
            {
                return true;
            }
        }

        if (!tieneCollider)
        {
            Vector3 centroFigura = ObtenerCentroVisual(gem);
            return Vector3.Distance(centroFigura, snapPoint.position) <= limite;
        }

        return false;
    }

    private Vector3 ObtenerCentroVisual(MemoryGem gem)
    {
        Renderer[] renderers = gem.GetComponentsInChildren<Renderer>(true);
        bool tieneBounds = false;
        Bounds bounds = new Bounds(gem.transform.position, Vector3.zero);

        foreach (Renderer rendererFigura in renderers)
        {
            if (rendererFigura == null)
            {
                continue;
            }

            if (!tieneBounds)
            {
                bounds = rendererFigura.bounds;
                tieneBounds = true;
            }
            else
            {
                bounds.Encapsulate(rendererFigura.bounds);
            }
        }

        return tieneBounds ? bounds.center : gem.transform.position;
    }

    private void SetReferenceVisible(bool visible)
    {
        if (referenceRoot != null)
        {
            referenceRoot.SetActive(visible);
        }

        if (referenceSpriteRenderer != null)
        {
            referenceSpriteRenderer.gameObject.SetActive(visible);
        }

        if (referenceImage != null)
        {
            referenceImage.gameObject.SetActive(visible);
        }

        if (referenceText != null)
        {
            referenceText.gameObject.SetActive(visible);
        }
    }

    private void SetFeedbackColor(Color color)
    {
        if (feedbackMaterial != null)
        {
            feedbackMaterial.color = color;
        }
    }
}
