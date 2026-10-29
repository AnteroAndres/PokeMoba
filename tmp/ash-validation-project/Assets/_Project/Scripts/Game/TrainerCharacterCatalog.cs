using System;
using System.Collections.Generic;

namespace PokeMoba.Game
{
    /// <summary>New trainer package, with legacy save keys redirected without touching Roshi or Ash.</summary>
    public static class TrainerCharacterCatalog
    {
        public readonly struct Entry
        {
            public readonly string Key, Name;
            public Entry(string folder, string name) { Key = "Package/" + folder; Name = name; }
        }
        public static readonly Entry[] Package =
        {
            new Entry("tr0002_00_doctor_b", "Laventon"), new Entry("tr0003_00_ouji", "Adaman"),
            new Entry("tr0004_00_hime", "Irida"), new Entry("tr0005_00_boss1", "Kamado"),
            new Entry("tr0006_00_boss2", "Kamado samurái"), new Entry("tr0007_00_si1", "Volo"),
            new Entry("tr0008_00_si2", "Volo ceremonial"), new Entry("tr0009_00_ninja1", "Ninja 1"),
            new Entry("tr0010_00_ninja2", "Ninja 2"), new Entry("tr0011_00_miko", "Cogita"),
            new Entry("tr0012_00_captain1", "Guardián 1"), new Entry("tr0013_00_captain2", "Guardián 2"),
            new Entry("tr0014_00_captain3", "Guardián 3"), new Entry("tr0015_00_captain4", "Guardián 4"),
            new Entry("tr0016_00_captain5", "Guardián 5"), new Entry("tr0017_00_captain6", "Guardián 6"),
            new Entry("tr0018_00_captain7", "Ingo"), new Entry("tr0019_00_captain8", "Guardián 8"),
            new Entry("tr0020_00_captain9", "Guardián 9"), new Entry("tr0021_00_captain10", "Guardián 10"),
            new Entry("tr0022_00_guard_leader", "Jefa de guardia"), new Entry("tr0023_00_guard_lower_m", "Guardia"),
            new Entry("tr0024_00_guard_lower_f", "Guardia femenina"), new Entry("tr0025_00_trade_boss", "Ginter"),
            new Entry("tr0027_00_me_leader", "Jefe de aldea"), new Entry("tr0031_00_trade_m", "Comerciante"),
            new Entry("tr0032_00_trade_f", "Comerciante femenina"), new Entry("tr0033_00_bandit_first", "Charm"),
            new Entry("tr0034_00_bandit_second", "Clover"), new Entry("tr0035_00_bandit_third", "Coin"),
            new Entry("tr0036_00_senpai_m", "Rei"), new Entry("tr0037_00_senpai_f", "Akari"),
            new Entry("tr0038_00_bu_leader_f", "Aldeana")
        };
        public static readonly string[] Keys = BuildKeys();
        static string[] BuildKeys()
        {
            var keys = new List<string> { "Procedural", "Ash/Ash" };
            foreach (var entry in Package) keys.Add(entry.Key);
            return keys.ToArray();
        }
        static readonly Dictionary<string,string> Legacy = new Dictionary<string,string>
        {
            {"tr0002_Laventon/Laventon", "tr0002_00_doctor_b"}, {"tr0003_Adaman/Adaman", "tr0003_00_ouji"},
            {"tr0004_Irida/Irida", "tr0004_00_hime"}, {"tr0005_Kamado/Kamado1", "tr0005_00_boss1"},
            {"tr0006_Komado/Komado_Samurai", "tr0006_00_boss2"}, {"tr0007_Volo/Volo", "tr0007_00_si1"},
            {"tr0008_Volo/Volo2", "tr0008_00_si2"}, {"tr0011_Cogita/Cogita", "tr0011_00_miko"},
            {"tr0018_Ingo/Ingo", "tr0018_00_captain7"}, {"tr0025_Ginter/Ginter", "tr0025_00_trade_boss"},
            {"tr0033_Charm/Charm", "tr0033_00_bandit_first"}, {"tr0034_Clover/Clover", "tr0034_00_bandit_second"},
            {"tr0035_Coin/Coin", "tr0035_00_bandit_third"}, {"tr0036_Rei/Rei", "tr0036_00_senpai_m"},
            {"tr0037_Akari/Akari", "tr0037_00_senpai_f"}
        };
        public static string Resolve(string key) => key != null && Legacy.TryGetValue(key, out var folder) ? "Package/" + folder : key;
        public static string Label(string key)
        {
            if (key == "Procedural" || key == "MasterRoshi/N02_321out") return "Roshi";
            if (string.IsNullOrWhiteSpace(key)) return "Entrenador";
            string resolved = Resolve(key);
            foreach (var entry in Package) if (entry.Key == resolved) return entry.Name;
            int slash = key.LastIndexOf('/');
            return slash >= 0 ? key.Substring(slash + 1) : key;
        }
    }
}
