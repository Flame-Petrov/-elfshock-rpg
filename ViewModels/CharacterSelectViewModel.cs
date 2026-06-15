using System;
using System.Collections.Generic;
using System.Windows.Input;
using RPG_Game_Elfshock.Data;
using RPG_Game_Elfshock.Models.Characters;

namespace RPG_Game_Elfshock.ViewModels
{
    /// <summary>
    /// CharacterSelect screen. Walks the player through choosing a race and
    /// optionally spending up to 3 buff points, then saves the hero to the database.
    /// </summary>
    public sealed class CharacterSelectViewModel : ViewModelBase, IKeyInput
    {
        private enum Step
        {
            ChooseClass,
            BuffStrength,
            BuffAgility,
            BuffIntelligence,
            ReadyToStart
        }

        private const int MaxBuffPoints = 3;
        private const int MaxInputLength = 2;

        private readonly MainViewModel _main;
        private Step _step = Step.ChooseClass;
        private Character? _hero;
        private string _heroType = string.Empty;
        private int _remaining = MaxBuffPoints;

        // Buff points the player has spent on each stat (for the selection summary).
        private int _addedStrength;
        private int _addedAgility;
        private int _addedIntelligence;

        /// <summary>
        /// The chosen hero. The state machine only reaches the buff/start steps
        /// after a class has been picked, so this is always set by then. We guard
        /// with a clear error instead of silently risking a NullReferenceException.
        /// </summary>
        private Character SelectedHero =>
            _hero ?? throw new InvalidOperationException("No character has been selected yet.");

        /// <summary>The stepped progress bar shown above the prompts.</summary>
        public IReadOnlyList<ProgressStep> Steps { get; } = new[]
        {
            new ProgressStep("Class"),
            new ProgressStep("Strength"),
            new ProgressStep("Agility"),
            new ProgressStep("Intelligence"),
        };

        /// <summary>The hero classes the player moves between with the arrow.</summary>
        public IReadOnlyList<ClassOption> ClassOptions { get; } = new[]
        {
            new ClassOption("Warrior", "STR 3  AGI 3  INT 0  Range 1",
                "SPECIAL — Lifesteal: heals 5 HP per kill. Strikes adjacent foes; damage scales with Strength."),
            new ClassOption("Archer",  "STR 2  AGI 4  INT 0  Range 2",
                "SPECIAL — Ricochet: the arrow bounces between up to 3 lined-up foes. Range 2; damage scales with Agility."),
            new ClassOption("Mage",    "STR 2  AGI 1  INT 3  Range 3",
                "SPECIAL — Arcane Blast: hits the target and all 8 surrounding tiles. Damage scales with Intelligence; mana extends range."),
        };

        private int _selectedClassIndex;

        private string _selectedClassSpecial = string.Empty;
        public string SelectedClassSpecial
        {
            get => _selectedClassSpecial;
            private set => SetProperty(ref _selectedClassSpecial, value);
        }

        public CharacterSelectViewModel(MainViewModel main)
        {
            _main = main;
            UpdateClassSelection();
            UpdateText();
        }

        /// <summary>Which progress segment the current step maps to (0-based).</summary>
        private int CurrentStepIndex => _step switch
        {
            Step.ChooseClass => 0,
            Step.BuffStrength => 1,
            Step.BuffAgility => 2,
            Step.BuffIntelligence => 3,
            Step.ReadyToStart => 4, // all segments complete
            _ => 0
        };

        private string _prompt = string.Empty;
        public string Prompt
        {
            get => _prompt;
            private set => SetProperty(ref _prompt, value);
        }

        private string _info = string.Empty;
        public string Info
        {
            get => _info;
            private set => SetProperty(ref _info, value);
        }

        // What the player has typed so far (buff steps only).
        private string _input = string.Empty;
        public string Input
        {
            get => _input;
            private set => SetProperty(ref _input, value);
        }

        // A short explanation of the input mechanic for the current step.
        private string _help = string.Empty;
        public string Help
        {
            get => _help;
            private set => SetProperty(ref _help, value);
        }

        // Drives which input UI is shown: the class menu vs. the typed buff entry.
        public bool IsChoosingClass => _step == Step.ChooseClass;
        public bool IsBuffing =>
            _step is Step.BuffStrength or Step.BuffAgility or Step.BuffIntelligence;

        private string _classLine = string.Empty;
        public string ClassLine
        {
            get => _classLine;
            private set => SetProperty(ref _classLine, value);
        }

        private IReadOnlyList<StatDisplay> _stats = Array.Empty<StatDisplay>();
        public IReadOnlyList<StatDisplay> Stats
        {
            get => _stats;
            private set => SetProperty(ref _stats, value);
        }

        public void OnKey(Key key)
        {
            switch (_step)
            {
                case Step.ReadyToStart:
                    // A single S press starts the game (no Enter needed).
                    if (KeyHelper.ToLetter(key) == 'S')
                        StartGame();
                    return;

                case Step.ChooseClass:
                    HandleClassMenu(key);
                    return;
            }

            // Buff steps use buffered text entry, confirmed with Enter.
            switch (key)
            {
                case Key.Enter:
                    Submit();
                    break;
                case Key.Back:
                    if (_input.Length > 0)
                        Input = _input.Substring(0, _input.Length - 1);
                    break;
                default:
                    int? digit = KeyHelper.ToDigit(key);
                    if (digit is not null && _input.Length < MaxInputLength)
                        Input = _input + digit.Value.ToString();
                    break;
            }
        }

        /// <summary>Moves the selection arrow with Up/Down or W/S, Enter confirms.</summary>
        private void HandleClassMenu(Key key)
        {
            char? letter = KeyHelper.ToLetter(key);

            if (key == Key.Up || letter == 'W')
                MoveClassSelection(-1);
            else if (key == Key.Down || letter == 'S')
                MoveClassSelection(1);
            else if (key == Key.Enter)
                ConfirmClass();
        }

        private void MoveClassSelection(int delta)
        {
            int count = ClassOptions.Count;
            _selectedClassIndex = (_selectedClassIndex + delta + count) % count; // wraps around
            UpdateClassSelection();
        }

        private void UpdateClassSelection()
        {
            for (int i = 0; i < ClassOptions.Count; i++)
                ClassOptions[i].IsSelected = i == _selectedClassIndex;

            SelectedClassSpecial = ClassOptions[_selectedClassIndex].Special;
        }

        private void ConfirmClass()
        {
            switch (_selectedClassIndex)
            {
                case 0: _hero = new Warrior(); _heroType = "Warrior"; break;
                case 1: _hero = new Archer(); _heroType = "Archer"; break;
                case 2: _hero = new Mage(); _heroType = "Mage"; break;
            }

            // After picking a class, go straight into spending buff points.
            _step = Step.BuffStrength;
            UpdateText();
        }

        /// <summary>Handles Enter on the buff steps using the typed input.</summary>
        private void Submit()
        {
            switch (_step)
            {
                case Step.BuffStrength:
                    if (TryApplyBuff(points => { SelectedHero.Strength += points; _addedStrength = points; }))
                        Advance(Step.BuffAgility);
                    break;
                case Step.BuffAgility:
                    if (TryApplyBuff(points => { SelectedHero.Agility += points; _addedAgility = points; }))
                        Advance(Step.BuffIntelligence);
                    break;
                case Step.BuffIntelligence:
                    if (TryApplyBuff(points => { SelectedHero.Intelligence += points; _addedIntelligence = points; }))
                        EnterReadyToStart();
                    break;
            }
        }

        /// <summary>
        /// Applies the typed buff amount. Empty input means 0 points for this stat.
        /// Returns false (and shows a message) when the amount is out of range.
        /// </summary>
        private bool TryApplyBuff(Action<int> apply)
        {
            int amount = ParseInput() ?? 0; // pressing Enter with no number = 0 points
            Input = string.Empty;

            if (amount > _remaining)
            {
                Info = $"Invalid input. Add between 0 and {_remaining}, then press Enter.";
                return false;
            }

            apply(amount);
            _remaining -= amount;
            return true;
        }

        /// <summary>The typed input as a number, or null when it is empty/invalid.</summary>
        private int? ParseInput() =>
            int.TryParse(_input, out int value) ? value : null;

        private void Advance(Step next)
        {
            // Once the points run out there is nothing left to ask — wait for start.
            if (_remaining == 0)
            {
                EnterReadyToStart();
                return;
            }

            _step = next;
            UpdateText();
        }

        private void EnterReadyToStart()
        {
            _step = Step.ReadyToStart;
            UpdateText();
        }

        private void StartGame()
        {
            SelectedHero.Setup();
            SaveHero();
            _main.Hero = SelectedHero;
            _main.ShowInGame();
        }

        private void SaveHero()
        {
            var record = new HeroRecord
            {
                HeroType = _heroType,
                Strength = SelectedHero.Strength,
                Agility = SelectedHero.Agility,
                Intelligence = SelectedHero.Intelligence,
                CreatedAt = DateTime.Now
            };

            using var db = new GameDbContext();
            db.Heroes.Add(record);
            db.SaveChanges();

            _main.HeroRecordId = record.Id;
        }

        /// <summary>Marks each progress segment as done / current / pending.</summary>
        private void UpdateProgress()
        {
            int current = CurrentStepIndex;
            for (int i = 0; i < Steps.Count; i++)
            {
                Steps[i].IsDone = i < current;
                Steps[i].IsCurrent = i == current;
            }
        }

        private void UpdateText()
        {
            UpdateProgress();
            BuildSelection();

            Input = string.Empty;
            OnPropertyChanged(nameof(IsChoosingClass));
            OnPropertyChanged(nameof(IsBuffing));

            string buffHelp =
                $"Type a number (0-{_remaining}) and press Enter.   " +
                "Enter alone = 0 points.   Backspace to edit.";

            switch (_step)
            {
                case Step.ChooseClass:
                    Prompt = "Choose your character:";
                    Help = "Use Up/Down or W/S to move, Enter to select.";
                    Info = string.Empty;
                    break;

                case Step.BuffStrength:
                    Prompt =
                        $"You chose: {_heroType}\n" +
                        "Spend up to 3 points to buff your stats.\n\n" +
                        $"Remaining Points: {_remaining}\n\n" +
                        $"Add to Strength (0-{_remaining}):";
                    Help = buffHelp;
                    Info = string.Empty;
                    break;

                case Step.BuffAgility:
                    Prompt = $"Remaining Points: {_remaining}\n\nAdd to Agility (0-{_remaining}):";
                    Help = buffHelp;
                    Info = string.Empty;
                    break;

                case Step.BuffIntelligence:
                    Prompt = $"Remaining Points: {_remaining}\n\nAdd to Intelligence (0-{_remaining}):";
                    Help = buffHelp;
                    Info = string.Empty;
                    break;

                case Step.ReadyToStart:
                    Prompt = "Your hero is ready!\n\nPress S to start.";
                    Help = string.Empty;
                    Info = string.Empty;
                    break;
            }
        }

        /// <summary>Rebuilds the running summary of everything the player has chosen.</summary>
        private void BuildSelection()
        {
            if (_hero is null)
            {
                ClassLine = string.Empty;
                Stats = Array.Empty<StatDisplay>();
                return;
            }

            ClassLine = $"Class: {_heroType}";
            Stats = new[]
            {
                new StatDisplay("STR", _hero.Strength, _addedStrength),
                new StatDisplay("AGI", _hero.Agility, _addedAgility),
                new StatDisplay("INT", _hero.Intelligence, _addedIntelligence),
            };
        }
    }
}
