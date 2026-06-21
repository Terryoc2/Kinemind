using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelPrincipalManager : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text textoTitulo;
    public TMP_Text textoIndicacion;

    [Header("Botones")]
    public Button botonCalibrar;
    public Button botonEmpezar;
    public Button botonContinuar;

    [Header("Panel VR")]
    public Transform panelInteractuableRoot;

    [Header("Tiempo")]
    public float segundosParaContinuar = 15f;

    [Header("Nivel 1")]
    public MemoryLevel1Manager nivel1Manager;

    [Header("Transicion Nivel 2")]
    public GameObject escenarioNivel2;
    public Transform jugadorVR;
    public Transform puntoJugadorNivel2;
    public Transform puntoPanelNivel2;
    public bool colocarPanelFrenteAlJugador = true;
    public float distanciaPanelNivel2 = 2f;
    public float alturaPanelNivel2 = 0f;
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
    private bool actividadIniciada = false;
    private bool actividadCompletada = false;
    private bool transicionandoNivel = false;
    private int nivelActual = 1;

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

        MostrarInicio();
    }

    public void MostrarInicio()
    {
        nivelActual = 1;
        textoTitulo.text = "JKInemind";
        textoIndicacion.text = "Bienvenido";

        botonCalibrar.gameObject.SetActive(true);
        botonEmpezar.gameObject.SetActive(true);

        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

        actividadIniciada = false;
        actividadCompletada = false;

        if (nivel2DibujoManager != null)
        {
            nivel2DibujoManager.OcultarNivel();
        }
    }

    public void Empezar()
    {
        if (nivelActual == 2)
        {
            MostrarActividadNivel2();
            return;
        }

        actividadIniciada = false;
        actividadCompletada = false;

        textoTitulo.text = "Nivel 1";

        if (nivel1Manager != null)
        {
            nivel1Manager.PrepararPatron();
            textoIndicacion.text = nivel1Manager.ObtenerTextoPatron();
        }
        else
        {
            textoIndicacion.text = "Memoriza el patron y coloca cada figura en su caja.";
        }

        botonCalibrar.gameObject.SetActive(false);
        botonEmpezar.gameObject.SetActive(false);

        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

        if (rutinaContinuar != null)
        {
            StopCoroutine(rutinaContinuar);
        }

        rutinaContinuar = StartCoroutine(MostrarContinuarDespuesDeTiempo());
    }

    private IEnumerator MostrarContinuarDespuesDeTiempo()
    {
        yield return new WaitForSeconds(segundosParaContinuar);

        botonContinuar.gameObject.SetActive(true);
        botonContinuar.interactable = true;
    }

    public void Continuar()
    {
        if (nivelActual == 2)
        {
            if (actividadCompletada)
            {
                textoTitulo.text = "Nivel 2 completado";
                textoIndicacion.text = "Actividad completada.";
                botonContinuar.interactable = false;
            }

            return;
        }

        if (actividadCompletada)
        {
            IniciarTransicionNivel2();
            botonContinuar.interactable = false;
            return;
        }

        if (actividadIniciada)
        {
            return;
        }

        actividadIniciada = true;

        textoTitulo.text = "Nivel 1";
        textoIndicacion.text = "Ordena y pon en las cajas";

        botonContinuar.gameObject.SetActive(true);
        botonContinuar.interactable = false;

        if (nivel1Manager != null)
        {
            nivel1Manager.IniciarActividad();
        }
    }

    public void CompletarActividad()
    {
        actividadCompletada = true;
        textoTitulo.text = "Nivel 1";
        textoIndicacion.text = "Actividad completada.";

        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

        IniciarTransicionNivel2();
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

        botonCalibrar.gameObject.SetActive(false);
        botonEmpezar.gameObject.SetActive(false);
        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

        textoTitulo.text = "Nivel 1 completado";
        textoIndicacion.text = "Se te transportara al siguiente nivel.";

        yield return new WaitForSeconds(segundosEntreCuenta);

        for (int i = 3; i >= 1; i--)
        {
            textoTitulo.text = "Siguiente nivel";
            textoIndicacion.text = "Se te transportara al siguiente nivel.\n" + i;
            yield return new WaitForSeconds(segundosEntreCuenta);
        }

        TeletransportarAlNivel2();
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

        if (puntoPanelNivel2 != null)
        {
            MoverPanelInteractuable(puntoPanelNivel2.position, puntoPanelNivel2.rotation);
        }
        else if (colocarPanelFrenteAlJugador)
        {
            ColocarPanelFrenteAlJugador();
        }
    }

    private Transform BuscarJugadorVR()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            return mainCamera.transform.root;
        }

        Camera cameraInScene = FindObjectOfType<Camera>();
        return cameraInScene != null ? cameraInScene.transform.root : null;
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

    private void ColocarPanelFrenteAlJugador()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            return;
        }

        Vector3 forward = mainCamera.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = mainCamera.transform.forward;
        }

        forward.Normalize();

        Vector3 panelPosition = mainCamera.transform.position + forward * distanciaPanelNivel2;
        panelPosition.y = mainCamera.transform.position.y + alturaPanelNivel2;

        Quaternion panelRotation = Quaternion.LookRotation(mainCamera.transform.position - panelPosition, Vector3.up);
        MoverPanelInteractuable(panelPosition, panelRotation);
    }

    private void MoverPanelInteractuable(Vector3 position, Quaternion rotation)
    {
        Transform panelRoot = ObtenerPanelInteractuableRoot();
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
        nivelActual = 2;
        actividadIniciada = false;
        actividadCompletada = false;

        textoTitulo.text = tituloNivel2;
        textoIndicacion.text = string.IsNullOrWhiteSpace(textoBienvenidaNivel2) || textoBienvenidaNivel2 == "Bienvenido"
            ? textoInstruccionNivel2
            : textoBienvenidaNivel2;

        botonCalibrar.gameObject.SetActive(false);
        botonEmpezar.gameObject.SetActive(true);

        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

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

        botonCalibrar.gameObject.SetActive(false);
        botonEmpezar.gameObject.SetActive(false);
        botonContinuar.gameObject.SetActive(false);
        botonContinuar.interactable = false;

        if (nivel2DibujoManager == null)
        {
            PrepararNivel2Dibujo();
        }

        if (nivel2DibujoManager != null)
        {
            nivel2DibujoManager.IniciarActividad();
            gameObject.SetActive(false);
        }
        else
        {
            textoIndicacion.text = "Falta configurar Nivel2dIBUJO.";
            botonContinuar.gameObject.SetActive(true);
            botonContinuar.interactable = false;
        }
    }

    public void CompletarNivel2()
    {
        gameObject.SetActive(true);

        nivelActual = 2;
        actividadIniciada = false;
        actividadCompletada = true;

        textoTitulo.text = tituloNivel2;
        textoIndicacion.text = textoNivel2Completado;

        botonCalibrar.gameObject.SetActive(false);
        botonEmpezar.gameObject.SetActive(false);
        botonContinuar.gameObject.SetActive(true);
        botonContinuar.interactable = true;
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
}
