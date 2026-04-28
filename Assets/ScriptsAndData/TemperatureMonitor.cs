using UnityEngine;
using System.Collections;
using TMPro;

public class TemperatureMonitor : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoTemperatura; 
    public TextAsset temperatureFile; 

    [Header("Configuración CSV")]
    public float updateDelay = 2f; 

    [Header("Control de Eventos (Fiebre)")]
    public float incrementoFiebre = 0.5f;
    public float temperaturaMaxima = 40.0f;
    public float intervaloFiebre = 1.0f; 
    public float temperaturaAlerta = 39.0f; 

    private float temperaturaActual = 36.5f; 
    private bool fiebreActivada = false;
    private Color colorInicial; 

    // Guardamos la referencia de la corrutina del CSV para poder matarla
    private Coroutine csvCoroutine;

    void Start()
    {
        if (textoTemperatura == null || temperatureFile == null) return;
        
        colorInicial = textoTemperatura.color; 
        ActualizarTexto();
        
        // Iniciamos la lectura y guardamos el proceso
        csvCoroutine = StartCoroutine(ReadTemperatureCSV());
    }

    public void InducirFiebreAdversa()
    {
        if (!fiebreActivada)
        {
            fiebreActivada = true; 
            
            // 1. DETENER EL CSV: Esto evita que el monitor siga actualizándose con el archivo
            if (csvCoroutine != null)
            {
                StopCoroutine(csvCoroutine);
                csvCoroutine = null;
            }

            // 2. SETEO FIJO: Forzamos el valor inicial de la reacción
            temperaturaActual = 36.5f;
            ActualizarTexto(); 
            
            // 3. SUBIDA GRADUAL: Empezamos el incremento cada segundo
            StartCoroutine(SubirTemperaturaGradualmente()); 
            Debug.Log("Fiebre Iniciada: CSV detenido. Monitor seteado a 36.5 °C.");
        }
    }

    IEnumerator SubirTemperaturaGradualmente()
    {
        // Mientras no lleguemos a 40...
        while (temperaturaActual < temperaturaMaxima)
        {
            // Espera exactamente 1 segundo (intervaloFiebre)
            yield return new WaitForSeconds(intervaloFiebre); 
            
            temperaturaActual += incrementoFiebre; // +0.5
            
            if (temperaturaActual > temperaturaMaxima)
                temperaturaActual = temperaturaMaxima;

            ActualizarTexto(); 
        }
    }

    private void ActualizarTexto()
    {
        if (textoTemperatura != null)
        {
            textoTemperatura.text = temperaturaActual.ToString("F1") + " °C"; 

            // Rojo solo a partir de 39.0 grados
            if (temperaturaActual >= temperaturaAlerta)
            {
                textoTemperatura.color = Color.red; 
            }
            else
            {
                textoTemperatura.color = colorInicial; 
            }
        }
    }

    IEnumerator ReadTemperatureCSV()
    {
        string[] lines = temperatureFile.text.Split('\n');
        
        while (!fiebreActivada) 
        {
            foreach (string line in lines)
            {
                // Si se activa la fiebre mientras el bucle está a la mitad, salimos de inmediato
                if (fiebreActivada) yield break;

                if (string.IsNullOrWhiteSpace(line)) continue;

                if (float.TryParse(line.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float tempValue))
                {
                    if (tempValue > 30.0f && tempValue < 45.0f) 
                    {
                        temperaturaActual = tempValue;
                        ActualizarTexto(); 
                    }
                }
                yield return new WaitForSeconds(updateDelay);
            }
            yield return null; 
        }
    }
}