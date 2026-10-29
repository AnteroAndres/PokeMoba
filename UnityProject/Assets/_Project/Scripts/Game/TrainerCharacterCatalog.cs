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
            new Entry("ch0243_00_yuri", "Yuri"),
            new Entry("ch0130_00_serena", "Serena"),
            new Entry("ch0250_00_saida", "Saida"),
            new Entry("ch0120_00_lusamine", "Lusamine"),
            new Entry("ch0221_00_homura", "Homura"),
            new Entry("ch0256_00_nezz", "Nezz"),
            new Entry("ch0257_00_qibana", "Qibana"),
            new Entry("ch0247_00_dande", "Dande"),
            new Entry("ch0222_00_kagari", "Kagari"),
            new Entry("ch0198_00_azami", "Azami"),
            new Entry("ch0223_00_ushio", "Ushio"),
            new Entry("ch0125_00_guzma", "Guzma"),
            new Entry("ch0249_00_rulina", "Rulina"),
            new Entry("ch0224_00_izumi", "Izumi"),
            new Entry("ch0193_00_aogiri", "Aogiri"),
            new Entry("ch0195_00_ghetsis", "Ghetsis"),
            new Entry("ch0241_00_kojiro", "Kojiro"),
            new Entry("ch0245_00_mary", "Mary"),
            new Entry("ch0240_00_musashi", "Musashi"),
            new Entry("ch0196_00_fleurdelis", "Fleurdelis"),
            new Entry("ch0190_00_ryuki", "Ryuki"),
            new Entry("ch0170_00_arty", "Arty"),
            new Entry("ch0192_00_matsubusa", "Matsubusa"),
            new Entry("ch0194_00_akagi", "Akagi"),
            new Entry("ch0137_00_orchid1", "Orchid1"),
            new Entry("ch0161_00_hara", "Hara"),
            new Entry("ch0183_00_molayne", "Molayne"),
            new Entry("ch0160_00_suiren", "Suiren"),
            new Entry("ch0157_00_mache", "Mache"),
            new Entry("ch0133_00_shilver", "Shilver"),
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
        public static string Resolve(string key)
        {
            string resolved = key != null && Legacy.TryGetValue(key, out var folder) ? "Package/" + folder : key;
            if (resolved != null && resolved.StartsWith("Package/", StringComparison.Ordinal))
            {
                foreach (var entry in Package) if (entry.Key == resolved) return resolved;
                return "Package/ch0130_00_serena";
            }
            return resolved;
        }
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
