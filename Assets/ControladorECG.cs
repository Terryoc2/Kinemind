using UnityEngine;
using System.Collections;
using ChartAndGraph; 

public class ECGMonitor : MonoBehaviour
{
    [Header("Referencias")]
    public GraphChart graph;
    public TextAsset ecgFile; // Aquí arrastras tu archivo ecg.csv

    [Header("Configuración")]
    public string categoryName = "ECG"; // El nombre en el Data del inspector
    public float updateDelay = 0.02f; // El ECG suele ser más rápido que el oxígeno

    void Start()
    {
        if (graph == null || ecgFile == null)
        {
            Debug.LogError("Falta asignar el Graph o el archivo ECG en el Inspector");
            return;
        }

        // Iniciamos la lectura idéntica al monitor de oxígeno
        StartCoroutine(ReadECGCSV());
    }

    IEnumerator ReadECGCSV()
    {
        string[] lines = ecgFile.text.Split('\n');

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = line.Split(',');

            if (values.Length >= 2)
            {
                // Usamos la misma lógica de parseo que en el de oxígeno
                if (float.TryParse(values[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float x) && 
                    float.TryParse(values[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float y))
                {
                    // En el ECG normalmente no dividimos entre 100 
                    // a menos que los valores sean muy grandes.
                    graph.DataSource.AddPointToCategory(categoryName, x, y);
                }
            }

            yield return new WaitForSeconds(updateDelay);
        }
    }
}