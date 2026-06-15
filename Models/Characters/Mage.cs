namespace RPG_Game_Elfshock.Models.Characters
{
    public sealed class Mage : Character
    {
        public Mage()
        {
            Strength = 2;
            Agility = 1;
            Intelligence = 3;
            Range = 3;
            Symbol = '*';
        }

        // The mage channels Intelligence into its spells.
        protected override int AttackStat => Intelligence;
    }
}
