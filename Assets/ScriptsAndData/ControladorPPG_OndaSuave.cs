using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using ChartAndGraph;

public class ControladorPPG_OndaSuave : MonoBehaviour
{
    [Header("Referencias")]
    public GraphChart graficaPlugin; 
    public TextAsset archivoDatosCSV;

    [Header("Configuración de la Onda")]
    public string CategoryName = "PPG";
    public float UpdateDelay = 0.01f;
    [Tooltip("Mantén esto en 2 o 3 para que se dibuje suave")]
    public int PuntosPorFrame = 2;
    public float TiempoMaximoPantalla = 5f;

    [Header("Parámetros Fisiológicos (Morfología)")]
    [Tooltip("Velocidad de la onda")]
    public float latidosPorMinuto = 75f;
    [Tooltip("Qué tan alta es la gráfica en la pantalla")]
    public float amplitudBase = 15f; 
    
    [Range(0f, 1f)]
    [Tooltip("El 'rebote' de la válvula. 0 = Sin rebote. 0.4 = Realista.")]
    public float intensidadDicrotica = 0.4f;

    private List<float> valoresOxigeno = new List<float>();
    private int indiceOxigeno = 0;
    private float temporizador = 0f;
    private float tiempoX = 0f;
    private float frecuencia;

    void Start()
    {
        CargarDatosDesdeCSV();
        if (graficaPlugin != null)
        {
            graficaPlugin.DataSource.ClearCategory(CategoryName);
        }
        // Frecuencia para que calce en el ciclo de 2*PI
        frecuencia = (latidosPorMinuto / 60f) * Mathf.PI * 2f; 
    }

    void CargarDatosDesdeCSV()
    {
        if (archivoDatosCSV == null) return;
        string[] lineas = archivoDatosCSV.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string linea in lineas)
        {
            if (float.TryParse(linea.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
            {
                if (val > 0.01f) valoresOxigeno.Add(val);
            }
        }
    }

    void Update()
    {
        if (valoresOxigeno.Count == 0 || graficaPlugin == null) return;
        temporizador += Time.deltaTime;

        if (temporizador >= UpdateDelay)
        {
            temporizador = 0f;

            for (int i = 0; i < PuntosPorFrame; i++)
            {
                float saturacionActual = valoresOxigeno[indiceOxigeno];

                // --- LA MAGIA MÉDICA: MODELO DE DOBLE CAMPANA ---
                
                // 1. Fase del latido (va de 0 a 2*PI, o sea un ciclo completo)
                float fase = (tiempoX * frecuencia) % (Mathf.PI * 2f);
                
                // 2. Pico Principal (Sístole - Contracción del corazón)
                float sistole = Mathf.Exp(-Mathf.Pow((fase - 1.5f) / 0.6f, 2f));
                
                // 3. Pico Secundario (Diástole - Cierre de válvula aórtica)
                float diastole = intensidadDicrotica * Mathf.Exp(-Mathf.Pow((fase - 3.2f) / 0.8f, 2f));

                // 4. Sumamos ambas campanas para crear la onda PPG perfecta
                float valorY_Onda = sistole + diastole;

                // 5. Aplicamos la amplitud y los datos de tu CSV de oxígeno
                float valorY_Final = valorY_Onda * amplitudBase * (saturacionActual / 100f);

                graficaPlugin.DataSource.AddPointToCategory(CategoryName, tiempoX, valorY_Final);
                tiempoX += UpdateDelay;

                float inicioX = tiempoX - TiempoMaximoPantalla;
                if (inicioX > 0)
                {
                    graficaPlugin.HorizontalScrolling = inicioX; 
                }
            }

            indiceOxigeno++;
            if (indiceOxigeno >= valoresOxigeno.Count) indiceOxigeno = 0; 
        }
    }
}