using UnityEngine;

public class FlujoSangreUvVR : MonoBehaviour
{
    [Header("Referencias Físicas")]
    public Transform palancaMovil;
    
    public float valorLocalCerrado = 1.6921f;
    public float valorLocalAbierto = 1.6075f;
    
    public bool usarEjeY = true; 

    [Header("Referencias Visuales")]
    public Renderer tuboRenderer;
    public float velocidadFlujo = 0.5f;

    [Header("Ajustes de Textura")]
    public float fraccionInvisibleInicial = 0.5f; 

    [Tooltip("Cambia esto si la sangre se pinta de izquierda a derecha en vez de arriba a abajo")]
    public bool moverEnEjeX = true; 
    
    [Tooltip("Activa esto si la sangre se pinta hacia atrás (de la mano a la bolsa)")]
    public bool invertirDireccion = false;

    private float desplazamientoTextura; 

    void Start()
    {
        if (tuboRenderer != null)
        {
            desplazamientoTextura = 0f;
            SetTextureUVs(tuboRenderer.material, fraccionInvisibleInicial, desplazamientoTextura);
        }
    }

    void Update()
    {
        if (palancaMovil == null || tuboRenderer == null) return;

        // Misma lectura de tu script original
        float posicionActual = usarEjeY ? palancaMovil.localPosition.y : palancaMovil.localPosition.z;
        float porcentajeApertura = Mathf.InverseLerp(valorLocalCerrado, valorLocalAbierto, posicionActual);

        if (porcentajeApertura > 0.05f) 
        {
            float avance = (velocidadFlujo * porcentajeApertura) * Time.deltaTime;

            // Dependiendo de la dirección, sumamos o restamos
            if (invertirDireccion)
            {
                desplazamientoTextura -= avance;
            }
            else
            {
                desplazamientoTextura += avance;
            }

            // Mismo límite de tu script original
            float offsetMaximo = 1.0f - fraccionInvisibleInicial;
            
            if (!invertirDireccion && desplazamientoTextura > offsetMaximo)
            {
                desplazamientoTextura = offsetMaximo; 
            }
            else if (invertirDireccion && desplazamientoTextura < -offsetMaximo)
            {
                desplazamientoTextura = -offsetMaximo;
            }

            SetTextureUVs(tuboRenderer.material, fraccionInvisibleInicial, desplazamientoTextura);
        }
    }

    private void SetTextureUVs(Material material, float scale, float offset)
    {
        Vector4 uvSettings = material.GetVector("_BaseMap_ST");
        
        // ¡Aquí está la magia de los interruptores!
        if (moverEnEjeX)
        {
            uvSettings.x = scale; 
            uvSettings.z = offset; 
        }
        else
        {
            uvSettings.y = scale; 
            uvSettings.w = offset; 
        }
        
        material.SetVector("_BaseMap_ST", uvSettings);
    }
}   