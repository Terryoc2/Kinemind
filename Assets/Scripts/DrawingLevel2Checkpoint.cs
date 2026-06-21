using UnityEngine;

public class DrawingLevel2Checkpoint : MonoBehaviour
{
    public int Indice { get; private set; }
    public bool EstaCompletado { get; private set; }

    private DrawingLevel2Manager manager;
    private Renderer puntoRenderer;

    public void Configurar(DrawingLevel2Manager nivel2Manager, int indice, Renderer rendererReferencia)
    {
        manager = nivel2Manager;
        Indice = indice;
        puntoRenderer = rendererReferencia;
        EstaCompletado = false;
    }

    public void MarcarCompletado()
    {
        EstaCompletado = true;
    }

    public void CambiarColor(Color color)
    {
        if (puntoRenderer == null || puntoRenderer.material == null)
        {
            return;
        }

        if (puntoRenderer.material.HasProperty("_BaseColor"))
        {
            puntoRenderer.material.SetColor("_BaseColor", color);
        }

        if (puntoRenderer.material.HasProperty("_Color"))
        {
            puntoRenderer.material.SetColor("_Color", color);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        manager?.RegistrarToque(this, other);
    }

    private void OnTriggerStay(Collider other)
    {
        manager?.RegistrarToque(this, other);
    }
}
