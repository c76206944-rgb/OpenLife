# OpenLife

A 3D life-simulation prototype in Unity: create Sims, build their home, and choose or design the world they live in. Inspired by The Sims, built to be more open, more customizable and more moddable.

See [GAME_DESIGN.md](GAME_DESIGN.md) for the full vision and [MODDING.md](MODDING.md) to add your own content.

## What works in this prototype

- Start screen with a **world creator** (6 presets, custom name, ground and sky colors, prop density, start hour, seed)
- **Sim creator** (name, skin tone, hair color, outfit color, up to 3 traits, household of up to 4)
- Six **needs** (Hunger, Energy, Fun, Social, Hygiene, Bladder) with trait-modified decay
- **Autonomy AI**: Sims choose what to do based on needs, traits, distance and chance
- **Relationships** and chatting between Sims
- **Goals** with money rewards (panel in the top-left, like the concept art)
- **Build mode**: floors, walls, doorways, furniture, decor, rotation, drag painting, recoloring, delete with refund
- Runtime **navmesh rebuild** (custom small agent type, so Sims fit through 1 m doorways) so Sims path around whatever you build
- Day and night cycle, pause and three speeds
- **Save and load** (JSON)
- Everything content-related is **JSON driven** and mod friendly

## Requirements

- Unity **6000.0 LTS** or newer (the 3D URP template or the built-in 3D template both work)
- Packages (already listed in `Packages/manifest.json`): Input System and uGUI. Navigation uses Unity's built-in AI module, so no extra package is needed.
- Windows, macOS or Linux (content loading uses `StreamingAssets`, which is not set up for Android or WebGL yet)
- For standalone builds: add the shader you use (`Universal Render Pipeline/Lit`, or `Standard`) and `Sprites/Default` to **Project Settings, Graphics, Always Included Shaders**. Not needed in the Editor.

## Run it

**Option A: open this repo directly**
1. Unity Hub, Add, Add project from disk, choose this folder.
2. Open with Unity 6. Accept the prompt to enable the new Input System if asked.
3. Open any scene (the default `SampleScene` is fine, or a totally empty one) and press **Play**. The game builds itself.

**Option B: add to an existing project**
1. Copy `Assets/Scripts` and `Assets/StreamingAssets` into your project's `Assets` folder.
2. Make sure the Input System and uGUI packages are installed.
3. Press Play in any scene.

If Unity says the Input System is not the active input handler, set **Edit, Project Settings, Player, Active Input Handling** to **Input System Package** (or Both) and restart.

## Controls

| Action | Input |
|---|---|
| Pan camera | W A S D or arrow keys |
| Rotate camera | Q / E, or hold right mouse and drag |
| Zoom | Mouse wheel |
| Focus selected Sim | F |
| Select Sim | Left click the Sim, click a name in the household bar, or press Tab |
| Interact | Left click an object, choose an action |
| Chat | Select a Sim, then click another Sim, choose Chat |
| Build mode | B |
| Rotate item (build) | R |
| Exit build mode / close menu | Esc |
| Pause / speeds | Space, 1, 2, 3 |
| Quick save | F5 |

## Project layout

```
Assets/
  Scripts/
    Core/      GameController, GameClock, GameState, SaveSystem, NavSetup
    Content/   ContentModels, ContentDatabase
    Sims/      SimCharacter, SimFactory, Interactable, GoalSystem
    World/     WorldBuilder, ObjectFactory, BuildMode, CameraRig
    UI/        UIFactory, StartScreen, Hud, BuildPanel, InteractionMenu
  StreamingAssets/Content/   traits.json, objects.json, goals.json, worlds.json
Packages/manifest.json
GAME_DESIGN.md  MODDING.md
```

## Known limitations (honest list)

- Characters and furniture are primitive shapes. Real models go in through the `prefab` field on objects (a `Resources` path) and by replacing `SimFactory.Create`.
- Walls are full grid cells (blocky). Thin edge-based walls, roofs and multiple floors are on the roadmap.
- Animations are minimal (walk bob only).
- The code was written without a Unity compile step in this environment, so expect to fix a small number of compile errors on first open. Please open an issue or PR with fixes.

## Put it on GitHub

```bash
cd OpenLife
git init
git add .
git commit -m "Initial OpenLife prototype"
git branch -M main
git remote add origin https://github.com/<your-username>/OpenLife.git
git push -u origin main
```

## License

MIT, see [LICENSE](LICENSE).
