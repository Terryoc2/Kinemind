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
    public float temperaturaMaxima = 40.5f;

    // Variables internas
    private float temperaturaActual = 36.5f; // Valor inicial seguro
    private bool fiebreActivada = false;

    void Start()
    {
        if (textoTemperatura == null || temperatureFile == null) return;
        
        // Forzamos la primera actualización visual
        ActualizarTexto();
        StartCoroutine(ReadTemperatureCSV());
    }

    // Esta función la llamará tu NUEVO botón físico de Meta
    public void InducirFiebreAdversa()
    {
        fiebreActivada = true; // Bloqueamos el CSV

        if (temperaturaActual < temperaturaMaxima)
        {
            temperaturaActual += incrementoFiebre;
            ActualizarTexto();
            Debug.Log("¡Reacción Transfusional! Temperatura forzada a: " + temperaturaActual);
        }
    }

    private void ActualizarTexto()
    {
        if (textoTemperatura != null)
        {
            textoTemperatura.text = temperaturaActual.ToString("F1") + " °C"; 

            // Control estricto de colores
            if (temperaturaActual >= 38.0f)
            {
                textoTemperatura.color = Color.red; // Peligro
            }
            else
            {
                // Pon aquí el color normal de tu monitor (blanco, verde, naranja, etc.)
                textoTemperatura.color = Color.white; 
            }
        }
    }

    IEnumerator ReadTemperatureCSV()
    {
        string[] lines = temperatureFile.text.Split('\n');
        
        while (true) 
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Solo leemos del CSV si el instructor NO ha activado la fiebre
                if (!fiebreActivada) 
                {
                    if (float.TryParse(line.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float tempValue))
                    {
                        // Filtramos datos basura del CSV (temperaturas irreales como 0 o 100)
                        if (tempValue > 30.0f && tempValue < 45.0f) 
                        {
                            temperaturaActual = tempValue;
                            ActualizarTexto(); 
                        }
                    }
                }

                yield return new WaitForSeconds(updateDelay);
            }
            yield return null; 
        }
    }
}   