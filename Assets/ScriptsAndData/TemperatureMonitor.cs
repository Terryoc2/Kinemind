using UnityEngine;
using System.Collections;
using TMPro;

public class TemperatureMonitor : MonoBehaviour
{
    [Header("Referencias Monitor")]
    public TextMeshProUGUI textoTemperatura; 
    public TextAsset temperatureFile; 

    [Header("Referencias Paciente")]
    public Animator pacienteAnimator;
    public SkinnedMeshRenderer rendererCabeza;

    [Header("Configuración Fiebre (Animación)")]
    public string nombreEstadoAnimacionFiebre = "FiebreSevera";

    [Header("Configuración Fiebre (Lógica y Visual)")]
    public float updateDelay = 2f; 
    public float temperaturaMaxima = 40.0f;
    public float incrementoFiebre = 0.5f;
    public float intervaloFiebre = 1.0f; 
    
    [Tooltip("Temperatura a la que empieza a aparecer el rubor")]
    public float tempInicioRubor = 38.0f; 
    
    [Tooltip("Temperatura a la que el rubor llega a su máximo")]
    public float tempMaxRubor = 40.0f;
    
    [Tooltip("Temperatura a la que el monitor se pone rojo")]
    public float temperaturaAlerta = 39.0f;

    private float temperaturaActual = 36.5f; 
    private bool fiebreActivada = false;
    private Color colorInicialTexto; 
    private Coroutine csvCoroutine;

    // Propiedad del Shader URP que regula la opacidad/intensidad del detalle
    private const string PROPIEDAD_INTENSIDAD_DETALLE = "_DetailAlbedoMapScale";

    void Start()
    {
        if (textoTemperatura != null) colorInicialTexto = textoTemperatura.color; 
        
        // Asegurarnos de que el rubor empiece en 0 (totalmente invisible)
        ActualizarIntensidadRubor(0f);
        
        ActualizarTextoYVisuales();
        if (temperatureFile != null) csvCoroutine = StartCoroutine(ReadTemperatureCSV());
    }

    public void InducirFiebreAdversa()
    {
        if (!fiebreActivada)
        {
            fiebreActivada = true; 
            if (csvCoroutine != null) StopCoroutine(csvCoroutine);

            if (pacienteAnimator != null)
            {
                pacienteAnimator.CrossFadeInFixedTime(nombreEstadoAnimacionFiebre, 0.5f);
            }

            temperaturaActual = 36.5f;
            ActualizarTextoYVisuales(); 
            StartCoroutine(SubirTemperaturaGradualmente()); 
        }
    }

    IEnumerator SubirTemperaturaGradualmente()
    {
        while (temperaturaActual < temperaturaMaxima)
        {
            yield return new WaitForSeconds(intervaloFiebre); 
            temperaturaActual += incrementoFiebre; 
            if (temperaturaActual > temperaturaMaxima) temperaturaActual = temperaturaMaxima;

            ActualizarTextoYVisuales(); 
        }
    }

    private void ActualizarTextoYVisuales()
    {
        if (textoTemperatura != null)
        {
            textoTemperatura.text = temperaturaActual.ToString("F1") + " °C"; 
            
            // El monitor sigue saltando a rojo solo a los 39°C
            textoTemperatura.color = (temperaturaActual >= temperaturaAlerta) ? Color.red : colorInicialTexto; 
        }

        // --- LÓGICA PROGRESIVA DEL RUBOR ---
        // InverseLerp calcula automáticamente un valor entre 0 y 1 basado en la temperatura actual
        float intensidad = Mathf.InverseLerp(tempInicioRubor, tempMaxRubor, temperaturaActual);
        ActualizarIntensidadRubor(intensidad);
    }

    private void ActualizarIntensidadRubor(float intensidad)
    {
        if (rendererCabeza != null && rendererCabeza.material != null)
        {
            // Le pasamos el nivel de intensidad (0.0 a 1.0) directamente al material
            rendererCabeza.material.SetFloat(PROPIEDAD_INTENSIDAD_DETALLE, intensidad);
        }
    }

    IEnumerator ReadTemperatureCSV()
    {
        if (temperatureFile == null) yield break;
        string[] lines = temperatureFile.text.Split('\n');
        
        while (!fiebreActivada) 
        {
            foreach (string line in lines)
            {
                if (fiebreActivada) yield break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (float.TryParse(line.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float tempValue))
                {
                    if (tempValue > 30.0f && tempValue < 45.0f) 
                    {
                        temperaturaActual = tempValue;
                        ActualizarTextoYVisuales(); 
                    }
                }
                yield return new WaitForSeconds(updateDelay);
            }
            yield return null; 
        }
    }
}