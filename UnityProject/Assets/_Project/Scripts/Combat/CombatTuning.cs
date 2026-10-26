namespace PokeMoba.Combat
{
    /// <summary>
    /// Valores de ajuste del combate del prototipo. Se mueven a un ScriptableObject
    /// cuando el balance deje de cambiar a diario.
    /// </summary>
    public static class CombatTuning
    {
        // En los modos competitivos todos pelean a nivel 50 (GDD §7.2).
        public const int DuelLevel = 50;

        // La vida se multiplica para que un combate en tiempo real dure más que unos pocos golpes.
        public const float HpMultiplier = 3.5f;

        public const float StabMultiplier = 1.5f;
        public const float CritChance = 0.0625f;
        public const float CritMultiplier = 1.5f;
        public const float MinVariance = 0.9f;

        // Movimiento
        public const float BaseMoveSpeed = 3.5f;
        public const float SpeedStatFactor = 0.025f;
        public const float BodyRadius = 0.6f;
        public const float ArenaRadius = 13f;
        public const float KnockbackForce = 4f;
        public const float KnockbackDecay = 18f;

        // Estamina y esquiva
        public const float MaxStamina = 100f;
        public const float StaminaRegen = 22f;
        public const float DodgeCost = 30f;
        public const float DodgeDuration = 0.3f;
        public const float DodgeSpeed = 13f;
        public const float DodgeInvulnerability = 0.25f;
        public const float DodgeCooldown = 0.35f;

        // Habilidades
        public const float MeleeAngle = 120f;
        public const float DashSpeed = 22f;

        // Estados: fracción de la vida máxima por segundo
        public const float StatusDuration = 4f;
        public const float BurnDamagePerSecond = 0.02f;
        public const float PoisonDamagePerSecond = 0.03f;
    }
}
