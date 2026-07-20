using System;
using System.Globalization;
using ArduinoBluetoothAPI;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public enum ImuCalibrationStep
{
    None,
    Rest,
    MaxComfort
}

public class ImuPokeMotorController : MonoBehaviour
{
    [Header("Bluetooth")]
    public string bluetoothDeviceName = "ESP32_IMU_LINK";
    public string fallbackBluetoothDeviceName = "";

    [Header("Calibracion")]
    public float calibrationSeconds = 5f;
    public float maxComfortCalibrationSeconds = 5f;
    public bool autoConnectOnCalibrate = true;
    public bool useTwoStepCalibration = true;
    public bool restartTwoStepCalibrationWhenComplete = true;
    public bool requireMaxCalibrationForMotor = true;
    [TextArea] public string restCalibrationMessage = "Deja el brazo izquierdo en reposo.";
    [TextArea] public string maxComfortCalibrationMessage = "Mueve el brazo izquierdo hasta tu maximo comodo sin dolor.";
    [TextArea] public string calibrationReadyMessage = "Calibracion lista.";
    public bool isCalibrating;
    public bool isCalibrated;
    public float calibrationSecondsRemaining;
    public Vector3 calibrationOffset;
    public int calibrationSampleCount;

    [Header("Dos bandas")]
    public bool usarDosBandas = true;
    public float staleSecondsUnity = 1.5f;
    public TMP_Text dataTextBanda1;
    public TMP_Text dataTextBanda2;
    public string vibrationCommandBanda1 = "V1";
    public string vibrationCommandBanda2 = "V2";

    [Header("Rangos configurables")]
    public bool useManualRanges = false;
    public bool useDifferentBandRanges = true;
    [Range(0f, 100f)] public float currentRangePercentBanda1 = 100f;
    [Range(0f, 100f)] public float currentRangePercentBanda2 = 100f;
    public Vector3 manualAllowedMovementRangeBanda1 = new Vector3(3000f, 3000f, 3000f);
    public Vector3 manualAllowedMovementRangeBanda2 = new Vector3(3000f, 3000f, 3000f);
    public Vector3 currentAllowedMovementRangeBanda1 = new Vector3(3000f, 3000f, 3000f);
    public Vector3 currentAllowedMovementRangeBanda2 = new Vector3(3000f, 3000f, 3000f);

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
    public bool logReceivedMessages = false;
    public Rect connectButtonRect = new Rect(20f, 20f, 170f, 48f);
    public Rect calibrateButtonRect = new Rect(200f, 20f, 150f, 48f);
    public Rect testVibrationButtonRect = new Rect(360f, 20f, 150f, 48f);
    public Rect resetButtonRect = new Rect(520f, 20f, 150f, 48f);
    public Rect logRect = new Rect(20f, 78f, 720f, 230f);
    public Rect dataOverlayRect = new Rect(20f, 318f, 720f, 270f);

    [Header("Datos actuales")]
    public Vector3 rawAcceleration;
    public Vector3 calibratedAcceleration;
    public Vector3 rawAccelerationBanda1;
    public Vector3 calibratedAccelerationBanda1;
    public Vector3 calibrationOffsetBanda1;
    public int calibrationSampleCountBanda1;
    public bool isCalibratedBanda1;
    public bool isReceivingBanda1;
    public bool isOutsideMovementRangeBanda1;
    public string exceededAxesBanda1 = "";
    public Vector3 rawAccelerationBanda2;
    public Vector3 calibratedAccelerationBanda2;
    public Vector3 calibrationOffsetBanda2;
    public int calibrationSampleCountBanda2;
    public bool isCalibratedBanda2;
    public bool isReceivingBanda2;
    public bool isOutsideMovementRangeBanda2;
    public string exceededAxesBanda2 = "";
    public ImuCalibrationStep activeCalibrationStep = ImuCalibrationStep.None;
    public Vector3 restOffsetBanda1;
    public Vector3 maxComfortOffsetBanda1;
    public Vector3 comfortableRangeBanda1;
    public bool isRestCalibratedBanda1;
    public bool isMaxCalibratedBanda1;
    public Vector3 restOffsetBanda2;
    public Vector3 maxComfortOffsetBanda2;
    public Vector3 comfortableRangeBanda2;
    public bool isRestCalibratedBanda2;
    public bool isMaxCalibratedBanda2;

    private BluetoothHelper bluetoothHelper;
    private SensorTargetReceiver targetReceiver;
    private bool bluetoothPrepared;
    private bool pendingCalibrationAfterConnect;
    private float pendingCalibrationSeconds = 5f;
    private ImuCalibrationStep pendingCalibrationStep = ImuCalibrationStep.Rest;
    private string activeBluetoothDeviceName = "";
    private string logText = "";
    private Vector3 calibrationAccumulator;
    private Vector3 calibrationAccumulatorBanda1;
    private Vector3 calibrationAccumulatorBanda2;
    private float calibrationEndTime;
    private float lastVibrationCommandTime = -999f;
    private float lastVibrationCommandTimeBanda1 = -999f;
    private float lastVibrationCommandTimeBanda2 = -999f;
    private float lastDataTimeBanda1 = -999f;
    private float lastDataTimeBanda2 = -999f;

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
        ActualizarEstadoRecepcion();

        if (isCalibrating)
        {
            calibrationSecondsRemaining = Mathf.Max(0f, calibrationEndTime - Time.time);
            UpdateCalibrationStatusText(false);
            UpdateDataText();

            if (Time.time >= calibrationEndTime)
            {
                FinishCalibration();
            }

            return;
        }

        UpdateDataText();
    }

    public void CalibrarDesdePoke()
    {
        if (useTwoStepCalibration)
        {
            StartNextTwoStepCalibration();
            return;
        }

        ConnectAndCalibrate(calibrationSeconds);
    }

    public void ConnectAndCalibrate(float seconds)
    {
        if (useTwoStepCalibration)
        {
            StartNextTwoStepCalibration(seconds);
            return;
        }

        ConnectAndCalibrateStep(ImuCalibrationStep.Rest, seconds);
    }

    private void StartNextTwoStepCalibration(float secondsOverride = -1f)
    {
        if (IsTwoStepCalibrationComplete() && restartTwoStepCalibrationWhenComplete)
        {
            ResetCalibration();
        }

        ImuCalibrationStep nextStep = IsRestCalibrationComplete()
            ? ImuCalibrationStep.MaxComfort
            : ImuCalibrationStep.Rest;

        float seconds = secondsOverride > 0f
            ? secondsOverride
            : nextStep == ImuCalibrationStep.MaxComfort ? maxComfortCalibrationSeconds : calibrationSeconds;

        ConnectAndCalibrateStep(nextStep, seconds);
    }

    private void ConnectAndCalibrateStep(ImuCalibrationStep step, float seconds)
    {
        pendingCalibrationAfterConnect = true;
        pendingCalibrationSeconds = Mathf.Max(0.1f, seconds);
        pendingCalibrationStep = step == ImuCalibrationStep.None ? ImuCalibrationStep.Rest : step;

        if (IsBluetoothConnected)
        {
            pendingCalibrationAfterConnect = false;
            StartCalibrationStep(pendingCalibrationStep, pendingCalibrationSeconds);
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

        AddLog("Conectar IMU central desde Unity.");

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
        StartCalibrationStep(ImuCalibrationStep.Rest, seconds);
    }

    private void StartCalibrationStep(ImuCalibrationStep step, float seconds)
    {
        activeCalibrationStep = step == ImuCalibrationStep.None ? ImuCalibrationStep.Rest : step;
        calibrationAccumulator = Vector3.zero;
        calibrationAccumulatorBanda1 = Vector3.zero;
        calibrationAccumulatorBanda2 = Vector3.zero;
        calibrationSampleCount = 0;
        calibrationSampleCountBanda1 = 0;
        calibrationSampleCountBanda2 = 0;
        calibrationSecondsRemaining = Mathf.Max(0.1f, seconds);
        calibrationEndTime = Time.time + calibrationSecondsRemaining;
        isCalibrating = true;

        if (!useTwoStepCalibration || activeCalibrationStep == ImuCalibrationStep.Rest)
        {
            isCalibrated = false;
            isCalibratedBanda1 = false;
            isCalibratedBanda2 = false;
            isRestCalibratedBanda1 = false;
            isRestCalibratedBanda2 = false;
            isMaxCalibratedBanda1 = false;
            isMaxCalibratedBanda2 = false;
            restOffsetBanda1 = Vector3.zero;
            restOffsetBanda2 = Vector3.zero;
            maxComfortOffsetBanda1 = Vector3.zero;
            maxComfortOffsetBanda2 = Vector3.zero;
            comfortableRangeBanda1 = Vector3.zero;
            comfortableRangeBanda2 = Vector3.zero;
        }
        else if (activeCalibrationStep == ImuCalibrationStep.MaxComfort)
        {
            isMaxCalibratedBanda1 = false;
            isMaxCalibratedBanda2 = false;
        }

        isOutsideMovementRange = false;
        isOutsideMovementRangeBanda1 = false;
        isOutsideMovementRangeBanda2 = false;
        exceededAxes = "";
        exceededAxesBanda1 = "";
        exceededAxesBanda2 = "";

        UpdateCalibrationStatusText(true);
        UpdateDataText();
    }

    public void ResetCalibration()
    {
        isCalibrating = false;
        isCalibrated = false;
        isCalibratedBanda1 = false;
        isCalibratedBanda2 = false;
        activeCalibrationStep = ImuCalibrationStep.None;
        calibrationOffset = Vector3.zero;
        calibrationOffsetBanda1 = Vector3.zero;
        calibrationOffsetBanda2 = Vector3.zero;
        restOffsetBanda1 = Vector3.zero;
        restOffsetBanda2 = Vector3.zero;
        maxComfortOffsetBanda1 = Vector3.zero;
        maxComfortOffsetBanda2 = Vector3.zero;
        comfortableRangeBanda1 = Vector3.zero;
        comfortableRangeBanda2 = Vector3.zero;
        currentAllowedMovementRangeBanda1 = useDifferentBandRanges ? manualAllowedMovementRangeBanda1 : allowedMovementRange;
        currentAllowedMovementRangeBanda2 = useDifferentBandRanges ? manualAllowedMovementRangeBanda2 : allowedMovementRange;
        isRestCalibratedBanda1 = false;
        isRestCalibratedBanda2 = false;
        isMaxCalibratedBanda1 = false;
        isMaxCalibratedBanda2 = false;
        calibrationAccumulator = Vector3.zero;
        calibrationAccumulatorBanda1 = Vector3.zero;
        calibrationAccumulatorBanda2 = Vector3.zero;
        calibrationSampleCount = 0;
        calibrationSampleCountBanda1 = 0;
        calibrationSampleCountBanda2 = 0;
        calibrationSecondsRemaining = 0f;
        calibratedAcceleration = rawAcceleration;
        calibratedAccelerationBanda1 = rawAccelerationBanda1;
        calibratedAccelerationBanda2 = rawAccelerationBanda2;
        isOutsideMovementRange = false;
        isOutsideMovementRangeBanda1 = false;
        isOutsideMovementRangeBanda2 = false;
        exceededAxes = "";
        exceededAxesBanda1 = "";
        exceededAxesBanda2 = "";

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
            SetStatus("ESP32 central conectado. Recibiendo ACC/ACC1/ACC2.");

            if (pendingCalibrationAfterConnect)
            {
                pendingCalibrationAfterConnect = false;
                StartCalibrationStep(pendingCalibrationStep, pendingCalibrationSeconds);
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
        string received = helper.Read();

        if (string.IsNullOrWhiteSpace(received))
        {
            return;
        }

        string[] lines = received.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string receivedMessage = line.Trim();

            if (receivedMessage.Length == 0)
            {
                continue;
            }

            if (logReceivedMessages)
            {
                AddLog("RX: " + receivedMessage);
            }

            ProcessAccelerometerMessage(receivedMessage);
        }
    }

    void ProcessAccelerometerMessage(string receivedMessage)
    {
        string[] parts = receivedMessage.Split(',');

        if (parts.Length != 4)
        {
            return;
        }

        string tag = parts[0].Trim().ToUpperInvariant();
        int banda;

        if (tag == "ACC" || tag == "ACC1")
        {
            banda = 1;
        }
        else if (tag == "ACC2")
        {
            banda = 2;
        }
        else
        {
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

        ProcesarBanda(banda, new Vector3(x, y, z));
    }

    void ProcesarBanda(int banda, Vector3 rawValue)
    {
        if (banda == 1)
        {
            rawAccelerationBanda1 = rawValue;
            rawAcceleration = rawValue;
            lastDataTimeBanda1 = Time.time;
            isReceivingBanda1 = true;

            if (isCalibrating)
            {
                calibrationAccumulatorBanda1 += rawAccelerationBanda1;
                calibrationAccumulator += rawAccelerationBanda1;
                calibrationSampleCountBanda1++;
                calibrationSampleCount++;
            }

            calibratedAccelerationBanda1 = ObtenerAceleracionCalibrada(1, rawAccelerationBanda1);

            calibratedAcceleration = calibratedAccelerationBanda1;
            calibrationOffset = restOffsetBanda1;

            EnviarBanda1ADestinoUnity();
            CheckRangeAndVibrateBand(1, calibratedAccelerationBanda1, isCalibratedBanda1);
        }
        else if (banda == 2)
        {
            rawAccelerationBanda2 = rawValue;
            lastDataTimeBanda2 = Time.time;
            isReceivingBanda2 = true;

            if (isCalibrating)
            {
                calibrationAccumulatorBanda2 += rawAccelerationBanda2;
                calibrationSampleCountBanda2++;
            }

            calibratedAccelerationBanda2 = ObtenerAceleracionCalibrada(2, rawAccelerationBanda2);

            CheckRangeAndVibrateBand(2, calibratedAccelerationBanda2, isCalibratedBanda2);
        }

        UpdateDataText();
    }

    void EnviarBanda1ADestinoUnity()
    {
        if (targetReceiver == null)
        {
            FindSensorTarget();
        }

        if (targetReceiver != null)
        {
            targetReceiver.ReceiveAccelerometer(calibratedAccelerationBanda1);
        }
    }

    void FinishCalibration()
    {
        isCalibrating = false;
        calibrationSecondsRemaining = 0f;

        bool recibioBanda1 = calibrationSampleCountBanda1 > 0;
        bool recibioBanda2 = calibrationSampleCountBanda2 > 0;

        if (!recibioBanda1 && !recibioBanda2)
        {
            activeCalibrationStep = ImuCalibrationStep.None;
            SetStatus("Calibracion cancelada: no llegaron datos ACC.");
            UpdateDataText();
            return;
        }

        if (!useTwoStepCalibration || activeCalibrationStep == ImuCalibrationStep.Rest)
        {
            GuardarReposo(recibioBanda1, recibioBanda2);
        }
        else if (activeCalibrationStep == ImuCalibrationStep.MaxComfort)
        {
            GuardarMaximoComodo(recibioBanda1, recibioBanda2);
        }

        activeCalibrationStep = ImuCalibrationStep.None;
        isCalibrated = useTwoStepCalibration ? IsTwoStepCalibrationComplete() : (isCalibratedBanda1 || isCalibratedBanda2);
        UpdateCurrentAllowedRanges();
        UpdateDataText();
    }

    private void GuardarReposo(bool recibioBanda1, bool recibioBanda2)
    {
        if (recibioBanda1)
        {
            restOffsetBanda1 = calibrationAccumulatorBanda1 / calibrationSampleCountBanda1;
            calibrationOffsetBanda1 = restOffsetBanda1;
            calibratedAccelerationBanda1 = rawAccelerationBanda1 - restOffsetBanda1;
            calibrationOffset = restOffsetBanda1;
            calibratedAcceleration = calibratedAccelerationBanda1;
            isRestCalibratedBanda1 = true;
            isCalibratedBanda1 = true;
        }

        if (recibioBanda2)
        {
            restOffsetBanda2 = calibrationAccumulatorBanda2 / calibrationSampleCountBanda2;
            calibrationOffsetBanda2 = restOffsetBanda2;
            calibratedAccelerationBanda2 = rawAccelerationBanda2 - restOffsetBanda2;
            isRestCalibratedBanda2 = true;
            isCalibratedBanda2 = true;
        }

        if (!useTwoStepCalibration)
        {
            SetStatus("Calibracion lista. B1 muestras: " + calibrationSampleCountBanda1 +
                " | B2 muestras: " + calibrationSampleCountBanda2);
        }
        else
        {
            string mensaje = "Reposo calibrado. Ahora mueve al maximo comodo y toca Calibrar.";

            if (usarDosBandas && (!recibioBanda1 || !recibioBanda2))
            {
                mensaje += " Falta revisar una banda.";
            }

            SetStatus(mensaje);
        }

        AddLog("Reposo B1 = " + FormatVector(restOffsetBanda1));
        AddLog("Reposo B2 = " + FormatVector(restOffsetBanda2));
    }

    private void GuardarMaximoComodo(bool recibioBanda1, bool recibioBanda2)
    {
        if (recibioBanda1)
        {
            maxComfortOffsetBanda1 = calibrationAccumulatorBanda1 / calibrationSampleCountBanda1;
            comfortableRangeBanda1 = AbsVector(maxComfortOffsetBanda1 - restOffsetBanda1);
            isMaxCalibratedBanda1 = isRestCalibratedBanda1;
        }

        if (recibioBanda2)
        {
            maxComfortOffsetBanda2 = calibrationAccumulatorBanda2 / calibrationSampleCountBanda2;
            comfortableRangeBanda2 = AbsVector(maxComfortOffsetBanda2 - restOffsetBanda2);
            isMaxCalibratedBanda2 = isRestCalibratedBanda2;
        }

        string mensaje = calibrationReadyMessage + " B1 rango: " + FormatVector(comfortableRangeBanda1);

        if (usarDosBandas)
        {
            mensaje += " | B2 rango: " + FormatVector(comfortableRangeBanda2);
        }

        if (usarDosBandas && (!isMaxCalibratedBanda1 || !isMaxCalibratedBanda2))
        {
            mensaje += " | Falta completar una banda.";
        }

        SetStatus(mensaje);
        AddLog("Maximo B1 = " + FormatVector(maxComfortOffsetBanda1));
        AddLog("Maximo B2 = " + FormatVector(maxComfortOffsetBanda2));
    }

    void CheckRangeAndVibrateBand(int banda, Vector3 value, bool bandCalibrated)
    {
        if (!enableMotorFeedback)
        {
            SetBandRangeState(banda, false, "");
            return;
        }

        if (requireCalibrationForMotor && !bandCalibrated)
        {
            SetBandRangeState(banda, false, "");
            return;
        }

        if (requireCalibrationForMotor && !IsBandReadyForMotor(banda, bandCalibrated))
        {
            SetBandRangeState(banda, false, "");
            return;
        }

        Vector3 allowedRange = GetAllowedMovementRangeForBand(banda);
        string axes = BuildExceededAxes(value, allowedRange);
        bool outside = !string.IsNullOrEmpty(axes);
        SetBandRangeState(banda, outside, axes);

        if (!outside)
        {
            return;
        }

        float cooldown = Mathf.Max(0.05f, vibrationCooldownSeconds);

        if (banda == 1)
        {
            if (Time.time - lastVibrationCommandTimeBanda1 < cooldown)
            {
                return;
            }

            SendBluetoothCommand(ResolveBandCommand(1), "Banda 1 fuera de rango " + axes);
            lastVibrationCommandTimeBanda1 = Time.time;
        }
        else
        {
            if (Time.time - lastVibrationCommandTimeBanda2 < cooldown)
            {
                return;
            }

            SendBluetoothCommand(ResolveBandCommand(2), "Banda 2 fuera de rango " + axes);
            lastVibrationCommandTimeBanda2 = Time.time;
        }
    }

    void SetBandRangeState(int banda, bool outside, string axes)
    {
        if (banda == 1)
        {
            isOutsideMovementRangeBanda1 = outside;
            exceededAxesBanda1 = axes;
        }
        else
        {
            isOutsideMovementRangeBanda2 = outside;
            exceededAxesBanda2 = axes;
        }

        isOutsideMovementRange = isOutsideMovementRangeBanda1 || isOutsideMovementRangeBanda2;
        exceededAxes = "";

        if (isOutsideMovementRangeBanda1)
        {
            exceededAxes = "B1:" + exceededAxesBanda1;
        }

        if (isOutsideMovementRangeBanda2)
        {
            if (!string.IsNullOrEmpty(exceededAxes))
            {
                exceededAxes += " ";
            }

            exceededAxes += "B2:" + exceededAxesBanda2;
        }
    }

    string ResolveBandCommand(int banda)
    {
        if (banda == 1 && !string.IsNullOrWhiteSpace(vibrationCommandBanda1))
        {
            return vibrationCommandBanda1;
        }

        if (banda == 2 && !string.IsNullOrWhiteSpace(vibrationCommandBanda2))
        {
            return vibrationCommandBanda2;
        }

        return vibrationCommand;
    }

    string BuildExceededAxes(Vector3 value, Vector3 range)
    {
        string axes = "";

        if (range.x > 0f && Mathf.Abs(value.x) > range.x)
        {
            AppendAxis(ref axes, "X");
        }

        if (range.y > 0f && Mathf.Abs(value.y) > range.y)
        {
            AppendAxis(ref axes, "Y");
        }

        if (range.z > 0f && Mathf.Abs(value.z) > range.z)
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

    public void AplicarConfiguracionNivel(float porcentajeBanda1, float porcentajeBanda2, bool activarVibracion, bool requerirCalibracion)
    {
        currentRangePercentBanda1 = Mathf.Clamp(porcentajeBanda1, 0f, 100f);
        currentRangePercentBanda2 = Mathf.Clamp(porcentajeBanda2, 0f, 100f);
        enableMotorFeedback = activarVibracion;
        requireCalibrationForMotor = requerirCalibracion;
        UpdateCurrentAllowedRanges();
        UpdateDataText();
    }

    private Vector3 ObtenerAceleracionCalibrada(int banda, Vector3 rawValue)
    {
        if (banda == 1 && isRestCalibratedBanda1)
        {
            return rawValue - restOffsetBanda1;
        }

        if (banda == 2 && isRestCalibratedBanda2)
        {
            return rawValue - restOffsetBanda2;
        }

        return rawValue;
    }

    private bool IsBandReadyForMotor(int banda, bool bandCalibrated)
    {
        if (!useTwoStepCalibration)
        {
            return bandCalibrated;
        }

        if (banda == 1)
        {
            return isRestCalibratedBanda1 && (!requireMaxCalibrationForMotor || isMaxCalibratedBanda1);
        }

        return isRestCalibratedBanda2 && (!requireMaxCalibrationForMotor || isMaxCalibratedBanda2);
    }

    private bool IsRestCalibrationComplete()
    {
        return isRestCalibratedBanda1 && (!usarDosBandas || isRestCalibratedBanda2);
    }

    private bool IsTwoStepCalibrationComplete()
    {
        bool banda1Lista = isRestCalibratedBanda1 && (!requireMaxCalibrationForMotor || isMaxCalibratedBanda1);
        bool banda2Lista = !usarDosBandas || (isRestCalibratedBanda2 && (!requireMaxCalibrationForMotor || isMaxCalibratedBanda2));
        return banda1Lista && banda2Lista;
    }

    private Vector3 GetAllowedMovementRangeForBand(int banda)
    {
        UpdateCurrentAllowedRanges();
        return banda == 1 ? currentAllowedMovementRangeBanda1 : currentAllowedMovementRangeBanda2;
    }

    private void UpdateCurrentAllowedRanges()
    {
        currentAllowedMovementRangeBanda1 = BuildAllowedMovementRange(1);
        currentAllowedMovementRangeBanda2 = BuildAllowedMovementRange(2);
    }

    private Vector3 BuildAllowedMovementRange(int banda)
    {
        if (useManualRanges)
        {
            if (!useDifferentBandRanges)
            {
                return allowedMovementRange;
            }

            return banda == 1 ? manualAllowedMovementRangeBanda1 : manualAllowedMovementRangeBanda2;
        }

        if (useTwoStepCalibration)
        {
            bool maxReady = banda == 1 ? isMaxCalibratedBanda1 : isMaxCalibratedBanda2;
            Vector3 baseRange = banda == 1 ? comfortableRangeBanda1 : comfortableRangeBanda2;
            float percent = banda == 1 ? currentRangePercentBanda1 : currentRangePercentBanda2;

            if (maxReady && baseRange != Vector3.zero)
            {
                return baseRange * Mathf.Clamp01(percent / 100f);
            }
        }

        if (!useDifferentBandRanges)
        {
            return allowedMovementRange;
        }

        return banda == 1 ? manualAllowedMovementRangeBanda1 : manualAllowedMovementRangeBanda2;
    }

    private Vector3 AbsVector(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private void UpdateCalibrationStatusText(bool addLog)
    {
        string baseMessage = activeCalibrationStep == ImuCalibrationStep.MaxComfort
            ? maxComfortCalibrationMessage
            : restCalibrationMessage;

        string message = baseMessage + " " + calibrationSecondsRemaining.ToString("F1") + "s";

        if (addLog)
        {
            SetStatus(message);
        }
        else if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private string ObtenerEstadoCalibracion()
    {
        if (isCalibrating)
        {
            return activeCalibrationStep == ImuCalibrationStep.MaxComfort
                ? "CALIBRANDO MAXIMO " + calibrationSecondsRemaining.ToString("F1") + "s"
                : "CALIBRANDO REPOSO " + calibrationSecondsRemaining.ToString("F1") + "s";
        }

        if (useTwoStepCalibration)
        {
            if (IsTwoStepCalibrationComplete())
            {
                return "CALIBRADO REPOSO + MAXIMO";
            }

            if (IsRestCalibrationComplete())
            {
                return "REPOSO LISTO";
            }
        }

        return isCalibrated ? "CALIBRADO" : "SIN CALIBRAR";
    }

    public void TestMotorVibration()
    {
        SendBluetoothCommand(vibrationCommand, "prueba manual ambas bandas");
        lastVibrationCommandTime = Time.time;
    }

    public void TestMotorBanda1()
    {
        SendBluetoothCommand(ResolveBandCommand(1), "prueba manual Banda 1");
    }

    public void TestMotorBanda2()
    {
        SendBluetoothCommand(ResolveBandCommand(2), "prueba manual Banda 2");
    }

    void SendBluetoothCommand(string command, string reason)
    {
        if (string.IsNullOrWhiteSpace(command) || !IsBluetoothConnected)
        {
            return;
        }

        try
        {
            string payload = command.EndsWith("\n") ? command : command + "\n";
            bluetoothHelper.SendData(payload);
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

    void ActualizarEstadoRecepcion()
    {
        isReceivingBanda1 = lastDataTimeBanda1 > 0f && Time.time - lastDataTimeBanda1 <= staleSecondsUnity;
        isReceivingBanda2 = lastDataTimeBanda2 > 0f && Time.time - lastDataTimeBanda2 <= staleSecondsUnity;
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
        string resumen = BuildDataSummary();

        if (dataText != null)
        {
            dataText.text = resumen;
        }

        if (dataTextBanda1 != null)
        {
            dataTextBanda1.text = BuildBandSummary(1);
        }

        if (dataTextBanda2 != null)
        {
            dataTextBanda2.text = BuildBandSummary(2);
        }
    }

    string BuildDataSummary()
    {
        string status = ObtenerEstadoCalibracion();
        UpdateCurrentAllowedRanges();
        return status + "\n" + BuildBandSummary(1) + "\n" + BuildBandSummary(2);
    }

    string BuildBandSummary(int banda)
    {
        if (banda == 1)
        {
            return "BANDA 1 " + (isReceivingBanda1 ? "RECIBIENDO" : "SIN DATOS") + "\n" +
                "RAW " + FormatVector(rawAccelerationBanda1) + "\n" +
                "CAL " + FormatVector(calibratedAccelerationBanda1) + "\n" +
                "REPOSO " + (isRestCalibratedBanda1 ? FormatVector(restOffsetBanda1) : "NO") + "\n" +
                "MAX " + (isMaxCalibratedBanda1 ? FormatVector(maxComfortOffsetBanda1) : "NO") + "\n" +
                "LIM " + FormatVector(currentAllowedMovementRangeBanda1) + " (" + currentRangePercentBanda1.ToString("F0") + "%)\n" +
                "RANGO " + (isOutsideMovementRangeBanda1 ? "FUERA " + exceededAxesBanda1 : "OK");
        }

        return "BANDA 2 " + (isReceivingBanda2 ? "RECIBIENDO" : "SIN DATOS") + "\n" +
            "RAW " + FormatVector(rawAccelerationBanda2) + "\n" +
            "CAL " + FormatVector(calibratedAccelerationBanda2) + "\n" +
            "REPOSO " + (isRestCalibratedBanda2 ? FormatVector(restOffsetBanda2) : "NO") + "\n" +
            "MAX " + (isMaxCalibratedBanda2 ? FormatVector(maxComfortOffsetBanda2) : "NO") + "\n" +
            "LIM " + FormatVector(currentAllowedMovementRangeBanda2) + " (" + currentRangePercentBanda2.ToString("F0") + "%)\n" +
            "RANGO " + (isOutsideMovementRangeBanda2 ? "FUERA " + exceededAxesBanda2 : "OK");
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

        GUI.Label(new Rect(20f, 0f, 720f, 22f), "BLUOFICIAL IMU CENTRAL: " + ResolveBluetoothDeviceName());

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
        GUI.TextArea(dataOverlayRect, BuildDataSummary());

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

