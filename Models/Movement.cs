using System.Collections.Generic;

namespace RPG_Game_Elfshock.Models
{
    /// <summary>A single step on the grid: change in row and column.</summary>
    public readonly record struct Offset(int DRow, int DCol);

    /// <summary>
    /// Maps the movement keys from the task to grid offsets.
    /// Rows grow downward, columns grow to the right.
    /// </summary>
    public static class Movement
    {
        public static readonly IReadOnlyDictionary<char, Offset> Keys = new Dictionary<char, Offset>
        {
            ['W'] = new Offset(-1, 0),  // up
            ['S'] = new Offset(1, 0),   // down
            ['A'] = new Offset(0, -1),  // left
            ['D'] = new Offset(0, 1),   // right
            ['E'] = new Offset(-1, 1),  // diagonally up & right
            ['X'] = new Offset(1, 1),   // diagonally down & right
            ['Q'] = new Offset(-1, -1), // diagonally up & left
            ['Z'] = new Offset(1, -1),  // diagonally down & left
        };
    }
}
