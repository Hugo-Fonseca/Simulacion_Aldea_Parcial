using UnityEngine;

public class Lobos : MonoBehaviour
{
    public LoboState currentState = LoboState.Explorando;

    public float speed = 3f;
    public float visionRange = 5f;
    public float attackRange = 1f;

    private Transform targetPrey;   // aldeano que persigue
    private Vector3 destination;

    void Update()
    {
        switch (currentState)
        {
            case LoboState.Explorando:
                Explore();
                break;
            case LoboState.Persiguiendo:
                Chase();
                break;
            case LoboState.Atacando:
                Attack();
                break;
            case LoboState.EvitandoGrupo:
                AvoidGroup();
                break;
            case LoboState.CazaFallida:
                FailHunt();
                break;
        }
    }

    void Explore()
    {
        if (Vector3.Distance(transform.position, destination) < 0.5f)
        {
            SelectNewDestination();
        }
        MoveTowards(destination);

        // Detectar aldeanos dentro de rango
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRange, LayerMask.GetMask("Aldeanos"));

        foreach (var h in hits)
        {
            Aldeanos aldeano = h.GetComponent<Aldeanos>();
            if (aldeano != null)
            {
                if (!aldeano.IsGrouped) // aldeano solitario
                {
                    targetPrey = aldeano.transform;
                    currentState = LoboState.Persiguiendo;
                    return;
                }
                else // aldeano en grupo
                {
                    targetPrey = aldeano.transform;
                    currentState = LoboState.EvitandoGrupo;
                    return;
                }
            }
        }
    }

    void Chase()
    {
        if (targetPrey == null)
        {
            currentState = LoboState.CazaFallida;
            return;
        }

        MoveTowards(targetPrey.position);

        float dist = Vector3.Distance(transform.position, targetPrey.position);
        if (dist <= attackRange)
        {
            currentState = LoboState.Atacando;
        }
        else if (dist > visionRange * 1.5f) // perdió a la presa
        {
            currentState = LoboState.CazaFallida;
        }
    }

    void Attack()
    {
        if (targetPrey != null)
        {
            Aldeanos aldeano = targetPrey.GetComponent<Aldeanos>();
            if (aldeano != null && !aldeano.IsGrouped) // solo ataca si está solo
            {
                aldeano.Morir(); // utiliza la función pública del aldeano
            }
        }
        targetPrey = null;
        currentState = LoboState.Explorando;
    }

    void AvoidGroup()
    {
        if (targetPrey != null)
        {
            Vector3 awayDir = (transform.position - targetPrey.position).normalized;
            MoveTowards(transform.position + awayDir * 3f);
        }
        targetPrey = null;
        currentState = LoboState.Explorando;
    }

    void FailHunt()
    {
        targetPrey = null;
        SelectNewDestination();
        currentState = LoboState.Explorando;
    }

    void SelectNewDestination()
    {
        Vector2 randomPos = Random.insideUnitCircle * 5f; // tamaño bosque
        destination = new Vector3(randomPos.x, randomPos.y, 0);
    }

    void MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
