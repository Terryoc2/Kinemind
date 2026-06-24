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
    Circulo,
    Estrella
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
    public Transform puntaMarcador;

    [Header("Trazo")]
    public int puntosNecesarios = 3;
    public bool exigirOrden = true;
    public float radioPunto = 0.06f;
    public float radioDeteccion = 0.08f;
    public float pausaEntrePuntos = 0.18f;
    public float separacionDelTablero = 0.02f;
    public bool invertirLadoDelTablero = false;
    public bool usarDeteccionPorDistancia = true;
    public bool ocultarPuntosAlCompletar = false;

    [Header("Dificultad")]
    public bool usarPatronesProgresivos = true;
    public float segundosEntrePatrones = 0.25f;
    public List<DrawingLevel2Pattern> patrones = new List<DrawingLevel2Pattern>
    {
        new DrawingLevel2Pattern("Linea", DrawingLevel2PatternType.Linea, 3),
        new DrawingLevel2Pattern("Cuadrado", DrawingLevel2PatternType.Cuadrado, 5),
        new DrawingLevel2Pattern("Circulo", DrawingLevel2PatternType.Circulo, 10),
        new DrawingLevel2Pattern("Estrella", DrawingLevel2PatternType.Estrella, 11)
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
    private DrawingBoardTexture tableroDibujo;
    private float puedeRegistrarDesde;

    private void Update()
    {
        if (!usarDeteccionPorDistancia || !actividadActiva || ObtenerPuntaMarcador() == null || puntos.Count == 0)
        {
            return;
        }

        if (exigirOrden)
        {
            if (puntoActual >= 0 && puntoActual < puntos.Count)
            {
                RegistrarPorDistancia(puntos[puntoActual]);
            }

            return;
        }

        foreach (DrawingLevel2Checkpoint punto in puntos)
        {
            RegistrarPorDistancia(punto);
        }
    }

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
        puedeRegistrarDesde = 0f;

        CrearPuntosDeTrazo();
        ActualizarColores();
    }

    public void RegistrarToque(DrawingLevel2Checkpoint punto, Collider other)
    {
        if (!actividadActiva || punto == null || !EsMarcador(other))
        {
            return;
        }

        RegistrarPunto(punto);
    }

    private void RegistrarPorDistancia(DrawingLevel2Checkpoint punto)
    {
        if (punto == null || punto.EstaCompletado)
        {
            return;
        }

        Transform referencia = ObtenerPuntaMarcador();

        if (referencia == null)
        {
            return;
        }

        float distancia = Vector3.Distance(referencia.position, punto.transform.position);

        if (distancia <= radioDeteccion)
        {
            RegistrarPunto(punto);
        }
    }

    private void RegistrarPunto(DrawingLevel2Checkpoint punto)
    {
        if (!actividadActiva || punto == null || Time.time < puedeRegistrarDesde)
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
        puedeRegistrarDesde = Time.time + pausaEntrePuntos;

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
            tableroDibujo = actividadRoot.GetComponentInChildren<DrawingBoardTexture>(true);

            if (tableroDibujo != null)
            {
                tablero = tableroDibujo.transform;
            }
        }

        if (tableroDibujo == null && tablero != null)
        {
            tableroDibujo = tablero.GetComponent<DrawingBoardTexture>();

            if (tableroDibujo == null)
            {
                tableroDibujo = tablero.GetComponentInChildren<DrawingBoardTexture>(true);
            }

            if (tableroDibujo != null)
            {
                tablero = tableroDibujo.transform;
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
                puntaMarcador = BuscarPuntaDelMarcador(marker.transform);
            }
        }

        if (marcador == null)
        {
            marcador = BuscarHijoPorNombre("Marker");
        }

        if (puntaMarcador == null && marcador != null)
        {
            puntaMarcador = BuscarPuntaDelMarcador(marcador);
        }
    }

    private Transform ObtenerPuntaMarcador()
    {
        if (puntaMarcador != null)
        {
            return puntaMarcador;
        }

        if (marcador != null)
        {
            puntaMarcador = BuscarPuntaDelMarcador(marcador);
            return puntaMarcador != null ? puntaMarcador : marcador;
        }

        return null;
    }

    private Transform BuscarPuntaDelMarcador(Transform raiz)
    {
        if (raiz == null)
        {
            return null;
        }

        Transform[] hijos = raiz.GetComponentsInChildren<Transform>(true);

        foreach (Transform hijo in hijos)
        {
            string nombre = hijo.name.ToLowerInvariant();

            if (nombre.Contains("tip") || nombre.Contains("punta"))
            {
                return hijo;
            }
        }

        return raiz;
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

        if (!ObtenerPlanoTablero(out Vector3 centro, out Vector3 normal, out Vector3 horizontal, out Vector3 vertical, out float mitadAncho, out float mitadAlto))
        {
            Debug.LogWarning("Nivel 2 dibujo: no se pudo calcular la superficie del tablero.");
            return;
        }

        List<Vector2> coordenadas = CrearCoordenadasPatron(ObtenerPatronActual());

        for (int i = 0; i < coordenadas.Count; i++)
        {
            Vector2 coordenada = coordenadas[i];
            Vector3 posicion = centro
                + horizontal * (coordenada.x * mitadAncho)
                + vertical * (coordenada.y * mitadAlto)
                + normal * separacionDelTablero;

            CrearPunto(i, posicion);
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
        if (patrones != null && patrones.Count > 0 && TienePatron(DrawingLevel2PatternType.Estrella))
        {
            return;
        }

        patrones = new List<DrawingLevel2Pattern>
        {
            new DrawingLevel2Pattern("Linea", DrawingLevel2PatternType.Linea, 3),
            new DrawingLevel2Pattern("Cuadrado", DrawingLevel2PatternType.Cuadrado, 5),
            new DrawingLevel2Pattern("Circulo", DrawingLevel2PatternType.Circulo, 10),
            new DrawingLevel2Pattern("Estrella", DrawingLevel2PatternType.Estrella, 11)
        };
    }

    private bool TienePatron(DrawingLevel2PatternType tipo)
    {
        if (patrones == null)
        {
            return false;
        }

        foreach (DrawingLevel2Pattern patron in patrones)
        {
            if (patron != null && patron.tipo == tipo)
            {
                return true;
            }
        }

        return false;
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
            case DrawingLevel2PatternType.Estrella:
                return CrearEstrella();
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
            new Vector2(-0.55f, -0.45f)
        };
    }

    private List<Vector2> CrearCirculo(int cantidad)
    {
        List<Vector2> resultado = new List<Vector2>();

        for (int i = 0; i < cantidad; i++)
        {
            float angulo = i / (float)cantidad * Mathf.PI * 2f;
            resultado.Add(new Vector2(Mathf.Cos(angulo) * 0.5f, Mathf.Sin(angulo) * 0.5f));
        }

        return resultado;
    }

    private List<Vector2> CrearEstrella()
    {
        List<Vector2> resultado = new List<Vector2>();
        const int puntas = 5;
        const float radioExterno = 0.55f;
        const float radioInterno = 0.24f;

        for (int i = 0; i < puntas * 2; i++)
        {
            float radio = i % 2 == 0 ? radioExterno : radioInterno;
            float angulo = Mathf.PI * 0.5f + i * Mathf.PI / puntas;
            resultado.Add(new Vector2(Mathf.Cos(angulo) * radio, Mathf.Sin(angulo) * radio));
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

    private bool ObtenerPlanoTablero(out Vector3 centro, out Vector3 normal, out Vector3 horizontal, out Vector3 vertical, out float mitadAncho, out float mitadAlto)
    {
        centro = tablero.position;
        normal = tablero.forward;
        horizontal = tablero.right;
        vertical = tablero.up;
        mitadAncho = 0.5f;
        mitadAlto = 0.35f;

        MeshCollider meshCollider = tablero.GetComponent<MeshCollider>();

        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            Mesh mesh = meshCollider.sharedMesh;
            Bounds bounds = mesh.bounds;
            Vector3 normalLocal = ObtenerNormalLocal(mesh);
            normal = tablero.TransformDirection(normalLocal).normalized;

            if (invertirLadoDelTablero)
            {
                normal = -normal;
            }

            centro = tablero.TransformPoint(bounds.center);

            if (tableroDibujo != null)
            {
                centro = tableroDibujo.GetPointOnBoard(centro);
            }

            int ejeNormal = ObtenerEjeMasAlineado(normalLocal);
            int ejeHorizontal = ObtenerEjeMasGrande(bounds.size, ejeNormal);
            int ejeVertical = ObtenerEjeRestante(ejeNormal, ejeHorizontal);

            horizontal = ProyectarEjeEnPlano(ObtenerEjeLocal(ejeHorizontal), normal);
            vertical = ProyectarEjeEnPlano(ObtenerEjeLocal(ejeVertical), normal);

            if (horizontal.sqrMagnitude < 0.001f || vertical.sqrMagnitude < 0.001f)
            {
                CrearEjesDesdeNormal(normal, out horizontal, out vertical);
            }
            else
            {
                horizontal.Normalize();
                vertical.Normalize();
            }

            if (Vector3.Dot(vertical, Vector3.up) < 0f)
            {
                vertical = -vertical;
            }

            if (Vector3.Dot(Vector3.Cross(horizontal, vertical), normal) < 0f)
            {
                horizontal = -horizontal;
            }

            mitadAncho = Mathf.Max(0.05f, tablero.TransformVector(ObtenerEjeLocal(ejeHorizontal) * bounds.extents[ejeHorizontal]).magnitude);
            mitadAlto = Mathf.Max(0.05f, tablero.TransformVector(ObtenerEjeLocal(ejeVertical) * bounds.extents[ejeVertical]).magnitude);
            return true;
        }

        Renderer renderer = tablero.GetComponentInChildren<Renderer>(true);

        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            centro = bounds.center;
            normal = invertirLadoDelTablero ? -tablero.forward : tablero.forward;
            horizontal = Vector3.ProjectOnPlane(tablero.right, normal).normalized;
            vertical = Vector3.ProjectOnPlane(tablero.up, normal).normalized;
            mitadAncho = Mathf.Max(0.05f, bounds.extents.x);
            mitadAlto = Mathf.Max(0.05f, bounds.extents.y);
            return true;
        }

        return tablero != null;
    }

    private Vector3 ObtenerNormalLocal(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;

        if (vertices.Length >= 3)
        {
            Vector3 normalLocal = Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]);

            if (normalLocal.sqrMagnitude > 0.001f)
            {
                return normalLocal.normalized;
            }
        }

        return Vector3.forward;
    }

    private int ObtenerEjeMasAlineado(Vector3 direccion)
    {
        Vector3 abs = new Vector3(Mathf.Abs(direccion.x), Mathf.Abs(direccion.y), Mathf.Abs(direccion.z));

        if (abs.x >= abs.y && abs.x >= abs.z)
        {
            return 0;
        }

        if (abs.y >= abs.x && abs.y >= abs.z)
        {
            return 1;
        }

        return 2;
    }

    private Vector3 ObtenerEjeLocal(int eje)
    {
        switch (eje)
        {
            case 0:
                return Vector3.right;
            case 1:
                return Vector3.up;
            default:
                return Vector3.forward;
        }
    }

    private Vector3 ProyectarEjeEnPlano(Vector3 ejeLocal, Vector3 normalPlano)
    {
        return Vector3.ProjectOnPlane(tablero.TransformDirection(ejeLocal), normalPlano);
    }

    private void CrearEjesDesdeNormal(Vector3 normalPlano, out Vector3 horizontal, out Vector3 vertical)
    {
        horizontal = Vector3.Cross(Vector3.up, normalPlano);

        if (horizontal.sqrMagnitude < 0.001f)
        {
            horizontal = Vector3.Cross(Vector3.right, normalPlano);
        }

        horizontal.Normalize();
        vertical = Vector3.Cross(normalPlano, horizontal).normalized;
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

        SphereCollider sphereCollider = collider as SphereCollider;

        if (sphereCollider != null)
        {
            sphereCollider.radius = 0.5f;
        }

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
        yield return new WaitForSeconds(Mathf.Clamp(segundosEntrePatrones, 0f, 0.25f));

        patronActual++;
        puntoActual = 0;
        actividadActiva = true;
        puedeRegistrarDesde = 0f;

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
