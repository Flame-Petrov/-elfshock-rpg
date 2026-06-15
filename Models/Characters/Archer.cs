namespace RPG_Game_Elfshock.Models.Characters
{
    public sealed class Archer : Character
    {
        public Archer()
        {
            Strength = 2;
            Agility = 4;
            Intelligence = 0;
            Range = 2;
            Symbol = '#';
        }

        // The archer's damage comes from Agility (dexterity with the bow).
        protected override int AttackStat => Agility;
    }
}
