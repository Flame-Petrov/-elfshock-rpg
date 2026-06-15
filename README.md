# Elfshock

A turn-based, console-style **RPG** built with **WPF** and the **MVVM** pattern on **.NET 10**.
Pick a hero (Warrior, Archer, or Mage), spend buff points, and survive as long as you
can against an endless tide of monsters on a 10×10 grid. Every hero and game you play
is logged to a local SQLite database with Entity Framework Core.

---

## Table of contents

- [Gameplay overview](#gameplay-overview)
- [Heroes & special abilities](#heroes--special-abilities)
- [Technologies & techniques](#technologies--techniques)
- [Project structure](#project-structure)
- [How the code works](#how-the-code-works)
- [Monster pathfinding (BFS)](#monster-pathfinding-bfs)
- [Getting started](#getting-started)
- [Build & distribute](#build--distribute)
- [Inspecting the database](#inspecting-the-database)
- [How to play](#how-to-play)

---

## Gameplay overview

The game runs through four screens: **Main Menu → Character Select → In-Game → Game Over**.

1. **Choose a hero** and optionally spend up to **3 buff points** on Strength, Agility, or Intelligence.
2. You start at the top-left of a **10×10** board (`▒` = empty).
3. Each turn you either **move** one cell (8 directions) or **attack** a monster in range.
4. After your action a **new monster spawns**, then every monster either attacks you
   (if adjacent) or paths toward you (shortest path, avoiding obstacles).
5. The game ends when your hero's health hits 0. Your kill count is your score.

---

## Heroes & special abilities

| Hero | STR | AGI | INT | Range | Damage scales with | Special ability |
|------|----:|----:|----:|------:|--------------------|-----------------|
| **Warrior** `@` | 3 | 3 | 0 | 1 | Strength | **Lifesteal** — heals 5 HP per kill |
| **Archer** `#` | 2 | 4 | 0 | 2 | Agility | **Ricochet** — the arrow bounces between up to 3 lined-up foes |
| **Mage** `*` | 2 | 1 | 3 | 3 | Intelligence | **Arcane Blast** — hits the target and all 8 surrounding tiles |

Derived stats (computed in `Character.Setup()`):

```
Health = Strength × 5
Mana   = Intelligence × 3
Damage = (class's attack stat) × 2
```

Extra rules that make every stat matter:

- **Mana extends range** — every 9 mana grants +1 attack range (`EffectiveRange`).
- **Moving regenerates mana** — each non-attack turn restores a little mana (even at 0 INT),
  so kiting slowly builds up your range.

---

## Technologies & techniques

- **Language / runtime:** C# on **.NET 10** (`net10.0-windows`)
- **UI framework:** **WPF** (Windows Presentation Foundation)
- **Architecture:** **MVVM** (Model–View–ViewModel) with data binding and `INotifyPropertyChanged`
- **Navigation:** view models swapped on a host; views resolved via implicit `DataTemplate`s
- **Persistence:** **Entity Framework Core 10** with the **SQLite** provider
- **Rendering:** the board is an `ItemsControl` + `UniformGrid` of bindable cells
- **Animations:** WPF `Storyboard`s and `DispatcherTimer` (screen transitions, mage blast,
  archer ricochet, damage flash, blinking UI)

---

## Project structure

```
RPG_Game_Elfshock/
├── App.xaml(.cs)              # App startup; ensures the SQLite database exists
├── MainWindow.xaml(.cs)       # Window host: maps VMs → Views, forwards keys, red damage flash
│
├── Models/                    # Pure game logic (no UI)
│   ├── Board.cs               #   10×10 grid: bounds, occupancy, Chebyshev distance
│   ├── GameEngine.cs          #   turn loop, spawning, attacks, monster AI (BFS pathfinding)
│   ├── Movement.cs            #   key → grid offset map (8 directions)
│   ├── GameScreen.cs          #   the four-screen enum
│   └── Characters/            #   Character base + Warrior / Archer / Mage / Monster
│
├── ViewModels/                # MVVM logic + state machines (one per screen)
│   ├── ViewModelBase.cs       #   INotifyPropertyChanged helper
│   ├── MainViewModel.cs       #   navigation host + shared state + damage-flash event
│   ├── MainMenuViewModel.cs
│   ├── CharacterSelectViewModel.cs
│   ├── InGameViewModel.cs     #   board rendering, turns, attack effects
│   ├── GameOverViewModel.cs
│   ├── IKeyInput.cs           #   contract for screens that take keyboard input
│   ├── KeyHelper.cs           #   WPF Key → letter/digit
│   └── BoardCell / ClassOption / ProgressStep / StatDisplay   # small bindable view objects
│
├── Views/                     # XAML for each screen
│   ├── MainMenuView, CharacterSelectView, InGameView, GameOverView
│
├── Controls/
│   └── TransitioningContentControl.cs   # fade + slide animation between screens
│
└── Data/                      # Entity Framework Core
    ├── GameDbContext.cs       #   DbContext (SQLite file: elfshock.db)
    ├── HeroRecord.cs          #   logged hero (class + final stats + created time)
    └── GameRecord.cs          #   logged game (FK → hero, monsters killed, played time)
```

---

## How the code works

**Separation of concerns (MVVM):**

- **Models** hold the rules and state with zero UI knowledge. `GameEngine` drives a turn:
  the player acts, a monster spawns, then monsters attack or path toward the hero using a
  **breadth-first search** (shortest path around obstacles).
- **ViewModels** expose bindable state and translate keyboard input into game actions.
  Each screen is a small **state machine** (e.g. `CharacterSelect` steps:
  Class → Strength → Agility → Intelligence → Ready).
- **Views** are pure XAML bound to the view models; no game logic lives in code-behind.

**Navigation:** `MainViewModel.Current` holds the active screen view model. `MainWindow.xaml`
uses implicit `DataTemplate`s to render the matching view, and a `TransitioningContentControl`
fades/slides between them.

**Input:** `MainWindow` forwards every key press to `Current as IKeyInput`, so whichever
screen is active handles it.

**Persistence:** `App` calls `EnsureCreated()` on startup. The chosen hero is saved when the
game starts; the result (monsters killed) is saved on game over — giving a log of every run.

---

## Monster pathfinding (BFS)

Every turn, each non-adjacent monster needs to take **one step toward the hero** while
walking around any other monsters in the way. This is handled by a **breadth-first search
(BFS)**, recomputed for each monster on every turn, in `GameEngine.FindNextStep`.

How it works:

- **The board is a graph.** Each cell is a node, and a monster may move to any of its
  **8 neighbouring cells** — straight or diagonal — using the same `Movement` offsets the
  hero uses.
- **BFS explores in rings.** Starting from the monster's cell, the search visits all cells
  one step away, then two steps away, and so on. Because every move costs the same, the
  first time the search reaches the hero it has found a **shortest path** (its length equals
  the Chebyshev distance).
- **Only free cells are walkable** (`Board.IsOccupied` is `false`). The hero's own cell is
  accepted as the goal even though it is "occupied", so the search can actually reach it.
- **A `parent` map rebuilds the route.** Each visited cell remembers which cell it came
  from. When the hero is reached, we follow those links back to the start and return only
  the **first step** of the path — the monster advances exactly one cell per turn.
- **No path → wait.** If the hero is unreachable (for example, fully boxed in by other
  monsters), BFS returns nothing and the monster simply holds its position.

**Why BFS instead of A\*:** the grid is tiny (10×10) and all moves are unweighted, so BFS is
already optimal and has no heuristic to tune. Recomputing it per monster each turn keeps the
movement correct as monsters shift around — they naturally route around traffic jams instead
of getting stuck single-file behind one another.

**Cost:** O(cells) per monster — at most ~100 visited cells, which is negligible.

---

## Getting started

### Prerequisites

- **Windows** (WPF is Windows-only)
- **.NET 10 SDK**

### Run it

```bash
# from the project folder
dotnet run
```

Or open `RPG_Game_Elfshock.slnx` in **Visual Studio 2022+** and press **F5**.

The SQLite database (`elfshock.db`) is created automatically next to the executable on first run.

---

## Build & distribute

To run the game on **another Windows PC**, publish it and copy the output over.
(WPF is Windows-only, so the target machine must be Windows.)

### Option A — Self-contained (recommended for testing)

Bundles the .NET runtime inside the app, so the other PC **does not need .NET installed**.

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output folder:

```
bin/Release/net10.0-windows/win-x64/publish/
```

Copy the whole **`publish`** folder to the other PC and run `RPG_Game_Elfshock.exe`.

- `IncludeNativeLibrariesForSelfExtract=true` bundles the native SQLite library so the database works.
- For an ARM machine, use `-r win-arm64` instead.
- The file is large (~150 MB) because it carries the runtime — that's normal for self-contained.

### Option B — Framework-dependent (smaller)

Smaller output, but the target PC must have the **.NET 10 Desktop Runtime** installed.

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

### Notes

- **Do not** use `PublishTrimmed=true` with WPF — trimming breaks XAML reflection.
- `elfshock.db` is created automatically next to the `.exe` on first run, so you don't need
  to copy it (only copy it if you want to carry your history over).

---

## Inspecting the database

Every created hero and every finished game is stored in a local **SQLite** file, so you can
open it and look at your history.

### Where the file is

It lives next to the built executable:

```
RPG_Game_Elfshock/bin/Debug/net10.0-windows/elfshock.db
```

(The path comes from `GameDbContext.OnConfiguring`, which points SQLite at
`elfshock.db` in the app's base directory.)

### What's inside

Two tables, created by EF Core from the entity classes:

**`Heroes`** — one row per hero you created
| Column | Meaning |
|--------|---------|
| `Id` | primary key |
| `HeroType` | `Warrior` / `Archer` / `Mage` |
| `Strength`, `Agility`, `Intelligence` | final stats (base + buff points) |
| `CreatedAt` | when the hero was created |

**`Games`** — one row per finished game
| Column | Meaning |
|--------|---------|
| `Id` | primary key |
| `HeroRecordId` | foreign key → `Heroes.Id` |
| `MonstersKilled` | your score that run |
| `PlayedAt` | when the game ended |

### How to view it

**Option 1 — DB Browser for SQLite (easiest, GUI)**
Download [DB Browser for SQLite](https://sqlitebrowser.org/), open `elfshock.db`, and use the
**Browse Data** tab to view the `Heroes` and `Games` tables.

**Option 2 — VS Code extension**
Install the *SQLite* / *SQLite Viewer* extension, then open `elfshock.db` from the Explorer.

**Option 3 — Command line (`sqlite3`)**

```bash
cd RPG_Game_Elfshock/bin/Debug/net10.0-windows
sqlite3 elfshock.db

# list tables
.tables

# show every hero
SELECT * FROM Heroes;

# show every game with the hero that played it
SELECT g.Id, h.HeroType, g.MonstersKilled, g.PlayedAt
FROM Games g
JOIN Heroes h ON h.Id = g.HeroRecordId
ORDER BY g.PlayedAt DESC;

# leave sqlite
.quit
```

> Tip: to start fresh, simply close the game and delete `elfshock.db` —
> it will be recreated automatically on the next run.

---

## How to play

### Main Menu
Press **any key** to begin.

### Character Select
- **↑ / ↓** or **W / S** — move the arrow between heroes
- **Enter** — choose the highlighted hero
- Then allocate up to **3 buff points**: type a number and press **Enter**
  (press **Enter** alone for 0 points; **Backspace** to edit)
- When points are spent, press **S** to start

### In-Game
Above the board you see your **Health / Mana / Kills** and **STR / AGI / INT / Range**.

**Move** — press **M**, then a direction:

```
 Q  W  E        ↖  ↑  ↗
 A  •  D    =   ←  •  →
 Z  S  X        ↙  ↓  ↘
```

**Attack** — press **A**, then:

- **W / A / S / D** or the **arrow keys** — move the cursor between monsters in range
  (the selected one is highlighted; the panel shows its HP before and after your hit)
- **Enter** — attack
- **Esc** — cancel

Watch your class ability fire: the **Mage** detonates a purple blast, the **Archer**'s arrow
ricochets through aligned foes (doomed ones flash red), and the **Warrior** heals on each kill.
If you take damage the whole screen pulses red. When you can't move, only **Attack** is offered.

### Game Over
Press **any key** to play again.
