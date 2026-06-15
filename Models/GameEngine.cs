using System;
using System.Collections.Generic;
using System.Linq;
using RPG_Game_Elfshock.Models.Characters;

namespace RPG_Game_Elfshock.Models
{
    /// <summary>
    /// Drives the turn-based combat: the player acts, a new monster spawns,
    /// then every monster either attacks (if adjacent) or steps toward the player.
    /// </summary>
    public sealed class GameEngine
    {
        // The hero has a safe zone of 1 cell in every direction; monsters never
        // spawn inside it (i.e. not on any of the 8 cells around the hero).
        private const int SafeZoneRadius = 1;

        // On every non-attack (move) turn the hero regenerates a little mana,
        // even with 0 Intelligence, up to this cap. Combined with EffectiveRange,
        // moving instead of attacking slowly builds up the hero's attack range.
        private const int ManaRegenPerMove = 1;
        private const int MaxMana = 30;

        // Archer ricochet and warrior life-steal tuning.
        private const int MaxArrowTargets = 3;
        private const int MaxBounceDistance = 4;
        private const int LateralTolerance = 2; // how far the arrow may drift sideways
        private const int WarriorHealPerKill = 5;

        private readonly Random _rng = new Random();

        public Board Board { get; }
        public int MonstersKilled { get; private set; }
        public bool IsGameOver => !Board.Hero.IsAlive;

        public GameEngine(Character hero)
        {
            Board = new Board(hero);
        }

        /// <summary>Moves the hero one cell if the target is on the board and free.</summary>
        public bool TryMoveHero(Offset offset)
        {
            int row = Board.Hero.Row + offset.DRow;
            int col = Board.Hero.Col + offset.DCol;

            if (!Board.InBounds(row, col) || Board.IsOccupied(row, col))
                return false;

            Board.Hero.Row = row;
            Board.Hero.Col = col;

            // A move is a non-attack turn, so the hero recovers some mana.
            RegenerateMana();
            return true;
        }

        /// <summary>Restores a little of the hero's mana, capped at MaxMana.</summary>
        private void RegenerateMana()
        {
            Character hero = Board.Hero;
            hero.Mana = Math.Min(MaxMana, hero.Mana + ManaRegenPerMove);
        }

        /// <summary>True if at least one of the 8 directions is a free cell to step onto.</summary>
        public bool CanHeroMove()
        {
            foreach (Offset off in Movement.Keys.Values)
            {
                int row = Board.Hero.Row + off.DRow;
                int col = Board.Hero.Col + off.DCol;
                if (Board.InBounds(row, col) && !Board.IsOccupied(row, col))
                    return true;
            }
            return false;
        }

        /// <summary>Every monster the hero can currently reach with an attack.</summary>
        public IReadOnlyList<Monster> TargetsInRange() =>
            Board.Monsters
                 .Where(m => Board.Distance(Board.Hero, m) <= Board.Hero.EffectiveRange)
                 .ToList();

        /// <summary>
        /// The monsters a hit on <paramref name="target"/> will strike, by class:
        /// Mage = 3x3 blast around the target; Archer = a ricochet chain of up to 3;
        /// everyone else = just the target. Pure: applies no damage.
        /// </summary>
        public IReadOnlyList<Monster> ResolveAttackTargets(Monster target)
        {
            if (Board.Hero is Mage)
                return Board.Monsters.Where(m => Board.Distance(m, target) <= 1).ToList();

            if (Board.Hero is Archer)
                return BounceChain(target);

            return new List<Monster> { target };
        }

        /// <summary>Resolves and applies a hit on the target (area/ricochet/single).</summary>
        public void Attack(Monster target) => ApplyDamage(ResolveAttackTargets(target));

        /// <summary>Deals the hero's damage to each monster in the list.</summary>
        public void ApplyDamage(IReadOnlyList<Monster> targets)
        {
            foreach (Monster m in targets.ToList())
                DealDamage(m);
        }

        /// <summary>
        /// The arrow chain: it flies in the direction from the hero to the first
        /// target and keeps going forward, hitting monsters lined up behind it. At
        /// each bounce it may drift only a little sideways (LateralTolerance) and
        /// never doubles back, so it won't jump to a monster on the opposite side.
        /// Up to 3 monsters total.
        /// </summary>
        private IReadOnlyList<Monster> BounceChain(Monster target)
        {
            var chain = new List<Monster> { target };

            // Travel direction (one of the 8 directions) from the hero to the target.
            int dirRow = Math.Sign(target.Row - Board.Hero.Row);
            int dirCol = Math.Sign(target.Col - Board.Hero.Col);
            if (dirRow == 0 && dirCol == 0)
                return chain;

            Monster current = target;
            while (chain.Count < MaxArrowTargets)
            {
                Monster? next = Board.Monsters
                    .Where(m => !chain.Contains(m)
                                && Forward(current, m, dirRow, dirCol) > 0            // ahead, never backward
                                && Lateral(current, m, dirRow, dirCol) <= LateralTolerance // only a slight sideways drift
                                && Board.Distance(current, m) <= MaxBounceDistance)
                    .OrderBy(m => Forward(current, m, dirRow, dirCol))                // the closest one ahead
                    .ThenBy(m => Lateral(current, m, dirRow, dirCol))
                    .FirstOrDefault();

                if (next is null)
                    break;

                chain.Add(next);
                current = next;
            }

            return chain;
        }

        /// <summary>Progress along the travel direction; positive means ahead of the last hit.</summary>
        private static int Forward(Character from, Character m, int dirRow, int dirCol)
            => (m.Row - from.Row) * dirRow + (m.Col - from.Col) * dirCol;

        /// <summary>Sideways offset from the travel line; 0 means dead on the line.</summary>
        private static int Lateral(Character from, Character m, int dirRow, int dirCol)
            => Math.Abs((m.Col - from.Col) * dirRow - (m.Row - from.Row) * dirCol);

        private void DealDamage(Monster monster)
        {
            monster.Health -= Board.Hero.Damage;
            if (!monster.IsAlive)
            {
                Board.Monsters.Remove(monster);
                MonstersKilled++;

                // The warrior drains life: each kill restores some health (capped).
                if (Board.Hero is Warrior)
                    Board.Hero.Health = Math.Min(Board.Hero.MaxHealth, Board.Hero.Health + WarriorHealPerKill);
            }
        }

        /// <summary>Resolves the rest of the turn after the player has acted.</summary>
        public void EndTurn()
        {
            // The existing monsters act first; the new monster only appears once
            // the dust settles, so it cannot move or attack on the turn it spawns.
            MonstersAct();
            if (!IsGameOver)
                SpawnMonster();
        }

        private void SpawnMonster()
        {
            var empty = Board.EmptyCells();

            // Prefer cells outside the hero's 1-cell safe zone, so the player is
            // never hit by a monster the instant it appears. Fall back to any free
            // cell only if the board is too crowded to honour the safe zone.
            var safe = empty
                .Where(cell => Board.Distance(cell.Row, cell.Col, Board.Hero.Row, Board.Hero.Col) > SafeZoneRadius)
                .ToList();
            var candidates = safe.Count > 0 ? safe : empty;

            if (candidates.Count == 0)
                return;

            var (row, col) = candidates[_rng.Next(candidates.Count)];
            Board.Monsters.Add(new Monster(_rng) { Row = row, Col = col });
        }

        private void MonstersAct()
        {
            foreach (var monster in Board.Monsters.ToList())
            {
                // Adjacent monsters always attack; the rest close in on the hero.
                if (Board.Distance(monster, Board.Hero) <= monster.Range)
                {
                    Board.Hero.Health -= monster.Damage;
                }
                else
                {
                    var step = FindNextStep(monster);
                    if (step is not null)
                    {
                        monster.Row = step.Value.Row;
                        monster.Col = step.Value.Col;
                    }
                }

                if (IsGameOver)
                    break;
            }
        }

        /// <summary>
        /// Breadth-first search for the shortest route from the monster to the hero
        /// across free cells (8-direction steps). Recomputed every time a monster
        /// acts, so it always reflects the current board and naturally routes around
        /// other monsters that are in the way. Returns the single next cell to step
        /// onto, or null when no route to the hero currently exists.
        /// </summary>
        private (int Row, int Col)? FindNextStep(Monster monster)
        {
            int size = Board.Size;
            Character hero = Board.Hero;

            var visited = new bool[size, size];
            var parent = new (int Row, int Col)[size, size];
            var queue = new Queue<(int Row, int Col)>();

            var start = (Row: monster.Row, Col: monster.Col);
            visited[start.Row, start.Col] = true;
            parent[start.Row, start.Col] = (-1, -1);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();

                foreach (Offset off in Movement.Keys.Values)
                {
                    int nr = cell.Row + off.DRow;
                    int nc = cell.Col + off.DCol;
                    if (!Board.InBounds(nr, nc))
                        continue;

                    // The hero's own cell is the destination: it counts as reached
                    // even though it is "occupied" by the hero.
                    if (nr == hero.Row && nc == hero.Col)
                        return FirstStep(parent, start, cell);

                    // Every other cell on the path must be free to walk through.
                    if (visited[nr, nc] || Board.IsOccupied(nr, nc))
                        continue;

                    visited[nr, nc] = true;
                    parent[nr, nc] = cell;
                    queue.Enqueue((nr, nc));
                }
            }

            return null; // The hero is unreachable right now (e.g. fully walled in).
        }

        /// <summary>Walks the parent links back to the first cell after the start.</summary>
        private static (int Row, int Col) FirstStep(
            (int Row, int Col)[,] parent, (int Row, int Col) start, (int Row, int Col) lastBeforeHero)
        {
            var step = lastBeforeHero;
            while (parent[step.Row, step.Col] != start)
                step = parent[step.Row, step.Col];
            return step;
        }
    }
}
