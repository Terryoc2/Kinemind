using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DetectorClickBoton : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI textoEstado;

    [Header("Prueba con mouse")]
    public bool enableMouseFallback = true;
    public bool invokeButtonOnMouseFallback = true;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Button button;
    private bool mousePressedInside;
    private int lastPointerClickFrame = -1;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        button = GetComponent<Button>();
    }

    void Update()
    {
        if (!enableMouseFallback || rectTransform == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            mousePressedInside = IsMouseInsideButton();
        }

        if (Input.GetMouseButtonUp(0))
        {
            bool shouldInvoke = mousePressedInside && IsMouseInsideButton();
            mousePressedInside = false;

            if (shouldInvoke)
            {
                StartCoroutine(InvokeMouseFallbackAtEndOfFrame(Time.frameCount));
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        lastPointerClickFrame = Time.frameCount;
        Debug.Log("BOTON PRESIONADO POR EVENTSYSTEM");

        if (textoEstado != null)
        {
            textoEstado.text = "BOTON PRESIONADO";
        }
    }

    IEnumerator InvokeMouseFallbackAtEndOfFrame(int clickFrame)
    {
        yield return new WaitForEndOfFrame();

        if (lastPointerClickFrame == clickFrame)
        {
            yield break;
        }

        Debug.Log("BOTON PRESIONADO CON MOUSE FALLBACK");

        if (textoEstado != null)
        {
            textoEstado.text = "BOTON PRESIONADO CON MOUSE";
        }

        if (invokeButtonOnMouseFallback && button != null && button.interactable)
        {
            button.onClick.Invoke();
        }
    }

    bool IsMouseInsideButton()
    {
        Camera eventCamera = null;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = parentCanvas.worldCamera;

            if (eventCamera == null)
            {
                eventCamera = Camera.main;
            }
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            rectTransform,
            Input.mousePosition,
            eventCamera
        );
    }
}