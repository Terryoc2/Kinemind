using UnityEngine;
using LiquidVolumeFX; // Plugin de fluidos

public class ReguladorRodilloFisicoVR : MonoBehaviour
{
    [Header("Referencias del Plugin")]
    public LiquidVolume tuboSangre;

    [Header("Referencias Físicas")]
    [Tooltip("El objeto de la palanca que tiene el Prismatic Joint y el Rigidbody")]
    public Transform palancaMovil;

    [Header("Lógica Analógica (Valores Físicos)")]
    [Tooltip("El valor local (Y o Z) donde la palanca está ARRIBA del todo (Tubo aplastado, 0% flujo)")]
    public float valorLocalCerrado = 0f;

    [Tooltip("El valor local (Y o Z) donde la palanca está ABAJO del todo (Flujo al 100%)")]
    public float valorLocalAbierto = -0.5f; // Ajusta según tus pruebas

    [Tooltip("¿La palanca se mueve en el eje Y? (Si está apagado, usará el eje Z)")]
    public bool usarEjeY = true;

    [Header("Configuración del Flujo")]
    [Tooltip("Velocidad de llenado del tubo cuando la llave está al 100% de apertura")]
    public float velocidadLlenadoMaxima = 0.3f;

    void Update()
    {
        if (palancaMovil == null || tuboSangre == null) return;

        // 1. Obtenemos la posición local de la palanca en el eje correcto
        float posicionActual = usarEjeY ? palancaMovil.localPosition.y : palancaMovil.localPosition.z;

        // 2. Calculamos el porcentaje de apertura (0.0 a 1.0)
        // Usamos InverseLerp para mapear el valor físico (ej: 0.1 a 0.5) a un porcentaje (0.0 a 1.0)
        float porcentajeApertura = Mathf.InverseLerp(valorLocalCerrado, valorLocalAbierto, posicionActual);

        // 3. Pequeña zona muerta de seguridad (si está muy arriba, el flujo es 0)
        if (porcentajeApertura < 0.02f)
        {
            porcentajeApertura = 0f;
        }

        // 4. Llenado dinámico: Si hay apertura, llenamos la vía
        if (porcentajeApertura > 0f)
        {
            if (tuboSangre.level < 1f)
            {
                // Incrementamos el nivel basado en el tiempo, la velocidad máxima y el porcentaje actual
                float flujoActual = velocidadLlenadoMaxima * porcentajeApertura;
                tuboSangre.level += flujoActual * Time.deltaTime;
            }
        }
    }
}