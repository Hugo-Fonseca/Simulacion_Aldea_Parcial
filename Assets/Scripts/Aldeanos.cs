using UnityEngine;
using UnityEngine.Analytics;

public enum Genero { Hombre, Mujer }

public class Aldeanos : MonoBehaviour
{
    [Header("Identidad")]
    public Genero genero; // Hombre o Mujer

    [Header("Stats")]
    public float energy = 100;
    public float age = 0;
    public float maxAge = 100;
    public bool isAlive = true;

    [Header("Movimiento & Visión")]
    public float baseSpeed = 4.5f;
    public float minSpeed = 1.5f;
    public float visionRange = 5f;
    private float currentSpeed;

    // destino para moverse
    private Vector2 destination;

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
    public bool IsGrouped => nearbyAldeanos > 0;

    void Start()
    {
        // asignar género aleatorio si no viene en el prefab
        // (si en el inspector ya está puesto, no lo cambia)
        if (genero != Genero.Hombre && genero != Genero.Mujer)
            genero = (Random.value > 0.5f) ? Genero.Hombre : Genero.Mujer;

        destination = transform.position;
        CambiarEstado(AldeanosState.Espera);
    }

    // --- Este es el entrypoint que llama SimulationManager ---
    public void Simulate(float deltaTime)
    {
        if (!isAlive) return;

        h = deltaTime;
        Age();
        UpdateSpeed();

        // Si está en reproducción no hace nada (la casa gestiona el estado)
        if (currentState == AldeanosState.Reproduccion) return;

        // prioridad: huir si hay lobo cerca y no está en grupo
        if (LoboEnRango() && currentState != AldeanosState.Huir && !IsGrouped)
            CambiarEstado(AldeanosState.Huir);

        // manejo de grupo
        if (IsGrouped && currentState != AldeanosState.Grupo)
            CambiarEstado(AldeanosState.Grupo);
        else if (!IsGrouped && currentState == AldeanosState.Grupo)
            CambiarEstado(AldeanosState.Espera);

        // ejecutar lógica de estado (NOTA: no movemos directamente aquí, solo fijamos destino)
        switch (currentState)
        {
            case AldeanosState.Espera: EstadoEspera(); break;
            case AldeanosState.BuscarRecursos: EstadoBuscarRecursos(); break;
            case AldeanosState.Recolectando: EstadoRecolectando(); break;
            case AldeanosState.Depositando: EstadoDepositando(); break;
            case AldeanosState.Huir: EstadoHuir(); break;
            case AldeanosState.Grupo: EstadoGrupo(); break;
        }

        // mover hacia destination usando la velocidad calculada y el deltaTime recibido
        Move(h);
    }

    // ----------------- ESTADOS -----------------
    void EstadoEspera()
    {
        // recarga progresiva en la aldea
        if (aldea != null && Vector2.Distance(transform.position, aldea.position) < 2f)
            energy = Mathf.Min(100, energy + rechargeRate * h);

        // Solo pueden salir si tienen entre 18 y 80 años y energía suficiente
        if (age >= 18 && age <= 80 && energy >= 90 && !IsGrouped)
        {
            if (Random.value < 0.01f)
                CambiarEstado(AldeanosState.BuscarRecursos);
        }
        // se queda en su lugar (destination actual)
    }

    void EstadoBuscarRecursos()
    {
        if (bosque != null) SetDestination(bosque.position);

        if (bosque != null && Vector2.Distance(transform.position, bosque.position) < 2f)
            CambiarEstado(AldeanosState.Recolectando);
    }

    void EstadoRecolectando()
    {
        // Aquí idealmente buscas un Arbol cercano y llamas a Arbol.Recolectar(this).
        // Para mantenerlo simple aumentamos recursos y consumimos energía.
        recursosRecolectados = Mathf.Min(maxRecursos, recursosRecolectados + 1);
        energy -= 0.1f;

        if (recursosRecolectados >= maxRecursos)
            CambiarEstado(AldeanosState.Depositando);
    }

    void EstadoDepositando()
    {
        if (aldea != null) SetDestination(aldea.position);

        if (aldea != null && Vector2.Distance(transform.position, aldea.position) < 2f)
        {
            Aldea aldeaComp = aldea.GetComponent<Aldea>();
            if (aldeaComp != null)
                aldeaComp.DepositarRecursos(recursosRecolectados);

            recursosRecolectados = 0;
            CambiarEstado(AldeanosState.Espera);
        }
    }

    void EstadoHuir()
    {
        if (aldea != null) SetDestination(aldea.position);

        if (aldea != null && Vector2.Distance(transform.position, aldea.position) < 2f)
            CambiarEstado(AldeanosState.Espera);
    }

    void EstadoGrupo()
    {
        // queda prácticamente quieto (puedes ajustar para que se muevan juntos)
        SetDestination(transform.position);
    }

    // ----------------- MOVIMIENTO -----------------
    // en vez de mover directamente al setear destino, guardamos el destino y movemos en Move()
    void SetDestination(Vector3 dest)
    {
        destination = dest;
    }

    void Move(float deltaTime)
    {
        // si la velocidad es 0 no se mueve (niños/ancianos)
        if (currentSpeed <= 0f) return;

        Vector2 pos = transform.position;
        if ((Vector2)pos == destination) return;

        Vector2 nuevaPos = Vector2.MoveTowards(pos, destination, currentSpeed * deltaTime);
        transform.position = (Vector3)nuevaPos;

        // gasto de energía por movimiento
        energy = Mathf.Max(0f, energy - currentSpeed * deltaTime * 0.1f);
    }

    // ----------------- UTILIDADES -----------------
    void CambiarEstado(AldeanosState nuevo)
    {
        currentState = nuevo;
    }

    void Age()
    {
        age += h;
        // condición extra: si pasa de 100 años muere (si querías 100 en vez de maxAge)
        if (age > maxAge || energy <= 0)
            Morir();
    }

    public void Morir()
    {
        if (!isAlive) return;
        isAlive = false;
        Destroy(gameObject);
    }

    bool LoboEnRango()
    {
        Collider2D[] lobos = Physics2D.OverlapCircleAll(transform.position, visionRange, LayerMask.GetMask("Lobos"));
        return lobos.Length > 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // si un lobo colisiona y no está en grupo → muere
        if (other.CompareTag("Lobo") && !IsGrouped)
        {
            Morir();
            return;
        }

        if (other.CompareTag("Aldeano") && other.gameObject != this.gameObject)
            nearbyAldeanos++;
    }

    private void OnTriggerExit2D(Collider2D other) // cuando otro aldeano sale del rango
    {
        if (other.CompareTag("Aldeano") && other.gameObject != this.gameObject)
            nearbyAldeanos = Mathf.Max(0, nearbyAldeanos - 1);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, destination);
    }

    void UpdateSpeed()
    {
        // Si no pueden salir, velocidad 0
        if (age < 18 || age > 80) { currentSpeed = 0; return; }

        float t = Mathf.InverseLerp(18, 80, age);
        currentSpeed = Mathf.Lerp(baseSpeed, minSpeed, t);
    }
}
