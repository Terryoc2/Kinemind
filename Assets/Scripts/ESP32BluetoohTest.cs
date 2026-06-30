using System;
using System.Collections.Generic;
using UnityEngine;
using ArduinoBluetoothAPI;

public class ESP32BluetoothTest : MonoBehaviour
{
    public string deviceName = "ESP32_KineMind";

    private BluetoothHelper bluetoothHelper;
    private string logText = "";

    void Start()
    {
        AddLog("Iniciando prueba Bluetooth...");

        try
        {
            BluetoothHelper.BLE = false; // Bluetooth Classic
            bluetoothHelper = BluetoothHelper.GetInstance(deviceName);

            bluetoothHelper.OnConnected += OnConnected;
            bluetoothHelper.OnConnectionFailed += OnConnectionFailed;
            bluetoothHelper.OnDataReceived += OnDataReceived;
            bluetoothHelper.OnScanEnded += OnScanEnded;

            // Cada mensaje termina con salto de línea
            bluetoothHelper.setTerminatorBasedStream("\n", true);

            AddLog("BluetoothHelper creado para: " + deviceName);
        }
        catch (Exception ex)
        {
            AddLog("ERROR Start: " + ex.Message);
            Debug.LogError(ex);
        }
    }

    void OnConnected(BluetoothHelper helper)
    {
        AddLog("Conectado correctamente.");

        try
        {
            helper.StartListening();
            AddLog("Escuchando datos...");
        }
        catch (Exception ex)
        {
            AddLog("ERROR StartListening: " + ex.Message);
        }
    }

    void OnConnectionFailed(BluetoothHelper helper)
    {
        AddLog("Falló la conexión.");
    }

    void OnDataReceived(BluetoothHelper helper)
    {
        string message = helper.Read();
        AddLog("RX: " + message);
    }

    void OnScanEnded(BluetoothHelper helper, LinkedList<BluetoothDevice> devices)
    {
        AddLog("Escaneo terminado. Dispositivos encontrados: " + devices.Count);
        foreach (BluetoothDevice device in devices)
        {
            AddLog(device.DeviceName + " / " + device.DeviceAddress);
        }
    }

    void Connect()
    {
        if (bluetoothHelper == null)
        {
            AddLog("BluetoothHelper es null.");
            return;
        }

        if (bluetoothHelper.isConnected())
        {
            AddLog("Ya está conectado.");
            return;
        }

        try
        {
            AddLog("Intentando conectar con " + deviceName + "...");
            bluetoothHelper.Connect();
        }
        catch (Exception ex)
        {
            AddLog("ERROR Connect: " + ex.Message);
        }
    }

    void SendCommand(string command)
    {
        if (bluetoothHelper == null || !bluetoothHelper.isConnected())
        {
            AddLog("No conectado. No se puede enviar: " + command);
            return;
        }

        try
        {
            bluetoothHelper.SendData(command);
            AddLog("TX: " + command);
        }
        catch (Exception ex)
        {
            AddLog("ERROR SendData: " + ex.Message);
        }
    }

    void AddLog(string message)
    {
        Debug.Log(message);
        logText = message + "\n" + logText;
        if (logText.Length > 3000)
        {
            logText = logText.Substring(0, 3000);
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(20, 20, 800, 30), "Dispositivo: " + deviceName);

        if (GUI.Button(new Rect(20, 60, 180, 50), "Connect")) Connect();
        if (GUI.Button(new Rect(220, 60, 180, 50), "LED ON")) SendCommand("O");
        if (GUI.Button(new Rect(420, 60, 180, 50), "LED OFF")) SendCommand("F");

        if (GUI.Button(new Rect(620, 60, 180, 50), "Disconnect"))
        {
            if (bluetoothHelper != null)
            {
                bluetoothHelper.Disconnect();
                AddLog("Desconectado.");
            }
        }

        GUI.TextArea(new Rect(20, 130, Screen.width - 40, Screen.height - 160), logText);
    }

    void OnDestroy()
    {
        if (bluetoothHelper != null)
        {
            bluetoothHelper.Disconnect();
        }
    }
}
