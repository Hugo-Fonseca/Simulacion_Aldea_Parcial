using UnityEngine;

public class Arbol : MonoBehaviour
{
    public int recursos = 5; // cantidad de recursos que da este árbol

    public void Recolectar(Aldeanos aldeano)
    {
        if (recursos > 0)
        {
            aldeano.recursosRecolectados += recursos;
            recursos = 0;
            Destroy(gameObject); // el árbol desaparece
        }
    }
}
