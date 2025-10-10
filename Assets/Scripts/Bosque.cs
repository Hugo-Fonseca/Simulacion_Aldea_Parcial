using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class Bosque : MonoBehaviour
{
    [Header("Generación de Árboles")]
    public GameObject prefabArbol;
    public Transform contenedorArboles;
    public int maxArboles = 20;
    public float tiempoRegeneracion = 10f;
    private float tiempoActual;

    [Header("Generación de Lobos")]
    public GameObject prefabLobo;
    public int maxLobos = 5;
    public float tiempoSpawnLobo = 15f;
    private float tiempoLobo;

    private List<GameObject> arboles = new List<GameObject>();
    private List<GameObject> lobos = new List<GameObject>();

    [Header("Área del bosque")]
    public float radioBosque = 5f; // qué tan grande es el bosque

    public void Simulate(float deltaTime)
    {
        // Regenerar árboles
        tiempoActual += deltaTime;
        if (tiempoActual >= tiempoRegeneracion)
        {
            tiempoActual = 0f;
            if (arboles.Count < maxArboles)
                GenerarArbol();
        }

        // Spawnear lobos
        tiempoLobo += deltaTime;
        if (tiempoLobo >= tiempoSpawnLobo)
        {
            tiempoLobo = 0f;
            if (lobos.Count < maxLobos)
                GenerarLobo();
        }
    }

    void GenerarArbol()
    {
        Vector2 posRandom = (Vector2)transform.position + Random.insideUnitCircle * radioBosque;
        GameObject nuevo = Instantiate(prefabArbol, posRandom, Quaternion.identity, contenedorArboles);
        arboles.Add(nuevo);
    }

    void GenerarLobo()
    {
        Vector2 posRandom = (Vector2)transform.position + Random.insideUnitCircle * radioBosque;
        GameObject nuevo = Instantiate(prefabLobo, posRandom, Quaternion.identity);
        lobos.Add(nuevo);

        // Registrar el lobo en SimulationManager
        SimulationManager sim = FindFirstObjectByType<SimulationManager>();
        if (sim != null)
        {
            Lobos loboComp = nuevo.GetComponent<Lobos>();
            if (loboComp != null)
                sim.RegisterLobo(loboComp);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radioBosque);
    }
}

