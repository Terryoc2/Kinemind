using UnityEngine;
using System.Collections;
using ChartAndGraph; // Asegúrate de tener el asset instalado

public class OxygenMonitor : MonoBehaviour
{
    [Header("Referencias")]
    public GraphChart graph;
    public TextAsset oxygen; // Aquí arrastras tu archivo oxygen.csv

    [Header("Configuración")]
    public string categoryName = "OXYGEN"; // El nombre que pusiste en el Data del inspector
    public float updateDelay = 0.05f; // Velocidad de la señal (ajusta a tu gusto)

    void Start()
    {
        if (graph == null || oxygen == null)
        {
            Debug.LogError("Falta asignar el Graph o el archivo Oxygen en el Inspector");
            return;
        }

        // Iniciamos la lectura
        StartCoroutine(ReadOxygenCSV());
    }

    IEnumerator ReadOxygenCSV()
    {
        // Dividimos el contenido del CSV por líneas
        string[] lines = oxygen.text.Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Separamos Tiempo de Valor (asumiendo formato: tiempo,valor)
            string[] values = line.Split(',');

            if (values.Length >= 2)
            {
                if (float.TryParse(values[0], out float x) &&
                    float.TryParse(values[1], out float y))
                {
                    // Normalización (ej: 90–100 → 0.9–1.0)
                    float normalizedY = y / 100f;

                    graph.DataSource.AddPointToCategory(categoryName, x, normalizedY);
                }
            }

            // Simula tiempo real
            yield return new WaitForSeconds(updateDelay);
        }
    }
}