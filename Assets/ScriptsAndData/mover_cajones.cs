using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlCajon : MonoBehaviour
{
    [Header("Límites de Apertura (Eje Z)")]
    public float zMin = 0.0f;      // Posición cerrada
    public float zMax = 0.534f;    // Posición máxima abierta

    private float posXFija;
    private float posYFija;

    void Start()
    {
        // Bloqueamos las posiciones iniciales de X e Y según tu Inspector
        posXFija = 0f; 
        posYFija = 0f;
    }

    // Usamos LateUpdate para asegurar que nuestra restricción sea la última en aplicarse
    void LateUpdate()
    {
        // Obtenemos la posición actual que el script de movimiento intentó aplicar
        Vector3 posActual = transform.localPosition;

        // 1. Forzamos X e Y a ser siempre 0
        float xFinal = posXFija;
        float yFinal = posYFija;

        // 2. Limitamos Z entre tu mínimo (0) y tu máximo (0.534)
        float zFinal = Mathf.Clamp(posActual.z, zMin, zMax);

        // 3. Aplicamos la posición corregida
        transform.localPosition = new Vector3(xFinal, yFinal, zFinal);
    }
}