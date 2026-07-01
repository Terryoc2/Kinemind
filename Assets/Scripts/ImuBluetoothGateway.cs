using System;
using System.Globalization;
using ArduinoBluetoothAPI;
using TMPro;
using UnityEngine;

public class ImuBluetoothGateway : MonoBehaviour
{
    [Header("Bluetooth")]
    public string bluetoothDeviceName = "ESP32_IMU_LINK";
    public string fallbackBluetoothDeviceName = "";

    [Header("Objeto destino")]
    public string sensorTargetTag = "SensorTarget";
    public string fallbackSensorTargetTag = "prueba";

    [Header("Interfaz UI")]
    public TextMeshProUGUI connectionLabel;
    public TextMeshProUGUI accelerationLabel;

    [Header("Prueba con mouse en Editor")]
    public bool showEditorMouseTestButton = true;
    public Rect editorTestButtonRect = new Rect(20f, 20f, 190f, 48f);
    public Rect editorCalibrate5ButtonRect = new Rect(220f, 20f, 130f, 48f);
    public Rect editorCalibrate10ButtonRect = new Rect(360f, 20f, 140f, 48f);
    public Rect editorResetCalibrationButtonRect = new Rect(510f, 20f, 130f, 48f);
    public Rect editorLogRect = new Rect(20f, 78f, 680f, 290f);

    [Header("Datos actuales")]
    public Vector3 accelerometerData;
    public Vector3 calibratedAccelerometerData;

    [Header("Calibracion IMU")]
    public bool isCalibrated;
    public bool isCalibrating;
    public float calibrationSecondsRemaining;
    public Vector3 calibrationOffset;

    private BluetoothHelper bluetoothHelper;
    private SensorTargetReceiver targetReceiver;

    private bool bluetoothPrepared = false;
    private string bluetoothError = "";
    private string activeBluetoothDeviceName = "";
    private string logText = "";

    private Vector3 calibrationAccumulator;
    private int calibrationSampleCount;
    private float calibrationEndTime;

    void Start()
    {
        activeBluetoothDeviceName = ResolveBluetoothDeviceName();
        FindSensorTarget();
        SetConnectionText("Listo. Pulsa CONECTAR IMU. Dispositivo: " + activeBluetoothDeviceName);
    }

    void Update()
    {
        if (!isCalibrating)
        {
            return;
        }

        calibrationSecondsRemaining = Mathf.Max(0f, calibrationEndTime - Time.time);

        if (Time.time >= calibrationEndTime)
        {
            FinishCalibration();
        }
    }

    public void ConnectImuBluetooth()
    {
        AddLog("CLICK conectar IMU.");

        if (!bluetoothPrepared)
        {
            if (!PrepareBluetooth())
            {
                return;
            }
        }

        if (bluetoothHelper == null)
        {
            SetConnectionText("Error: BluetoothHelper es null.");
            return;
        }

        if (bluetoothHelper.isConnected())
        {
            SetConnectionText("El ESP32 ya esta conectado.");
            return;
        }

        try
        {
            SetConnectionText("Conectando con:\n" + activeBluetoothDeviceName + "...");
            bluetoothHelper.Connect();
        }
        catch (Exception ex)
        {
            SetConnectionText("Error Connect:\n" + ex.Message);
            Debug.LogError(ex);
        }
    }

    public void StartCalibration5Seconds()
    {
        StartCalibration(5f);
    }

    public void StartCalibration10Seconds()
    {
        StartCalibration(10f);
    }

    public void StartCalibration(float seconds)
    {
        calibrationAccumulator = Vector3.zero;
        calibrationSampleCount = 0;
        calibrationSecondsRemaining = Mathf.Max(0.1f, seconds);
        calibrationEndTime = Time.time + calibrationSecondsRemaining;
        isCalibrating = true;

        AddLog("Calibrando IMU por " + calibrationSecondsRemaining.ToString("F1") + " segundos. Mantener quieto.");
    }

    public void ResetCalibration()
    {
        isCalibrating = false;
        isCalibrated = false;
        calibrationOffset = Vector3.zero;
        calibrationAccumulator = Vector3.zero;
        calibrationSampleCount = 0;
        calibrationSecondsRemaining = 0f;
        calibratedAccelerometerData = accelerometerData;

        AddLog("Calibracion reiniciada. Se muestran datos crudos.");
    }

    bool PrepareBluetooth()
    {
        try
        {
            activeBluetoothDeviceName = ResolveBluetoothDeviceName();
            SetConnectionText("Preparando Bluetooth para " + activeBluetoothDeviceName + "...");

            BluetoothHelper.BLE = false;
            BluetoothHelper.ASYNC_EVENTS = false;

            bluetoothHelper = BluetoothHelper.GetInstance(activeBluetoothDeviceName);

            if (bluetoothHelper == null)
            {
                bluetoothError = "GetInstance devolvio null. Revisa que este emparejado: " + activeBluetoothDeviceName + ".";
                SetConnectionText("ERROR:\n" + bluetoothError);
                Debug.LogError(bluetoothError);
                return false;
            }

            bluetoothHelper.OnConnected += OnBluetoothConnected;
            bluetoothHelper.OnConnectionFailed += OnBluetoothConnectionFailed;
            bluetoothHelper.OnDataReceived += OnBluetoothDataReceived;
            bluetoothHelper.setTerminatorBasedStream("\n", true);

            bluetoothPrepared = true;
            AddLog("BluetoothHelper creado para: " + activeBluetoothDeviceName);
            return true;
        }
        catch (Exception ex)
        {
            bluetoothError = ex.GetType().Name + ": " + ex.Message;
            bluetoothPrepared = false;
            SetConnectionText("ERROR Bluetooth:\n" + bluetoothError);
            Debug.LogError("ERROR Bluetooth: " + bluetoothError);
            return false;
        }
    }

    string ResolveBluetoothDeviceName()
    {
        if (!string.IsNullOrWhiteSpace(bluetoothDeviceName))
        {
            return bluetoothDeviceName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fallbackBluetoothDeviceName))
        {
            return fallbackBluetoothDeviceName.Trim();
        }

        return "ESP32_IMU_LINK";
    }

    void OnBluetoothConnected(BluetoothHelper helper)
    {
        try
        {
            helper.StartListening();
            SetConnectionText("Conectado.\nRecibiendo acelerometro.");
            AddLog("ESP32 conectado. Escuchando datos ACC...");
        }
        catch (Exception ex)
        {
            SetConnectionText("Error StartListening:\n" + ex.Message);
            Debug.LogError(ex);
        }
    }

    void OnBluetoothConnectionFailed(BluetoothHelper helper)
    {
        SetConnectionText("Fallo la conexion.\nRevisa Bluetooth de Windows y el nombre: " + activeBluetoothDeviceName);
        Debug.LogError("No se pudo conectar al ESP32: " + activeBluetoothDeviceName);
    }

    void OnBluetoothDataReceived(BluetoothHelper helper)
    {
        string receivedMessage = helper.Read().Trim();
        AddLog("RX: " + receivedMessage);
        ProcessAccelerometerMessage(receivedMessage);
    }

    void ProcessAccelerometerMessage(string receivedMessage)
    {
        if (!receivedMessage.StartsWith("ACC,"))
        {
            return;
        }

        string[] parts = receivedMessage.Split(',');

        if (parts.Length != 4)
        {
            Debug.LogWarning("Mensaje ACC incompleto: " + receivedMessage);
            return;
        }

        bool xOk = float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
        bool yOk = float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
        bool zOk = float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z);

        if (!xOk || !yOk || !zOk)
        {
            Debug.LogWarning("No se pudo convertir ACC: " + receivedMessage);
            return;
        }

        accelerometerData = new Vector3(x, y, z);

        if (isCalibrating)
        {
            calibrationAccumulator += accelerometerData;
            calibrationSampleCount++;
        }

        calibratedAccelerometerData = isCalibrated
            ? accelerometerData - calibrationOffset
            : accelerometerData;

        UpdateAccelerationLabel();

        if (targetReceiver == null)
        {
            FindSensorTarget();
        }

        if (targetReceiver != null)
        {
            targetReceiver.ReceiveAccelerometer(calibratedAccelerometerData);
        }
    }

    void FinishCalibration()
    {
        isCalibrating = false;
        calibrationSecondsRemaining = 0f;

        if (calibrationSampleCount <= 0)
        {
            AddLog("Calibracion cancelada: no llegaron muestras ACC.");
            return;
        }

        calibrationOffset = calibrationAccumulator / calibrationSampleCount;
        isCalibrated = true;
        calibratedAccelerometerData = accelerometerData - calibrationOffset;

        AddLog(
            "Calibracion lista. Offset = " +
            FormatVector(calibrationOffset) +
            " | Muestras: " + calibrationSampleCount
        );

        UpdateAccelerationLabel();
    }

    void UpdateAccelerationLabel()
    {
        if (accelerationLabel == null)
        {
            return;
        }

        string status = isCalibrating
            ? "CALIBRANDO " + calibrationSecondsRemaining.ToString("F1") + "s"
            : (isCalibrated ? "CALIBRADO" : "SIN CALIBRAR");

        accelerationLabel.text =
            status + "\n\n" +
            "RAW\n" +
            "X: " + accelerometerData.x.ToString("F2") + "\n" +
            "Y: " + accelerometerData.y.ToString("F2") + "\n" +
            "Z: " + accelerometerData.z.ToString("F2") + "\n\n" +
            "CAL\n" +
            "X: " + calibratedAccelerometerData.x.ToString("F2") + "\n" +
            "Y: " + calibratedAccelerometerData.y.ToString("F2") + "\n" +
            "Z: " + calibratedAccelerometerData.z.ToString("F2");
    }

    void FindSensorTarget()
    {
        targetReceiver = FindReceiverByTag(sensorTargetTag);

        if (targetReceiver == null && !string.IsNullOrWhiteSpace(fallbackSensorTargetTag))
        {
            targetReceiver = FindReceiverByTag(fallbackSensorTargetTag);
        }

        if (targetReceiver == null)
        {
            targetReceiver = FindFirstObjectByType<SensorTargetReceiver>();
        }

        if (targetReceiver != null)
        {
            AddLog("SensorTargetReceiver encontrado: " + targetReceiver.name);
        }
        else
        {
            Debug.LogWarning("No encontre SensorTargetReceiver. Igual puedo mostrar datos RX en pantalla.");
        }
    }

    SensorTargetReceiver FindReceiverByTag(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return null;
        }

        try
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(tagName);

            if (targetObject == null)
            {
                return null;
            }

            return targetObject.GetComponent<SensorTargetReceiver>();
        }
        catch (UnityException)
        {
            return null;
        }
    }

    void SetConnectionText(string message)
    {
        AddLog(message);

        if (connectionLabel != null)
        {
            connectionLabel.text = message;
        }
    }

    void AddLog(string message)
    {
        Debug.Log(message);
        logText = message + "\n" + logText;

        if (logText.Length > 5000)
        {
            logText = logText.Substring(0, 5000);
        }
    }

    string FormatVector(Vector3 value)
    {
        return "X:" + value.x.ToString("F2") +
               " Y:" + value.y.ToString("F2") +
               " Z:" + value.z.ToString("F2");
    }

    void OnGUI()
    {
        if (!showEditorMouseTestButton || !Application.isEditor)
        {
            return;
        }

        GUI.Label(new Rect(20f, 0f, 680f, 22f), "IMU: " + ResolveBluetoothDeviceName());

        if (GUI.Button(editorTestButtonRect, "Conectar IMU"))
        {
            ConnectImuBluetooth();
        }

        if (GUI.Button(editorCalibrate5ButtonRect, "Calibrar 5s"))
        {
            StartCalibration5Seconds();
        }

        if (GUI.Button(editorCalibrate10ButtonRect, "Calibrar 10s"))
        {
            StartCalibration10Seconds();
        }

        if (GUI.Button(editorResetCalibrationButtonRect, "Reset Calib"))
        {
            ResetCalibration();
        }

        GUI.TextArea(editorLogRect, logText);

        if (bluetoothHelper != null)
        {
            bluetoothHelper.DrawGUI();
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