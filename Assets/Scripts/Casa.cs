using System.Collections.Generic;
using UnityEngine;

public class Casa : MonoBehaviour
{
    [Header("Reproducción")]
    public float tiempoReproduccion = 10f;
    private float timer = 0f;

    [Header("Prefabs")]
    public GameObject aldeanoPrefab;

    private List<Aldeanos> aldeanosDentro = new List<Aldeanos>();

    // --- Simulación de la casa ---
    public void Simulate(float deltaTime)
    {
        if (aldeanosDentro.Count == 2)
        {
            Aldeanos a1 = aldeanosDentro[0];
            Aldeanos a2 = aldeanosDentro[1];

            // Ambos deben estar vivos, ser de distinto género y tener edad adecuada
            if (a1.isAlive && a2.isAlive &&
                a1.genero != a2.genero &&
                a1.edad >= 20 && a2.edad >= 20)
            {
                // Empiezan a "pasar tiempo en la casa"
                timer += deltaTime;

                if (timer >= tiempoReproduccion)
                {
                    CrearNuevoAldeano();
                    timer = 0f;

                    // Los dos aldeanos regresan a la aldea
                    a1.CambiarEstado(AldeanoState.EnAldea);
                    a2.CambiarEstado(AldeanoState.EnAldea);

                    VaciarCasa();
                }
            }
        }
        else
        {
            // Reiniciar si no hay pareja completa
            timer = 0f;
        }
    }

    // --- Métodos accesibles desde Aldeanos ---
    public List<Aldeanos> GetResidentes()
    {
        return aldeanosDentro;
    }

    public void VaciarCasa()
    {
        aldeanosDentro.Clear();
    }

    public void CrearNuevoAldeano()
    {
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 1f;
        Instantiate(aldeanoPrefab, spawnPos, Quaternion.identity);
        Debug.Log("¡Nuevo aldeano nacido en la casa!");
    }

    // --- Triggers ---
    private void OnTriggerEnter2D(Collider2D other)
    {
        Aldeanos aldeano = other.GetComponent<Aldeanos>();
        if (aldeano != null && !aldeanosDentro.Contains(aldeano))
        {
            if (aldeanosDentro.Count < 2)
            {
                aldeanosDentro.Add(aldeano);
                aldeano.AsignarCasa(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Aldeanos aldeano = other.GetComponent<Aldeanos>();
        if (aldeano != null && aldeanosDentro.Contains(aldeano))
        {
            aldeanosDentro.Remove(aldeano);
            aldeano.AsignarCasa(null);
        }
    }
}
