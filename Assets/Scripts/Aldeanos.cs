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
    public float moveSpeed = 2f;
    private Vector2 destino;

    [Header("Referencias")]
    public Aldea aldeaComp;
    public Bosque bosqueComp; // sólo referencia por defecto
    private Bosque bosqueDestino; // bosque elegido cuando sale
    private Casa casaActual;

    public AldeanoState currentState = AldeanoState.EnAldea;

    public bool isGrouped = false;
    private List<Aldeanos> grupoActual = new List<Aldeanos>();

    private float timer = 0f;
    private float tiempoEnAldea = 0f;
    public float tiempoMinimoEnAldea = 5f; // tiempo de descanso en la aldea

    void Start()
    {
        if (aldeaComp == null) aldeaComp = FindObjectOfType<Aldea>();
        if (bosqueComp == null) bosqueComp = FindObjectOfType<Bosque>();

        SimulationManager sim = FindObjectOfType<SimulationManager>();
        if (sim != null) sim.RegisterAldeano(this);

        // destino inicial dentro de la aldea (si hay aldea)
        if (aldeaComp != null)
            destino = (Vector2)aldeaComp.transform.position + Random.insideUnitCircle * (aldeaComp.radioAldea);

        CambiarEstado(AldeanoState.EnAldea);
    }

    public void Simulate(float deltaTime)
    {
        if (!isAlive || currentState == AldeanoState.Muerto) return;

        // Envejecer: 1 año cada 2 segundos
        timer += deltaTime;
        if (timer >= 2f)
        {
            timer = 0f;
            edad++;
            if (edad >= edadMaxima)
            {
                Morir();
                return;
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
            case AldeanoState.EnCasa: EstadoEnCasa(deltaTime); break;
            case AldeanoState.Grupo: EstadoGrupo(deltaTime); break;
            case AldeanoState.Muerto: /* No hace nada */ break;
        }
    }

    // ---------- Estados ----------
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
            if (edad >= 20 && edad <= 50)
            {
                CambiarEstado(AldeanoState.Reproducción);
                return;
            }

            // Si no, sale a recolectar
            if (edad >= 15 && edad <= 60)
            {
                CambiarEstado(AldeanoState.Saliendo);
            }
        }
    }

    void EstadoReproduccion(float dt)
    {
        // Si no tiene casa, buscar una disponible en la aldea
        if (casaActual == null)
        {
            Casa casaDisponible = BuscarCasaDisponible();
            if (casaDisponible != null)
                casaActual = casaDisponible;
            else
            {
                // No hay casa: se queda paseando en la aldea hasta la próxima decisión
                if (aldeaComp != null && Vector2.Distance(transform.position, destino) < 0.25f)
                    destino = (Vector2)aldeaComp.transform.position + Random.insideUnitCircle * (aldeaComp.radioAldea);
                MoverHacia(destino, dt);
                return;
            }
        }

        // Ir hacia la casa asignada
        destino = casaActual.transform.position;
        MoverHacia(destino, dt);

        // El cambio a EnCasa ocurre cuando entra al trigger de la casa (OnTriggerEnter2D)
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

    void EstadoEnCasa(float dt)
    {
        // Inmóvil; la lógica de reproducción la controla Casa.Simulate
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
        destino = aldeaComp.transform.position;
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

    // ---------- Utilidades ----------
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
        else if (nuevo == AldeanoState.EnCasa)
        {
            // inmóvil
            destino = (Vector2)transform.position;
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

    public void AsignarCasa(Casa casa)
    {
        casaActual = casa;
        if (casa != null)
        {
            CambiarEstado(AldeanoState.EnCasa);
        }
        else
        {
            // si se le quita la casa -> volver a EnAldea
            CambiarEstado(AldeanoState.EnAldea);
        }
    }

    Casa BuscarCasaDisponible()
    {
        if (aldeaComp == null) return null;

        foreach (var go in aldeaComp.casas)
        {
            Casa casa = go.GetComponent<Casa>();
            if (casa == null) continue;

            List<Aldeanos> residentes = casa.GetResidentes();

            // Caso 1: Casa vacía
            if (residentes.Count == 0)
                return casa;

            // Caso 2: Un residente de género opuesto
            if (residentes.Count == 1 && residentes[0].genero != this.genero)
                return casa;
        }

        return null; // No hay casa disponible
    }
}
