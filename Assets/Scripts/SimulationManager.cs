using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    private float time = 0f;

    [Header("Escenario")]
    public Aldea aldea;
    public Bosque bosque;

    [Header("Entidades")]
    public List<Aldeanos> aldeanos = new List<Aldeanos>();
    public List<Lobos> lobos = new List<Lobos>();

    void Start()
    {
        // Buscar aldeanos y lobos en la escena automáticamente
        Aldeanos[] foundAldeanos = FindObjectsOfType<Aldeanos>();
        aldeanos = new List<Aldeanos>(foundAldeanos);

        Lobos[] foundLobos = FindObjectsOfType<Lobos>();
        lobos = new List<Lobos>(foundLobos);

        // Buscar aldea y bosque si no están asignados
        if (aldea == null) aldea = FindFirstObjectByType<Aldea>();
        if (bosque == null) bosque = FindFirstObjectByType<Bosque>();
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;
        time += deltaTime;

        Simulate(deltaTime);
    }

    public void Simulate(float deltaTime)
    {
        // Aldea
        if (aldea != null)
            aldea.Simulate(deltaTime);

        // Bosque
        if (bosque != null)
            bosque.Simulate(deltaTime);

        // Aldeanos
        for (int i = aldeanos.Count - 1; i >= 0; i--)
        {
            if (aldeanos[i] == null) { aldeanos.RemoveAt(i); continue; }
            aldeanos[i].Simulate(deltaTime);
        }

        // Lobos
        for (int i = lobos.Count - 1; i >= 0; i--)
        {
            if (lobos[i] == null) { lobos.RemoveAt(i); continue; }
            lobos[i].Simulate(deltaTime);
        }
    }

    // Registro manual opcional (por si instancias en runtime)
    public void RegisterAldeano(Aldeanos aldeano)
    {
        if (!aldeanos.Contains(aldeano))
            aldeanos.Add(aldeano);
    }

    public void RegisterLobo(Lobos lobo)
    {
        if (!lobos.Contains(lobo))
            lobos.Add(lobo);
    }

    public void RemoveAldeano(Aldeanos aldeano)
    {
        if (aldeanos.Contains(aldeano))
            aldeanos.Remove(aldeano);
    }

    public void RemoveLobo(Lobos lobo)
    {
        if (lobos.Contains(lobo))
            lobos.Remove(lobo);
    }
}
