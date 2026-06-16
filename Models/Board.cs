using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RPG_Game_Elfshock.Models.Characters;

namespace RPG_Game_Elfshock.Models
{
    /// <summary>
    /// The 10x10 battlefield. Owns the hero and the live monsters and knows how to
    /// answer spatial questions (bounds, occupancy, distance) and draw itself.
    /// </summary>
    public sealed class Board
    {
        public const int Size = 10;
        public const char EmptyCell = '▒';

        public Character Hero { get; }
        public List<Monster> Monsters { get; } = new List<Monster>();
        public List<Pickup> Pickups { get; } = new List<Pickup>();

        public Board(Character hero)
        {
            Hero = hero;
            // The hero starts at point (1,1) — the top-left cell in 0-based terms.
            Hero.Row = 0;
            Hero.Col = 0;
        }

        public bool InBounds(int row, int col) =>
            row >= 0 && row < Size && col >= 0 && col < Size;

        public bool IsOccupied(int row, int col) =>
            (Hero.Row == row && Hero.Col == col) ||
            Monsters.Any(m => m.Row == row && m.Col == col);

        /// <summary>The pickup on a cell, or null. Pickups never block movement.</summary>
        public Pickup? PickupAt(int row, int col) =>
            Pickups.FirstOrDefault(p => p.Row == row && p.Col == col);

        /// <summary>
        /// Chebyshev distance: a diagonal step counts as 1, matching the 8-direction movement.
        /// </summary>
        public int Distance(Character a, Character b) =>
            Distance(a.Row, a.Col, b.Row, b.Col);

        /// <summary>Chebyshev distance between two raw board coordinates.</summary>
        public int Distance(int rowA, int colA, int rowB, int colB) =>
            Math.Max(Math.Abs(rowA - rowB), Math.Abs(colA - colB));

        public List<(int Row, int Col)> EmptyCells()
        {
            var cells = new List<(int, int)>();
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (!IsOccupied(r, c))
                        cells.Add((r, c));
            return cells;
        }

        /// <summary>Renders the field as monospace text using the task's symbols.</summary>
        public string Render()
        {
            var grid = new char[Size, Size];
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    grid[r, c] = EmptyCell;

            foreach (var monster in Monsters)
                grid[monster.Row, monster.Col] = monster.Symbol;

            grid[Hero.Row, Hero.Col] = Hero.Symbol;

            var sb = new StringBuilder();
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    sb.Append(grid[r, c]);
                    if (c < Size - 1)
                        sb.Append(' ');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
