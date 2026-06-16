# Implementation Plan — Stat Pickups

A new feature: collectible **stat points** spawn on the board every few turns. Walking
onto one lets the player add **+1 to a stat of their choice** (Strength, Agility, or
Intelligence).

---

## 1. Behaviour

- A pickup appears on a **random free cell** every **5 turns**.
- The hero **collects** it by stepping onto that cell while moving.
- On collection the player picks **one stat** to gain **+1**.
- Allocating the point **does not cost a turn** — it happens right after the turn resolves.

---

## 2. Data model

### New file: `Models/Pickup.cs`
```csharp
public sealed class Pickup
{
    public int Row { get; set; }
    public int Col { get; set; }
    public char Symbol { get; } = '✚'; // drawn green on the board
}
```

### `Models/Board.cs`
- Add `public List<Pickup> Pickups { get; } = new();`
- Add helper `Pickup? PickupAt(int row, int col)`.
- Pickups **do not block movement** — keep `IsOccupied` to hero + monsters only, so the
  hero can step onto a pickup. Spawning (monsters/pickups) must still avoid pickup cells.

---

## 3. Spawning every 5 turns — `Models/GameEngine.cs`

- Add `public int TurnCount { get; private set; }` and `private const int PickupInterval = 5;`
- In `EndTurn()` (after the monster phase): `TurnCount++; if (TurnCount % PickupInterval == 0) SpawnPickup();`
- `SpawnPickup()`: pick a free cell (like `SpawnMonster`, but also excluding existing
  pickups) and add a `Pickup`. If no free cell exists, skip.
- Optional: cap simultaneous pickups with `MaxPickupsOnBoard` (e.g. 3) so they don't pile up.

---

## 4. Collection — in `TryMoveHero`

- After the hero moves, check `Board.PickupAt(newRow, newCol)`.
- If found: remove it from `Board.Pickups` and increment a counter
  `public int PendingStatPoints { get; private set; }` (supports collecting several quickly).

---

## 5. Applying the point (without resetting current resources)

**Do not call `Character.Setup()`** — it refills Health/Mana to max. Use an incremental method.

### `Models/Characters/Character.cs`
```csharp
public enum StatKind { Strength, Agility, Intelligence }

public void GainStatPoint(StatKind stat)
{
    switch (stat)
    {
        case StatKind.Strength:     Strength++;     Health += 5; break; // +max HP and +5 current
        case StatKind.Agility:      Agility++;                   break;
        case StatKind.Intelligence: Intelligence++; Mana += 3;   break; // +mana → +range
    }
    Damage = AttackStat * 2; // recompute only the damage
}
```
This keeps current Health/Mana intact: STR adds health, INT adds mana (and thus range via
`EffectiveRange`), AGI feeds the class's attack stat — and `Damage` is recomputed.

---

## 6. UI flow — `ViewModels/InGameViewModel.cs`

- New mode: `enum Mode { ChooseAction, Move, Attack, AllocatePoint }`.
- In `ResolveTurn()`, **after** the Game Over check: if `_engine.PendingStatPoints > 0`,
  switch to `Mode.AllocatePoint`.
- In `AllocatePoint`, the input chooses a stat (recommended: **arrows / W,S + Enter**,
  consistent with class select and targeting; alternative: number keys `1/2/3`).
- On choose: `hero.GainStatPoint(stat)`, then `PendingStatPoints--`. If more remain, stay in
  the mode; otherwise return to `Mode.ChooseAction`.
- `BuildOptions()` for `AllocatePoint`: a prompt like
  `Pickup collected! Add a point:  [↑/↓] choose   [Enter] confirm`, plus the current stats.

---

## 7. Rendering — `ViewModels/BoardCell.cs` + `Views/InGameView.xaml`

- `BoardCell`: add an `IsPickup` flag.
- In `BuildCells()`: if a cell holds a pickup (and no hero/monster sits on it), emit a cell
  with `IsPickup = true` and the pickup symbol.
- XAML: a `DataTrigger` on `IsPickup` colours the symbol green (`#FF66FF99`) with a gentle
  pulse storyboard so it stands out.
- Priority: hero/monster on a cell take visual precedence over the pickup symbol.

---

## 8. Files touched

| File | Change |
|------|--------|
| `Models/Pickup.cs` | **new** — pickup data |
| `Models/Board.cs` | `Pickups` list, `PickupAt`, exclude pickups when spawning |
| `Models/GameEngine.cs` | `TurnCount`, `SpawnPickup`, `PendingStatPoints`, collection in `TryMoveHero` |
| `Models/Characters/Character.cs` | `GainStatPoint` + `StatKind` enum |
| `ViewModels/InGameViewModel.cs` | `AllocatePoint` mode, choose flow, pickup rendering |
| `ViewModels/BoardCell.cs` | `IsPickup` flag |
| `Views/InGameView.xaml` | pickup colour/pulse + allocation prompt |

---

## 9. Tunable constants (in `GameEngine`)

- `PickupInterval = 5` — how often a pickup spawns.
- *(optional)* `MaxPickupsOnBoard = 3` — cap on simultaneous pickups.

---

## 10. Edge cases

- **No free cell** → skip spawning that turn.
- **Hero dies the same turn** → Game Over first; the pending point is dropped (check before
  entering `AllocatePoint`).
- **Two pickups collected quickly** → `PendingStatPoints` stacks; the player chooses one by one.
- **Monsters and pickups** → by default only the hero collects; monsters ignore/pass over them.
- **Spawning onto a pickup** → prevented (free-cell search excludes pickup cells).

---

## 11. Suggested build order

1. `Pickup` + `Board.Pickups` / `PickupAt`.
2. `GameEngine`: turn counter, `SpawnPickup`, collection, `PendingStatPoints`.
3. `Character.GainStatPoint` + `StatKind`.
4. Rendering: `BoardCell.IsPickup` + `BuildCells` + XAML colour.
5. `AllocatePoint` mode + prompt.
6. Test: on turn 5 a pickup appears; step on it → choose a stat → see the change in the top bar.

---

## Open decision

- **Stat selection input:** number keys `1/2/3` (fastest) **or** arrows / `W`,`S` + `Enter`
  (consistent with the rest of the menus). Recommended: arrows / `W`,`S` + `Enter`.
