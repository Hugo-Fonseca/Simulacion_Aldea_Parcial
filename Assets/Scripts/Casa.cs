using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class Casa : MonoBehaviour
{
    public float tiempoReproduccion = 10f;
    private float timer = 0f;

    private List<Aldeanos> aldeanosDentro = new List<Aldeanos>();
    public GameObject aldeanoPrefab;

    public void Simulate(float deltaTime)
    {
        // Solo si hay exactamente 2 aldeanos dentro
        if (aldeanosDentro.Count == 2)
        {
            Aldeanos a1 = aldeanosDentro[0];
            Aldeanos a2 = aldeanosDentro[1];

            if (a1.isAlive && a2.isAlive &&
                ((a1.genero == Genero.Hombre && a2.genero == Genero.Mujer) ||
                 (a1.genero == Genero.Mujer && a2.genero == Genero.Hombre)))
            {
                // Ambos en estado Reproducción
                a1.currentState = AldeanosState.Reproduccion;
                a2.currentState = AldeanosState.Reproduccion;

                timer += deltaTime;
                if (timer >= tiempoReproduccion)
                {
                    CrearNuevoAldeano();
                    timer = 0f;

                    // Regresan a Espera
                    a1.currentState = AldeanosState.Espera;
                    a2.currentState = AldeanosState.Espera;

                    // Vaciar la casa (para que puedan entrar otros después)
                    aldeanosDentro.Clear();
                }
            }
        }
        else
        {
            timer = 0f; // reset si no hay pareja válida
        }
    }

    private void CrearNuevoAldeano()
    {
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 1f;
        Instantiate(aldeanoPrefab, spawnPos, Quaternion.identity);
        Debug.Log("¡Nuevo aldeano nacido en la casa!");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Aldeanos aldeano = other.GetComponent<Aldeanos>();
        if (aldeano != null)
        {
            // Solo permitir máximo 2 aldeanos dentro
            if (aldeanosDentro.Count < 2)
            {
                aldeanosDentro.Add(aldeano);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Aldeanos aldeano = other.GetComponent<Aldeanos>();
        if (aldeano != null && aldeanosDentro.Contains(aldeano))
        {
            aldeanosDentro.Remove(aldeano);
        }
    }
}
