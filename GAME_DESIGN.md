# OpenLife: Game Design Document

Working title: **OpenLife**. A 3D life simulation where you create people, build their homes, and shape the world they live in.

## 1. Vision

OpenLife keeps what people love about The Sims 4 (expressive characters, needs and moods, easy building, emergent stories) and fixes what players complain about: shallow simulation, loading screens, paywalled basics, and fragile modding.

**One-line pitch:** a life sim where *you* decide the setting, the look and the rules, and the game is complete from day one.

## 2. Design pillars

1. **Your world, your setting.** The setting is data, not hard-coded. Pick a world preset (green campus, savanna, snowy pines, desert oasis, island, neon city), then recolor it, change its climate, time of day and prop density, or author a brand new one in JSON.
2. **People with depth.** Traits change what Sims want, what they like, how fast their needs fall, and how they treat each other. Relationships remember.
3. **Build anything.** Grid-based build mode with floors, walls, doorways, furniture and decor, all recolorable.
4. **Open by design.** Every object, trait, goal and world lives in plain JSON files that anyone can edit or add to without touching code.
5. **Fair and complete.** One game, no paywalled basics, free updates.

## 3. Comparison with The Sims 4

| Area | The Sims 4 | OpenLife goal |
|---|---|---|
| Setting | Fixed neighborhoods | Any setting through world data and the world creator |
| World loading | Loading screens between lots | One continuous world (planned) |
| Personality | Light | Traits affect needs, desires, social and money behavior |
| Modding | Fragile script mods | JSON content packs plus a clear code API |
| Business model | Many paid packs | One complete game |
| Representation | Limited hair and skin range | Wide skin tones and textured/braided hair as first-class options |

## 4. Core systems

### 4.1 Needs
Six needs on a 0 to 100 scale: Hunger, Energy, Fun, Social, Hygiene, Bladder. Each decays over game time. Traits multiply decay rates (for example Lazy tires faster).

### 4.2 Autonomy (Sim AI)
When a Sim has no player-queued action, it scores every available interaction by how much it would fix their lowest needs, adds trait likes and dislikes, subtracts distance, adds a little randomness, and picks the best. Autonomy can be turned off per Sim.

### 4.3 Mood
Mood is derived from the average of all needs: Happy, Fine, Uncomfortable, Miserable. The HUD shows the weakest need as the reason.

### 4.4 Traits
A Sim picks up to three traits at creation. A trait defines need-decay multipliers, interaction tags it likes or dislikes, and a charm value that scales friendship gains.

### 4.5 Relationships
Each pair of Sims has a friendship score. Chatting raises it (scaled by charm and shared likes). Reaching the friend threshold fires the `friend_made` event.

### 4.6 Goals
Goals are JSON-defined: an event id, a target count and a money reward. Interactions fire events, the goal system counts them, and completed goals pay out. Goals are setting-neutral, so a modder can write "Harvest 5 crops" or "Attend class" with no code.

### 4.7 Build mode
Grid placement with rotation, drag-painting for floors and walls, delete with 50 percent refund, per-item color tinting, and a runtime-rebuilt navigation mesh so Sims always path around new walls and furniture.

### 4.8 World creator
At the start screen the player picks a preset and then customizes: world name, ground color, sky color, prop density, starting hour, and a seed. The result is saved with the game.

### 4.9 Time
Day and night cycle with a moving sun, sky color shift, and three speed settings plus pause.

## 5. Art direction

- **Stylized realism:** realistic proportions, warm and polished lighting, readable silhouettes.
- **Representation:** skin tones across the full range, plus textured, braided, curly, locs and straight hair styles as equal first-class options.
- **Prototype note:** the code builds characters and furniture from primitive shapes so the game runs with zero imported art. Real models plug in through the `prefab` field on an object definition (Resources path) and, for characters, by replacing `SimFactory.Create`.

## 6. Technical architecture

- Engine: Unity 6 (6000.0 LTS), C#, URP or built-in pipeline.
- Packages: Input System, uGUI, AI Navigation.
- No scene dependency: `GameController` boots itself in any scene and creates camera, lights, UI and world at runtime.
- Data-driven content: `StreamingAssets/Content/*.json` plus user mods in `<persistentDataPath>/Mods/**/*.json`, merged by id so a mod can override a built-in.
- Save files: JSON in `persistentDataPath`.

```
GameController
 ├─ ContentDatabase   (loads JSON packs)
 ├─ GameClock         (time and speed)
 ├─ WorldBuilder      (ground, sky, sun, props)
 ├─ BuildMode         (grid placement, navmesh rebuild)
 ├─ SimCharacter[]    (needs, traits, autonomy, relationships)
 ├─ GoalSystem        (events to goals to rewards)
 └─ UI                (start screen, HUD, build panel, interaction menu)
```

## 7. Roadmap

**Prototype (this repo):** one lot, up to four Sims, six needs, autonomy, goals, build mode, world creator, JSON content, save and load.

**Alpha:** real character models with a morph-based creator, hair card and strand-based braids, animation set (eat, sleep, talk, exercise, study), thin edge-based walls and roofs, multi-floor homes.

**Beta:** open world with streaming (no loading screens), careers and skills, aging and life stages, weather, emotions with memories, family trees and generational consequences.

**1.0:** in-game mod browser, controller support, localization, mobile build.

## 8. Risks

- Character and hair art are the biggest cost. Mitigation: start stylized, buy or license base assets, add detail later.
- Scope creep. Mitigation: ship vertical slices and keep every feature data-driven.
- Navigation with player-built geometry. Mitigation: runtime navmesh rebuild, debounced.
