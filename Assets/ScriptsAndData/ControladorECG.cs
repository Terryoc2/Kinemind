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
    [Tooltip("Valor fijo que se muestra al iniciar la simulación")]
    public int bpmValorInicial = 75; 
    
    [Tooltip("No se mostrarán valores por debajo de este límite")]
    public float bpmMinimo = 65f;

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

        // Seteamos el valor fijo inicial
        if (textBPM != null)
        {
            textBPM.text = bpmValorInicial.ToString() + " Bpm";
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
                    graph.DataSource.AddPointToCategory(categoryName, currentTimeX, yValue);
                    CalcularBPM(yValue, tiempoAbsoluto);

                    currentTimeX += updateDelay;
                    tiempoAbsoluto += updateDelay;
                    contadorFrame++;
                }

                if (currentTimeX >= tiempoMaximoPantalla)
                {
                    currentTimeX = 0f;
                    graph.DataSource.ClearCategory(categoryName);
                }

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
        if (valorY > umbralDeteccion && puedeDetectar)
        {
            float intervalo = tiempoActual - tiempoUltimoPico;

            if (intervalo > 0.3f) 
            {
                float bpm = 60f / intervalo;
                
                // FILTRO DE SEGURIDAD: Solo actualiza si es 65 o más
                if (textBPM != null && bpm >= bpmMinimo)
                {
                    textBPM.text = Mathf.RoundToInt(bpm).ToString() + " Bpm";
                }

                tiempoUltimoPico = tiempoActual;
                puedeDetectar = false; 
                StartCoroutine(ResetDeteccion());
            }
        }
    }

    IEnumerator ResetDeteccion()
    {
        yield return new WaitForSeconds(0.2f);
        puedeDetectar = true;
    }
}