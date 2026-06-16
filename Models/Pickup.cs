namespace RPG_Game_Elfshock.Models
{
    /// <summary>
    /// A collectible point on the board. Walking onto it lets the hero add +1 to a stat.
    /// Pickups do not block movement — the hero steps onto the cell to collect.
    /// </summary>
    public sealed class Pickup
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public char Symbol => '✚';
    }
}
