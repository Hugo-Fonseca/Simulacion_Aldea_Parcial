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

    public float rechargeRate = 5f; // energía por segundo en la aldea

    // --- control de grupo ---
    private int nearbyAldeanos = 0;

    // propiedad pública que consultan los lobos
    public bool IsGrouped
    {
        get { return nearbyAldeanos > 0; }
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        CambiarEstado(AldeanosState.Espera);
    }

    public void Simulate(float deltaTime)
    {
        if (!isAlive) return;

        h = deltaTime;
        Age();
        UpdateSpeed();

        // prioridad: huir de lobos (si no está en grupo)
        if (LoboEnRango() && currentState != AldeanosState.Huir && !IsGrouped)
        {
            CambiarEstado(AldeanosState.Huir);
        }

        // si hay otros aldeanos cerca, forzamos el estado Grupo
        if (IsGrouped && currentState != AldeanosState.Grupo)
        {
            CambiarEstado(AldeanosState.Grupo);
        }
        else if (!IsGrouped && currentState == AldeanosState.Grupo)
        {
            // si dejó de estar en grupo, volver a Espera (o al estado lógico)
            CambiarEstado(AldeanosState.Espera);
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
        if (aldea != null && Vector3.Distance(transform.position, aldea.position) < 2f)
        {
            energy = Mathf.Min(100, energy + rechargeRate * h);
        }

        // Solo pueden salir si tienen entre 18 y 80 años y energía suficiente
        if (age >= 18 && age <= 80 && energy >= 90 && !IsGrouped)
        {
            if (Random.value < 0.01f)
            {
                CambiarEstado(AldeanosState.BuscarRecursos);
            }
        }
    }

    void EstadoBuscarRecursos()
    {
        if (bosque != null) agent.SetDestination(bosque.position);

        if (bosque != null && Vector3.Distance(transform.position, bosque.position) < 2f)
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
        if (aldea != null) agent.SetDestination(aldea.position);

        if (aldea != null && Vector3.Distance(transform.position, aldea.position) < 2f)
        {
            recursosRecolectados = 0;
            CambiarEstado(AldeanosState.Espera);
        }
    }

    void EstadoHuir()
    {
        if (aldea != null) agent.SetDestination(aldea.position);

        if (aldea != null && Vector3.Distance(transform.position, aldea.position) < 2f)
        {
            CambiarEstado(AldeanosState.Espera);
        }
    }

    void EstadoGrupo()
    {
        // mientras estén en grupo, se quedan juntos (por ahora)
        if (agent != null) agent.SetDestination(transform.position);
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

    // ahora público para que otros scripts (lobos) puedan invocarlo
    public void Morir()
    {
        if (!isAlive) return;
        isAlive = false;
        // aquí podrías reproducir animación/efecto antes de destruir
        Destroy(gameObject);
    }

    bool LoboEnRango()
    {
        Collider[] lobos = Physics.OverlapSphere(transform.position, visionRange, LayerMask.GetMask("Lobos"));
        return lobos.Length > 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Lobo") && !IsGrouped)
        {
            Morir();
            return;
        }

        if (other.CompareTag("Aldeano"))
        {
            // evita contar a sí mismo si por alguna razón colliders se detectan
            if (other.gameObject != this.gameObject)
                nearbyAldeanos++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Aldeano"))
        {
            if (other.gameObject != this.gameObject)
                nearbyAldeanos = Mathf.Max(0, nearbyAldeanos - 1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }

    void UpdateSpeed()
    {
        if (agent == null) return;
        if (age < 18) { agent.speed = 0; return; } // niños no salen
        if (age > 80) { agent.speed = 0; return; } // viejos no salen

        float t = Mathf.InverseLerp(18, 80, age);
        agent.speed = Mathf.Lerp(baseSpeed, minSpeed, t);
    }
}
