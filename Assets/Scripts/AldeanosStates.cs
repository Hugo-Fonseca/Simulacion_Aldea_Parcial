public enum AldeanoState
{
    EnAldea,        // esperando o moviéndose dentro de la aldea
    Saliendo,       // empieza a dirigirse hacia el bosque
    Reproducción,   // buscando pareja en la aldea
    Recolectando,   // buscando árbol y cortándolo
    Regresando,     // trayendo recursos a la aldea
    Huyendo,        // escapando de lobo
    Grupo,         // yendo hacia un grupo de aldeanos
    Muerto          // alcanzó edad máxima o lo mató un lobo
}
