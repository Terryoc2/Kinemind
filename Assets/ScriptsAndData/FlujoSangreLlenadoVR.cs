using UnityEngine;

public class FlujoSangreLlenadoVR : MonoBehaviour
{
    [Header("Referencias Físicas")]
    [Tooltip("El objeto de la palanca que mueves con la mano")]
    public Transform palancaMovil;
    
    // Tus valores exactos
    public float valorLocalCerrado = 1.6921f;
    public float valorLocalAbierto = 1.6075f;
    
    [Tooltip("¡Corregido al eje Y!")]
    public bool usarEjeY = true; 

    [Header("Referencias Visuales")]
    [Tooltip("El MeshRenderer de tu Via_2_sangre")]
    public Renderer tuboRenderer;
    
    [Tooltip("Velocidad a la que avanza la sangre por el tubo")]
    public float velocidadFlujo = 0.5f;

    [Header("Ajustes de Textura (Opcionales)")]
    [Tooltip("Ajusta esto si la sangre no arranca exactamente en la punta de la bolsa (0.5 = mitad invisible)")]
    public float fraccionInvisibleInicial = 0.5f; 

    // Variables internas
    private float desplazamientoTextura; 
    
    // ¡LA CORRECCIÓN CRÍTICA! Variable para guardar el material una sola vez
    private Material tuboMaterialGuardado; 

    void Start()
    {
        // Reseteamos el desplazamiento para que el tubo empiece vacío
        if (tuboRenderer != null)
        {
            // CLONAMOS Y GUARDAMOS EL MATERIAL SOLO AQUÍ, UNA ÚNICA VEZ
            tuboMaterialGuardado = tuboRenderer.material;
            
            desplazamientoTextura = 0f;
            SetTextureUVs(tuboMaterialGuardado, fraccionInvisibleInicial, desplazamientoTextura);
        }
    }

    void Update()
    {
        // Revisamos que el material guardado exista
        if (palancaMovil == null || tuboMaterialGuardado == null) return;

        // 1. Calculamos la apertura de la palanca (¡AHORA LEE EL EJE Y!)
        float posicionActual = usarEjeY ? palancaMovil.localPosition.y : palancaMovil.localPosition.z;
        float porcentajeApertura = Mathf.InverseLerp(valorLocalCerrado, valorLocalAbierto, posicionActual);

        // 2. Si la palanca está abierta, movemos la textura
        if (porcentajeApertura > 0.05f) // Zona muerta del 5%
        {
            // Avanzamos el offset para que la sangre baje
            desplazamientoTextura += (velocidadFlujo * porcentajeApertura) * Time.deltaTime;

            // Calculamos el límite para que la sangre se detenga al llegar a la mano
            float offsetMaximo = 1.0f - fraccionInvisibleInicial;
            if (desplazamientoTextura > offsetMaximo)
            {
                desplazamientoTextura = offsetMaximo; 
            }

            // 3. ¡USAMOS EL MATERIAL GUARDADO EN LUGAR DE tuboRenderer.material!
            SetTextureUVs(tuboMaterialGuardado, fraccionInvisibleInicial, desplazamientoTextura);
        }
    }

    private void SetTextureUVs(Material material, float scale, float offset)
    {
        // Modificamos el Tiling (Y) y el Offset (W) internos de Unity
        Vector4 uvSettings = material.GetVector("_BaseMap_ST");
        uvSettings.y = scale; 
        uvSettings.w = offset; 
        material.SetVector("_BaseMap_ST", uvSettings);
    }
}