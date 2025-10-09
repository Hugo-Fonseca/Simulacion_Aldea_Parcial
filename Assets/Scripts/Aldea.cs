using System.Collections.Generic;
using UnityEngine;

public class Aldea : MonoBehaviour
{
    [Header("Configuración de la Aldea")]
    public Transform puntoCentral;

    public int recursosTotales = 0;
    public int capacidadRecursos = 200;
    public int recursosPorCasa = 20;

    [Header("Estructuras")]
    public GameObject casaPrefab;
    public GameObject depositoPrefab;
    public List<GameObject> casas = new List<GameObject>();
    private GameObject depositoCentral;

    [Header("Curación")]
    public float curacionPorSegundo = 5f;

    public void Initialize()
    {
        depositoCentral = Instantiate(depositoPrefab, transform.position, Quaternion.identity, transform);
        CrearCasa();
    }

    public void Simulate(float deltaTime)
    {
        foreach (var casa in casas)
        {
            Casa casaComp = casa.GetComponent<Casa>();
            if (casaComp != null)
            {
                casaComp.Simulate(deltaTime);
            }
        }
    }

    public void DepositarRecursos(int cantidad)
    {
        recursosTotales += cantidad;
        if (recursosTotales > capacidadRecursos)
            recursosTotales = capacidadRecursos;

        int casasEsperadas = recursosTotales / recursosPorCasa;
        while (casas.Count < casasEsperadas)
        {
            CrearCasa();
        }
    }

    private void CrearCasa()
    {
        Vector2 posAleatoria = (Vector2)transform.position + Random.insideUnitCircle * 3f;
        GameObject nuevaCasa = Instantiate(casaPrefab, posAleatoria, Quaternion.identity, transform);
        casas.Add(nuevaCasa);
        Debug.Log("Nueva casa construida en la aldea!");
    }
}
