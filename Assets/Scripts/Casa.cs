using System.Collections.Generic;
using UnityEngine;

public class Casa : MonoBehaviour
{
    [Header("Reproducción")]
    public float radioDeteccion = 2f;
    public float tiempoReproduccion = 10f;
    private float timer = 0f;

    [Header("Prefabs")]
    public GameObject aldeanoPrefab;

    private List<Aldeanos> candidatos = new List<Aldeanos>();

    public void Simulate(float deltaTime)
    {
        // Buscar aldeanos cercanos
        Collider2D[] colls = Physics2D.OverlapCircleAll(transform.position, radioDeteccion);

        candidatos.Clear();
        foreach (var c in colls)
        {
            Aldeanos a = c.GetComponent<Aldeanos>();
            if (a != null && a.isAlive && a.edad >= 20 && !candidatos.Contains(a))
            {
                candidatos.Add(a);
            }
        }

        // Necesitamos mínimo 2 aldeanos
        if (candidatos.Count >= 2)
        {
            // Buscar pareja hombre-mujer
            Aldeanos macho = null;
            Aldeanos hembra = null;

            foreach (var a in candidatos)
            {
                if (a.genero == Genero.Hombre && macho == null)
                    macho = a;

                if (a.genero == Genero.Mujer && hembra == null)
                    hembra = a;
            }

            if (macho != null && hembra != null)
            {
                timer += deltaTime;

                if (timer >= tiempoReproduccion)
                {
                    CrearNuevoAldeano(macho, hembra);
                    timer = 0f;
                }
            }
            else
            {
                timer = 0f; // no hay pareja válida
            }
        }
        else
        {
            timer = 0f;
        }
    }

    private void CrearNuevoAldeano(Aldeanos padre, Aldeanos madre)
    {
        if (aldeanoPrefab == null) return;

        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 1f;
        GameObject hijoGO = Instantiate(aldeanoPrefab, spawnPos, Quaternion.identity);

        Aldeanos hijo = hijoGO.GetComponent<Aldeanos>();
        if (hijo != null)
        {
            hijo.edad = 0;
            hijo.vida = 5;

            SimulationManager sim = FindObjectOfType<SimulationManager>();
            if (sim != null) sim.RegisterAldeano(hijo);
        }

        Debug.Log("¡Nuevo aldeano nacido en la casa!");
    }

    // Gizmos para ver el radio en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}

