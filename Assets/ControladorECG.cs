using UnityEngine;
using ChartAndGraph; // Si esto sale en rojo, ignora por ahora y guarda el archivo
using System.Collections.Generic;

public class ControladorECG : MonoBehaviour
{
    [Header("Asignaciones")]
    public GraphChart grafico; 
    public TextAsset archivoCSV;

    [Header("Configuración")]
    public string nombreCategoria = "ECG"; 
    public float velocidadMuestreo = 0.05f; 

    private List<float> datosY = new List<float>();
    private int indiceActual = 0;
    private float tiempoX = 0f;
    private float cronometro = 0f;

    void Start()
    {
        if (archivoCSV != null)
        {
            // Leemos el CSV
            string[] lineas = archivoCSV.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string linea in lineas)
            {
                if (float.TryParse(linea, out float valor))
                    datosY.Add(valor);
            }
            Debug.Log("Datos cargados: " + datosY.Count);
        }
    }

    void Update()
    {
        if (grafico == null || datosY.Count == 0) return;

        cronometro += Time.deltaTime;
        if (cronometro >= velocidadMuestreo)
        {
            if (indiceActual < datosY.Count)
            {
                // Enviamos el dato al gráfico
                grafico.DataSource.AddPointToCategory(nombreCategoria, tiempoX, datosY[indiceActual]);
                
                tiempoX += 0.1f;
                indiceActual++;
                cronometro = 0f;
            }
            else
            {
                indiceActual = 0; // Reinicia cuando termina el CSV
                tiempoX = 0;
                grafico.DataSource.ClearCategory(nombreCategoria); // Limpia para volver a empezar
            }
        }
    }
}