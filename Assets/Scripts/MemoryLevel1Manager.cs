using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MemoryLevel1Manager : MonoBehaviour
{
    public MemoryGem[] availableGems;
    public MemoryBoxTarget[] boxTargets;

    [Header("Patron")]
    public int patternLength = 3;
    public bool allowRepeats = false;
    public bool ocultarCajasSobrantes = true;

    [Header("Panel")]
    public PanelPrincipalManager panelPrincipal;

    [Header("Aparicion")]
    public Transform[] spawnPoints;
    public Vector3 randomAreaCenter = Vector3.zero;
    public Vector3 randomAreaSize = new Vector3(4f, 0f, 4f);

    public AudioSource audioSource;
    public AudioClip correctClip;
    public AudioClip wrongClip;

    private MemoryGem[] currentPattern;
    private readonly HashSet<MemoryGem> placedGems = new HashSet<MemoryGem>();
    private bool activityStarted = false;

    public void AsignarPanel(PanelPrincipalManager panel)
    {
        panelPrincipal = panel;
    }

    public void OcultarNivel()
    {
        activityStarted = false;
        placedGems.Clear();
        OcultarFiguras();
        OcultarReferencias();
    }

    public void PrepararPatron()
    {
        PrepararPatron(patternLength);
    }

    public void PrepararPatron(int cantidadFiguras)
    {
        activityStarted = false;
        placedGems.Clear();
        GenerateRandomPattern(cantidadFiguras);
        OcultarFiguras();
        OcultarReferencias();
    }

    public void PrepararPatronInvertido(MemoryGem[] patronBase, int cantidadFallback)
    {
        activityStarted = false;
        placedGems.Clear();

        if (patronBase == null || patronBase.Length == 0)
        {
            PrepararPatron(cantidadFallback);
            return;
        }

        List<MemoryGem> patronLimpio = new List<MemoryGem>();

        for (int i = patronBase.Length - 1; i >= 0; i--)
        {
            if (patronBase[i] != null)
            {
                patronLimpio.Add(patronBase[i]);
            }
        }

        currentPattern = patronLimpio.ToArray();
        OcultarFiguras();
        OcultarReferencias();
        Debug.Log("Patron invertido preparado");
    }

    public MemoryGem[] ObtenerPatronActual()
    {
        if (currentPattern == null || currentPattern.Length == 0)
        {
            return new MemoryGem[0];
        }

        MemoryGem[] copia = new MemoryGem[currentPattern.Length];
        currentPattern.CopyTo(copia, 0);
        return copia;
    }

    public int ObtenerCantidadPatronActual()
    {
        return currentPattern != null ? currentPattern.Length : 0;
    }

    public string ObtenerTextoPatron()
    {
        if (currentPattern == null || currentPattern.Length == 0)
        {
            return "No hay figuras configuradas para el patron.";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("Memoriza el patron:\n");

        for (int i = 0; i < currentPattern.Length; i++)
        {
            if (currentPattern[i] == null)
            {
                continue;
            }

            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(currentPattern[i].NombreVisible);

            if (i < currentPattern.Length - 1)
            {
                builder.Append("  ");
            }
        }

        return builder.ToString();
    }

    public void IniciarActividad()
    {
        if (currentPattern == null || currentPattern.Length == 0)
        {
            PrepararPatron();
        }

        activityStarted = true;
        placedGems.Clear();

        gameObject.SetActive(true);
        ConfigurarCajas();
        MostrarFigurasEnPosicionesAleatorias();
    }

    private void GenerateRandomPattern(int cantidadFiguras)
    {
        List<MemoryGem> pool = ObtenerFigurasDisponibles();

        if (pool.Count == 0)
        {
            currentPattern = new MemoryGem[0];
            Debug.LogWarning("No hay figuras disponibles para generar el patron.");
            return;
        }

        int finalLength = Mathf.Max(1, cantidadFiguras);

        if (!allowRepeats)
        {
            finalLength = Mathf.Min(finalLength, pool.Count);
        }

        currentPattern = new MemoryGem[finalLength];

        for (int i = 0; i < currentPattern.Length; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            currentPattern[i] = pool[randomIndex];

            if (!allowRepeats)
            {
                pool.RemoveAt(randomIndex);
            }
        }

        Debug.Log("Patron aleatorio generado con " + finalLength + " figuras");
    }

    private List<MemoryGem> ObtenerFigurasDisponibles()
    {
        List<MemoryGem> pool = new List<MemoryGem>();

        if (availableGems == null)
        {
            return pool;
        }

        foreach (MemoryGem gem in availableGems)
        {
            if (gem != null)
            {
                pool.Add(gem);
            }
        }

        return pool;
    }

    private void ConfigurarCajas()
    {
        if (boxTargets == null || boxTargets.Length == 0)
        {
            Debug.LogWarning("No hay cajas configuradas para el nivel de memoria.");
            return;
        }

        if (currentPattern == null)
        {
            currentPattern = new MemoryGem[0];
        }

        int count = Mathf.Min(boxTargets.Length, currentPattern.Length);

        for (int i = 0; i < boxTargets.Length; i++)
        {
            if (boxTargets[i] == null)
            {
                continue;
            }

            if (i < count)
            {
                if (!boxTargets[i].gameObject.activeSelf)
                {
                    boxTargets[i].gameObject.SetActive(true);
                }

                boxTargets[i].Configurar(this, currentPattern[i], i + 1);
                Debug.Log($"Caja {boxTargets[i].gameObject.name} espera {currentPattern[i].NombreVisible}");
            }
            else
            {
                boxTargets[i].OcultarReferencia();

                if (ocultarCajasSobrantes)
                {
                    boxTargets[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void MostrarFigurasEnPosicionesAleatorias()
    {
        OcultarFiguras();

        if (currentPattern == null)
        {
            return;
        }

        for (int i = 0; i < currentPattern.Length; i++)
        {
            MemoryGem gem = currentPattern[i];

            if (gem == null)
            {
                continue;
            }

            gem.ActivarEn(GetSpawnPosition(i));
        }
    }

    private Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && spawnPoints.Length > index && spawnPoints[index] != null)
        {
            return spawnPoints[index].position;
        }

        Vector3 halfSize = randomAreaSize * 0.5f;

        return randomAreaCenter + new Vector3(
            Random.Range(-halfSize.x, halfSize.x),
            Random.Range(-halfSize.y, halfSize.y),
            Random.Range(-halfSize.z, halfSize.z));
    }

    public void IntentarColocar(MemoryBoxTarget target, MemoryGem gem)
    {
        if (!activityStarted || target == null || gem == null || placedGems.Contains(gem))
        {
            return;
        }

        if (target.EsCorrecta(gem))
        {
            Debug.Log($"Correcto: {gem.NombreVisible} en {target.gameObject.name}");

            if (audioSource != null && correctClip != null)
            {
                audioSource.PlayOneShot(correctClip);
            }

            placedGems.Add(gem);
            target.MarcarCorrecta();
            gem.FijarEn(target.snapPoint);

            if (placedGems.Count >= currentPattern.Length)
            {
                Debug.Log("Nivel de memoria completado");
                activityStarted = false;

                if (panelPrincipal != null)
                {
                    panelPrincipal.CompletarActividad();
                }
            }
        }
        else
        {
            Debug.Log($"Incorrecto: {gem.NombreVisible} en {target.gameObject.name}. Esa caja espera {target.NombreEsperado}");

            if (audioSource != null && wrongClip != null)
            {
                audioSource.PlayOneShot(wrongClip);
            }

            target.MarcarIncorrecta();
            gem.VolverAlInicio();
        }
    }

    private void OcultarFiguras()
    {
        if (availableGems == null)
        {
            return;
        }

        foreach (MemoryGem gem in availableGems)
        {
            if (gem != null)
            {
                gem.PrepararOculta();
            }
        }
    }

    private void OcultarReferencias()
    {
        if (boxTargets == null)
        {
            return;
        }

        foreach (MemoryBoxTarget target in boxTargets)
        {
            if (target != null)
            {
                target.OcultarReferencia();
            }
        }
    }
}
