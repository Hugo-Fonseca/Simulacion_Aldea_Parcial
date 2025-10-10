using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Lobos : MonoBehaviour
{
    [Header("Vida")]
    public int edad = 0;
    public int edadMaxima = 40;
    private float edadTimer = 0f;

    [Header("Movimiento")]
    public float moveSpeed = 2f;
    public float visionRange = 5f;

    [Header("Ataque")]
    public float attackRange = 1f;
    public float attackCooldown = 1.0f;

    [Header("Aldea (evitación)")]
    public Aldea aldeaComp;              // referencia (asignar en inspector o se busca)
    public float fleeDistance = 2f;      // distancia de huida
    public float avoidMargin = 1f;       // margen extra alrededor de la zona de la aldea

    public LoboState currentState = LoboState.Patrullar;
    private Aldeanos objetivo;
    private Vector2 destino;
    private float timer = 0f;

    // proteccion contra toggles rápidos
    private float stateLock = 0f;
    public float stateLockDuration = 0.25f;

    // collider de la zona de la aldea (trigger)
    private Collider2D zonaAldeaCollider = null;

    void Start()
    {
        // si no se asignó la aldea, buscarla
        if (aldeaComp == null) aldeaComp = FindObjectOfType<Aldea>();

        // si Aldea tiene un campo publico zonaAldeaCollider, úsalo; si no, buscar CircleCollider2D en Aldea
        if (aldeaComp != null)
        {
            // intenta usar la propiedad pública si la tienes
            var field = aldeaComp.GetType().GetField("zonaAldeaCollider");
            if (field != null)
            {
                zonaAldeaCollider = field.GetValue(aldeaComp) as Collider2D;
            }

            // fallback: busca un CircleCollider2D en el GameObject Aldea
            if (zonaAldeaCollider == null)
            {
                zonaAldeaCollider = aldeaComp.GetComponent<CircleCollider2D>();
            }
        }

        // asegurarnos de que haya un Rigidbody2D (necesario para que OnTriggerEnter2D funcione)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        else
        {
            // configurarlo como kinemático si no lo está (seguro para agentes controlados por script)
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // registrar en SimulationManager si lo usas
        SimulationManager sim = FindObjectOfType<SimulationManager>();
        if (sim != null) sim.RegisterLobo(this);

        // empezar patrullando
        destino = (Vector2)transform.position + Random.insideUnitCircle * 4f;
        CambiarEstado(LoboState.Patrullar);
    }

    // método llamado por SimulationManager
    public void Simulate(float deltaTime)
    {
        if (stateLock > 0f) stateLock -= deltaTime;

        // envejecimiento
        edadTimer += deltaTime;
        if (edadTimer >= 4f)
        {
            edadTimer = 0f;
            edad++;
            if (edad >= edadMaxima)
            {
                SimulationManager sim = FindObjectOfType<SimulationManager>();
                if (sim != null) sim.RemoveLobo(this);
                Destroy(gameObject);
                return;
            }
        }

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

    void EstadoPatrullar(float dt)
    {
        if (Vector2.Distance(transform.position, destino) < 0.25f)
            destino = (Vector2)transform.position + Random.insideUnitCircle * 4f;

        MoverHacia(destino, dt);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, visionRange);
        Aldeanos mejor = null;
        foreach (var c in hits)
        {
            Aldeanos a = c.GetComponent<Aldeanos>();
            if (a != null && a.isAlive)
            {
                if (mejor == null || Vector2.Distance(transform.position, a.transform.position) < Vector2.Distance(transform.position, mejor.transform.position))
                    mejor = a;
            }
        }

        if (mejor != null)
        {
            objetivo = mejor;
            CambiarEstado(LoboState.Perseguir);
        }
    }

    void EstadoPerseguir(float dt)
    {
        if (objetivo == null || !objetivo.isAlive)
        {
            CambiarEstado(LoboState.Patrullar);
            return;
        }

        float dist = Vector2.Distance(transform.position, objetivo.transform.position);
        if (dist <= attackRange)
        {
            CambiarEstado(LoboState.Atacar);
            return;
        }

        // si el aldeano entra en la zona de la aldea, caza fallida (o huir)
        if (zonaAldeaCollider != null)
        {
            Vector2 center = (Vector2)zonaAldeaCollider.transform.position + (zonaAldeaCollider is CircleCollider2D cc ? (Vector2)cc.offset : Vector2.zero);
            float radius = 0f;
            if (zonaAldeaCollider is CircleCollider2D cc2)
                radius = cc2.radius * Mathf.Max(cc2.transform.lossyScale.x, cc2.transform.lossyScale.y);

            if (radius > 0f && Vector2.Distance(objetivo.transform.position, center) <= radius)
            {
                CambiarEstado(LoboState.CazaFallida);
                return;
            }
        }

        destino = objetivo.transform.position;
        MoverHacia(destino, dt);
    }

    void EstadoAtacar(float dt)
    {
        if (objetivo == null || !objetivo.isAlive) { CambiarEstado(LoboState.Patrullar); return; }

        float dist = Vector2.Distance(transform.position, objetivo.transform.position);
        if (dist > attackRange) { CambiarEstado(LoboState.Perseguir); return; }

        timer += dt;
        if (timer >= attackCooldown)
        {
            timer = 0f;
            objetivo.Morir();
            objetivo = null;
            CambiarEstado(LoboState.Comer);
        }
    }

    void EstadoComer(float dt)
    {
        timer += dt;
        if (timer >= 2f) CambiarEstado(LoboState.Patrullar);
    }

    void EstadoHuir(float dt)
    {
        // mover hacia destino de huida
        MoverHacia(destino, dt);

        timer += dt;
        if (timer >= 3f) CambiarEstado(LoboState.Patrullar);
    }

    void EstadoCazaFallida(float dt)
    {
        destino = (Vector2)transform.position + Random.insideUnitCircle * 3f;
        MoverHacia(destino, dt);

        timer += dt;
        if (timer > 2f) CambiarEstado(LoboState.Patrullar);
    }

    void MoverHacia(Vector2 target, float dt)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.position += (Vector3)(dir * moveSpeed * dt);
    }

    void CambiarEstado(LoboState nuevo)
    {
        if (currentState == nuevo) return;
        currentState = nuevo;
        timer = 0f;
        stateLock = stateLockDuration;

        if (nuevo == LoboState.Patrullar) destino = (Vector2)transform.position + Random.insideUnitCircle * 4f;
        else if (nuevo == LoboState.CazaFallida) destino = (Vector2)transform.position + Random.insideUnitCircle * 3f;
    }

    // ---- TRIGGERS ----
    private void OnTriggerEnter2D(Collider2D other)
    {
        // si es el collider asignado a la Aldea -> huir inmediatamente
        if (aldeaComp != null && zonaAldeaCollider != null && other == zonaAldeaCollider)
        {
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)aldeaComp.transform.position).normalized;
            if (fleeDir.sqrMagnitude < 0.001f) fleeDir = Random.insideUnitCircle.normalized;
            destino = (Vector2)transform.position + fleeDir * fleeDistance;
            CambiarEstado(LoboState.Huir);
            Debug.Log($"Lobo {name} entró en zona Aldea → Huir");
            return;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (aldeaComp != null && zonaAldeaCollider != null && other == zonaAldeaCollider)
        {
            CambiarEstado(LoboState.Patrullar);
            Debug.Log($"Lobo {name} salió de zona Aldea → Patrullar");
        }
    }

    // para debug visual en editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        if (zonaAldeaCollider is CircleCollider2D cc)
        {
            Gizmos.color = Color.green;
            Vector2 center = (Vector2)cc.transform.position + cc.offset;
            float worldRadius = cc.radius * Mathf.Max(cc.transform.lossyScale.x, cc.transform.lossyScale.y);
            Gizmos.DrawWireSphere(center, worldRadius + avoidMargin);
        }
    }
}
