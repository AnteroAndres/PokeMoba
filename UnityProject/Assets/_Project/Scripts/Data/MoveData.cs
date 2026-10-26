namespace PokeMoba.Data
{
    public enum MoveCategory { Fisico, Especial }

    /// <summary>Plantilla de forma: define cómo se ejecuta el movimiento en tiempo real.</summary>
    public enum MoveShape
    {
        Melee,       // golpe corto delante
        Projectile,  // proyectil en línea recta
        Cone,        // cono delante del lanzador
        GroundArea,  // círculo en el suelo con aviso previo
        Dash         // embestida que golpea al primero que toca
    }

    public enum StatusEffect { None, Quemado, Envenenado }

    /// <summary>
    /// Datos de un movimiento. En el prototipo se crean por código (PrototypeDex);
    /// en la fase 2 pasarán a ScriptableObjects generados desde PokeAPI.
    /// </summary>
    public class MoveData
    {
        public string Name;
        public PokemonType Type;
        public MoveCategory Category;
        public int Power;
        public MoveShape Shape;

        /// <summary>Alcance en metros (distancia máxima de lanzamiento o de recorrido).</summary>
        public float Range;
        /// <summary>Radio del golpe, del área o del proyectil; en Cone es el ángulo total en grados.</summary>
        public float Radius;
        public float Cooldown;
        /// <summary>Tiempo de preparación antes de golpear (el lanzador queda quieto).</summary>
        public float Windup;
        /// <summary>Solo GroundArea: segundos que se ve el aviso en el suelo antes del impacto.</summary>
        public float TelegraphTime;
        public float ProjectileSpeed;

        public StatusEffect Status;
        public float StatusChance;
    }
}
