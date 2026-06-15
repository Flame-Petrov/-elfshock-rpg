using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using RPG_Game_Elfshock.Data;
using RPG_Game_Elfshock.Models;
using RPG_Game_Elfshock.Models.Characters;

namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// InGame screen. Renders the board and drives one turn at a time:
    /// the player chooses Move or Attack, then the engine resolves the monsters.
    /// </summary>
    public sealed class InGameViewModel : ViewModelBase, IKeyInput
    {
        private enum Mode { ChooseAction, Move, Attack }

        private readonly MainViewModel _main;
        private readonly GameEngine _engine;
        private Mode _mode = Mode.ChooseAction;
        private IReadOnlyList<Monster> _targets = new List<Monster>();
        private int _selectedTargetIndex;
        private bool _gameSaved;

        // While an attack effect plays, input is ignored.
        private bool _resolving;

        // Mage blast: cells that blink purple.
        private IReadOnlyList<(int Row, int Col)> _blastCells = Array.Empty<(int, int)>();

        // Archer ricochet: the ordered chain and how many have been struck so far.
        private IReadOnlyList<Monster> _arrowChain = Array.Empty<Monster>();
        private int _arrowHitCount;

        public InGameViewModel(MainViewModel main)
        {
            _main = main;
            Character hero = main.Hero
                ?? throw new InvalidOperationException("Cannot start the game without a selected hero.");
            _engine = new GameEngine(hero);
            Refresh();
        }

        private IReadOnlyList<BoardCell> _cells = new List<BoardCell>();
        public IReadOnlyList<BoardCell> Cells
        {
            get => _cells;
            private set => SetProperty(ref _cells, value);
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        private string _statsText = string.Empty;
        public string StatsText
        {
            get => _statsText;
            private set => SetProperty(ref _statsText, value);
        }

        private string _optionsText = string.Empty;
        public string OptionsText
        {
            get => _optionsText;
            private set => SetProperty(ref _optionsText, value);
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            private set => SetProperty(ref _message, value);
        }

        public void OnKey(Key key)
        {
            if (_resolving)
                return; // ignore input while the blast effect is playing

            switch (_mode)
            {
                case Mode.ChooseAction:
                    HandleChooseAction(key);
                    break;
                case Mode.Move:
                    HandleMove(key);
                    break;
                case Mode.Attack:
                    HandleAttack(key);
                    break;
            }
        }

        private void HandleChooseAction(Key key)
        {
            char? letter = KeyHelper.ToLetter(key);
            if (letter == 'M')
            {
                if (!_engine.CanHeroMove())
                {
                    Message = "There's no room to move — you can only attack.";
                    Refresh();
                    return;
                }

                _mode = Mode.Move;
                Message = string.Empty;
                Refresh();
            }
            else if (letter == 'A')
            {
                EnterAttackMode();
            }
        }

        private void HandleMove(Key key)
        {
            char? letter = KeyHelper.ToLetter(key);
            if (letter is null || !Movement.Keys.TryGetValue(letter.Value, out var offset))
                return; // ignore keys that are not movement keys

            if (_engine.TryMoveHero(offset))
            {
                ResolveTurn();
            }
            else
            {
                Message = "You can't move there. Try another direction.";
                Refresh();
            }
        }

        private void EnterAttackMode()
        {
            // Only monsters within range are selectable, nearest first.
            _targets = TargetsByDistance();
            if (_targets.Count == 0)
            {
                // No target means the turn is not spent; stay on the action menu.
                Message = "No available targets in your range.";
                _mode = Mode.ChooseAction;
                Refresh();
                return;
            }

            _mode = Mode.Attack;
            _selectedTargetIndex = 0; // the closest target
            Message = string.Empty;
            Refresh();
        }

        private IReadOnlyList<Monster> TargetsByDistance()
        {
            Character hero = _engine.Board.Hero;
            return _engine.TargetsInRange()
                .OrderBy(m => _engine.Board.Distance(hero, m))
                .ToList();
        }

        private void HandleAttack(Key key)
        {
            char? letter = KeyHelper.ToLetter(key);

            bool previous = key is Key.Up or Key.Left || letter is 'W' or 'A';
            bool next = key is Key.Down or Key.Right || letter is 'S' or 'D';

            if (previous)
            {
                MoveTargetSelection(-1);
            }
            else if (next)
            {
                MoveTargetSelection(1);
            }
            else if (key == Key.Enter)
            {
                PerformAttack(_targets[_selectedTargetIndex]);
            }
            else if (key == Key.Escape)
            {
                // Back out without spending the turn.
                _mode = Mode.ChooseAction;
                Message = string.Empty;
                Refresh();
            }
        }

        private void MoveTargetSelection(int delta)
        {
            int count = _targets.Count;
            _selectedTargetIndex = (_selectedTargetIndex + delta + count) % count; // wraps around
            Refresh();
        }

        private void PerformAttack(Monster target)
        {
            if (_engine.Board.Hero is Mage)
                PerformMageAttack(target);
            else if (_engine.Board.Hero is Archer)
                PerformArcherAttack(target);
            else
            {
                _engine.Attack(target);
                ResolveTurn();
            }
        }

        private void PerformMageAttack(Monster target)
        {
            // The 8 cells around the target blink purple (the blast).
            _blastCells = BlastArea(target);
            _engine.Attack(target);

            if (_blastCells.Count == 0)
            {
                ResolveTurn();
                return;
            }

            _resolving = true;
            Refresh();
            RunAfter(500, () =>
            {
                _blastCells = Array.Empty<(int, int)>();
                _resolving = false;
                ResolveTurn();
            });
        }

        private void PerformArcherAttack(Monster target)
        {
            // Work out the ricochet chain up front so we can animate each hit and
            // mark the doomed monsters before any damage is applied.
            _arrowChain = _engine.ResolveAttackTargets(target);
            _arrowHitCount = 0;
            _resolving = true;
            Refresh();
            StepArrow();
        }

        private void StepArrow()
        {
            RunAfter(280, () =>
            {
                _arrowHitCount++;
                Refresh(); // shake the monster just struck and mark the next one

                if (_arrowHitCount < _arrowChain.Count)
                {
                    StepArrow();
                    return;
                }

                // Last monster struck; let its shake finish, then apply the damage.
                RunAfter(280, () =>
                {
                    _engine.ApplyDamage(_arrowChain);
                    _arrowChain = Array.Empty<Monster>();
                    _arrowHitCount = 0;
                    _resolving = false;
                    ResolveTurn();
                });
            });
        }

        /// <summary>Runs an action once after the given delay (UI thread).</summary>
        private static void RunAfter(int milliseconds, Action action)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                action();
            };
            timer.Start();
        }

        /// <summary>The 8 in-bounds cells around the target — the mage's blast area.</summary>
        private IReadOnlyList<(int Row, int Col)> BlastArea(Monster target)
        {
            var cells = new List<(int Row, int Col)>();
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0)
                        continue; // skip the target cell itself

                    int r = target.Row + dr;
                    int c = target.Col + dc;
                    if (_engine.Board.InBounds(r, c))
                        cells.Add((r, c));
                }
            }
            return cells;
        }

        /// <summary>Runs the monster phase and checks for game over.</summary>
        private void ResolveTurn()
        {
            int healthBefore = _engine.Board.Hero.Health;
            _engine.EndTurn();

            // Flash the screen red if the monsters managed to hurt the hero.
            if (_engine.Board.Hero.Health < healthBefore)
                _main.FlashDamage();

            if (_engine.IsGameOver)
            {
                SaveGame();
                _main.ShowGameOver(_engine.MonstersKilled);
                return;
            }

            _mode = Mode.ChooseAction;
            Refresh();
        }

        private void SaveGame()
        {
            if (_gameSaved)
                return;
            _gameSaved = true;

            using var db = new GameDbContext();
            db.Games.Add(new GameRecord
            {
                HeroRecordId = _main.HeroRecordId,
                MonstersKilled = _engine.MonstersKilled,
                PlayedAt = DateTime.Now
            });
            db.SaveChanges();
        }

        private void Refresh()
        {
            BuildCells();

            Character hero = _engine.Board.Hero;
            StatusText = $"Health: {hero.Health}    Mana: {hero.Mana}    Kills: {_engine.MonstersKilled}";
            StatsText = $"STR {hero.Strength}    AGI {hero.Agility}    INT {hero.Intelligence}    Range {hero.EffectiveRange}";

            OptionsText = BuildOptions();
        }

        /// <summary>Builds the 10x10 grid of cells, flagging the hero, monsters and target.</summary>
        private void BuildCells()
        {
            Board board = _engine.Board;
            Monster? selected = _mode == Mode.Attack && _targets.Count > 0
                ? _targets[_selectedTargetIndex]
                : null;

            var cells = new List<BoardCell>(Board.Size * Board.Size);
            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    bool isBlast = _blastCells.Any(p => p.Row == r && p.Col == c);

                    if (board.Hero.Row == r && board.Hero.Col == c)
                    {
                        cells.Add(new BoardCell(board.Hero.Symbol.ToString(), isHero: true, isMonster: false,
                            isSelectedTarget: false, isBlast: isBlast, isArrowHit: false, isArrowNext: false, isDoomed: false));
                        continue;
                    }

                    Monster? monster = board.Monsters.Find(m => m.Row == r && m.Col == c);
                    if (monster is not null)
                    {
                        int chainIndex = IndexInChain(monster);
                        bool isArrowHit = chainIndex >= 0 && chainIndex == _arrowHitCount - 1; // struck this step → shakes
                        bool isArrowNext = chainIndex >= 0 && chainIndex == _arrowHitCount;    // where the arrow goes next
                        bool isDoomed = chainIndex >= 0 && monster.Health <= board.Hero.Damage;

                        cells.Add(new BoardCell(monster.Symbol.ToString(), isHero: false, isMonster: true,
                            isSelectedTarget: ReferenceEquals(monster, selected), isBlast: isBlast,
                            isArrowHit: isArrowHit, isArrowNext: isArrowNext, isDoomed: isDoomed));
                    }
                    else
                    {
                        cells.Add(new BoardCell(Board.EmptyCell.ToString(), isHero: false, isMonster: false,
                            isSelectedTarget: false, isBlast: isBlast, isArrowHit: false, isArrowNext: false, isDoomed: false));
                    }
                }
            }

            Cells = cells;
        }

        private int IndexInChain(Monster monster)
        {
            for (int i = 0; i < _arrowChain.Count; i++)
                if (ReferenceEquals(_arrowChain[i], monster))
                    return i;
            return -1;
        }

        private string BuildOptions()
        {
            if (_resolving)
                return _engine.Board.Hero is Archer ? "↗ Ricochet shot!" : "✦ Arcane blast! ✦";

            return BuildModeOptions();
        }

        private string BuildModeOptions() => _mode switch
        {
            Mode.ChooseAction =>
                _engine.CanHeroMove()
                    ? "Your move:  [M] Move    [A] Attack"
                    : "Surrounded — no room to move.  [A] Attack",
            Mode.Move =>
                "Move:  W=Up  S=Down  A=Left  D=Right\n" +
                "       Q=Up-Left  E=Up-Right  Z=Down-Left  X=Down-Right",
            Mode.Attack => BuildTargetInfo(),
            _ => string.Empty
        };

        /// <summary>Details of the highlighted monster and its HP after your hit.</summary>
        private string BuildTargetInfo()
        {
            Monster m = _targets[_selectedTargetIndex];

            int after = Math.Max(0, m.Health - _engine.Board.Hero.Damage);
            string outcome = after > 0
                ? $"HP: {m.Health}  →  {after} after your hit"
                : $"HP: {m.Health}  →  defeated!";

            return
                $"TARGET   Monster at ({m.Row + 1}, {m.Col + 1})\n" +
                $"{outcome}\n" +
                "Move: W/A/S/D or arrows    Enter = attack    Esc = cancel";
        }
    }
}
