using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelPrincipalManager : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text textoTitulo;
    public TMP_Text textoIndicacion;

    [Header("Botones")]
    public Button botonCalibrar;
    public Button botonEmpezar;
    public Button botonContinuar;

    [Header("Tiempo")]
    public float segundosParaContinuar = 15f;

    [Header("Nivel 1")]
    public MemoryLevel1Manager nivel1Manager;

    private Coroutine rutinaContinuar;
    private bool actividadIniciada = false;
    private bool actividadCompletada = false;

    private void Start()
    {
        if (nivel1Manager == null)
        {
            nivel1Manager = FindObjectOfType<MemoryLevel1Manager>(true);
        }

        if (nivel1Manager != null)
        {
            nivel1Manager.AsignarPanel(this);
            nivel1Manager.OcultarNivel();
        }

        MostrarInicio();
    }

    public void MostrarInicio()
    {
        textoTitulo.text = "JKInemind";
        textoIndicacion.text = "Bienvenido";

        botonCalibrar.gameObject.SetActive(true);
        botonEmpezar.gameObject.SetActive(true);

        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

        actividadIniciada = false;
        actividadCompletada = false;
    }

    public void Empezar()
    {
        actividadIniciada = false;
        actividadCompletada = false;

        textoTitulo.text = "Nivel 1";

        if (nivel1Manager != null)
        {
            nivel1Manager.PrepararPatron();
            textoIndicacion.text = nivel1Manager.ObtenerTextoPatron();
        }
        else
        {
            textoIndicacion.text = "Memoriza el patron y coloca cada figura en su caja.";
        }

        botonCalibrar.gameObject.SetActive(false);
        botonEmpezar.gameObject.SetActive(false);

        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

        if (rutinaContinuar != null)
        {
            StopCoroutine(rutinaContinuar);
        }

        rutinaContinuar = StartCoroutine(MostrarContinuarDespuesDeTiempo());
    }

    private IEnumerator MostrarContinuarDespuesDeTiempo()
    {
        yield return new WaitForSeconds(segundosParaContinuar);

        botonContinuar.gameObject.SetActive(true);
        botonContinuar.interactable = true;
    }

    public void Continuar()
    {
        if (actividadCompletada)
        {
            textoTitulo.text = "Nivel 1";
            textoIndicacion.text = "Nivel 1 completado.";
            botonContinuar.interactable = false;
            return;
        }

        if (actividadIniciada)
        {
            return;
        }

        actividadIniciada = true;

        textoTitulo.text = "Nivel 1";
        textoIndicacion.text = "Ordena y pon en las cajas";

        botonContinuar.gameObject.SetActive(true);
        botonContinuar.interactable = false;

        if (nivel1Manager != null)
        {
            nivel1Manager.IniciarActividad();
        }
    }

    public void CompletarActividad()
    {
        actividadCompletada = true;
        textoTitulo.text = "Nivel 1";
        textoIndicacion.text = "Actividad completada. Puedes continuar.";

        botonContinuar.gameObject.SetActive(true);
        botonContinuar.interactable = true;
    }
}
