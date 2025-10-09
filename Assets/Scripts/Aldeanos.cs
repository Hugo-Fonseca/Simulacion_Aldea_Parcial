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
    public Bosque bosqueComp;
    private Casa casaActual;

    public AldeanoState currentState = AldeanoState.EnAldea;

    public bool isGrouped = false;
    private List<Aldeanos> grupoActual = new List<Aldeanos>();

    private float timer = 0f;

    void Start()
    {
        if (aldeaComp == null) aldeaComp = FindObjectOfType<Aldea>();
        if (bosqueComp == null) bosqueComp = FindObjectOfType<Bosque>();

        SimulationManager sim = FindObjectOfType<SimulationManager>();
        if (sim != null) sim.RegisterAldeano(this);

        CambiarEstado(AldeanoState.EnAldea);
    }

    public void Simulate(float deltaTime)
    {
        if (!isAlive) return;

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
            case AldeanoState.Recolectando: EstadoRecolectando(deltaTime); break;
            case AldeanoState.Regresando: EstadoRegresando(deltaTime); break;
            case AldeanoState.Huyendo: EstadoHuyendo(deltaTime); break;
            case AldeanoState.EnCasa: /* Casa controla el tiempo */ break;
            case AldeanoState.Grupo: EstadoGrupo(deltaTime); break;
        }
    }

    // ---------- Estados ----------
    void EstadoEnAldea(float dt)
    {
        if (Vector2.Distance(transform.position, destino) < 0.25f)
            destino = (Vector2)aldeaComp.transform.position + Random.insideUnitCircle * 3f;

        MoverHacia(destino, dt);

        if (Random.value < 0.001f)
        {
            if (edad >= 15 && edad <= 60)
                CambiarEstado(AldeanoState.Saliendo);
        }
    }

    void EstadoSaliendo(float dt)
    {
        if (bosqueComp == null) { CambiarEstado(AldeanoState.EnAldea); return; }

        destino = (Vector2)bosqueComp.transform.position + Random.insideUnitCircle * bosqueComp.radioBosque;
        MoverHacia(destino, dt);

        if (Vector2.Distance(transform.position, destino) < 0.5f)
        {
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
        destino = aldeaComp.transform.position;
        MoverHacia(destino, dt);

        if (Vector2.Distance(transform.position, aldeaComp.transform.position) < 1.5f)
        {
            CambiarEstado(AldeanoState.EnAldea);
        }
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
    }

    public void Morir()
    {
        isAlive = false;
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
    }
}
