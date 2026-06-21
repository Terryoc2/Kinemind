using System.Collections;
using System.Collections.Generic;
using BeyondLimitsStudios.VRInteractables;
using UnityEngine;

public enum DrawingLevel2PatternType
{
    Linea,
    ZigZag,
    Triangulo,
    Cuadrado,
    Circulo
}

[System.Serializable]
public class DrawingLevel2Pattern
{
    public string nombre;
    public DrawingLevel2PatternType tipo;
    public int puntos;

    public DrawingLevel2Pattern(string nombre, DrawingLevel2PatternType tipo, int puntos)
    {
        this.nombre = nombre;
        this.tipo = tipo;
        this.puntos = puntos;
    }
}

public class DrawingLevel2Manager : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject actividadRoot;
    public Transform tablero;
    public Transform marcador;

    [Header("Trazo")]
    public int puntosNecesarios = 3;
    public bool exigirOrden = true;
    public float radioPunto = 0.12f;
    public float separacionDelTablero = 0.03f;
    public bool ocultarPuntosAlCompletar = false;

    [Header("Dificultad")]
    public bool usarPatronesProgresivos = true;
    public float segundosEntrePatrones = 0.7f;
    public List<DrawingLevel2Pattern> patrones = new List<DrawingLevel2Pattern>
    {
        new DrawingLevel2Pattern("Linea", DrawingLevel2PatternType.Linea, 3),
        new DrawingLevel2Pattern("Zigzag", DrawingLevel2PatternType.ZigZag, 5),
        new DrawingLevel2Pattern("Triangulo", DrawingLevel2PatternType.Triangulo, 4)
    };

    [Header("Colores")]
    public Color colorPendiente = new Color(1f, 0.85f, 0.15f, 0.85f);
    public Color colorActual = new Color(0.1f, 0.65f, 1f, 0.9f);
    public Color colorCompletado = new Color(0.15f, 1f, 0.35f, 0.85f);

    private readonly List<DrawingLevel2Checkpoint> puntos = new List<DrawingLevel2Checkpoint>();
    private PanelPrincipalManager panel;
    private Transform puntosRoot;
    private int puntoActual;
    private bool actividadActiva;
    private Coroutine rutinaCompletar;
    private int patronActual;

    public void AsignarPanel(PanelPrincipalManager panelPrincipal)
    {
        panel = panelPrincipal;

        if (actividadRoot == null)
        {
            actividadRoot = gameObject;
        }

        BuscarReferencias();
    }

    public void OcultarNivel()
    {
        actividadActiva = false;
        LimpiarPuntos();

        if (actividadRoot == null)
        {
            actividadRoot = gameObject;
        }

        actividadRoot.SetActive(false);
    }

    public void IniciarActividad()
    {
        if (actividadRoot == null)
        {
            actividadRoot = gameObject;
        }

        actividadRoot.SetActive(true);
        BuscarReferencias();
        AsegurarPatrones();

        patronActual = 0;
        puntoActual = 0;
        actividadActiva = true;

        CrearPuntosDeTrazo();
        ActualizarColores();
    }

    public void RegistrarToque(DrawingLevel2Checkpoint punto, Collider other)
    {
        if (!actividadActiva || punto == null || !EsMarcador(other))
        {
            return;
        }

        if (exigirOrden && punto.Indice != puntoActual)
        {
            return;
        }

        if (punto.EstaCompletado)
        {
            return;
        }

        punto.MarcarCompletado();

        if (exigirOrden)
        {
            puntoActual++;
        }
        else
        {
            puntoActual = ContarPuntosCompletados();
        }

        ActualizarColores();

        if (puntoActual >= puntos.Count)
        {
            CompletarActividad();
        }
    }

    private void BuscarReferencias()
    {
        if (actividadRoot == null)
        {
            actividadRoot = gameObject;
        }

        if (tablero == null)
        {
            DrawingBoardTexture board = actividadRoot.GetComponentInChildren<DrawingBoardTexture>(true);

            if (board != null)
            {
                tablero = board.transform;
            }
        }

        if (tablero == null)
        {
            tablero = BuscarHijoPorNombre("Tablero");
        }

        if (marcador == null)
        {
            Marker marker = actividadRoot.GetComponentInChildren<Marker>(true);

            if (marker != null)
            {
                marcador = marker.transform;
            }
        }

        if (marcador == null)
        {
            marcador = BuscarHijoPorNombre("Marker");
        }
    }

    private Transform BuscarHijoPorNombre(string nombre)
    {
        Transform[] hijos = actividadRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform hijo in hijos)
        {
            if (hijo.name.Contains(nombre))
            {
                return hijo;
            }
        }

        return null;
    }

    private void CrearPuntosDeTrazo()
    {
        LimpiarPuntos();

        if (tablero == null)
        {
            Debug.LogWarning("Nivel 2 dibujo: falta asignar el Tablero.");
            return;
        }

        puntosRoot = new GameObject("PuntosTrazoNivel2").transform;
        puntosRoot.SetParent(actividadRoot.transform, true);

        Bounds bounds = ObtenerBoundsTablero();
        int ejeNormal = ObtenerEjeMasPequeno(bounds.size);
        int ejeHorizontal = ObtenerEjeMasGrande(bounds.size, ejeNormal);
        int ejeVertical = ObtenerEjeRestante(ejeNormal, ejeHorizontal);

        float ladoUsuario = ObtenerLadoUsuario(bounds, ejeNormal);
        float offsetNormal = ObtenerOffsetLocal(ejeNormal);
        List<Vector2> coordenadas = CrearCoordenadasPatron(ObtenerPatronActual());

        for (int i = 0; i < coordenadas.Count; i++)
        {
            Vector2 coordenada = coordenadas[i];
            Vector3 posicionLocal = bounds.center;

            posicionLocal[ejeHorizontal] = bounds.center[ejeHorizontal] + bounds.extents[ejeHorizontal] * coordenada.x;
            posicionLocal[ejeVertical] = bounds.center[ejeVertical] + bounds.extents[ejeVertical] * coordenada.y;
            posicionLocal[ejeNormal] = bounds.center[ejeNormal] + ladoUsuario * (bounds.extents[ejeNormal] + offsetNormal);

            CrearPunto(i, tablero.TransformPoint(posicionLocal));
        }
    }

    private DrawingLevel2Pattern ObtenerPatronActual()
    {
        AsegurarPatrones();

        if (!usarPatronesProgresivos)
        {
            return new DrawingLevel2Pattern("Linea", DrawingLevel2PatternType.Linea, puntosNecesarios);
        }

        patronActual = Mathf.Clamp(patronActual, 0, patrones.Count - 1);
        return patrones[patronActual];
    }

    private void AsegurarPatrones()
    {
        if (patrones != null && patrones.Count > 0)
        {
            return;
        }

        patrones = new List<DrawingLevel2Pattern>
        {
            new DrawingLevel2Pattern("Linea", DrawingLevel2PatternType.Linea, 3),
            new DrawingLevel2Pattern("Zigzag", DrawingLevel2PatternType.ZigZag, 5),
            new DrawingLevel2Pattern("Triangulo", DrawingLevel2PatternType.Triangulo, 4)
        };
    }

    private List<Vector2> CrearCoordenadasPatron(DrawingLevel2Pattern patron)
    {
        switch (patron.tipo)
        {
            case DrawingLevel2PatternType.ZigZag:
                return CrearZigZag(Mathf.Max(5, patron.puntos));
            case DrawingLevel2PatternType.Triangulo:
                return CrearTriangulo();
            case DrawingLevel2PatternType.Cuadrado:
                return CrearCuadrado();
            case DrawingLevel2PatternType.Circulo:
                return CrearCirculo(Mathf.Max(8, patron.puntos));
            default:
                return CrearLinea(Mathf.Max(2, patron.puntos));
        }
    }

    private List<Vector2> CrearLinea(int cantidad)
    {
        List<Vector2> resultado = new List<Vector2>();

        for (int i = 0; i < cantidad; i++)
        {
            float t = cantidad == 1 ? 0.5f : i / (float)(cantidad - 1);
            resultado.Add(new Vector2(Mathf.Lerp(-0.65f, 0.65f, t), Mathf.Lerp(0.45f, -0.45f, t)));
        }

        return resultado;
    }

    private List<Vector2> CrearZigZag(int cantidad)
    {
        List<Vector2> resultado = new List<Vector2>();

        for (int i = 0; i < cantidad; i++)
        {
            float t = cantidad == 1 ? 0.5f : i / (float)(cantidad - 1);
            float x = Mathf.Lerp(-0.65f, 0.65f, t);
            float y = i % 2 == 0 ? 0.42f : -0.42f;
            resultado.Add(new Vector2(x, y));
        }

        return resultado;
    }

    private List<Vector2> CrearTriangulo()
    {
        return new List<Vector2>
        {
            new Vector2(0f, 0.55f),
            new Vector2(-0.6f, -0.45f),
            new Vector2(0.6f, -0.45f),
            new Vector2(0f, 0.55f)
        };
    }

    private List<Vector2> CrearCuadrado()
    {
        return new List<Vector2>
        {
            new Vector2(-0.55f, 0.45f),
            new Vector2(0.55f, 0.45f),
            new Vector2(0.55f, -0.45f),
            new Vector2(-0.55f, -0.45f),
            new Vector2(-0.55f, 0.45f)
        };
    }

    private List<Vector2> CrearCirculo(int cantidad)
    {
        List<Vector2> resultado = new List<Vector2>();

        for (int i = 0; i <= cantidad; i++)
        {
            float angulo = i / (float)cantidad * Mathf.PI * 2f;
            resultado.Add(new Vector2(Mathf.Cos(angulo) * 0.5f, Mathf.Sin(angulo) * 0.5f));
        }

        return resultado;
    }

    private Bounds ObtenerBoundsTablero()
    {
        MeshCollider meshCollider = tablero.GetComponent<MeshCollider>();

        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            return meshCollider.sharedMesh.bounds;
        }

        Renderer renderer = tablero.GetComponentInChildren<Renderer>(true);

        if (renderer != null)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 min = tablero.InverseTransformPoint(worldBounds.min);
            Vector3 max = tablero.InverseTransformPoint(worldBounds.max);
            Bounds localBounds = new Bounds();
            localBounds.SetMinMax(Vector3.Min(min, max), Vector3.Max(min, max));
            return localBounds;
        }

        return new Bounds(Vector3.zero, new Vector3(1f, 0.7f, 0.05f));
    }

    private void CrearPunto(int indice, Vector3 posicion)
    {
        GameObject punto = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        punto.name = "PuntoTrazo_" + (indice + 1);
        punto.transform.position = posicion;
        punto.transform.localScale = Vector3.one * radioPunto;
        punto.transform.SetParent(puntosRoot, true);

        Collider collider = punto.GetComponent<Collider>();
        collider.isTrigger = true;

        Renderer renderer = punto.GetComponent<Renderer>();
        renderer.material = CrearMaterial(colorPendiente);

        DrawingLevel2Checkpoint checkpoint = punto.AddComponent<DrawingLevel2Checkpoint>();
        checkpoint.Configurar(this, indice, renderer);
        puntos.Add(checkpoint);
    }

    private Material CrearMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        AplicarColor(material, color);
        return material;
    }

    private void AplicarColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void ActualizarColores()
    {
        for (int i = 0; i < puntos.Count; i++)
        {
            if (puntos[i].EstaCompletado)
            {
                puntos[i].CambiarColor(colorCompletado);
            }
            else if (i == puntoActual)
            {
                puntos[i].CambiarColor(colorActual);
            }
            else
            {
                puntos[i].CambiarColor(colorPendiente);
            }
        }
    }

    private bool EsMarcador(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        Marker marker = other.GetComponentInParent<Marker>();

        if (marker != null)
        {
            return marcador == null || marker.transform == marcador || marker.transform.IsChildOf(marcador) || marcador.IsChildOf(marker.transform);
        }

        return marcador != null && (other.transform == marcador || other.transform.IsChildOf(marcador));
    }

    private void CompletarActividad()
    {
        actividadActiva = false;

        if (ocultarPuntosAlCompletar)
        {
            LimpiarPuntos();
        }

        if (rutinaCompletar != null)
        {
            StopCoroutine(rutinaCompletar);
        }

        if (DebeAvanzarAlSiguientePatron())
        {
            rutinaCompletar = StartCoroutine(AvanzarAlSiguientePatron());
        }
        else
        {
            rutinaCompletar = StartCoroutine(NotificarCompletado());
        }
    }

    private bool DebeAvanzarAlSiguientePatron()
    {
        return usarPatronesProgresivos && patrones != null && patronActual < patrones.Count - 1;
    }

    private IEnumerator AvanzarAlSiguientePatron()
    {
        yield return new WaitForSeconds(segundosEntrePatrones);

        patronActual++;
        puntoActual = 0;
        actividadActiva = true;

        CrearPuntosDeTrazo();
        ActualizarColores();

        rutinaCompletar = null;
    }

    private IEnumerator NotificarCompletado()
    {
        yield return new WaitForSeconds(0.5f);

        if (panel != null)
        {
            panel.CompletarNivel2();
        }

        rutinaCompletar = null;
    }

    private int ContarPuntosCompletados()
    {
        int completados = 0;

        foreach (DrawingLevel2Checkpoint punto in puntos)
        {
            if (punto.EstaCompletado)
            {
                completados++;
            }
        }

        return completados;
    }

    private void LimpiarPuntos()
    {
        puntos.Clear();

        if (puntosRoot == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(puntosRoot.gameObject);
        }
        else
        {
            DestroyImmediate(puntosRoot.gameObject);
        }

        puntosRoot = null;
    }

    private float ObtenerLadoUsuario(Bounds bounds, int ejeNormal)
    {
        Transform referencia = marcador != null ? marcador : Camera.main != null ? Camera.main.transform : null;

        if (referencia == null)
        {
            return 1f;
        }

        Vector3 posicionLocal = tablero.InverseTransformPoint(referencia.position);
        return posicionLocal[ejeNormal] >= bounds.center[ejeNormal] ? 1f : -1f;
    }

    private float ObtenerOffsetLocal(int ejeNormal)
    {
        Vector3 escala = tablero.lossyScale;
        float escalaEje = Mathf.Abs(escala[ejeNormal]);

        if (escalaEje < 0.001f)
        {
            escalaEje = 1f;
        }

        return separacionDelTablero / escalaEje;
    }

    private int ObtenerEjeMasPequeno(Vector3 size)
    {
        if (size.x <= size.y && size.x <= size.z)
        {
            return 0;
        }

        if (size.y <= size.x && size.y <= size.z)
        {
            return 1;
        }

        return 2;
    }

    private int ObtenerEjeMasGrande(Vector3 size, int ejeIgnorado)
    {
        int mejorEje = -1;
        float mejorValor = float.MinValue;

        for (int i = 0; i < 3; i++)
        {
            if (i == ejeIgnorado)
            {
                continue;
            }

            if (size[i] > mejorValor)
            {
                mejorValor = size[i];
                mejorEje = i;
            }
        }

        return mejorEje;
    }

    private int ObtenerEjeRestante(int ejeA, int ejeB)
    {
        for (int i = 0; i < 3; i++)
        {
            if (i != ejeA && i != ejeB)
            {
                return i;
            }
        }

        return 0;
    }
}
