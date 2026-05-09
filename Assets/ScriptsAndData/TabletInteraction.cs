using UnityEngine;
using UnityEngine.EventSystems;

public class TabletInteraction : MonoBehaviour, IPointerClickHandler
{
    [Header("Referencia al manager del teclado")]
    public KeyboardManager keyboardManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (keyboardManager != null)
            keyboardManager.ShowKeyboard();
    }
}