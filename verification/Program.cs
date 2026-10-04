using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using GunplayHUD;

internal static class Program
{
    private static string game;
    private static int checks;
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    private static Assembly Resolve(object sender, ResolveEventArgs e)
    {
        foreach (string folder in new[] { "BepInEx/core", "Apocalypter_Data/Managed" })
        {
            string path = Path.Combine(game, folder, new AssemblyName(e.Name).Name + ".dll");
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null; // Deliberately no sibling resolution.
    }
    private static int Main(string[] args)
    {
        game = Path.GetFullPath(args[0]);
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Run(Path.GetFullPath(args[1])); Console.WriteLine("PASS: " + checks + " managed checks; gameplay not executed."); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    private static void Run(string repo)
    {
        var gate = new HeadshotSoundGate();
        Check(gate.Acquire(0, 1) == 0, "First sound");
        Check(gate.Acquire(0, 1) == -1, "Simultaneous hits coalesce");
        Check(gate.Acquire(.05f, 1) == -1, "Minimum interval");
        Check(gate.Acquire(.1f, 1) == 1, "Second voice");
        Check(gate.Acquire(.2f, 1) == -1, "Full voices must not restart");
        Check(gate.Acquire(1, 1) == 0, "Finished voice reused");
        gate.Reset(); Check(gate.Acquire(0, .04f) == 0, "Scene reset");
        Check(gate.Acquire(.08f, .04f) == 0, "Spaced shots play individually");

        string root = Path.Combine(repo, ".verification/test-runtime");
        Directory.CreateDirectory(root);
        typeof(BepInEx.Paths).GetMethod("SetExecutablePath", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { Assembly.GetExecutingAssembly().Location, root, Path.Combine(game, "Apocalypter_Data/Managed"), new string[0] });
        Migration(root, "old-on", "[Hud]\nTurnOffCrosshair = true\nOther = retain\n", true, 0, false);
        Migration(root, "old-off", "[Hud]\nTurnOffCrosshair = false\nOther = retain\n", true, 1, true);
        Migration(root, "explicit", "[Hud]\nTurnOffCrosshair = true\nCrosshair = 3\nSelectionDot = true\nOther = retain\n", true, 3, true);
        Directory.CreateDirectory(BepInEx.Paths.ConfigPath);
        string legacy = Path.Combine(BepInEx.Paths.ConfigPath, "com.denis.apocalypter.apocaraider.cfg");
        string contents = "[Hud]\nTurnOffCrosshair = true\n";
        File.WriteAllText(legacy, contents);
        Migration(root, "first-run", "[Hud]\nOther = retain\n", false, 0, false);
        Check(File.ReadAllText(legacy) == contents, "Legacy file unchanged");

        var assembly = Assembly.LoadFrom(Path.Combine(repo, "bin/Release/GunplayHUD.dll"));
        Check(assembly.GetTypes().Length > 0, "Types load without sibling mods");
        Check(!assembly.GetReferencedAssemblies().Any(x => new[] { "Gunplay", "NPCAI", "WomenOfWasteland", "Apocaraider" }.Contains(x.Name)), "No sibling assembly reference");
        var api = assembly.GetType("GunplayHUD.Api", true);
        Check(api.GetMethod("PlayerHit").GetParameters().Length == 4 && api.GetMethod("PartHit").GetParameters().Length == 3, "Optional API preserved");
        var metadata = assembly.GetType("GunplayHUD.Plugin", true).GetCustomAttributesData().Single(x => x.AttributeType.FullName == "BepInEx.BepInPlugin");
        Check((string)metadata.ConstructorArguments[2].Value == "1.2.0", "Release metadata");
    }
    private static void Migration(string root, string name, string source, bool existing, int expected, bool dotExpected)
    {
        string path = Path.Combine(root, name + ".cfg"); File.WriteAllText(path, source);
        var config = new ConfigFile(path, false);
        var migration = new CrosshairMigration(config, existing);
        var mode = config.Bind("Hud", "Crosshair", 1, new ConfigDescription("mode", new AcceptableValueRange<int>(0, 3)));
        var dot = config.Bind("Hud", "SelectionDot", true);
        var sound = config.Bind("Hud", "HeadshotSound", true);
        migration.Apply(config, mode, dot); config.Save();
        Check(mode.Value == expected && dot.Value == dotExpected, name + " migration");
        Check(sound.Value, "Sound enabled by default");
        string result = File.ReadAllText(path);
        Check(!result.Contains("TurnOffCrosshair"), "Retired key removed");
        Check(result.Contains("Other = retain"), "Unrelated config preserved");
        mode.Value = 20; Check(mode.Value == 3, "Slider clamps upper bound");
        mode.Value = -1; Check(mode.Value == 0, "Slider clamps lower bound");
    }
}
