public enum LoboState
{
    Patrullar,     // Camina aleatoriamente en el bosque
    Perseguir,     // Ha detectado un aldeano y lo persigue
    Atacar,        // Si alcanza al aldeano
    Comer,         // Se queda quieto un tiempo tras matar
    Huir,          // Si se enfrenta a un grupo de aldeanos o entra en la aldea
    CazaFallida    // Si el aldeano escapa a la aldea o pasa el tiempo de persecución
}
