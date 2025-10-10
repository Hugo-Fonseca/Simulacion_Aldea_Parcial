using System.Collections.Generic;
using UnityEngine;

public enum Genero { Hombre, Mujer }

[RequireComponent(typeof(Collider2D))]
public class Aldeanos : MonoBehaviour
{
    [Header("Stats")]
    public int vida = 5;
    public int edad = 0;
    public int edadMaxima = 100;
    public bool isAlive = true;

    [Header("Datos del aldeano")]
    public Genero genero;

    [Header("Recolección")]
    public int recursosRecolectados = 0;
    public int capacidadRecursos = 10;

    [Header("Movimiento")]
    public float moveSpeed = 3f;
    private Vector2 destino;

    [Header("Referencias")]
    public Aldea aldeaComp;
    public Bosque bosqueComp; // sólo referencia por defecto
    private Bosque bosqueDestino; // bosque elegido cuando sale
   

    [Header("Percepción")]
    public float rangoVision = 5f;

    public AldeanoState currentState = AldeanoState.EnAldea;

    public bool isGrouped = false;
    private List<Aldeanos> grupoActual = new List<Aldeanos>();

    private float timer = 0f;
    private float tiempoEnAldea = 0f;
    private float velocidadOriginal;
    private float tiempoBoost = 2f; // Duración del boost de velocidad
    private float timerBoost = 0f;
    private bool enBoost = false;
    public float tiempoMinimoEnAldea = 5f;

    void Start()
    {
        if (aldeaComp == null) aldeaComp = FindObjectOfType<Aldea>();
        if (bosqueComp == null) bosqueComp = FindObjectOfType<Bosque>();

        SimulationManager sim = FindObjectOfType<SimulationManager>();
        if (sim != null) sim.RegisterAldeano(this);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.isKinematic = true;
        }

        velocidadOriginal = moveSpeed; // 🔹 Guardamos la velocidad original

        if (aldeaComp != null)
            destino = (Vector2)aldeaComp.transform.position + Random.insideUnitCircle * (aldeaComp.radioAldea);

        CambiarEstado(AldeanoState.EnAldea);
    }


    public void Simulate(float deltaTime)
    {
        if (!isAlive || currentState == AldeanoState.Muerto) return;

        // Manejo del boost de velocidad
        if (enBoost)
        {
            timerBoost += deltaTime;
            if (timerBoost >= tiempoBoost)
            {
                moveSpeed = velocidadOriginal; // Volver a la velocidad normal
                enBoost = false;
            }
        }

        timer += deltaTime;
        if (timer >= 4f)
        {
            timer = 0f;
            edad++;
            if (edad >= edadMaxima)
            {
                Morir();
                return;
            }
        }

        Lobos[] lobos = FindObjectsOfType<Lobos>();
        float distanciaMasCercana = Mathf.Infinity;
        Lobos loboCercano = null;

        foreach (var lobo in lobos)
        {
            float distLobo = Vector2.Distance(transform.position, lobo.transform.position);
            if (distLobo < distanciaMasCercana)
            {
                distanciaMasCercana = distLobo;
                loboCercano = lobo;
            }
        }

        if (loboCercano != null && distanciaMasCercana < rangoVision)
        {
            // Si recién detectó un lobo, cambia a huir y genera un destino de escape
            if (currentState != AldeanoState.Huyendo)
            {
                CambiarEstado(AldeanoState.Huyendo);
                Vector2 fleeDir = ((Vector2)transform.position - (Vector2)loboCercano.transform.position).normalized;
                if (fleeDir.sqrMagnitude < 0.001f) fleeDir = Random.insideUnitCircle.normalized;
                destino = (Vector2)transform.position + fleeDir * 5f;

                moveSpeed = 2f; // Aumentar la velocidad al huir
                enBoost = true;
                timerBoost = 0f;
            }
            else
            {
                // Solo cada cierto tiempo recalcula el destino
                timer += deltaTime;
                if (timer > 1.5f)
                {
                    timer = 0f;
                    Vector2 fleeDir = ((Vector2)transform.position - (Vector2)loboCercano.transform.position).normalized;
                    destino = (Vector2)transform.position + fleeDir * 5f;
                }
            }
        }

        switch (currentState)
        {
            case AldeanoState.EnAldea: EstadoEnAldea(deltaTime); break;
            case AldeanoState.Saliendo: EstadoSaliendo(deltaTime); break;
            case AldeanoState.Reproducción: EstadoReproduccion(deltaTime); break;
            case AldeanoState.Recolectando: EstadoRecolectando(deltaTime); break;
            case AldeanoState.Regresando: EstadoRegresando(deltaTime); break;
            case AldeanoState.Huyendo: EstadoHuyendo(deltaTime); break;
            case AldeanoState.Grupo: EstadoGrupo(deltaTime); break;
            case AldeanoState.Muerto: break;
        }
    }

    void EstadoEnAldea(float dt)
    {
        // Explorar dentro de un área amplia de la aldea
        if (aldeaComp != null)
        {
            if (Vector2.Distance(transform.position, destino) < 0.25f)
                destino = (Vector2)aldeaComp.transform.position + Random.insideUnitCircle * (aldeaComp.radioAldea);
        }

        MoverHacia(destino, dt);

        tiempoEnAldea += dt;
        if (tiempoEnAldea >= tiempoMinimoEnAldea)
        {
            tiempoEnAldea = 0f;

            // Si está en edad fértil -> ir a reproducirse (buscar casa)
            if (edad >= 20 && edad <= 60)
            {
                CambiarEstado(AldeanoState.Reproducción);
                return;
            }

            // Si no, sale a recolectar
            if (edad >= 15 && edad <= 80)
            {
                CambiarEstado(AldeanoState.Saliendo);
            }
        }
    }

    void EstadoReproduccion(float dt)
    {
        if (aldeaComp != null)
        {
            // Si no tengo destino aún o ya llegué al destino → buscar la casa más cercana
            if (destino == Vector2.zero || Vector2.Distance(transform.position, destino) < 0.25f)
            {
                Casa casaMasCercana = BuscarCasaMasCercana();
                if (casaMasCercana != null)
                    destino = casaMasCercana.transform.position;
            }
        }

        // Mover hacia el destino (la casa más cercana)
        MoverHacia(destino, dt);
    }

    Casa BuscarCasaMasCercana()
    {
        Casa[] casas = FindObjectsOfType<Casa>();
        Casa cercana = null;
        float distMin = Mathf.Infinity;

        foreach (var c in casas)
        {
            float d = Vector2.Distance(transform.position, c.transform.position);
            if (d < distMin)
            {
                distMin = d;
                cercana = c;
            }
        }
        return cercana;
    }



    void EstadoSaliendo(float dt)
    {
        // Elegir un bosque aleatorio (si hay varios)
        if (bosqueDestino == null)
        {
            Bosque[] bosques = FindObjectsOfType<Bosque>();
            if (bosques.Length == 0) { CambiarEstado(AldeanoState.EnAldea); return; }
            bosqueDestino = bosques[Random.Range(0, bosques.Length)];
            destino = (Vector2)bosqueDestino.transform.position + Random.insideUnitCircle * bosqueDestino.radioBosque;
        }

        MoverHacia(destino, dt);

        if (Vector2.Distance(transform.position, destino) < 0.5f)
        {
            bosqueDestino = null;
            CambiarEstado(AldeanoState.Recolectando);
        }
    }

    void EstadoRecolectando(float dt)
    {
        Arbol[] arboles = FindObjectsOfType<Arbol>();
        Arbol masCercano = null;
        float distMin = Mathf.Infinity;

        foreach (var arbol in arboles)
        {
            float d = Vector2.Distance(transform.position, arbol.transform.position);
            if (d < distMin)
            {
                distMin = d;
                masCercano = arbol;
            }
        }

        if (masCercano != null)
        {
            MoverHacia(masCercano.transform.position, dt);

            if (Vector2.Distance(transform.position, masCercano.transform.position) < 0.5f)
            {
                masCercano.Recolectar(this);
                if (recursosRecolectados >= capacidadRecursos)
                {
                    CambiarEstado(AldeanoState.Regresando);
                }
            }
        }
        else
        {
            CambiarEstado(AldeanoState.Regresando);
        }
    }

    void EstadoRegresando(float dt)
    {
        MoverHacia(aldeaComp.transform.position, dt);

        if (Vector2.Distance(transform.position, aldeaComp.transform.position) < 1.5f)
        {
            aldeaComp.DepositarRecursos(recursosRecolectados);
            recursosRecolectados = 0;
            CambiarEstado(AldeanoState.EnAldea);
        }
    }

    void EstadoHuyendo(float dt)
    {
        if (aldeaComp == null) return;

        destino = (Vector2)aldeaComp.transform.position + Random.insideUnitCircle * 0.5f;

        MoverHacia(destino, dt);

        if (Vector2.Distance(transform.position, aldeaComp.transform.position) < 1.5f)
            CambiarEstado(AldeanoState.EnAldea);
    }

    void EstadoGrupo(float dt)
    {
        if (grupoActual == null || grupoActual.Count == 0)
        {
            isGrouped = false;
            CambiarEstado(AldeanoState.EnAldea);
            return;
        }

        Aldeanos lider = grupoActual[0];
        foreach (var a in grupoActual)
        {
            a.MoverHacia(lider.destino, dt);
        }

        // al llegar a la aldea se separan
        if (Vector2.Distance(transform.position, aldeaComp.transform.position) < 1.5f)
        {
            foreach (var a in grupoActual)
            {
                a.isGrouped = false;
                a.grupoActual = null;
                a.CambiarEstado(AldeanoState.EnAldea);
            }
            grupoActual.Clear();
        }
    }

    void MoverHacia(Vector2 target, float dt)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.position += (Vector3)(dir * moveSpeed * dt);
    }

    public void CambiarEstado(AldeanoState nuevo)
    {
        currentState = nuevo;

        // inicializaciones por estado
        if (nuevo == AldeanoState.EnAldea)
        {
            tiempoEnAldea = 0f;
            // nuevo destino de exploración en la aldea
            if (aldeaComp != null) destino = (Vector2)aldeaComp.transform.position + Random.insideUnitCircle * (aldeaComp.radioAldea);
        }
        else if (nuevo == AldeanoState.Saliendo)
        {
            bosqueDestino = null; // se elegirá al entrar en EstadoSaliendo
        }
    }

    public void Morir()
    {
        isAlive = false;
        currentState = AldeanoState.Muerto;

        SimulationManager sim = FindObjectOfType<SimulationManager>();
        if (sim != null) sim.RemoveAldeano(this);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangoVision);
    }
}
