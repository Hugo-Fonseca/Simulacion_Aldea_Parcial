public enum AldeanoState
{
    EnAldea,        // esperando o movi�ndose dentro de la aldea
    Saliendo,       // empieza a dirigirse hacia el bosque
    Reproducci�n,   // buscando pareja en la aldea
    Recolectando,   // buscando �rbol y cort�ndolo
    Regresando,     // trayendo recursos a la aldea
    Huyendo,        // escapando de lobo
    Grupo,         // yendo hacia un grupo de aldeanos
    Muerto          // alcanz� edad m�xima o lo mat� un lobo
}
