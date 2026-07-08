using System;
using System.Globalization;
using ArduinoBluetoothAPI;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class ImuPokeMotorController : MonoBehaviour
{
    [Header("Bluetooth")]
    public string bluetoothDeviceName = "ESP32_IMU_LINK";
    public string fallbackBluetoothDeviceName = "";

    [Header("Calibracion")]
    public float calibrationSeconds = 5f;
    public bool autoConnectOnCalibrate = true;
    public bool isCalibrating;
    public bool isCalibrated;
    public float calibrationSecondsRemaining;
    public Vector3 calibrationOffset;
    public int calibrationSampleCount;

    [Header("Rango y motor")]
    public bool enableMotorFeedback = true;
    public bool requireCalibrationForMotor = true;
    public Vector3 allowedMovementRange = new Vector3(3000f, 3000f, 3000f);
    public string vibrationCommand = "V";
    public float vibrationCooldownSeconds = 3f;
    public bool isOutsideMovementRange;
    public string exceededAxes = "";

    [Header("Destino Unity")]
    public string sensorTargetTag = "SensorTarget";
    public string fallbackSensorTargetTag = "prueba";
    public TMP_Text statusText;
    public TMP_Text dataText;

    [Header("Prueba en Editor")]
    public bool showEditorOverlay = true;
    public Rect connectButtonRect = new Rect(20f, 20f, 170f, 48f);
    public Rect calibrateButtonRect = new Rect(200f, 20f, 150f, 48f);
    public Rect testVibrationButtonRect = new Rect(360f, 20f, 150f, 48f);
    public Rect resetButtonRect = new Rect(520f, 20f, 150f, 48f);
    public Rect logRect = new Rect(20f, 78f, 720f, 290f);

    [Header("Datos actuales")]
    public Vector3 rawAcceleration;
    public Vector3 calibratedAcceleration;

    private BluetoothHelper bluetoothHelper;
    private SensorTargetReceiver targetReceiver;
    private bool bluetoothPrepared;
    private bool pendingCalibrationAfterConnect;
    private float pendingCalibrationSeconds = 5f;
    private string activeBluetoothDeviceName = "";
    private string logText = "";
    private Vector3 calibrationAccumulator;
    private float calibrationEndTime;
    private float lastVibrationCommandTime = -999f;

    public bool IsBluetoothConnected
    {
        get { return bluetoothHelper != null && bluetoothHelper.isConnected(); }
    }

    void Start()
    {
        activeBluetoothDeviceName = ResolveBluetoothDeviceName();
        FindSensorTarget();
        SetStatus("IMU listo. Poke Calibrar para conectar y calibrar.");
        UpdateDataText();
    }

    void Update()
    {
        if (!isCalibrating)
        {
            return;
        }

        calibrationSecondsRemaining = Mathf.Max(0f, calibrationEndTime - Time.time);
        UpdateDataText();

        if (Time.time >= calibrationEndTime)
        {
            FinishCalibration();
        }
    }

    public void CalibrarDesdePoke()
    {
        ConnectAndCalibrate(calibrationSeconds);
    }

    public void ConnectAndCalibrate(float seconds)
    {
        pendingCalibrationAfterConnect = true;
        pendingCalibrationSeconds = Mathf.Max(0.1f, seconds);

        if (IsBluetoothConnected)
        {
            pendingCalibrationAfterConnect = false;
            StartCalibration(pendingCalibrationSeconds);
            return;
        }

        if (!autoConnectOnCalibrate)
        {
            SetStatus("IMU no conectado. Activa autoConnectOnCalibrate o conecta primero.");
            return;
        }

        ConnectBluetooth();
    }

    public void ConnectBluetooth()
    {
        if (!EnsureBluetoothPermissions())
        {
            return;
        }

        AddLog("Conectar IMU desde BLUOFICIAL.");

        if (!bluetoothPrepared && !PrepareBluetooth())
        {
            return;
        }

        if (bluetoothHelper == null)
        {
            SetStatus("Error: BluetoothHelper es null.");
            return;
        }

        if (bluetoothHelper.isConnected())
        {
            SetStatus("ESP32 ya conectado. Recibiendo IMU.");
            return;
        }

        try
        {
            SetStatus("Conectando con " + activeBluetoothDeviceName + "...");
            bluetoothHelper.Connect();
        }
        catch (Exception ex)
        {
            SetStatus("Error Connect: " + ex.Message);
            Debug.LogError(ex);
        }
    }

    public void StartCalibration(float seconds)
    {
        calibrationAccumulator = Vector3.zero;
        calibrationSampleCount = 0;
        calibrationSecondsRemaining = Mathf.Max(0.1f, seconds);
        calibrationEndTime = Time.time + calibrationSecondsRemaining;
        isCalibrating = true;
        isCalibrated = false;
        isOutsideMovementRange = false;
        exceededAxes = "";

        SetStatus("Calibrando IMU " + calibrationSecondsRemaining.ToString("F1") + "s. Mantener quieto.");
        UpdateDataText();
    }

    public void ResetCalibration()
    {
        isCalibrating = false;
        isCalibrated = false;
        calibrationOffset = Vector3.zero;
        calibrationAccumulator = Vector3.zero;
        calibrationSampleCount = 0;
        calibrationSecondsRemaining = 0f;
        calibratedAcceleration = rawAcceleration;
        isOutsideMovementRange = false;
        exceededAxes = "";

        SetStatus("Calibracion reiniciada.");
        UpdateDataText();
    }


    bool EnsureBluetoothPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        bool missingPermission = false;

        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
        {
            Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT");
            missingPermission = true;
        }

        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
        {
            Permission.RequestUserPermission("android.permission.BLUETOOTH_SCAN");
            missingPermission = true;
        }

        if (missingPermission)
        {
            pendingCalibrationAfterConnect = true;
            SetStatus("Acepta permisos Bluetooth y vuelve a tocar Calibrar.");
            AddLog("Permisos Bluetooth solicitados a Android.");
            return false;
        }
#endif
        return true;
    }

    bool PrepareBluetooth()
    {
        try
        {
            activeBluetoothDeviceName = ResolveBluetoothDeviceName();
            BluetoothHelper.BLE = false;
            BluetoothHelper.ASYNC_EVENTS = false;

            bluetoothHelper = BluetoothHelper.GetInstance(activeBluetoothDeviceName);

            if (bluetoothHelper == null)
            {
                SetStatus("No se pudo crear BluetoothHelper para " + activeBluetoothDeviceName + ".");
                return false;
            }

            bluetoothHelper.OnConnected += OnBluetoothConnected;
            bluetoothHelper.OnConnectionFailed += OnBluetoothConnectionFailed;
            bluetoothHelper.OnDataReceived += OnBluetoothDataReceived;
            bluetoothHelper.setTerminatorBasedStream("\n", true);

            bluetoothPrepared = true;
            AddLog("BluetoothHelper creado para " + activeBluetoothDeviceName + ".");
            return true;
        }
        catch (Exception ex)
        {
            bluetoothPrepared = false;
            SetStatus("ERROR Bluetooth: " + ex.Message);
            Debug.LogError(ex);
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
            SetStatus("ESP32 conectado. Recibiendo ACC.");

            if (pendingCalibrationAfterConnect)
            {
                pendingCalibrationAfterConnect = false;
                StartCalibration(pendingCalibrationSeconds);
            }
        }
        catch (Exception ex)
        {
            SetStatus("Error StartListening: " + ex.Message);
            Debug.LogError(ex);
        }
    }

    void OnBluetoothConnectionFailed(BluetoothHelper helper)
    {
        SetStatus("Fallo conexion ESP32. Revisa Bluetooth y nombre: " + activeBluetoothDeviceName);
        pendingCalibrationAfterConnect = false;
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

        rawAcceleration = new Vector3(x, y, z);

        if (isCalibrating)
        {
            calibrationAccumulator += rawAcceleration;
            calibrationSampleCount++;
        }

        calibratedAcceleration = isCalibrated ? rawAcceleration - calibrationOffset : rawAcceleration;

        if (targetReceiver == null)
        {
            FindSensorTarget();
        }

        if (targetReceiver != null)
        {
            targetReceiver.ReceiveAccelerometer(calibratedAcceleration);
        }

        CheckRangeAndVibrate();
        UpdateDataText();
    }

    void FinishCalibration()
    {
        isCalibrating = false;
        calibrationSecondsRemaining = 0f;

        if (calibrationSampleCount <= 0)
        {
            SetStatus("Calibracion cancelada: no llegaron datos ACC.");
            UpdateDataText();
            return;
        }

        calibrationOffset = calibrationAccumulator / calibrationSampleCount;
        isCalibrated = true;
        calibratedAcceleration = rawAcceleration - calibrationOffset;

        SetStatus("IMU calibrado. Ese eje inicial ahora es 0,0,0.");
        AddLog("Offset = " + FormatVector(calibrationOffset) + " | Muestras: " + calibrationSampleCount);
        UpdateDataText();
    }

    void CheckRangeAndVibrate()
    {
        if (!enableMotorFeedback)
        {
            isOutsideMovementRange = false;
            exceededAxes = "";
            return;
        }

        if (requireCalibrationForMotor && !isCalibrated)
        {
            isOutsideMovementRange = false;
            exceededAxes = "";
            return;
        }

        exceededAxes = BuildExceededAxes(calibratedAcceleration);
        isOutsideMovementRange = !string.IsNullOrEmpty(exceededAxes);

        if (!isOutsideMovementRange)
        {
            return;
        }

        float cooldown = Mathf.Max(0.05f, vibrationCooldownSeconds);
        if (Time.time - lastVibrationCommandTime < cooldown)
        {
            return;
        }

        SendBluetoothCommand(vibrationCommand, "fuera de rango " + exceededAxes);
        lastVibrationCommandTime = Time.time;
    }

    string BuildExceededAxes(Vector3 value)
    {
        string axes = "";

        if (allowedMovementRange.x > 0f && Mathf.Abs(value.x) > allowedMovementRange.x)
        {
            AppendAxis(ref axes, "X");
        }

        if (allowedMovementRange.y > 0f && Mathf.Abs(value.y) > allowedMovementRange.y)
        {
            AppendAxis(ref axes, "Y");
        }

        if (allowedMovementRange.z > 0f && Mathf.Abs(value.z) > allowedMovementRange.z)
        {
            AppendAxis(ref axes, "Z");
        }

        return axes;
    }

    void AppendAxis(ref string axes, string axis)
    {
        if (!string.IsNullOrEmpty(axes))
        {
            axes += ",";
        }

        axes += axis;
    }

    public void TestMotorVibration()
    {
        SendBluetoothCommand(vibrationCommand, "prueba manual");
    }

    void SendBluetoothCommand(string command, string reason)
    {
        if (string.IsNullOrWhiteSpace(command) || !IsBluetoothConnected)
        {
            return;
        }

        try
        {
            bluetoothHelper.SendData(command);
            AddLog("TX motor (" + reason + "): " + command);
        }
        catch (Exception ex)
        {
            AddLog("ERROR motor TX: " + ex.Message);
        }
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
            return targetObject != null ? targetObject.GetComponent<SensorTargetReceiver>() : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    void SetStatus(string message)
    {
        AddLog(message);

        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    void UpdateDataText()
    {
        if (dataText == null)
        {
            return;
        }

        string status = isCalibrating
            ? "CALIBRANDO " + calibrationSecondsRemaining.ToString("F1") + "s"
            : (isCalibrated ? "CALIBRADO" : "SIN CALIBRAR");

        dataText.text =
            status + "\n" +
            "RAW " + FormatVector(rawAcceleration) + "\n" +
            "CAL " + FormatVector(calibratedAcceleration) + "\n" +
            "RANGO " + (isOutsideMovementRange ? "FUERA " + exceededAxes : "OK");
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
        if (!showEditorOverlay || !Application.isEditor)
        {
            return;
        }

        GUI.Label(new Rect(20f, 0f, 720f, 22f), "BLUOFICIAL IMU: " + ResolveBluetoothDeviceName());

        if (GUI.Button(connectButtonRect, "Conectar IMU"))
        {
            ConnectBluetooth();
        }

        if (GUI.Button(calibrateButtonRect, "Calibrar Poke"))
        {
            CalibrarDesdePoke();
        }

        if (GUI.Button(testVibrationButtonRect, "Probar Vibra"))
        {
            TestMotorVibration();
        }

        if (GUI.Button(resetButtonRect, "Reset Calib"))
        {
            ResetCalibration();
        }

        GUI.TextArea(logRect, logText);

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

