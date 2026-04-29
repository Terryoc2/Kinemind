using UnityEngine;

public class FlujoSangreLlenadoVR : MonoBehaviour
{
    [Header("Referencias Físicas")]
    public Transform palancaMovil;
    
    public float valorLocalCerrado = 1.6921f;
    public float valorLocalAbierto = 1.6075f;
    
    public bool usarEjeY = true; 

    [Header("Referencias Visuales")]
    public Renderer tuboRenderer;
    public float velocidadFlujo = 0.5f;

    [Header("Ajustes de Llenado")]
    [Tooltip("Controla el tamaño de la parte invisible. (1.0 = totalmente invisible)")]
    public float escalaInvisible = 1.0f; 

    [Tooltip("Activa esto si la sangre se llena hacia atrás (de la mano a la bolsa)")]
    public bool invertirLlenado = false;

    // Guardamos el material para no buscarlo en cada Update
    private Material tuboMaterial;

    void Start()
    {
        if (tuboRenderer != null)
        {
            // Usamos .material para asegurar que modificamos la instancia única de este objeto
            tuboMaterial = tuboRenderer.material;
            
            // Ponemos el tubo totalmente vacío al inicio
            ActualizarLlenado(0f);
        }
    }

    void Update()
    {
        if (palancaMovil == null || tuboMaterial == null) return;

        // Misma lectura de tu script original
        float posicionActual = usarEjeY ? palancaMovil.localPosition.y : palancaMovil.localPosition.z;
        float porcentajeApertura = Mathf.InverseLerp(valorLocalCerrado, valorLocalAbierto, posicionActual);

        // Si la palanca está abierta, avanzamos el llenado
        if (porcentajeApertura > 0.05f) 
        {
            // Calculamos cuánto avanzar en base a la velocidad y el tiempo
            float avanceLlenado = (velocidadFlujo * porcentajeApertura) * Time.deltaTime;

            // Actualizamos el efecto de llenado
            ActualizarLlenado(avanceLlenado);
        }
    }

    private void ActualizarLlenado(float avance)
    {
        // Vector4 internamente en Unity es: X=TilingX, Y=TilingY, Z=OffsetX, W=OffsetY
        // Según tu video, el mapeo sigue el eje Y (vertical)
        Vector4 uvSettings = tuboMaterial.GetVector("_BaseMap_ST");
        
        // La lógica para el efecto de llenado es:
        // Offset Y (W) aumenta para desplazar la parte invisible hacia afuera
        // Tiling Y (Y) disminuye proporcionalmente para "encoger" la parte invisible, revelando la sangre roja detrás

        if (!invertirLlenado)
        {
            uvSettings.w += avance;
            uvSettings.y = escalaInvisible - uvSettings.w;
        }
        else
        {
            // Lógica inversa
            uvSettings.w -= avance;
            uvSettings.y = escalaInvisible + uvSettings.w;
        }

        // Limitamos para no pasarnos del tamaño total
        uvSettings.y = Mathf.Max(0.0f, uvSettings.y); // Tiling no puede ser negativo
        uvSettings.w = Mathf.Clamp(uvSettings.w, -escalaInvisible, escalaInvisible); // Limitamos el offset

        // Si el tubo está lleno, detenemos el efecto
        if (uvSettings.y == 0.0f) return;

        tuboMaterial.SetVector("_BaseMap_ST", uvSettings);
    }
}