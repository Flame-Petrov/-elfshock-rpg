using System;

namespace RPG_Game_Elfshock.Models.Characters
{
    public sealed class Monster : Character
    {
        public Monster(Random rng)
        {
            // Strength, Agility and Intelligence are random between 1 and 3.
            Strength = rng.Next(1, 4);
            Agility = rng.Next(1, 4);
            Intelligence = rng.Next(1, 4);
            Range = 1;
            Symbol = '◙';
            Setup();
        }

        // Monsters keep the original Agility-based damage.
        protected override int AttackStat => Agility;
    }
}
