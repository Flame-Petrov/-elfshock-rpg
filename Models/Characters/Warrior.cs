namespace RPG_Game_Elfshock.Models.Characters
{
    public sealed class Warrior : Character
    {
        public Warrior()
        {
            Strength = 3;
            Agility = 3;
            Intelligence = 0;
            Range = 1;
            Symbol = '@';
        }

        // The warrior swings with raw Strength.
        protected override int AttackStat => Strength;
    }
}
