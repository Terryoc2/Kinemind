using System;
using System.Globalization;
using ArduinoBluetoothAPI;
using TMPro;
using UnityEngine;

public class IMUBluetoothUI2 : MonoBehaviour
{
    [Header("Bluetooth")]
    public string deviceName = "ESP32_KineMind";

    [Header("UI TextMeshPro")]
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtAcelerometro;

    [Header("Dato actual recibido")]
    public Vector3 acelerometroActual;

    // Otros scripts pueden suscribirse a este evento.
    public event Action<Vector3> OnAcelerometroActualizado;

    private BluetoothHelper bluetoothHelper;

    private bool bluetoothInicializado = false;
    private string errorInicializacion = "";

    void Start()
    {
        InicializarBluetooth();
    }

    void InicializarBluetooth()
    {
        try
        {
            MostrarEstado("Inicializando Bluetooth...");

            // Estamos usando ESP32 Bluetooth Classic.
            BluetoothHelper.BLE = false;

            // Para poder actualizar TextMeshPro desde los eventos Bluetooth.
            BluetoothHelper.ASYNC_EVENTS = false;

            bluetoothHelper = BluetoothHelper.GetInstance(deviceName);

            if (bluetoothHelper == null)
            {
                bluetoothInicializado = false;
                errorInicializacion =
                    "BluetoothHelper devolvió null. Revisa el emparejamiento y scripts duplicados.";

                MostrarEstado("ERROR: " + errorInicializacion);
                Debug.LogError(errorInicializacion);
                return;
            }

            bluetoothHelper.OnConnected += OnConnected;
            bluetoothHelper.OnConnectionFailed += OnConnectionFailed;
            bluetoothHelper.OnDataReceived += OnDataReceived;

            // El ESP32 manda:
            // ACC,x,y,z\n
            bluetoothHelper.setTerminatorBasedStream("\n", true);

            bluetoothInicializado = true;

            MostrarEstado("Listo. Presiona CONECTAR.");
            Debug.Log("Bluetooth preparado para: " + deviceName);
        }
        catch (Exception ex)
        {
            bluetoothInicializado = false;

            errorInicializacion =
                ex.GetType().Name + ": " + ex.Message;

            MostrarEstado("ERROR Bluetooth:\n" + errorInicializacion);
            Debug.LogError("ERROR Bluetooth: " + errorInicializacion);
        }
    }

    // ESTA FUNCIÓN DEBES CONECTARLA AL BOTÓN.
    public void ConectarESP32()
    {
        if (!bluetoothInicializado || bluetoothHelper == null)
        {
            MostrarEstado(
                "No se pudo iniciar Bluetooth.\n" +
                errorInicializacion
            );

            Debug.LogError(
                "BluetoothHelper es null.\n" +
                "Motivo: " + errorInicializacion
            );

            return;
        }

        if (bluetoothHelper.isConnected())
        {
            MostrarEstado("Ya está conectado.");
            return;
        }

        try
        {
            MostrarEstado("Conectando con:\n" + deviceName + "...");

            bluetoothHelper.Connect();
        }
        catch (Exception ex)
        {
            MostrarEstado("Error Connect:\n" + ex.Message);
            Debug.LogError(ex);
        }
    }

    void OnConnected(BluetoothHelper helper)
    {
        try
        {
            helper.StartListening();

            MostrarEstado("Conectado.\nRecibiendo acelerómetro.");

            Debug.Log("ESP32 conectado. Escuchando datos ACC...");
        }
        catch (Exception ex)
        {
            MostrarEstado("Error StartListening:\n" + ex.Message);
            Debug.LogError(ex);
        }
    }

    void OnConnectionFailed(BluetoothHelper helper)
    {
        MostrarEstado("Falló la conexión.\nRevisa Bluetooth Windows.");

        Debug.LogError("No se pudo conectar al ESP32.");
    }

    void OnDataReceived(BluetoothHelper helper)
    {
        string mensaje = helper.Read().Trim();

        Debug.Log("RX: " + mensaje);

        ProcesarAcelerometro(mensaje);
    }

    void ProcesarAcelerometro(string mensaje)
    {
        // Solo acepta:
        // ACC,x,y,z
        //
        // Ejemplo:
        // ACC,-145.30,82.40,16270.15

        if (!mensaje.StartsWith("ACC,"))
        {
            return;
        }

        string[] datos = mensaje.Split(',');

        if (datos.Length != 4)
        {
            Debug.LogWarning("Mensaje ACC incompleto: " + mensaje);
            return;
        }

        bool xOk = float.TryParse(
            datos[1].Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float x
        );

        bool yOk = float.TryParse(
            datos[2].Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float y
        );

        bool zOk = float.TryParse(
            datos[3].Trim(),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float z
        );

        if (!xOk || !yOk || !zOk)
        {
            Debug.LogWarning("No se pudo convertir ACC: " + mensaje);
            return;
        }

        // Guarda los tres ejes recibidos.
        acelerometroActual = new Vector3(x, y, z);

        // Muestra los valores en el panel.
        if (txtAcelerometro != null)
        {
            txtAcelerometro.text =
                "ACELERÓMETRO\n\n" +
                "X: " + x.ToString("F2") + "\n" +
                "Y: " + y.ToString("F2") + "\n" +
                "Z: " + z.ToString("F2");
        }

        // Envía los datos a cualquier objeto/script suscrito.
        OnAcelerometroActualizado?.Invoke(acelerometroActual);
    }

    void MostrarEstado(string mensaje)
    {
        Debug.Log(mensaje);

        if (txtEstado != null)
        {
            txtEstado.text = mensaje;
        }
    }

    void OnDestroy()
    {
        if (bluetoothHelper != null)
        {
            bluetoothHelper.Disconnect();
        }
    }
}