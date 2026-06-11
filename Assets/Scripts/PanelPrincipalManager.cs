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

    private bool actividadCompletada = false;

    private void Start()
    {
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
    }

    public void Empezar()
    {
        textoTitulo.text = "Nivel 1";
        textoIndicacion.text = "Memoriza el patron que se mostrara y dejalas en sus respectivas cajas";

        botonCalibrar.gameObject.SetActive(false);
        botonEmpezar.gameObject.SetActive(false);

        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

        StartCoroutine(MostrarContinuarDespuesDeTiempo());
    }

    private IEnumerator MostrarContinuarDespuesDeTiempo()
    {
        yield return new WaitForSeconds(segundosParaContinuar);

        botonContinuar.gameObject.SetActive(true);
        botonContinuar.interactable = true;
    }

    public void Continuar()
    {
        textoTitulo.text = "Nivel 1";
        textoIndicacion.text = "Ordena y pon en las cajas";

        botonContinuar.gameObject.SetActive(true);
        botonContinuar.interactable = false;

        actividadCompletada = false;
    }

    public void CompletarActividad()
    {
        actividadCompletada = true;
        botonContinuar.interactable = true;
    }
}