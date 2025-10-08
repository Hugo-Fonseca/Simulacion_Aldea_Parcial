using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Lobos : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 2f;
    public float visionRange = 5f;

    [Header("Ataque")]
    public float attackRange = 1f;
    public float attackCooldown = 1.0f;

    [Header("Aldea (evitación)")]
    public Aldea aldeaComp;              // componente Aldea (mejor asignar en el prefab/inspector)
    public float fleeDistance = 5f;      // distancia a la que huye desde su posición actual
    public float rangoAldea = 1.0f;      // umbral (en unidades) desde el borde del collider de la aldea para activar huida

    // estado interno
    public LoboState currentState = LoboState.Patrullar;
    private Aldeanos objetivo;
    private Vector2 destino;             // destino actual (patrulla / huida / persecución)
    private float timer = 0f;

    // referencia directa al collider de la aldea (si existe)
    private Collider2D aldeaCollider;

    void Start()
    {
        // intentar auto-asignar la aldea si no se asignó en el inspector
        if (aldeaComp == null)
        {
            aldeaComp = FindObjectOfType<Aldea>();
        }
        if (aldeaComp != null)
        {
            aldeaCollider = aldeaComp.GetComponent<Collider2D>();
        }

        // registrarse en el SimulationManager si existe
        SimulationManager sim = FindObjectOfType<SimulationManager>();
        if (sim != null) sim.RegisterLobo(this);

        CambiarEstado(LoboState.Patrullar);
    }

    // método que llama SimulationManager
    public void Simulate(float deltaTime)
    {
        // ========== Chequeo PRIORITARIO: si estamos cerca/encima del collider de la aldea → huir ==========
        if (aldeaCollider != null)
        {
            // ClosestPoint devuelve el punto del collider más cercano a la posición del lobo
            Vector2 closest = aldeaCollider.ClosestPoint(transform.position);
            float distToAldeaEdge = Vector2.Distance(transform.position, closest); // 0 si está dentro

            if (distToAldeaEdge <= rangoAldea)
            {
                // si no está ya en Huir, calcular destino de huida y forzar Huir
                if (currentState != LoboState.Huir)
                {
                    Vector2 fleeDir = ((Vector2)transform.position - (Vector2)aldeaComp.transform.position).normalized;
                    if (fleeDir.sqrMagnitude < 0.001f) fleeDir = Random.insideUnitCircle.normalized;
                    destino = (Vector2)transform.position + fleeDir * fleeDistance;
                    CambiarEstado(LoboState.Huir);
                }

                // ejecutar estado Huir inmediatamente (prioritario)
                EstadoHuir(deltaTime);
                return;
            }
        }

        // ========== comportamiento normal según estado ==========
        switch (currentState)
        {
            case LoboState.Patrullar: EstadoPatrullar(deltaTime); break;
            case LoboState.Perseguir: EstadoPerseguir(deltaTime); break;
            case LoboState.Atacar: EstadoAtacar(deltaTime); break;
            case LoboState.Comer: EstadoComer(deltaTime); break;
            case LoboState.Huir: EstadoHuir(deltaTime); break;
            case LoboState.CazaFallida: EstadoCazaFallida(deltaTime); break;
        }
    }

    // ---------- Estados ----------
    void EstadoPatrullar(float dt)
    {
        if (Vector2.Distance(transform.position, destino) < 0.25f)
            destino = (Vector2)transform.position + Random.insideUnitCircle * 4f;

        MoverHacia(destino, dt);

        // buscar aldeanos solitarios en visionRange
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange, LayerMask.GetMask("Aldeanos"));
        foreach (var c in hits)
        {
            Aldeanos a = c.GetComponent<Aldeanos>();
            if (a != null && a.isAlive && !a.IsGrouped)
            {
                objetivo = a;
                CambiarEstado(LoboState.Perseguir);
                break;
            }
        }
    }

    void EstadoPerseguir(float dt)
    {
        if (objetivo == null || !objetivo.isAlive)
        {
            CambiarEstado(LoboState.Patrullar);
            return;
        }

        // Si el objetivo se mete en la aldea -> caza fallida (no entrar)
        if (aldeaCollider != null && aldeaCollider.OverlapPoint(objetivo.transform.position))
        {
            CambiarEstado(LoboState.CazaFallida);
            return;
        }

        float dist = Vector2.Distance(transform.position, objetivo.transform.position);
        if (dist <= attackRange)
        {
            CambiarEstado(LoboState.Atacar);
            return;
        }

        // Si el lobo se está acercando demasiado a la aldea mientras persigue -> huir
        if (aldeaCollider != null)
        {
            Vector2 closest = aldeaCollider.ClosestPoint(transform.position);
            float distToAldeaEdge = Vector2.Distance(transform.position, closest);
            if (distToAldeaEdge <= rangoAldea)
            {
                Vector2 fleeDir = ((Vector2)transform.position - (Vector2)aldeaComp.transform.position).normalized;
                if (fleeDir.sqrMagnitude < 0.001f) fleeDir = Random.insideUnitCircle.normalized;
                destino = (Vector2)transform.position + fleeDir * fleeDistance;
                CambiarEstado(LoboState.Huir);
                return;
            }
        }

        MoverHacia(objetivo.transform.position, dt);
    }

    void EstadoAtacar(float dt)
    {
        if (objetivo == null || !objetivo.isAlive)
        {
            CambiarEstado(LoboState.Patrullar);
            return;
        }

        float dist = Vector2.Distance(transform.position, objetivo.transform.position);
        if (dist > attackRange)
        {
            CambiarEstado(LoboState.Perseguir);
            return;
        }

        timer += dt;
        if (timer >= attackCooldown)
        {
            timer = 0f;
            // comer: destruir al aldeano
            objetivo.isAlive = false;
            Destroy(objetivo.gameObject);

            objetivo = null;
            CambiarEstado(LoboState.Comer);
        }
    }

    void EstadoComer(float dt)
    {
        timer += dt;
        if (timer >= 2f) // ejemplo: 2s comiendo
            CambiarEstado(LoboState.Patrullar);
    }

    void EstadoHuir(float dt)
    {
        // recalcular siempre un destino de huida mientras huye
        if (aldeaComp != null)
        {
            Vector2 dir = ((Vector2)transform.position - (Vector2)aldeaComp.transform.position).normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Random.insideUnitCircle.normalized;
            destino = (Vector2)transform.position + dir * fleeDistance;
        }

        MoverHacia(destino, dt);

        timer += dt;

        // condición 1: ya se alejó lo suficiente
        if (aldeaCollider != null)
        {
            Vector2 closest = aldeaCollider.ClosestPoint(transform.position);
            float distToAldeaEdge = Vector2.Distance(transform.position, closest);
            if (distToAldeaEdge > rangoAldea + 1f)
            {
                CambiarEstado(LoboState.Patrullar);
                return;
            }
        }

        // condición 2: estuvo huyendo más de 3s
        if (timer >= 3f)
        {
            CambiarEstado(LoboState.Patrullar);
        }
    }


    void EstadoCazaFallida(float dt)
    {
        // frustrado: moverse un rato y volver a patrullar
        destino = (Vector2)transform.position + Random.insideUnitCircle * 3f;
        MoverHacia(destino, dt);

        timer += dt;
        if (timer > 2f)
            CambiarEstado(LoboState.Patrullar);
    }

    // ---------- utilidades ----------
    void MoverHacia(Vector2 target, float dt)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.position += (Vector3)(dir * moveSpeed * dt);
    }

    void CambiarEstado(LoboState nuevo)
    {
        currentState = nuevo;
        timer = 0f;

        if (nuevo == LoboState.Patrullar)
        {
            destino = (Vector2)transform.position + Random.insideUnitCircle * 4f;
        }
        else if (nuevo == LoboState.CazaFallida)
        {
            destino = (Vector2)transform.position + Random.insideUnitCircle * 3f;
        }
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        if (aldeaComp != null)
        {
            Gizmos.color = Color.green;
            // dibuja la "zona de evitación" alrededor del collider (aprox usando transform)
            Gizmos.DrawWireSphere(aldeaComp.transform.position, rangoAldea);
        }
    }
}
