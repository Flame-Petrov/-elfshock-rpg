using System;
using System.Collections.Generic;

namespace RPG_Game_Elfshock.Data
{
    /// <summary>
    /// EF Core entity that logs a created hero: which race was chosen, the final
    /// stats (base + buff points) and when it was created.
    /// </summary>
    public class HeroRecord
    {
        public int Id { get; set; }

        public string HeroType { get; set; } = string.Empty;

        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Intelligence { get; set; }

        public DateTime CreatedAt { get; set; }

        // Every game that was played with this hero.
        public List<GameRecord> Games { get; set; } = new List<GameRecord>();
    }
}
