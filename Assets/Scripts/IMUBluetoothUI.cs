using System;
using System.Globalization;
using ArduinoBluetoothAPI;
using TMPro;
using UnityEngine;

public class IMUBluetoothUI : MonoBehaviour
{
    [Header("Bluetooth")]
    public string deviceName = "ESP32_KineMind";

    [Header("TextMeshPro UI")]
    public TextMeshProUGUI txtEstado;
    public TextMeshProUGUI txtAcelerometro;
    public TextMeshProUGUI txtGiroscopio;

    private BluetoothHelper bluetoothHelper;

    void Start()
    {
        if (txtEstado != null)
        {
            txtEstado.text = "Estado: listo. Presiona CONECTAR.";
        }

        try
        {
            BluetoothHelper.BLE = false;

            bluetoothHelper = BluetoothHelper.GetInstance(deviceName);

            bluetoothHelper.OnConnected += OnConnected;
            bluetoothHelper.OnConnectionFailed += OnConnectionFailed;
            bluetoothHelper.OnDataReceived += OnDataReceived;

            // Cada mensaje del Arduino termina con \n
            bluetoothHelper.setTerminatorBasedStream("\n", true);

            Debug.Log("Bluetooth preparado para: " + deviceName);
        }
        catch (Exception ex)
        {
            MostrarEstado("Error Bluetooth: " + ex.Message);
            Debug.LogError(ex);
        }
    }

    public void ConectarESP32()
    {
        if (bluetoothHelper == null)
        {
            MostrarEstado("Error: BluetoothHelper es null.");
            return;
        }

        if (bluetoothHelper.isConnected())
        {
            MostrarEstado("Estado: ya está conectado.");
            return;
        }

        try
        {
            MostrarEstado("Estado: conectando con " + deviceName + "...");
            bluetoothHelper.Connect();
        }
        catch (Exception ex)
        {
            MostrarEstado("Error al conectar: " + ex.Message);
            Debug.LogError(ex);
        }
    }

    void OnConnected(BluetoothHelper helper)
    {
        try
        {
            helper.StartListening();
            MostrarEstado("Estado: conectado. Recibiendo IMU.");
            Debug.Log("ESP32 conectado. Escuchando IMU...");
        }
        catch (Exception ex)
        {
            MostrarEstado("Error StartListening: " + ex.Message);
            Debug.LogError(ex);
        }
    }

    void OnConnectionFailed(BluetoothHelper helper)
    {
        MostrarEstado("Estado: falló la conexión.");
        Debug.LogError("No se pudo conectar al ESP32.");
    }

    void OnDataReceived(BluetoothHelper helper)
    {
        string message = helper.Read().Trim();

        Debug.Log("RX: " + message);

        ProcesarMensajeIMU(message);
    }

    void ProcesarMensajeIMU(string message)
    {
        // Formatos esperados:
        // ACC,x,y,z
        // GYRO,x,y,z

        string[] datos = message.Split(',');

        if (datos.Length != 4)
        {
            return;
        }

        string tipo = datos[0].Trim();

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
            Debug.LogWarning("No se pudo leer: " + message);
            return;
        }

        if (tipo == "ACC" && txtAcelerometro != null)
        {
            txtAcelerometro.text =
                "ACELERÓMETRO PROMEDIO\n\n" +
                "X: " + x.ToString("F2") + "\n" +
                "Y: " + y.ToString("F2") + "\n" +
                "Z: " + z.ToString("F2");
        }
        else if (tipo == "GYRO" && txtGiroscopio != null)
        {
            txtGiroscopio.text =
                "GIROSCOPIO PROMEDIO\n\n" +
                "X: " + x.ToString("F2") + "\n" +
                "Y: " + y.ToString("F2") + "\n" +
                "Z: " + z.ToString("F2");
        }
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