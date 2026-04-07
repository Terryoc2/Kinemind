// using UnityEngine;
// using System.Collections;
// using ChartAndGraph; 

// public class ECGMonitor : MonoBehaviour
// {
//     [Header("Referencias")]
//     public GraphChart graph;
//     public TextAsset ecgFile; // Aquí arrastras tu archivo ecg.csv

//     [Header("Configuración")]
//     public string categoryName = "ECG"; // El nombre en el Data del inspector
//     public float updateDelay = 0.02f; // El ECG suele ser más rápido que el oxígeno

//     void Start()
//     {
//         if (graph == null || ecgFile == null)
//         {
//             Debug.LogError("Falta asignar el Graph o el archivo ECG en el Inspector");
//             return;
//         }

//         // Iniciamos la lectura idéntica al monitor de oxígeno
//         StartCoroutine(ReadECGCSV());
//     }

//     IEnumerator ReadECGCSV()
//     {
//         string[] lines = ecgFile.text.Split('\n');

//         foreach (string line in lines)
//         {
//             if (string.IsNullOrWhiteSpace(line)) continue;

//             string[] values = line.Split(',');

//             if (values.Length >= 2)
//             {
//                 // Usamos la misma lógica de parseo que en el de oxígeno
//                 if (float.TryParse(values[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float x) && 
//                     float.TryParse(values[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float y))
//                 {
//                     // En el ECG normalmente no dividimos entre 100 
//                     // a menos que los valores sean muy grandes.
//                     graph.DataSource.AddPointToCategory(categoryName, x, y);
//                 }
//             }

//             yield return new WaitForSeconds(updateDelay);
//         }
//     }
// }

using UnityEngine;
using System.Collections;
using ChartAndGraph; 

public class ControladorECG : MonoBehaviour
{
    [Header("Referencias")]
    public GraphChart graph;
    public TextAsset ecgFile;

    [Header("Configuración")]
    public string categoryName = "ECG"; 
    public float updateDelay = 0.01f; 
    
    // NUEVO: ¿Cuántos puntos dibujamos por cada frame de Unity?
    // Sube este número si quieres que el corazón lata más rápido, bájalo si va muy veloz.
    public int puntosPorFrame = 3; 

    void Start()
    {
        if (graph == null || ecgFile == null)
        {
            Debug.LogError("Falta asignar el Graph o el archivo ECG en el Inspector");
            return;
        }

        StartCoroutine(ReadECGCSV());
    }

    IEnumerator ReadECGCSV()
    {
        string[] lines = ecgFile.text.Split('\n');
        float currentTimeX = 0f; 
        
        while (true) 
        {
            graph.DataSource.ClearCategory(categoryName);
            
            // Un contador para saber cuántos puntos hemos dibujado en este frame
            int contadorFrame = 0;

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (float.TryParse(line.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float yValue))
                {
                    graph.DataSource.AddPointToCategory(categoryName, currentTimeX, yValue);
                    currentTimeX += updateDelay; 
                    contadorFrame++;
                }

                // Si ya dibujamos los puntos que queríamos en este frame...
                if (contadorFrame >= puntosPorFrame)
                {
                    // ...le decimos a Unity que pase al siguiente frame
                    yield return null; 
                    // Reiniciamos el contador
                    contadorFrame = 0;
                }
            }
            
            yield return null; 
        }
    }
}