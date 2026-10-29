using System.Linq;

namespace PokeMoba.Game
{
    /// <summary>Reglas de desbloqueo de zonas, arenas y modos (GDD §5, §6 y §7.4).</summary>
    public static class Progression
    {
        public static bool IsZoneUnlocked(ZoneDef zone, SaveData save) => save.UnlockAll || LockReason(zone, save) == null;

        /// <summary>Qué falta para abrir la zona, o null si ya está abierta.</summary>
        public static string LockReason(ZoneDef zone, SaveData save)
        {
            if (!save.HasStarter) return "Elige tu Pokémon inicial";
            if (zone.RequiresLeague && !save.LeagueChampion) return "Vence a la Liga Pokémon";
            if (zone.RequiresAllMedals && save.Medals.Count < ZoneCatalog.MedalOrder.Length)
                return $"Necesitas las 8 medallas ({save.Medals.Count}/8)";
            if (zone.RequiredMedal != null && !save.HasMedal(zone.RequiredMedal))
                return $"Necesitas la Medalla {zone.RequiredMedal}";
            if (save.CaughtCount < zone.RequiredCaught)
                return $"Captura {zone.RequiredCaught} especies ({save.CaughtCount}/{zone.RequiredCaught})";
            return null;
        }

        public static int HighestUnlockedZone(SaveData save) =>
            ZoneCatalog.Zones.Where(z => IsZoneUnlocked(z, save)).Select(z => z.Id).DefaultIfEmpty(0).Max();

        public static bool IsArenaUnlocked(ArenaDef arena, SaveData save) =>
            save.UnlockAll || HighestUnlockedZone(save) >= arena.RequiredZone;

        public static bool DuelUnlocked(SaveData save) => save.UnlockAll || save.HasStarter;
        public static bool MobaUnlocked(SaveData save) => save.UnlockAll || save.HasMedal("Trueno");
    }
}
