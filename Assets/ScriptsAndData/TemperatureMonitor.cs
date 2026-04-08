using UnityEngine;
using System.Collections;
using TMPro;

public class TemperatureMonitor : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoTemperatura; 
    public TextAsset temperatureFile; 

    [Header("Configuración")]
    public float updateDelay = 2f; // La temperatura cambia lento, cada 2 segundos está perfecto

    void Start()
    {
        if (textoTemperatura == null || temperatureFile == null)
        {
            Debug.LogError("Falta asignar el Texto o el CSV de temperatura en el Inspector");
            return;
        }
        StartCoroutine(ReadTemperatureCSV());
    }

    IEnumerator ReadTemperatureCSV()
    {
        string[] lines = temperatureFile.text.Split('\n');
        
        while (true) 
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (float.TryParse(line.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float tempValue))
                {
                    // ToString("F1") asegura que siempre tenga 1 decimal (ej. 36.5)
                    textoTemperatura.text = tempValue.ToString("F1") + " °C"; 
                }

                yield return new WaitForSeconds(updateDelay);
            }
            yield return null; 
        }
    }
}
