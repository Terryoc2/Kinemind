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

    private void Awake()
    {
        if (snapPoint == null)
        {
            snapPoint = transform;
        }

        if (feedbackRenderer != null)
        {
            feedbackMaterial = feedbackRenderer.material;
            normalColor = feedbackMaterial.color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (solved || manager == null)
        {
            return;
        }

        MemoryGem gem = other.GetComponentInParent<MemoryGem>();

        if (gem != null)
        {
            manager.IntentarColocar(this, gem);
        }
    }

    public void Configurar(MemoryLevel1Manager levelManager, MemoryGem gem, int orderNumber)
    {
        manager = levelManager;
        expectedGem = gem;
        solved = false;

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
        return expectedGem == gem;
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
