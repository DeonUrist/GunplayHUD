# GunplayHUD 1.1.0 execution plan

## Scope
Add Crosshair integer slider (0 none, 1 vanilla, 2 horizontal lines, 3 dot), independent SelectionDot toggle, and enabled-by-default HeadshotSound using supplied MP3 with bounded rapid-hit playback. Preserve existing damage feedback and optional Gunplay API. Only GunplayHUD repository and authorized workspace _releases ZIP output may change. No game deployment, Git push or GitHub release.

## Investigation
- Existing crosshair removal changes native MousePoint/MouseCrosshair scales, leaving FSM activation intact.
- Game FSM dump: Weapons.off enables MousePoint and disables MouseCrosshair; weapon selection disables MousePoint; AimDownSights.off enables MouseCrosshair and .on hides it.
- ApocaSetter supports integer AcceptableValueRange sliders and rounds integer values. General must bind before HUD controls.
- ModDev/Textures descriptors reference PNGs not present as loose files. User supplied their location in assets.zip during implementation. Archive inspection shows unversioned PNGs fully transparent; the (v1) PNGs contain the requested lines and dot. Copy those visible assets byte-for-byte into repository Textures with crosshair_lines/crosshair_dot names. Initial generated placeholders were replaced before packaging. Game/archive files remain unchanged.
- Supplied ModDev/Sounds/headshot.mp3 will be copied unchanged into repository/output Sounds. Decode asynchronously once with UnityWebRequestMultimedia/MPEG, using local plugin-relative path.

## Design
Native weapon crosshair receives custom per-component Image sprite or RawImage texture while its active state remains game-owned. Mode0 hides by scale; mode1 restores original asset. SelectionDot independently controls MousePoint scale. Custom textures are local assets, never globally replacing shared game textures. Recover on scene changes and restore originals as needed.
Headshot notification enters before optional visual-feedback gating. HeadshotSound works even when numbers and hit marker are disabled, while EnableHudEffects disables hit feedback/sound together. A short minimum interval coalesces near-simultaneous hits; bounded non-restarting voices prevent rapid overlap. Missing assets are logged and do not disable other HUD features.
Retire old Hud/TurnOffCrosshair binding while migrating its true value into Crosshair0 + SelectionDotfalse only when the new keys are absent. Preserve explicit new config values and all unrelated settings.

## Milestones
- [x] Read current HUD/plugin/config/game FSM/assets.
- [x] Add settings/migration and native crosshair controller; build (0 warnings/errors).
- [x] Add local MP3 playback and hit callback integration; build (0 warnings/errors).
- [x] Validate settings/migration/rate limiter/API/asset integrity and independent loading.
- [x] Document and package GunplayHUD-1.1.0.zip in _releases; verify ZIP contents/hash.

## Verification limits
No game execution has occurred. Builds and managed/static checks do not prove actual rendering or audio decoding/playback in Unity.

## Results and decisions
- Crosshair defaults to vanilla (1), SelectionDot and HeadshotSound default on. Existing TurnOffCrosshair=true migrates to 0/false unless explicit new keys exist.
- HeadshotSound uses existing player-hit callbacks before visual gating; Api.Enabled includes sound so standalone/sound-only observation remains active. Damage observer and Harmony patch remain unchanged.
- Audio policy: 75 ms minimum interval, two voices, no restarting busy clips. Same-instant pellets/rapid bursts may coalesce, protecting playback from repeated interruption and excessive overlap.
- No new sibling assembly dependencies. Native UI original assets are restored for vanilla mode; FSM active-state control remains untouched. Fixed lookup to stop at the first matching scene Canvas rather than switching among canvases.
- Commands: `dotnet build GunplayHUD.csproj -c Release -p:GameDir="<game>"`; focused verifier built with same GameDir then run with game/repository paths. 37 focused managed checks pass. Existing independent combination verifier run read-only for masks 2,3,6,7,10,11,14,15: all pass (19/36/37/54/42/59/60/77 checks). Log: .verification/combinations.log (ignored).
- Both PNGs exactly match archive (v1) entries; MP3 matches original bytes. All new code compiled without errors. No game deployment or network operations.
- Packaging inspection caught MSBuild PreserveNewest retaining earlier placeholder images because ZIP extraction preserves older asset timestamps. Changed asset copying to Always, rebuilt successfully and verified every packaged file against repository/build bytes. No placeholders remain in the release.
- Final release: ../_releases/GunplayHUD-1.1.0.zip; exactly README.md, GunplayHUD/GunplayHUD.dll, GunplayHUD/Textures/crosshair_lines.png, GunplayHUD/Textures/crosshair_dot.png and GunplayHUD/Sounds/headshot.mp3. SHA256: 276254B39B86BBA9F7C0139675DF6B7AB8ABE70F85E7C9C1E0C63AD7AA137DDA. DLL: bin/Release/GunplayHUD.dll. Final build 0 warnings/errors; focused checks pass. Apocaraider git status remains clean. New source/assets/docs remain local uncommitted changes in GunplayHUD.
- Required gameplay follow-up: crosshair rendering/size and ADS/weapon visibility, selection-dot toggle, scene persistence, Windows Unity local MPEG decoding, vanilla/Gunplay headshot sounds including rapid hits and sound-only mode, settings in ApocaSetter.

## 1.2.0 follow-up
User requested half-size dot and +30% sound. Crosshair mode3 uses original scale times0.5 (width/height); other modes and selection dot retain previous scale. MP3 is decoded uncompressed/non-streaming and samples multiplied by1.3, clamped to [-1,1], because AudioSource.volume cannot exceed1. Original MP3 unchanged. Version and verifier metadata updated to1.2.0. Build: zero warnings/errors; 37 managed checks passed. No gameplay verification. Release: ../_releases/GunplayHUD-1.2.0.zip; old releases preserved. No deploy, push or upload.

## Publication
User subsequently authorized pushing this code and publishing GitHub release v1.2.0 in DeonUrist/GunplayHUD without attaching the local ZIP. Remote main matched the local baseline before publication; no existing v1.2.0 tag/release was found. Source, packaged runtime assets and verification code are committed; build/test outputs and local release ZIP stay outside the commit. Release notes describe the full change since published v1.0.0 and explicitly retain the in-game verification limitation.
