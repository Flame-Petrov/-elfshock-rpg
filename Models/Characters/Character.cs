namespace RPG_Game_Elfshock.Models.Characters
{
    /// <summary>The primary attributes a stat pickup can boost.</summary>
    public enum StatKind { Strength, Agility, Intelligence }

    /// <summary>
    /// Shared base for every fighter on the board (the three races and the monster).
    /// Holds the primary attributes, the derived combat stats and the board position.
    /// </summary>
    public abstract class Character
    {
        // Primary attributes.
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Intelligence { get; set; }

        // How far this character can move/attack and how it is drawn on the field.
        public int Range { get; protected set; }
        public char Symbol { get; protected set; }

        // Derived combat stats (filled in by Setup).
        public int Health { get; set; }
        public int Mana { get; set; }
        public int Damage { get; set; }

        // Position on the 10x10 board (0-based).
        public int Row { get; set; }
        public int Col { get; set; }

        public bool IsAlive => Health > 0;

        /// <summary>Full health, used to cap healing (matches the Setup formula).</summary>
        public int MaxHealth => Strength * 5;

        // Every full chunk of this much mana lends +1 to the attack range.
        public const int ManaPerRangeBonus = 9;

        /// <summary>
        /// The attack range actually used in combat: the base Range plus a bonus
        /// granted by mana (so Intelligence/Mana finally matter for casters).
        /// </summary>
        public int EffectiveRange => Range + Mana / ManaPerRangeBonus;

        /// <summary>
        /// The attribute this character attacks with; the points spent on it drive
        /// Damage. Each class picks the stat that fits its identity.
        /// </summary>
        protected abstract int AttackStat { get; }

        /// <summary>
        /// Derives the combat stats from the primary attributes. Health and Mana use
        /// the task's formulas; Damage scales with the class's own attack stat, so
        /// every class's invested points actually affect combat.
        /// </summary>
        public void Setup()
        {
            Health = Strength * 5;
            Mana = Intelligence * 3;
            Damage = AttackStat * 2;
        }

        /// <summary>
        /// Adds +1 to a primary stat from a pickup, updating derived stats
        /// incrementally so current Health/Mana are not reset to full.
        /// </summary>
        public void GainStatPoint(StatKind stat)
        {
            switch (stat)
            {
                case StatKind.Strength:
                    Strength++;
                    Health += 5; // +max HP and a little current health
                    break;
                case StatKind.Agility:
                    Agility++;
                    break;
                case StatKind.Intelligence:
                    Intelligence++;
                    Mana += 3; // more mana → more range via EffectiveRange
                    break;
            }

            Damage = AttackStat * 2; // recompute damage for the class's attack stat
        }
    }
}
