using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class NivelMemoriaConfig
{
    public string titulo = "Nivel 1";
    public int cantidadFiguras = 2;
    public float segundosMemorizacion = 60f;
    public bool usarPatronInvertidoAnterior = false;
    [Range(0f, 100f)] public float porcentajeRangoBanda1 = 40f;
    [Range(0f, 100f)] public float porcentajeRangoBanda2 = 45f;
    public bool activarVibracion = true;
    public bool permitirJugarSinCalibracion = false;

    public NivelMemoriaConfig()
    {
    }

    public NivelMemoriaConfig(
        string titulo,
        int cantidadFiguras,
        float segundosMemorizacion,
        bool usarPatronInvertidoAnterior = false,
        float porcentajeRangoBanda1 = 40f,
        float porcentajeRangoBanda2 = 45f,
        bool activarVibracion = true,
        bool permitirJugarSinCalibracion = false)
    {
        this.titulo = titulo;
        this.cantidadFiguras = cantidadFiguras;
        this.segundosMemorizacion = segundosMemorizacion;
        this.usarPatronInvertidoAnterior = usarPatronInvertidoAnterior;
        this.porcentajeRangoBanda1 = porcentajeRangoBanda1;
        this.porcentajeRangoBanda2 = porcentajeRangoBanda2;
        this.activarVibracion = activarVibracion;
        this.permitirJugarSinCalibracion = permitirJugarSinCalibracion;
    }
}

public class PanelPrincipalManager : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text textoTitulo;
    public TMP_Text textoIndicacion;

    [Header("Botones")]
    public Button botonCalibrar;
    public Button botonEmpezar;
    public Button botonContinuar;

    [Header("Botones Poke Reemplazo")]
    public GameObject botonPokeCalibrar;
    public GameObject botonPokeEmpezar;
    public GameObject botonPokeContinuar;

    [Header("Texto Boton Poke")]
    public TMP_Text textoBotonPokePrincipal;
    public string textoPokeCalibrar = "Calibrar";
    public string textoPokeEmpezar = "Empezar";
    public string textoPokeContinuar = "Continuar";
    public Vector3 posicionTextoBotonPoke = new Vector3(0f, 0f, -0.38f);
    public float anchoTextoBotonPoke = 1.2f;
    public float altoTextoBotonPoke = 0.35f;
    public float tamanoTextoBotonPoke = 0.28f;
    public Color colorTextoBotonPoke = Color.black;

    [Header("Posiciones Boton Poke")]
    public bool moverPokePrincipalAlContinuar = true;
    public Vector3 posicionLocalPokeEmpezar = new Vector3(318f, -249f, 0f);
    public Vector3 posicionLocalPokeContinuar = new Vector3(0f, -229f, 0f);

    [Header("Panel VR")]
    public Transform panelInteractuableRoot;

    [Header("Tiempo")]
    public float segundosParaContinuar = 15f;

    [Header("IMU BLUOFICIAL")]
    public bool habilitarImuEnBluOficial = true;
    public string escenaImuBluOficial = "BLUOFICIAL";
    public float segundosCalibracionImu = 5f;
    public ImuPokeMotorController imuPokeMotorController;
    public TMP_Text textoEstadoImu;
    public TMP_Text textoDatosImu;
    public TMP_Text textoDatosBanda1;
    public TMP_Text textoDatosBanda2;

    [Header("Nivel Memoria")]
    public MemoryLevel1Manager nivel1Manager;
    public int totalSecuenciasNivel1 = 3;
    public string textoOrdenarNivelMemoria = "Ordena y pon en las cajas";
    public string textoSiguienteSecuenciaNivel1 = "Pulsa continuar para ver la siguiente secuencia.";
    public NivelMemoriaConfig[] nivelesMemoria = new NivelMemoriaConfig[]
    {
        new NivelMemoriaConfig("Nivel 1", 2, 60f, false, 40f, 45f),
        new NivelMemoriaConfig("Nivel 2", 3, 45f, false, 55f, 60f),
        new NivelMemoriaConfig("Nivel 3", 4, 30f, false, 70f, 75f),
        new NivelMemoriaConfig("Nivel 4", 5, 15f, false, 85f, 85f),
        new NivelMemoriaConfig("Nivel 5 - Inverso", 5, 15f, true, 85f, 85f)
    };

    [Header("Transicion Nivel 2")]
    public GameObject escenarioNivel2;
    public Transform jugadorVR;
    public Transform puntoJugadorNivel2;
    public Transform puntoPanelNivel2;
    public bool colocarPanelFrenteAlJugador = true;
    public float distanciaPanelNivel2 = 1.8f;
    public float alturaPanelNivel2 = -0.35f;
    public float segundosEntreCuenta = 1f;
    public string tituloNivel2 = "Nivel 2";
    public string textoBienvenidaNivel2 = "Bienvenido";
    public string textoAlEmpezarNivel2 = "Preparate para la siguiente actividad.";

    [Header("Nivel 2 Dibujo")]
    public DrawingLevel2Manager nivel2DibujoManager;
    public string textoInstruccionNivel2 = "Traza el dibujo siguiendo la referencia.";
    public string textoNivel2Completado = "Trazo completado. Puedes continuar.";

    private Coroutine rutinaContinuar;
    private Coroutine rutinaTransicionNivel2;
    private TMP_Text textoBotonPokeCalibrar;
    private string textoMemorizacionActual = "";
    private bool actividadIniciada = false;
    private bool actividadCompletada = false;
    private bool transicionandoNivel = false;
    private bool botonPokeEmpezarVisible = false;
    private bool botonPokeContinuarVisible = false;
    private bool botonPokeContinuarInteractuable = false;
    private bool accionPokeEnProceso = false;
    private bool panelNivel2Colocado = false;
    private int nivelActual = 1;
    private int secuenciaNivel1Actual = 1;
    private MemoryGem[] patronParaNivelInverso;

    private void OnEnable()
    {
        accionPokeEnProceso = false;
    }

    private void Start()
    {
        if (nivel1Manager == null)
        {
            nivel1Manager = FindObjectOfType<MemoryLevel1Manager>(true);
        }

        if (nivel1Manager != null)
        {
            nivel1Manager.AsignarPanel(this);
            nivel1Manager.OcultarNivel();
        }

        PrepararNivel2Dibujo();
        PrepararImuBluOficial();

        MostrarInicio();
    }

    public void MostrarInicio()
    {
        nivelActual = 1;
        secuenciaNivel1Actual = 1;
        patronParaNivelInverso = null;
        panelNivel2Colocado = false;
        textoTitulo.text = "JKInemind";
        textoIndicacion.text = "Bienvenido";

        MostrarBotonCalibrar(true);
        MostrarBotonEmpezar(true);

        MostrarBotonContinuar(false, false);

        actividadIniciada = false;
        actividadCompletada = false;

        if (nivel2DibujoManager != null)
        {
            nivel2DibujoManager.OcultarNivel();
        }
    }

    public void Empezar()
    {
        Debug.Log("EMPEZAR EJECUTADO");

        if (nivelActual == 2)
        {
            MostrarActividadNivel2();
            return;
        }

        actividadIniciada = false;
        actividadCompletada = false;
        secuenciaNivel1Actual = 1;
        patronParaNivelInverso = null;
        PrepararSecuenciaNivel1();
    }

    public void AccionarBotonPokePrincipal()
    {
        if (accionPokeEnProceso)
        {
            return;
        }

        bool puedeContinuar = botonPokeContinuarVisible
            && botonPokeContinuarInteractuable;

        if (puedeContinuar)
        {
            BloquearAccionPokeTemporalmente();
            Continuar();
            return;
        }

        bool puedeEmpezar = botonPokeEmpezarVisible
            || botonEmpezar == null
            || botonEmpezar.gameObject.activeSelf;

        if (puedeEmpezar)
        {
            BloquearAccionPokeTemporalmente();
            Empezar();
        }
    }

    public void Calibrar()
    {
        Debug.Log("CALIBRAR EJECUTADO");

        if (!DebeUsarImuEnEstaEscena())
        {
            return;
        }

        ImuPokeMotorController controller = ObtenerImuPokeMotorController();

        if (controller == null)
        {
            Debug.LogWarning("No se pudo crear/encontrar ImuPokeMotorController.");
            return;
        }

        controller.calibrationSeconds = segundosCalibracionImu;
        controller.maxComfortCalibrationSeconds = segundosCalibracionImu;
        controller.CalibrarDesdePoke();
    }

    private void PrepararImuBluOficial()
    {
        if (!DebeUsarImuEnEstaEscena())
        {
            return;
        }

        ObtenerImuPokeMotorController();
    }

    private bool DebeUsarImuEnEstaEscena()
    {
        if (!habilitarImuEnBluOficial)
        {
            return false;
        }

        if (botonPokeCalibrar != null
            || imuPokeMotorController != null
            || FindObjectOfType<ImuPokeMotorController>(true) != null)
        {
            return true;
        }

        return string.Equals(SceneManager.GetActiveScene().name, escenaImuBluOficial, StringComparison.OrdinalIgnoreCase);
    }

    private ImuPokeMotorController ObtenerImuPokeMotorController()
    {
        if (imuPokeMotorController == null)
        {
            imuPokeMotorController = FindObjectOfType<ImuPokeMotorController>(true);
        }

        if (imuPokeMotorController == null)
        {
            GameObject imuObject = new GameObject("IMU_PokeMotorController_BLUOFICIAL");
            imuPokeMotorController = imuObject.AddComponent<ImuPokeMotorController>();
        }

        if (imuPokeMotorController.statusText == null)
        {
            imuPokeMotorController.statusText = textoEstadoImu != null ? textoEstadoImu : textoIndicacion;
        }

        if (imuPokeMotorController.dataText == null)
        {
            imuPokeMotorController.dataText = textoDatosImu;
        }

        if (imuPokeMotorController.dataTextBanda1 == null)
        {
            imuPokeMotorController.dataTextBanda1 = textoDatosBanda1;
        }

        if (imuPokeMotorController.dataTextBanda2 == null)
        {
            imuPokeMotorController.dataTextBanda2 = textoDatosBanda2;
        }

        return imuPokeMotorController;
    }

    private IEnumerator MostrarContinuarDespuesDeTiempo()
    {
        int segundosRestantes = Mathf.Max(0, Mathf.CeilToInt(ObtenerSegundosMemorizacionActual()));

        while (segundosRestantes > 0)
        {
            ActualizarTextoMemorizacion(segundosRestantes);
            yield return new WaitForSeconds(1f);
            segundosRestantes--;
        }

        ActualizarTextoMemorizacion(0);
        MostrarBotonContinuar(true, true);
        rutinaContinuar = null;
    }

    public void Continuar()
    {
        if (nivelActual == 2)
        {
            if (actividadCompletada)
            {
                if (EsEscenaAgarrar())
                {
                    textoTitulo.text = "Juego completado";
                    textoIndicacion.text = "Nivel 2 terminado.";
                    MostrarBotonContinuar(false, false);
                }
                else
                {
                    textoTitulo.text = "Nivel 2 completado";
                    textoIndicacion.text = "Actividad completada.";
                    MostrarBotonContinuar(true, false);
                }
            }

            return;
        }

        if (actividadCompletada)
        {
            if (secuenciaNivel1Actual < ObtenerTotalSecuenciasNivel1())
            {
                secuenciaNivel1Actual++;
                PrepararSecuenciaNivel1();
            }
            else
            {
                IniciarTransicionNivel2();
                MostrarBotonContinuar(true, false);
            }

            return;
        }

        if (actividadIniciada)
        {
            return;
        }

        actividadIniciada = true;

        textoTitulo.text = ObtenerTituloSecuenciaNivel1();
        textoIndicacion.text = textoOrdenarNivelMemoria;

        MostrarBotonContinuar(true, false);

        if (nivel1Manager != null)
        {
            nivel1Manager.IniciarActividad();
        }
    }

    public void CompletarActividad()
    {
        actividadCompletada = true;
        actividadIniciada = false;

        if (secuenciaNivel1Actual < ObtenerTotalSecuenciasNivel1())
        {
            textoTitulo.text = ObtenerTituloSecuenciaNivel1() + " completado";
            textoIndicacion.text = textoSiguienteSecuenciaNivel1;
            MostrarBotonContinuar(true, true);
            return;
        }

        textoTitulo.text = "Memoria completada";
        textoIndicacion.text = "Actividad completada.";
        MostrarBotonContinuar(false, false);
        IniciarTransicionNivel2();
    }

    private void PrepararSecuenciaNivel1()
    {
        actividadIniciada = false;
        actividadCompletada = false;

        textoTitulo.text = ObtenerTituloSecuenciaNivel1();
        AplicarConfiguracionImuNivelActual();

        if (nivel1Manager != null)
        {
            int cantidadFiguras = ObtenerCantidadFigurasActual();

            if (DebeUsarPatronInvertidoActual())
            {
                nivel1Manager.PrepararPatronInvertido(patronParaNivelInverso, cantidadFiguras);
            }
            else
            {
                nivel1Manager.PrepararPatron(cantidadFiguras);
                MemoryGem[] patronPreparado = nivel1Manager.ObtenerPatronActual();

                if (patronPreparado.Length > 0)
                {
                    patronParaNivelInverso = patronPreparado;
                }
            }

            textoMemorizacionActual = nivel1Manager.ObtenerTextoPatron();
        }
        else
        {
            textoMemorizacionActual = "Memoriza el patron y coloca cada figura en su caja.";
        }

        ActualizarTextoMemorizacion(Mathf.Max(0, Mathf.CeilToInt(ObtenerSegundosMemorizacionActual())));

        MostrarBotonCalibrar(false);
        MostrarBotonEmpezar(false);
        MostrarBotonContinuar(false, false);

        if (rutinaContinuar != null)
        {
            StopCoroutine(rutinaContinuar);
        }

        rutinaContinuar = StartCoroutine(MostrarContinuarDespuesDeTiempo());
    }

    private bool EsEscenaAgarrar()
    {
        return string.Equals(SceneManager.GetActiveScene().name, "AGARRAR", StringComparison.OrdinalIgnoreCase);
    }

    private void ActualizarTextoMemorizacion(int segundosRestantes)
    {
        if (textoIndicacion == null)
        {
            return;
        }

        string textoBase = string.IsNullOrWhiteSpace(textoMemorizacionActual)
            ? "Memoriza el patron y coloca cada figura en su caja."
            : textoMemorizacionActual;

        if (segundosRestantes > 0)
        {
            textoIndicacion.text = textoBase + "\n\nTiempo para memorizar: " + segundosRestantes + " s";
        }
        else
        {
            textoIndicacion.text = textoBase + "\n\nTiempo terminado. Pulsa continuar para ordenar.";
        }
    }

    private int ObtenerTotalSecuenciasNivel1()
    {
        if (nivelesMemoria != null && nivelesMemoria.Length > 0)
        {
            return nivelesMemoria.Length;
        }

        return Mathf.Max(1, totalSecuenciasNivel1);
    }

    private NivelMemoriaConfig ObtenerConfigMemoriaActual()
    {
        if (nivelesMemoria == null || nivelesMemoria.Length == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(secuenciaNivel1Actual - 1, 0, nivelesMemoria.Length - 1);
        return nivelesMemoria[index];
    }

    private int ObtenerCantidadFigurasActual()
    {
        NivelMemoriaConfig config = ObtenerConfigMemoriaActual();

        if (config != null)
        {
            return Mathf.Max(1, config.cantidadFiguras);
        }

        if (nivel1Manager != null)
        {
            return Mathf.Max(1, nivel1Manager.patternLength);
        }

        return 3;
    }

    private float ObtenerSegundosMemorizacionActual()
    {
        NivelMemoriaConfig config = ObtenerConfigMemoriaActual();

        if (config != null)
        {
            return Mathf.Max(0f, config.segundosMemorizacion);
        }

        return Mathf.Max(0f, segundosParaContinuar);
    }

    private bool DebeUsarPatronInvertidoActual()
    {
        NivelMemoriaConfig config = ObtenerConfigMemoriaActual();
        return config != null && config.usarPatronInvertidoAnterior;
    }

    private void AplicarConfiguracionImuNivelActual()
    {
        NivelMemoriaConfig config = ObtenerConfigMemoriaActual();

        if (config == null || !DebeUsarImuEnEstaEscena())
        {
            return;
        }

        ImuPokeMotorController controller = ObtenerImuPokeMotorController();

        if (controller == null)
        {
            return;
        }

        controller.AplicarConfiguracionNivel(
            config.porcentajeRangoBanda1,
            config.porcentajeRangoBanda2,
            config.activarVibracion,
            !config.permitirJugarSinCalibracion);
    }

    private string ObtenerTituloSecuenciaNivel1()
    {
        NivelMemoriaConfig config = ObtenerConfigMemoriaActual();

        if (config != null && !string.IsNullOrWhiteSpace(config.titulo))
        {
            return config.titulo;
        }

        return "Nivel " + secuenciaNivel1Actual;
    }

    private void IniciarTransicionNivel2()
    {
        if (transicionandoNivel)
        {
            return;
        }

        if (rutinaContinuar != null)
        {
            StopCoroutine(rutinaContinuar);
            rutinaContinuar = null;
        }

        if (rutinaTransicionNivel2 != null)
        {
            StopCoroutine(rutinaTransicionNivel2);
        }

        rutinaTransicionNivel2 = StartCoroutine(TransicionarAlNivel2());
    }

    private IEnumerator TransicionarAlNivel2()
    {
        transicionandoNivel = true;

        if (nivel1Manager != null)
        {
            nivel1Manager.OcultarNivel();
        }

        MostrarBotonCalibrar(false);
        MostrarBotonEmpezar(false);
        MostrarBotonContinuar(false, false);

        textoTitulo.text = "Memoria completada";
        textoIndicacion.text = "Se te transportara al siguiente nivel.";

        yield return new WaitForSeconds(segundosEntreCuenta);

        for (int i = 3; i >= 1; i--)
        {
            textoTitulo.text = "Siguiente nivel";
            textoIndicacion.text = "Se te transportara al siguiente nivel.\n" + i;
            yield return new WaitForSeconds(segundosEntreCuenta);
        }

        panelNivel2Colocado = false;
        TeletransportarAlNivel2();
        yield return null;
        ColocarPanelNivel2();
        MostrarInicioNivel2();
        transicionandoNivel = false;
        rutinaTransicionNivel2 = null;
    }

    private void TeletransportarAlNivel2()
    {
        if (escenarioNivel2 == null)
        {
            GameObject escenarioEncontrado = GameObject.Find("Escenario");

            if (escenarioEncontrado != null)
            {
                escenarioNivel2 = escenarioEncontrado;
            }
        }

        if (escenarioNivel2 != null)
        {
            escenarioNivel2.SetActive(true);
        }

        Transform destinoJugador = puntoJugadorNivel2 != null
            ? puntoJugadorNivel2
            : escenarioNivel2 != null ? escenarioNivel2.transform : null;

        Transform jugador = jugadorVR != null ? jugadorVR : BuscarJugadorVR();

        if (jugador != null && destinoJugador != null)
        {
            MoverJugador(jugador, destinoJugador);
        }
        else
        {
            Debug.LogWarning("Falta asignar Jugador VR o Punto Jugador Nivel 2 para el teletransporte.");
        }

    }

    private Transform BuscarJugadorVR()
    {
        Camera mainCamera = ObtenerCamaraVR();

        if (mainCamera != null)
        {
            return mainCamera.transform.root;
        }

        Camera cameraInScene = FindObjectOfType<Camera>();
        return cameraInScene != null ? cameraInScene.transform.root : null;
    }

    private Camera ObtenerCamaraVR()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            return mainCamera;
        }

        Camera[] cameras = FindObjectsOfType<Camera>(true);

        foreach (Camera camera in cameras)
        {
            if (camera != null && camera.isActiveAndEnabled)
            {
                return camera;
            }
        }

        return cameras.Length > 0 ? cameras[0] : null;
    }

    private void MoverJugador(Transform jugador, Transform destino)
    {
        CharacterController characterController = jugador.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        jugador.SetPositionAndRotation(destino.position, destino.rotation);

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private bool ColocarPanelFrenteAlJugador()
    {
        Camera mainCamera = ObtenerCamaraVR();

        if (mainCamera == null)
        {
            Debug.LogWarning("No se encontro una camara VR para colocar el panel del Nivel 2 frente al jugador.");
            return false;
        }

        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = mainCamera.transform.forward;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.001f)
        {
            return false;
        }

        forward.Normalize();

        float distanciaComoda = Mathf.Approximately(distanciaPanelNivel2, 2f) ? 1.8f : distanciaPanelNivel2;
        float alturaComoda = Mathf.Approximately(alturaPanelNivel2, 0f) ? -0.35f : alturaPanelNivel2;

        Vector3 panelPosition = mainCamera.transform.position + forward * Mathf.Max(1.2f, distanciaComoda);
        panelPosition.y = mainCamera.transform.position.y + alturaComoda;

        Quaternion panelRotation = Quaternion.LookRotation(-forward, Vector3.up);
        MoverPanelInteractuable(panelPosition, panelRotation);
        return true;
    }

    private void ColocarPanelNivel2()
    {
        if (panelNivel2Colocado)
        {
            return;
        }

        if (puntoPanelNivel2 != null)
        {
            MoverPanelInteractuable(puntoPanelNivel2.position, puntoPanelNivel2.rotation);
            panelNivel2Colocado = true;
        }
        else if (colocarPanelFrenteAlJugador)
        {
            panelNivel2Colocado = ColocarPanelFrenteAlJugador();
        }
    }

    private void MoverPanelInteractuable(Vector3 position, Quaternion rotation)
    {
        Transform panelRoot = ObtenerPanelInteractuableRoot();

        if (!panelRoot.gameObject.activeSelf)
        {
            panelRoot.gameObject.SetActive(true);
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        panelRoot.SetPositionAndRotation(position, rotation);
    }

    private Transform ObtenerPanelInteractuableRoot()
    {
        if (panelInteractuableRoot != null)
        {
            return panelInteractuableRoot;
        }

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            panelInteractuableRoot = canvas.transform;
            return panelInteractuableRoot;
        }

        return transform;
    }

    private void MostrarInicioNivel2()
    {
        gameObject.SetActive(true);
        ObtenerPanelInteractuableRoot().gameObject.SetActive(true);

        nivelActual = 2;
        actividadIniciada = false;
        actividadCompletada = false;

        textoTitulo.text = tituloNivel2;
        textoIndicacion.text = string.IsNullOrWhiteSpace(textoBienvenidaNivel2) || textoBienvenidaNivel2 == "Bienvenido"
            ? textoInstruccionNivel2
            : textoBienvenidaNivel2;

        MostrarBotonCalibrar(false);
        MostrarBotonEmpezar(true);

        MostrarBotonContinuar(false, false);

        if (nivel2DibujoManager != null)
        {
            nivel2DibujoManager.OcultarNivel();
        }
    }

    private void MostrarActividadNivel2()
    {
        actividadIniciada = true;
        actividadCompletada = false;

        textoTitulo.text = tituloNivel2;
        textoIndicacion.text = textoInstruccionNivel2;

        MostrarBotonCalibrar(false);
        MostrarBotonEmpezar(false);
        MostrarBotonContinuar(false, false);

        if (nivel2DibujoManager == null)
        {
            PrepararNivel2Dibujo();
        }

        if (nivel2DibujoManager != null)
        {
            nivel2DibujoManager.IniciarActividad();
            accionPokeEnProceso = false;
            gameObject.SetActive(false);
        }
        else
        {
            textoIndicacion.text = "Falta configurar Nivel2dIBUJO.";
            MostrarBotonContinuar(true, false);
        }
    }

    public void CompletarNivel2()
    {
        ObtenerPanelInteractuableRoot().gameObject.SetActive(true);
        gameObject.SetActive(true);
        accionPokeEnProceso = false;

        nivelActual = 2;
        actividadIniciada = false;
        actividadCompletada = true;

        textoTitulo.text = tituloNivel2;
        textoIndicacion.text = textoNivel2Completado;

        MostrarBotonCalibrar(false);
        MostrarBotonEmpezar(false);
        MostrarBotonContinuar(true, true);
    }

    private void PrepararNivel2Dibujo()
    {
        if (nivel2DibujoManager == null)
        {
            nivel2DibujoManager = FindObjectOfType<DrawingLevel2Manager>(true);
        }

        if (nivel2DibujoManager == null)
        {
            GameObject nivel2Root = BuscarObjetoEscenaPorNombre("Nivel2dIBUJO");

            if (nivel2Root != null)
            {
                nivel2DibujoManager = nivel2Root.GetComponent<DrawingLevel2Manager>();

                if (nivel2DibujoManager == null)
                {
                    nivel2DibujoManager = nivel2Root.AddComponent<DrawingLevel2Manager>();
                }
            }
        }

        if (nivel2DibujoManager != null)
        {
            nivel2DibujoManager.AsignarPanel(this);
            nivel2DibujoManager.OcultarNivel();
        }
    }

    private GameObject BuscarObjetoEscenaPorNombre(string nombre)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform item in transforms)
        {
            if (item.name == nombre && item.gameObject.scene.IsValid())
            {
                return item.gameObject;
            }
        }

        return null;
    }

    private void MostrarBotonCalibrar(bool visible)
    {
        if (botonCalibrar != null)
        {
            botonCalibrar.gameObject.SetActive(visible && botonPokeCalibrar == null);
        }

        if (botonPokeCalibrar != null)
        {
            CambiarVisibilidadBotonPoke(botonPokeCalibrar, visible);

            if (visible)
            {
                ActualizarTextoBotonPoke(botonPokeCalibrar, ref textoBotonPokeCalibrar, "TextoPokeCalibrar", textoPokeCalibrar);
            }
        }

    }

    private void MostrarBotonEmpezar(bool visible)
    {
        botonPokeEmpezarVisible = visible;

        if (botonEmpezar != null)
        {
            botonEmpezar.gameObject.SetActive(visible && botonPokeEmpezar == null);
        }

        if (botonPokeEmpezar != null)
        {
            if (visible)
            {
                MoverBotonPokePrincipal(botonPokeEmpezar, posicionLocalPokeEmpezar);
            }

            CambiarVisibilidadBotonPoke(botonPokeEmpezar, visible);
            if (visible)
            {
                ActualizarTextoBotonPoke(textoPokeEmpezar);
            }
        }

    }

    private void MostrarBotonContinuar(bool visible, bool interactable)
    {
        botonPokeContinuarVisible = visible;
        botonPokeContinuarInteractuable = interactable;

        GameObject botonPoke = ObtenerBotonPokeContinuar();
        bool usarBotonPoke = botonPoke != null;

        if (botonContinuar != null)
        {
            botonContinuar.gameObject.SetActive(visible && !usarBotonPoke);
            botonContinuar.interactable = interactable;
        }

        if (botonPoke != null)
        {
            bool mostrarComoContinuar = visible && interactable;
            bool usaElMismoBotonDeEmpezar = botonPokeContinuar == null && botonPoke == botonPokeEmpezar;

            if (mostrarComoContinuar || !usaElMismoBotonDeEmpezar || !botonPokeEmpezarVisible)
            {
                if (mostrarComoContinuar && moverPokePrincipalAlContinuar)
                {
                    MoverBotonPokePrincipal(botonPoke, posicionLocalPokeContinuar);
                }

                CambiarVisibilidadBotonPoke(botonPoke, mostrarComoContinuar);
            }

            if (mostrarComoContinuar)
            {
                ActualizarTextoBotonPoke(textoPokeContinuar);
            }
        }

    }

    private GameObject ObtenerBotonPokeContinuar()
    {
        return botonPokeContinuar != null ? botonPokeContinuar : botonPokeEmpezar;
    }

    private void ActualizarTextoBotonPoke(string texto)
    {
        TMP_Text textoPoke = ObtenerTextoBotonPoke();

        if (textoPoke != null)
        {
            textoPoke.text = texto;
        }
    }

    private void ActualizarTextoBotonPoke(GameObject botonPoke, ref TMP_Text textoReferencia, string nombreTexto, string texto)
    {
        TMP_Text textoPoke = ObtenerTextoBotonPoke(botonPoke, ref textoReferencia, nombreTexto);

        if (textoPoke != null)
        {
            textoPoke.text = texto;
        }
    }

    private TMP_Text ObtenerTextoBotonPoke()
    {
        if (textoBotonPokePrincipal != null)
        {
            return textoBotonPokePrincipal;
        }

        GameObject botonPoke = botonPokeEmpezar != null ? botonPokeEmpezar : botonPokeContinuar;

        if (botonPoke == null)
        {
            return null;
        }

        Transform textoExistente = botonPoke.transform.Find("TextoPokePrincipal");

        if (textoExistente != null)
        {
            textoBotonPokePrincipal = textoExistente.GetComponent<TMP_Text>();
            return textoBotonPokePrincipal;
        }

        GameObject textoObject = new GameObject("TextoPokePrincipal");
        textoObject.transform.SetParent(botonPoke.transform, false);
        textoObject.transform.localPosition = posicionTextoBotonPoke;
        textoObject.transform.localRotation = Quaternion.identity;
        textoObject.transform.localScale = Vector3.one;

        TextMeshPro texto3D = textoObject.AddComponent<TextMeshPro>();
        texto3D.alignment = TextAlignmentOptions.Center;
        texto3D.color = colorTextoBotonPoke;
        texto3D.fontSize = tamanoTextoBotonPoke;
        texto3D.enableWordWrapping = false;
        texto3D.raycastTarget = false;
        texto3D.rectTransform.sizeDelta = new Vector2(anchoTextoBotonPoke, altoTextoBotonPoke);

        textoBotonPokePrincipal = texto3D;
        return textoBotonPokePrincipal;
    }

    private TMP_Text ObtenerTextoBotonPoke(GameObject botonPoke, ref TMP_Text textoReferencia, string nombreTexto)
    {
        if (textoReferencia != null)
        {
            return textoReferencia;
        }

        if (botonPoke == null)
        {
            return null;
        }

        Transform textoExistente = botonPoke.transform.Find(nombreTexto);

        if (textoExistente != null)
        {
            textoReferencia = textoExistente.GetComponent<TMP_Text>();
            return textoReferencia;
        }

        GameObject textoObject = new GameObject(nombreTexto);
        textoObject.transform.SetParent(botonPoke.transform, false);
        textoObject.transform.localPosition = posicionTextoBotonPoke;
        textoObject.transform.localRotation = Quaternion.identity;
        textoObject.transform.localScale = Vector3.one;

        TextMeshPro texto3D = textoObject.AddComponent<TextMeshPro>();
        texto3D.alignment = TextAlignmentOptions.Center;
        texto3D.color = colorTextoBotonPoke;
        texto3D.fontSize = tamanoTextoBotonPoke;
        texto3D.enableWordWrapping = false;
        texto3D.raycastTarget = false;
        texto3D.rectTransform.sizeDelta = new Vector2(anchoTextoBotonPoke, altoTextoBotonPoke);

        textoReferencia = texto3D;
        return textoReferencia;
    }

    private void BloquearAccionPokeTemporalmente()
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
        {
            return;
        }

        accionPokeEnProceso = true;
        StartCoroutine(DesbloquearAccionPoke());
    }

    private IEnumerator DesbloquearAccionPoke()
    {
        yield return new WaitForSeconds(0.45f);
        accionPokeEnProceso = false;
    }

    private void CambiarVisibilidadBotonPoke(GameObject botonPoke, bool visible)
    {
        if (botonPoke == null)
        {
            return;
        }

        if (!botonPoke.activeSelf)
        {
            if (!visible)
            {
                return;
            }

            botonPoke.SetActive(true);
        }

        Renderer[] renderers = botonPoke.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }

        Collider[] colliders = botonPoke.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            collider.enabled = visible;
        }
    }

    private void MoverBotonPokePrincipal(GameObject botonPoke, Vector3 posicionLocal)
    {
        if (botonPoke == null)
        {
            return;
        }

        Transform raizPoke = ObtenerRaizMovimientoPoke(botonPoke.transform);
        raizPoke.localPosition = posicionLocal;
    }

    private Transform ObtenerRaizMovimientoPoke(Transform botonPoke)
    {
        if (botonPoke.parent != null && botonPoke.parent.name.StartsWith("Poke Interaction"))
        {
            return botonPoke.parent;
        }

        return botonPoke;
    }
}

