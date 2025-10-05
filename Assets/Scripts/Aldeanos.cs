using UnityEngine;
using UnityEngine.AI;

public class Aldeanos : MonoBehaviour
{
    [Header("Stats")]
    public float energy = 100;
    public float age = 0;
    public float maxAge = 100;
    public bool isAlive = true;

    [Header("Movement & Vision")]
    public float baseSpeed = 4.5f;  // velocidad máxima a los 18
    public float minSpeed = 1.5f;   // velocidad mínima a los 80
    public float visionRange = 5f;
    private NavMeshAgent agent;

    [Header("Recolección")]
    public int recursosRecolectados = 0;
    public int maxRecursos = 5;

    [Header("Referencias")]
    public Transform aldea;
    public Transform bosque;

    public AldeanosState currentState = AldeanosState.Espera;
    private float h;

    // recarga progresiva de energía
    public float rechargeRate = 5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        CambiarEstado(AldeanosState.Espera);
    }

    // 🚀 este reemplaza a Update()
    public void Simulate(float deltaTime)
    {
        if (!isAlive) return;

        h = deltaTime;
        Age();

        // 🔥 ajustar velocidad según edad
        UpdateSpeed();

        // prioridad: detección de lobos
        if (LoboEnRango() && currentState != AldeanosState.Huir && currentState != AldeanosState.Grupo)
        {
            CambiarEstado(AldeanosState.Huir);
        }

        switch (currentState)
        {
            case AldeanosState.Espera:
                EstadoEspera();
                break;
            case AldeanosState.BuscarRecursos:
                EstadoBuscarRecursos();
                break;
            case AldeanosState.Recolectando:
                EstadoRecolectando();
                break;
            case AldeanosState.Depositando:
                EstadoDepositando();
                break;
            case AldeanosState.Huir:
                EstadoHuir();
                break;
            case AldeanosState.Grupo:
                EstadoGrupo();
                break;
        }
    }

    void EstadoEspera()
    {
        // recarga progresiva en la aldea
        if (Vector3.Distance(transform.position, aldea.position) < 2f)
        {
            energy = Mathf.Min(100, energy + rechargeRate * h);
        }

        // Solo pueden salir si tienen entre 18 y 80 años y energía suficiente
        if (age >= 18 && age <= 80 && energy >= 90)
        {
            if (Random.value < 0.01f)
            {
                CambiarEstado(AldeanosState.BuscarRecursos);
            }
        }
    }

    void EstadoBuscarRecursos()
    {
        agent.SetDestination(bosque.position);

        if (Vector3.Distance(transform.position, bosque.position) < 2f)
        {
            CambiarEstado(AldeanosState.Recolectando);
        }
    }

    void EstadoRecolectando()
    {
        recursosRecolectados++;
        energy -= 0.1f;

        if (recursosRecolectados >= maxRecursos)
        {
            CambiarEstado(AldeanosState.Depositando);
        }
    }

    void EstadoDepositando()
    {
        agent.SetDestination(aldea.position);

        if (Vector3.Distance(transform.position, aldea.position) < 2f)
        {
            recursosRecolectados = 0;
            CambiarEstado(AldeanosState.Espera);
        }
    }

    void EstadoHuir()
    {
        agent.SetDestination(aldea.position);

        if (Vector3.Distance(transform.position, aldea.position) < 2f)
        {
            CambiarEstado(AldeanosState.Espera);
        }
    }

    void EstadoGrupo()
    {
        // En grupo aún no hace nada especial
    }

    void CambiarEstado(AldeanosState nuevo)
    {
        currentState = nuevo;
    }

    void Age()
    {
        age += h;
        if (age > maxAge || energy <= 0)
        {
            Morir();
        }
    }

    void Morir()
    {
        isAlive = false;
        Destroy(gameObject);
    }

    bool LoboEnRango()
    {
        Collider[] lobos = Physics.OverlapSphere(transform.position, visionRange, LayerMask.GetMask("Lobos"));
        return lobos.Length > 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Lobo") && currentState != AldeanosState.Grupo)
        {
            Morir();
        }

        if (other.CompareTag("Aldeano"))
        {
            CambiarEstado(AldeanosState.Grupo);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }

    void UpdateSpeed()
    {
        if (age < 18) { agent.speed = 0; return; } // niños no salen
        if (age > 80) { agent.speed = 0; return; } // viejos no salen

        // interpolación lineal entre velocidad máxima (18 años) y mínima (80 años)
        float t = Mathf.InverseLerp(18, 80, age);
        agent.speed = Mathf.Lerp(baseSpeed, minSpeed, t);
    }
}
