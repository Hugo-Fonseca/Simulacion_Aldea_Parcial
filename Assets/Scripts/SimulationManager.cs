using System.Collections.Generic;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public float secondsPerIteration = 1.0f;
    private float time = 0f;

    [Header("Entidades")]
    public List<Aldeanos> aldeanos = new List<Aldeanos>();
    public List<Lobos> lobos = new List<Lobos>();

    [Header("Escenarios")]
    public Aldea aldea;
    public Bosque bosque;

    void Start()
    {
        // Buscar aldeanos existentes en la escena
        Aldeanos[] foundAldeanos = FindObjectsByType<Aldeanos>(FindObjectsSortMode.InstanceID);
        aldeanos = new List<Aldeanos>(foundAldeanos);

        // Buscar lobos existentes en la escena
        Lobos[] foundLobos = FindObjectsByType<Lobos>(FindObjectsSortMode.InstanceID);
        lobos = new List<Lobos>(foundLobos);

        // Buscar referencias de aldea y bosque
        aldea = FindFirstObjectByType<Aldea>();
        bosque = FindFirstObjectByType<Bosque>();
    }

    void Update()
    {
        time += Time.deltaTime;

        if (time >= secondsPerIteration)
        {
            time = 0f;
            Simulate();
        }
    }

    void Simulate()
    {
        foreach (Aldeanos a in aldeanos)
        {
            if (a != null)
            {
                a.Simulate(secondsPerIteration);
            }
        }

        foreach (Lobos l in lobos)
        {
            if (l != null)
            {
                // igual que con los aldeanos
                // hacemos que cada lobo simule su comportamiento
                l.Simulate(secondsPerIteration);
            }
        }

        // Aquí podríamos poner lógica de aldea/bosque
        // Ejemplo: bosque genera lobos o recursos cada cierto tiempo
        // aldea regenera aldeanos si hay recursos suficientes
    }
}
