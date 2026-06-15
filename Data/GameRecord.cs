using System;

namespace RPG_Game_Elfshock.Data
{
    /// <summary>
    /// EF Core entity that logs one played game: a reference to the chosen hero
    /// and how many monsters that hero killed.
    /// </summary>
    public class GameRecord
    {
        public int Id { get; set; }

        public int HeroRecordId { get; set; }
        public HeroRecord Hero { get; set; } = null!;

        public int MonstersKilled { get; set; }

        public DateTime PlayedAt { get; set; }
    }
}
