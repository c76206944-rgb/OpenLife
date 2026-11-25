# OpenLife next-generation art pipeline

This repository is a Unity 6 life-simulation project. The graphics branch adds an adaptive URP quality baseline, improved ambient lighting, and a clear route for replacing the prototype geometry with production character and environment assets. It does not replace the simulation systems.

## What this branch changes

- Adds `GraphicsQualityProfile` at runtime.
- Uses higher shadow distance, anti-aliasing, HDR, LOD bias and post-processing on desktop.
- Reduces shadow, anti-aliasing, post-processing and frame-rate targets on lower-memory mobile devices.
- Adds subtle ACES tonemapping, bloom, color grading and vignette on supported devices.
- Improves the world ambient light from flat lighting to a three-color sky/equator/ground setup.
- Keeps the existing Sim needs, autonomy, relationships, goals, navigation, build mode and save format intact.

## What is still required for true high-end character graphics

A Sims-level visual result cannot be achieved by changing lighting settings alone. The current `SimFactory` intentionally creates capsule/sphere placeholder characters. Production-quality Sims require imported, licensed 3D assets:

1. A rigged humanoid base mesh with blend shapes for face and body customization.
2. Separate hair meshes or groom assets, including textured/braided/loc styles.
3. PBR skin, eye, hair and clothing textures.
4. A humanoid animation set and Animator Controller (idle, walk, run, sit, eat, sleep, talk, cook).
5. Furniture and environment prefabs with LODs and mobile texture variants.

Use assets that you own or have a license to redistribute in the game. Do not commit ripped assets from The Sims or other commercial games.

## Character and furniture prefab conventions

- Add a rigged character prefab under `Assets/Resources/Characters/` (e.g. `AdultFemale_A.prefab`). Set the Sim's `modelId` to `Characters/AdultFemale_A`. The prefab is instantiated under the gameplay Sim root, so its root should be at ground level, face local +Z, and be approximately 1.8–1.9 metres tall.
- Use a Humanoid-rigged `Animator` with root motion disabled. Keep the visual mesh and Animator below the prefab root; keep gameplay collider and NavMeshAgent on the Sim root. The existing Sim AI and save system remain on the parent root.
- `SimFactory` now loads `modelId` from Resources and retains a detailed placeholder fallback for old saves and Sims without a model assigned.
- Add furniture and building prefabs under `Assets/Resources/Furniture/` and use the object's `prefab` Resources-relative path in `objects.json`. Keep interaction definitions and object IDs unchanged so existing save files still resolve.

## Repeatable player builds

Open the project in Unity 6.0.23f1 and use the `OpenLife/Build` menu:

- `Windows x86_64` writes to `Builds/Windows/`.
- `Android ARM64 APK` writes to `Builds/Android/` (requires Android Build Support, SDK/NDK and OpenJDK modules).
- `WebGL for Vercel` writes to `Builds/WebGL/` with compression disabled for simple static hosting.

Enable the game scene(s) in Build Profiles / Build Settings first. The WebGL output directory is ignored by Git until you intentionally add a generated build artifact or deploy it from a build pipeline.

## Prefab integration conventions

- Character prefabs: place under `Assets/Resources/Characters/`; replace the body construction in `Assets/Scripts/Sims/SimFactory.cs` with prefab instantiation while retaining the root `CapsuleCollider`, `NavMeshAgent`, `SimCharacter`, and `SimCharacter.Init(data, visualRoot)` contract.
- Furniture prefabs: place under `Assets/Resources/Furniture/`; set the matching object definition's `prefab` field to the Resources-relative path, e.g. `Furniture/ModernSofa`.
- Add LODGroup components to high-poly environment and furniture prefabs.
- Keep colliders simple and separate from render meshes so NavMesh baking and build placement remain stable.
- Keep animation and rendering components below the visual root; keep gameplay components on the Sim root.

## Build and release targets

### Windows / desktop

Build a Windows x86_64 player from Unity 6. Use URP high quality, HDR and post-processing. Desktop builds can use higher shadow distance and LOD bias.

### Android

Build an ARM64 Android player from Unity 6. Use URP mobile quality, compressed textures (ASTC where supported, ETC2 fallback), baked lighting for static environment pieces, reduced shadow distance, and LODs. Test on physical devices; a high-end desktop profile is not a mobile performance guarantee.

### Vercel

Vercel can host static web files, including a Unity WebGL build, but it does not compile Unity projects. A WebGL build must first be produced by Unity and then deployed as static output. Treat the browser build as an optional desktop preview: Unity's current Web platform has browser compatibility guidance, but high-end mobile browser performance is device-dependent. For reliable Android play, ship a native Android build rather than promising that the WebGL build will run well on every phone.

The Vercel deployment should point to a dedicated web-build output directory only after a valid Unity WebGL build exists. Do not point Vercel at the Unity source tree and expect it to produce a playable game.

## Validation still needed

This branch has not been compiled in the Unity Editor or tested on a physical Android device in this environment. Open it in Unity 6.0.23f1, allow package import, inspect the Console, then build and profile Windows and Android targets before merging.
