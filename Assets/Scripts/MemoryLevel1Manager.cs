using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MemoryLevel1Manager : MonoBehaviour
{
    public MemoryGem[] availableGems;
    public MemoryBoxTarget[] boxTargets;
    public int patternLength = 3;
    public bool allowRepeats = false;

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
        activityStarted = false;
        placedGems.Clear();
        GenerateRandomPattern();
        OcultarFiguras();
        OcultarReferencias();
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

    private void GenerateRandomPattern()
    {
        if (availableGems == null || availableGems.Length == 0)
        {
            currentPattern = new MemoryGem[0];
            Debug.LogWarning("No hay figuras disponibles para generar el patron.");
            return;
        }

        int finalLength = Mathf.Max(1, patternLength);

        if (!allowRepeats)
        {
            finalLength = Mathf.Min(finalLength, availableGems.Length);
        }

        currentPattern = new MemoryGem[finalLength];

        List<MemoryGem> pool = new List<MemoryGem>(availableGems);

        for (int i = 0; i < currentPattern.Length; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            currentPattern[i] = pool[randomIndex];

            if (!allowRepeats)
            {
                pool.RemoveAt(randomIndex);
            }
        }

        Debug.Log("Patron aleatorio generado");
    }

    private void ConfigurarCajas()
    {
        if (boxTargets == null || boxTargets.Length == 0)
        {
            Debug.LogWarning("No hay cajas configuradas para el nivel 1.");
            return;
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
                boxTargets[i].Configurar(this, currentPattern[i], i + 1);
            }
            else
            {
                boxTargets[i].OcultarReferencia();
            }
        }
    }

    private void MostrarFigurasEnPosicionesAleatorias()
    {
        OcultarFiguras();

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
            Debug.Log("Correcto");

            if (audioSource != null && correctClip != null)
            {
                audioSource.PlayOneShot(correctClip);
            }

            placedGems.Add(gem);
            target.MarcarCorrecta();
            gem.FijarEn(target.snapPoint);

            if (placedGems.Count >= currentPattern.Length)
            {
                Debug.Log("Nivel 1 completado");
                activityStarted = false;

                if (panelPrincipal != null)
                {
                    panelPrincipal.CompletarActividad();
                }
            }
        }
        else
        {
            Debug.Log("Incorrecto");

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
