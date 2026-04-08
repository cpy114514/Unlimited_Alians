# Pixel 2D Multiplayer Parkour

`Pixel 2D Multiplayer Parkour` is a local multiplayer 2D platformer built in Unity.
It mixes fast platform racing, party-style building, traps, and short competitive rounds.
The project is inspired by games like *Ultimate Chicken Horse*, but it also includes extra modes such as Tag and Story mode.

## Project Status

This project is currently a playable prototype with:

- local multiplayer support
- lobby join system
- party build phase (`Party Box`)
- multiple race maps
- tag mode
- story mode with hearts / damage / respawn
- interactive objects such as trampolines, portals, pipes, launchers, ladders, coins, diamonds, keys, locked chests, saws, enemies, and beacons

## Unity Version

This project uses:

- `Unity 2022.3.62f3`

## Scenes

Main scenes in the project:

- `Assets/Scenes/Lobby.unity`
  Party / local multiplayer lobby
- `Assets/Scenes/Map1.unity`
  Main party race map
- `Assets/Scenes/Map2.unity`
  Smaller farm-themed race map
- `Assets/Scenes/Tag1.unity`
  Tag mode map
- `Assets/Scenes/Story_Start.unity`
  Story mode start / ready screen
- `Assets/Scenes/Story1.unity`
  Story mode level

## Controls

The game supports keyboard players and gamepads.
Players join from the lobby, then keep that input binding in game.

Keyboard layouts:

- Player set 1: `W A S D` + `Q / E / Space`
- Player set 2: `I J K L` + `O / U`
- Player set 3: `Arrow Keys` + `Shift / Enter`

Gamepad:

- movement: `Left Stick` or `D-pad`
- confirm / join: `A`
- leave / back: `B`

## Game Modes

### Party Mode

Players join in the lobby, enter a race map, then play rounds of:

1. `Party Box` item selection
2. building / placing objects
3. racing to the goal
4. score / match result

Objects can include blocks, ladders, portals, launchers, saws, trampolines, coins, and more.

### Tag Mode

Players try to survive while the active tagged player pressures the others.
This mode ends in a single result screen instead of a normal score loop.

### Story Mode

Story mode uses hearts instead of party scoring.
Players can take damage, respawn, collect coins, and progress through a separate level flow.

## How To Run

1. Open the project in Unity `2022.3.62f3`
2. Open one of these scenes:
   - `Assets/Scenes/Lobby.unity` for party / tag testing
   - `Assets/Scenes/Story_Start.unity` for story mode
3. Press Play in the Unity Editor

## Notes

- This is a local multiplayer project. It is not an online multiplayer game.
- Many systems are designed to be edited directly in the Unity Editor.
- The project contains several runtime-generated UI elements, but most gameplay objects are exposed for editing through prefabs and inspector fields.

## Credits

This project uses pixel-art resources and tile packs from Kenney alongside custom project-specific logic and setup.
