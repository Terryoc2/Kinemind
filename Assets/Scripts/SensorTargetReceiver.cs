using UnityEngine;

public class SensorTargetReceiver : MonoBehaviour
{
    [Header("Acelerometro calibrado recibido")]
    public float axisX;
    public float axisY;
    public float axisZ;

    public Vector3 receivedAcceleration;

    // Este metodo recibe los valores ya calibrados desde ImuBluetoothGateway.
    public void ReceiveAccelerometer(Vector3 acceleration)
    {
        receivedAcceleration = acceleration;

        axisX = acceleration.x;
        axisY = acceleration.y;
        axisZ = acceleration.z;

        Debug.Log(
            "SensorCube recibe calibrado -> " +
            "X: " + axisX.ToString("F2") +
            " | Y: " + axisY.ToString("F2") +
            " | Z: " + axisZ.ToString("F2")
        );
    }
}