using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace GunplayHUD
{
    internal sealed class CrosshairMigration
    {
        private readonly bool _hasCrosshair, _hasDot, _oldOff;
        private static IDictionary Orphans(ConfigFile config)
        {
            var property = typeof(ConfigFile).GetProperty("OrphanedEntries", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return property != null ? property.GetValue(config, null) as IDictionary : null;
        }
        internal CrosshairMigration(ConfigFile config, bool existingConfig)
        {
            var entries = Orphans(config);
            _hasCrosshair = entries != null && entries.Contains(new ConfigDefinition("Hud", "Crosshair"));
            _hasDot = entries != null && entries.Contains(new ConfigDefinition("Hud", "SelectionDot"));
            var old = new ConfigDefinition("Hud", "TurnOffCrosshair");
            bool off;
            if (entries != null && entries.Contains(old) && bool.TryParse(entries[old] as string, out off)) _oldOff = off;
            else if (!existingConfig)
            {
                var path = Path.Combine(Paths.ConfigPath, "com.denis.apocalypter.apocaraider.cfg");
                if (!File.Exists(path)) return;
                string section = null;
                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (line.StartsWith("[") && line.EndsWith("]")) { section = line.Substring(1, line.Length - 2); continue; }
                    int eq = line.IndexOf('=');
                    if (section == "Hud" && eq > 0 && line.Substring(0, eq).Trim() == "TurnOffCrosshair" &&
                        bool.TryParse(line.Substring(eq + 1).Trim(), out off)) { _oldOff = off; break; }
                }
            }
        }
        internal void Apply(ConfigFile config, ConfigEntry<int> crosshair, ConfigEntry<bool> dot)
        {
            if (_oldOff && !_hasCrosshair) crosshair.Value = 0;
            if (_oldOff && !_hasDot) dot.Value = false;
            var entries = Orphans(config);
            var old = new ConfigDefinition("Hud", "TurnOffCrosshair");
            if (entries == null || !entries.Contains(old)) return;
            entries.Remove(old); config.Save();
        }
    }
}
