using UnityEngine;

public class ObjetoPruebaAcelerometro : MonoBehaviour
{
    [Header("Datos recibidos desde ESP32")]
    public float ejeX;
    public float ejeY;
    public float ejeZ;

    private IMUBluetoothUI2 bluetoothManager;

    void Start()
    {
        // Verifica que este objeto tenga el tag correcto.
        if (!CompareTag("prueba"))
        {
            Debug.LogWarning(
                "Este objeto no tiene el tag 'prueba'. " +
                "Cámbialo en Inspector > Tag."
            );
        }

        // Busca el script de Bluetooth que ya tienes en Bluetoothmanager.
        bluetoothManager = FindFirstObjectByType<IMUBluetoothUI2>();

        if (bluetoothManager == null)
        {
            Debug.LogError(
                "No encontré IMUBluetoothUI. " +
                "Revisa que Bluetoothmanager tenga ese script activo."
            );
            return;
        }

        // Este objeto empieza a escuchar los datos del acelerómetro.
        bluetoothManager.OnAcelerometroActualizado += RecibirAcelerometro;

        Debug.Log("Objeto prueba conectado al acelerómetro.");
    }

    void RecibirAcelerometro(Vector3 datosACC)
    {
        // Guardamos los datos que vienen desde el ESP32.
        ejeX = datosACC.x;
        ejeY = datosACC.y;
        ejeZ = datosACC.z;

        Debug.Log(
            "OBJETO PRUEBA RECIBE -> " +
            "X: " + ejeX.ToString("F2") +
            " | Y: " + ejeY.ToString("F2") +
            " | Z: " + ejeZ.ToString("F2")
        );

        // Por ahora SOLO recibe y guarda datos.
        // No movemos todavía el objeto para evitar resultados raros.
    }

    void OnDestroy()
    {
        // Deja de escuchar al destruir el objeto.
        if (bluetoothManager != null)
        {
            bluetoothManager.OnAcelerometroActualizado -= RecibirAcelerometro;
        }
    }
}