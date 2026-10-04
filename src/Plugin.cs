using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace GunplayHUD
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string GUID = "com.denis.apocalypter.gunplayhud", NAME = "Gunplay HUD", VERSION = "1.2.0";
        internal static ManualLogSource Log;
        internal static string Dir;
        internal static ConfigEntry<bool> HudEnabled, HitMarker, SelectionDot, HeadshotSound;
        internal static ConfigEntry<int> DamageNumbers, DamageFontSize, HitMarkerSize, Crosshair;
        private static GameObject _runner;
        private void Awake()
        {
            bool existingConfig = File.Exists(Config.ConfigFilePath);
            var migration = new CrosshairMigration(Config, existingConfig);
            Log = Logger;
            Dir = Path.GetDirectoryName(Info.Location);
            Config.Bind("General", "Apocasetter", true, "Show this mod in the Apocasetter Mods menu");
            HudEnabled = Config.Bind("General", "EnableHudEffects", true, "Damage numbers, hit marker and headshot sound for your hits.");
            DamageNumbers = Config.Bind("Hud", "FloatingDamage", 2, new ConfigDescription(
                "Damage your hits do, shown as: 0 = nothing, 1 = a list in the top right corner, 2 = numbers floating up from the hit point. White = damage, red with '!' = headshot, light blue = % of condition taken off a vehicle part.",
                new AcceptableValueRange<int>(0, 2)));
            HitMarker = Config.Bind("Hud", "HitMarker", true, "A diagonal cross flashes at the screen centre when your bullet hits a creature (white, red for a headshot).");
            Crosshair = Config.Bind("Hud", "Crosshair", 1, new ConfigDescription(
                "Weapon crosshair: 0 = none, 1 = vanilla, 2 = two horizontal lines, 3 = dot. Follows the game's weapon and aiming visibility.", new AcceptableValueRange<int>(0, 3)));
            SelectionDot = Config.Bind("Hud", "SelectionDot", true, "Show the game's selection dot without a weapon. Independent of the weapon crosshair.");
            HeadshotSound = Config.Bind("Hud", "HeadshotSound", true, "Play a sound for your headshots. Near-simultaneous hits are coalesced and overlap is limited without restarting sounds.");


            var hidden = new ConfigFile(Path.Combine(Paths.ConfigPath, "GunplayHUD/hidden-settings.not-saved"), false) { SaveOnConfigSet = false };
            DamageFontSize = hidden.Bind("Hud", "DamageFontSize", 14, new ConfigDescription("Font size of the damage numbers, px.", new AcceptableValueRange<int>(8, 40)));
            HitMarkerSize = hidden.Bind("Hud", "HitMarkerSize", 22, new ConfigDescription("Hit marker size, px.", new AcceptableValueRange<int>(6, 100)));
            LegacyConfig.Import(Config, Log, existingConfig);
            migration.Apply(Config, Crosshair, SelectionDot);
            var harmony = new Harmony(GUID);
            harmony.Patch(AccessTools.Method(typeof(SetFsmFloat), "OnEnter"), prefix: new HarmonyMethod(typeof(VanillaHits), nameof(VanillaHits.BeforeSetFsmFloat)));
            SceneManager.sceneLoaded += SceneLoaded;
            EnsureRunner();
            Log.LogInfo(NAME + " " + VERSION + " loaded");
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode) { EnsureRunner(); VanillaHits.Clear(); Hud.OnSceneLoaded(); CrosshairController.OnSceneLoaded(); HeadshotAudio.Stop(); }
        private static void EnsureRunner()
        {
            if (_runner != null) return;
            _runner = new GameObject("GunplayHUD.Runner") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.Object.DontDestroyOnLoad(_runner); _runner.AddComponent<Runner>();
        }
    }
    internal sealed class Runner : MonoBehaviour
    {
        private void Update()
        {
            try { CrosshairController.Tick(); } catch (Exception e) { Plugin.Log.LogError("Crosshair: " + e); }
            try { HeadshotAudio.Tick(this); } catch (Exception e) { Plugin.Log.LogError("Headshot audio: " + e); }
        }
        private void LateUpdate()
        {
            try { VanillaHits.Flush(); Hud.Flush(); } catch (Exception e) { Plugin.Log.LogError("Hit feedback: " + e); }
        }
        private void OnGUI() { try { Hud.OnGUI(); } catch (Exception e) { Plugin.Log.LogError("HUD: " + e); } }
    }
    /// <summary>Optional reflection contract. Damage is accepted in the game's negative-damage convention or positive magnitude.</summary>
    public static class Api
    {
        public static bool Enabled { get { return Plugin.HudEnabled != null && Plugin.HudEnabled.Value && (Plugin.DamageNumbers.Value != 0 || Plugin.HitMarker.Value || Plugin.HeadshotSound.Value); } }
        public static void PlayerHit(GameObject target, Vector3 point, float damage, bool head) { Hud.PlayerHit(target, point, damage, head); }
        public static void PartHit(GameObject target, Vector3 point, float percent) { Hud.PartHit(target, point, percent); }
    }
}
