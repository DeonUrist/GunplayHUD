# Gunplay HUD

Independent BepInEx 5 / Harmony mod for Apocalypter. Provides white/red player hit markers, floating damage numbers or a damage list, blue vehicle condition damage percentages, selectable weapon crosshairs, an independent selection dot toggle, and headshot sound. It changes feedback only; game damage and ballistics remain owned by the game or optional Gunplay mod.

No Gunplay, NPCAI, Women of Wasteland, or ApocaSetter dependency is required. Remove the original combined Apocaraider mod when installing its replacements to avoid running both implementations.

## Build and install

Requires a .NET SDK capable of building .NET Framework 4.7.2 and the installed BepInEx/game assemblies. Set `APOCALYPTER_GAME_DIR` to the installed Apocalypter directory, then run:

```powershell
dotnet build GunplayHUD.csproj -c Release
```

Alternatively pass `-p:GameDir="<installed game directory>"`. The build does not deploy. Copy `bin/Release/GunplayHUD.dll` **and its Textures and Sounds folders** into `BepInEx/plugins/GunplayHUD/`. From the release ZIP, copy the complete GunplayHUD folder there. Replace the previous DLL rather than installing a second copy. The alternative `build.sh` compiles only the DLL; copy the repository's Textures and Sounds folders alongside its output yourself.

## Settings and integration

Version 1.2.0 halves the weapon dot's displayed width and height (Crosshair=3). Headshot sound has 1.3x sample amplitude, with peaks limited to the valid audio range. Original assets, selection dot and other crosshair modes are unchanged.

The existing `Hud/FloatingDamage` (0 off, 1 list, 2 floating) and `Hud/HitMarker` settings are preserved. Font/marker sizes retain their hidden defaults. `General/EnableHudEffects` controls damage numbers, hit marker and headshot sound. Crosshair and selection-dot controls remain separate from it.

| Setting | Values | Default |
| --- | --- | --- |
| Hud/Crosshair | 0 none, 1 vanilla, 2 two horizontal lines, 3 dot | 1 |
| Hud/SelectionDot | Show the selection dot without a weapon | On |
| Hud/HeadshotSound | Play headshot.mp3 for player headshots | On |

Weapon/aiming FSMs still control when the weapon crosshair appears. Returning to mode 1 restores its original graphic. The supplied graphics are copied unchanged from `assets/crosshair_UI(v1).png` and `assets/mouse_point(v1).png` in the provided assets.zip; the unversioned PNGs in that archive are fully transparent. Assets are resolved relative to the plugin DLL, without requiring ModDev files at installation time.

The old `Hud/TurnOffCrosshair` setting is retired. A stored true value migrates to Crosshair=0 and SelectionDot=false unless those new settings already exist. Explicit new settings always win; unrelated entries are preserved. This migration also reads the original combined mod's config on first installation without changing that file.

Sound loads asynchronously once from the local MP3. Hits less than 75 ms apart are coalesced, with at most two overlapping voices. A busy voice is never restarted or cut off; additional hits while both voices are busy do not play another sound. Sound continues to work when floating damage and hit marker are off. Scene changes and disabling hit feedback/sound stop active voices. Missing/undecodable sound is logged and leaves visual feedback working.

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

Version 1.2.0: **BUILD VERIFIED**, zero warnings/errors against installed game assemblies. **STATICALLY VERIFIED**: 37 managed checks cover migration, explicit-value preservation, retired-key removal, slider bounds, default sound setting, timing/voice limits, API signatures and independent type loading. Managed dependency/GUID/API checks pass for all eight installation combinations containing GunplayHUD. Supplied image/MP3 bytes and release ZIP contents are verified. Vanilla hook semantics were checked against the gun/vehicle FSM dump; the damage observer and Harmony patch are unchanged.

Run focused checks after building:

```powershell
dotnet build verification/Verifier.csproj -c Release
& ./.verification/bin/Verifier.exe $env:APOCALYPTER_GAME_DIR (Get-Location).Path
```

**RUNTIME NOT VERIFIED**: the game was not launched. In-game checks include all four crosshair modes, selection dot with/without weapons, aiming visibility, scene changes, vanilla/Gunplay headshots, sound-only mode, rapid-fire sound overlap, MP3 decoding, damage/condition feedback, and settings changes through ApocaSetter.
