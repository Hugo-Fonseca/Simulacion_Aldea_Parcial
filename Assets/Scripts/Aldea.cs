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

    [Header("Distribución casas")]
    public float radioAldea = 6f;
    public float minDistanceFromDeposito = 3f;
    public float minDistanceBetweenCasas = 2f;

    [Header("Curación")]
    public float curacionPorSegundo = 5f;

    [Header("Zona (colisión)")]
    public Collider2D zonaAldeaCollider;

    void Start()
    {
        Initialize();

        if (zonaAldeaCollider == null)
        {
            zonaAldeaCollider = GetComponent<CircleCollider2D>();
            if (zonaAldeaCollider == null)
            {
                CircleCollider2D cc = gameObject.AddComponent<CircleCollider2D>();
                cc.isTrigger = true;
                cc.radius = radioAldea;
                zonaAldeaCollider = cc;
            }
        }
    }

    public void Initialize()
    {
        if (depositoCentral == null && depositoPrefab != null)
        {
            depositoCentral = Instantiate(depositoPrefab, transform.position, Quaternion.identity, transform);
        }

        // Crear una primera casa si aún no hay ninguna
        if (casas.Count == 0 && casaPrefab != null)
            CrearCasa();
    }

    public void Simulate(float deltaTime)
    {
        for (int i = 0; i < casas.Count; i++)
        {
            Casa casaComp = casas[i].GetComponent<Casa>();
            if (casaComp != null)
            {
                // Debug.Log($"[Aldea] Simulando casa {casas[i].name}");
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
        if (casaPrefab == null) return;

        Vector2 pos = GenerarPosicionCasa();
        GameObject nuevaCasa = Instantiate(casaPrefab, pos, Quaternion.identity, transform);
        casas.Add(nuevaCasa);
        Debug.Log("Nueva casa construida en la aldea!");
    }

    Vector2 GenerarPosicionCasa()
    {
        Vector2 pos = (Vector2)transform.position;
        int intentos = 0;

        do
        {
            pos = (Vector2)transform.position + Random.insideUnitCircle * radioAldea;
            intentos++;

            // evitar deposito muy cercano
            bool cercaDeposito = depositoCentral != null && Vector2.Distance(pos, depositoCentral.transform.position) < minDistanceFromDeposito;

            // evitar superposición con otras casas
            bool overlapCasa = false;
            foreach (var c in casas)
            {
                if (c == null) continue;
                if (Vector2.Distance(pos, c.transform.position) < minDistanceBetweenCasas)
                {
                    overlapCasa = true;
                    break;
                }
            }

            if (!cercaDeposito && !overlapCasa) break;

        } while (intentos < 40);

        return pos;
    }
}
