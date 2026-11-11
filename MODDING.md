# Modding OpenLife

All content is plain JSON. No code needed.

## Where files go

- Built-in content: `Assets/StreamingAssets/Content/*.json`
- Your mods: `<persistentDataPath>/Mods/<any-folder>/*.json`
  - Windows: `%USERPROFILE%\AppData\LocalLow\<Company>\<Product>\Mods\`
  - The game logs the exact path in the console on start.

Files load alphabetically, built-in first, then mods. Items are merged **by `id`**: reuse an id to override something, use a new id to add something.

Each file may contain any of these four arrays (all optional): `traits`, `objects`, `goals`, `worlds`.

## Needs

Valid need names: `Hunger`, `Energy`, `Fun`, `Social`, `Hygiene`, `Bladder`.

## Objects

```json
{
  "objects": [
    {
      "id": "hammock",
      "name": "Hammock",
      "category": "Furniture",
      "price": 250,
      "sizeX": 2, "sizeZ": 1,
      "height": 0.6,
      "shape": "box",
      "color": "#c9a66b",
      "prefab": "",
      "interactions": [
        {
          "id": "nap_hammock",
          "name": "Nap in Hammock",
          "tag": "relax",
          "minutes": 90,
          "effects": [ { "need": "Energy", "amount": 40 }, { "need": "Fun", "amount": 10 } ],
          "money": 0,
          "goalEvent": "rest"
        }
      ]
    }
  ]
}
```

- `category`: `Floor` and `Wall` get special placement behavior (drag painting, floors are walkable, walls block). Any other name creates a new tab in build mode.
- `shape`: `box`, `cylinder`, `sphere`, `capsule`, `lintel` (a doorway top).
- `prefab`: optional path under a `Resources` folder. When set, that prefab is used instead of the primitive shape.
- `effects.amount` is the **total** change over the interaction's `minutes` (negative values drain a need).
- `tag` is matched against trait `likes` and `dislikes`.
- `goalEvent` fires a goal event when the interaction finishes.

## Traits

```json
{
  "traits": [
    {
      "id": "night_owl",
      "name": "Night Owl",
      "description": "Gets tired later and loves late fun.",
      "decayMultipliers": [ { "need": "Energy", "amount": 0.7 } ],
      "likes": [ "fun", "social" ],
      "dislikes": [ "study" ],
      "charm": 1.1
    }
  ]
}
```

## Goals

```json
{
  "goals": [
    { "id": "harvest_five", "title": "Harvest 5 crops", "eventId": "harvest", "target": 5, "reward": 300 }
  ]
}
```

Any interaction with `"goalEvent": "harvest"` counts toward it. Built-in events: `chat`, `friend_made`.

## Worlds (settings)

```json
{
  "worlds": [
    {
      "id": "volcano_isle",
      "name": "Volcano Isle",
      "description": "Black sand and warm light.",
      "groundColor": "#2b2b2f",
      "skyColor": "#f0a36b",
      "sunColor": "#ffb36b",
      "sunIntensity": 1.0,
      "fog": 0.01,
      "size": 70,
      "startHour": 17,
      "scatter": [
        { "shape": "rock",  "color": "#3a3a3f", "density": 0.5, "minScale": 0.8, "maxScale": 2.2, "height": 1.2, "width": 1.0 },
        { "shape": "palm",  "color": "#3f8f4a", "density": 0.3, "minScale": 0.9, "maxScale": 1.4, "height": 4.0, "width": 0.5 }
      ]
    }
  ]
}
```

Scatter shapes: `tree`, `pine`, `palm`, `rock`, `cactus`, `tower`. `density` is roughly props per 100 square meters (the player can scale it in the world creator).

Colors are `#RRGGBB` hex strings.

## Tips

- If a file fails to parse, the game logs a warning with the file name and skips only that file.
- Keep ids lowercase with underscores.
- To share a mod, zip the folder and tell people to unzip it into their `Mods` folder.
