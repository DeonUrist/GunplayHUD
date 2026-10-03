# Gunplay HUD

Independent BepInEx 5 / Harmony mod for Apocalypter. Provides white/red player hit markers, floating damage numbers or a damage list, blue vehicle condition damage percentages, and optional crosshair/centre-dot removal. It changes feedback only; game damage and ballistics remain owned by the game or optional Gunplay mod.

No Gunplay, NPCAI, Women of Wasteland, or ApocaSetter dependency is required. Remove the original combined Apocaraider mod when installing its replacements to avoid running both implementations.

## Build and install

Requires a .NET SDK capable of building .NET Framework 4.7.2 and the installed BepInEx/game assemblies. Set `APOCALYPTER_GAME_DIR` to the installed Apocalypter directory, then run:

```powershell
dotnet build GunplayHUD.csproj -c Release
```

Alternatively pass `-p:GameDir="<installed game directory>"`. The build does not deploy. Install `bin/Release/GunplayHUD.dll` into its own `BepInEx/plugins/GunplayHUD/` folder.

## Settings and integration

The original `General/EnableHudEffects`, `Hud/FloatingDamage` (0 off, 1 list, 2 floating), `Hud/HitMarker`, and `Hud/TurnOffCrosshair` keys and semantics are preserved. Font/marker sizes retain their original hidden defaults. Crosshair control remains separate from EnableHudEffects, as in the original.

ApocaSetter discovers normal BepInEx Config entries through `General/Apocasetter=true`. The first new config imports matching exposed settings from `com.denis.apocalypter.apocaraider.cfg` without modifying it; existing replacement configs take precedence. No change to ApocaSetter is needed. External index additions are required only for summaries/releases/update listings.

Without Gunplay, a narrow `SetFsmFloat.OnEnter` prefix observes player gun `Attack` FSMs with a `Reload` FSM writing `Bodypart/Damage`. It captures creature Health or vehicle Condition before the write and measures the result at LateUpdate. It never changes the write, events, FSM states, or damage. Corpses and scenery lacking creature Health or vehicle Condition are excluded. Same-frame shotgun hits are merged. If a vanilla damage event has not reduced its value before LateUpdate, the reported value falls back to the gun's nominal damage.

Gunplay discovers public `GunplayHUD.Api` methods through cached reflection delegates:

```text
PlayerHit(UnityEngine.GameObject target, UnityEngine.Vector3 point, float damage, bool head)
PartHit(UnityEngine.GameObject target, UnityEngine.Vector3 point, float percent)
Enabled : bool
```

Negative game damage or positive magnitudes are accepted. Gunplay's custom projectile replay writes damage variables directly, so the vanilla observer does not duplicate its API reports. This also permits the vanilla observer when Gunplay is present but projectiles are disabled. Gunplay custom melee-wheel condition feedback uses the same PartHit API. Rendering and pending-hit flushing belong exclusively to the HUD's persistent runner. AI ghost/navigation debug drawing stays in NPCAI.

## Verification

Release compilation: **BUILD VERIFIED**, zero warnings/errors with .NET SDK 10.0.401 against installed game assemblies. Vanilla hook semantics were checked against the supplied gun/vehicle FSM dump. Config discovery, API signatures and absence of sibling assembly dependencies were checked statically. **RUNTIME NOT VERIFIED**: the game was not launched. Remaining in-game checks include vanilla/projectile hit feedback, shotgun aggregation, headshots, condition percentages, disabled projectiles, crosshair restoration, and scene changes.
