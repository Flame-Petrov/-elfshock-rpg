namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// One cell of the rendered battlefield. The flags let the view colour the
    /// hero, the monsters, the highlighted target and the attack effects
    /// (mage blast, archer ricochet hit/next markers, and foes about to be killed).
    /// </summary>
    public sealed class BoardCell
    {
        public BoardCell(
            string symbol,
            bool isHero,
            bool isMonster,
            bool isSelectedTarget,
            bool isBlast,
            bool isArrowHit,
            bool isArrowNext,
            bool isDoomed)
        {
            Symbol = symbol;
            IsHero = isHero;
            IsMonster = isMonster;
            IsSelectedTarget = isSelectedTarget;
            IsBlast = isBlast;
            IsArrowHit = isArrowHit;
            IsArrowNext = isArrowNext;
            IsDoomed = isDoomed;
        }

        public string Symbol { get; }
        public bool IsHero { get; }
        public bool IsMonster { get; }
        public bool IsSelectedTarget { get; }
        public bool IsBlast { get; }

        /// <summary>The monster being struck this step (it shakes).</summary>
        public bool IsArrowHit { get; }

        /// <summary>The next monster the arrow will fly to (marked).</summary>
        public bool IsArrowNext { get; }

        public bool IsDoomed { get; }
    }
}
