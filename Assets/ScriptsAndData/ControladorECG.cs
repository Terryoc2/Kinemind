using UnityEngine;
using System.Collections;
using ChartAndGraph;
using TMPro;

public class ControladorECG : MonoBehaviour
{
    [Header("Referencias")]
    public GraphChart graph;
    public TextAsset ecgFile;
    public TextMeshProUGUI textBPM;

    [Header("Configuración Onda")]
    public string categoryName = "ECG";
    public float updateDelay = 0.01f;
    public int puntosPorFrame = 3;
    public float tiempoMaximoPantalla = 5f;

    [Header("Lógica de BPM (Cálculo Médico)")]
    [Tooltip("Como tus picos llegan a 70, un umbral de 40-50 es ideal")]
    public float umbralDeteccion = 50f; 
    private float tiempoUltimoPico = 0f;
    private bool puedeDetectar = true;

    void Start()
    {
        if (graph == null || ecgFile == null)
        {
            Debug.LogError("Asigna el Graph y el CSV en el Inspector");
            return;
        }
        StartCoroutine(ReadECGCSV());
    }

    IEnumerator ReadECGCSV()
    {
        string[] lines = ecgFile.text.Split('\n');
        float currentTimeX = 0f;
        float tiempoAbsoluto = 0f;

        while (true)
        {
            int contadorFrame = 0;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (float.TryParse(line.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float yValue))
                {
                    // Dibujamos el punto en la gráfica
                    graph.DataSource.AddPointToCategory(categoryName, currentTimeX, yValue);
                    Debug.Log("Valor recibido: " + yValue);
                    
                    // Calculamos los BPM detectando el Pico R
                    CalcularBPM(yValue, tiempoAbsoluto);

                    currentTimeX += updateDelay;
                    tiempoAbsoluto += updateDelay;
                    contadorFrame++;
                }

                // Efecto de barrido (Sweep)
                if (currentTimeX >= tiempoMaximoPantalla)
                {
                    currentTimeX = 0f;
                    graph.DataSource.ClearCategory(categoryName);
                }

                // Control de rendimiento
                if (contadorFrame >= puntosPorFrame)
                {
                    yield return null;
                    contadorFrame = 0;
                }
            }
            yield return null;
        }
    }

    void CalcularBPM(float valorY, float tiempoActual)
    {
        // Detectamos si la onda cruza el umbral (Pico R)
        if (valorY > umbralDeteccion && puedeDetectar)
        {
            float intervalo = tiempoActual - tiempoUltimoPico;

            // Filtro para evitar detectar el mismo pico varias veces (máximo 200 BPM)
            if (intervalo > 0.3f) 
            {
                float bpm = 60f / intervalo;
                
                if (textBPM != null)
                {
                    textBPM.text = Mathf.RoundToInt(bpm).ToString();
                }

                tiempoUltimoPico = tiempoActual;
                puedeDetectar = false; 
                StartCoroutine(ResetDeteccion());
            }
        }
    }

    IEnumerator ResetDeteccion()
    {
        // Esperamos un momento para que la onda baje del umbral
        yield return new WaitForSeconds(0.2f);
        puedeDetectar = true;
    }
}