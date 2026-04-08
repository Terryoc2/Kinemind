using UnityEngine;
// Es vital incluir esta línea para que Unity reconozca el plugin
using LiquidVolumeFX; 

public class ControladorFlujoSangre : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí el objeto 3D de la vía que tiene el componente Liquid Volume")]
    public LiquidVolume tuboSangre;

    [Header("Configuración del Flujo")]
    [Tooltip("Velocidad a la que se llena el tubo (0 a 1 por segundo)")]
    public float velocidadLlenado = 0.2f;
    
    [Tooltip("Si está marcado, la vía se vaciará al subir la palanca")]
    public bool vaciarAlCerrar = false;

    // Variable interna para saber el estado de la palanca
    private bool llaveAbierta = false;

    void Start()
    {
        // Nos aseguramos de que el tubo empiece vacío al iniciar la simulación
        if (tuboSangre != null)
        {
            tuboSangre.level = 0f;
        }
        else
        {
            Debug.LogWarning("¡Chalaco, te olvidaste de asignar el Tubo de Sangre en el Inspector!");
        }
    }

    void Update()
    {
        if (tuboSangre == null) return;

        // Si la llave está abierta, llenamos la vía poco a poco
        if (llaveAbierta)
        {
            if (tuboSangre.level < 1f)
            {
                // Incrementamos el nivel basado en el tiempo
                tuboSangre.level += velocidadLlenado * Time.deltaTime;
            }
        }
        // Si la llave está cerrada y elegimos que se vacíe
        else if (!llaveAbierta && vaciarAlCerrar)
        {
            if (tuboSangre.level > 0f)
            {
                tuboSangre.level -= velocidadLlenado * Time.deltaTime;
            }
        }
    }

    // --- MÉTODOS PÚBLICOS PARA LOS EVENTOS VR ---

    public void AbrirLlave()
    {
        llaveAbierta = true;
        Debug.Log("Llave abierta: La sangre está fluyendo.");
    }

    public void CerrarLlave()
    {
        llaveAbierta = false;
        Debug.Log("Llave cerrada: Flujo detenido.");
    }
}
