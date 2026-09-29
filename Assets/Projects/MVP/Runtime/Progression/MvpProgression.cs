using System;
using System.Collections.Generic;

namespace BounceFit.Mvp
{
    /// <summary>Run-local earned progression. Stats are rebuilt from live config and earned counters.</summary>
    public sealed class MvpProgression
    {
        private const string Ball = "ball";
        private const string Fire = "fire";
        private const string Ice = "ice";
        private const string Storm = "storm";
        private const string Fusion = "fusion";
        private const string Power = "power";
        private const string Haste = "haste";
        private const string Magnet = "magnet";
        private const string Boots = "boots";
        private const string Mastery = "mastery";
        private const string Precision = "precision";
        private const string Rhythm = "rhythm";
        private const int MaxRefreshLevels = 100000;

        private static readonly UpgradeCard[] Cards =
        {
            new UpgradeCard(Ball, "바운스 볼", "공 개수가 1개 증가합니다.", "기본"),
            new UpgradeCard(Fire, "화염구", "적에게 강한 화염 피해를 줍니다.", "액티브"),
            new UpgradeCard(Ice, "빙결구", "적의 움직임을 얼려 둔화시킵니다.", "액티브"),
            new UpgradeCard(Storm, "폭풍구", "적을 꿰뚫고 지나갑니다.", "액티브"),
            new UpgradeCard(Power, "강화", "피해량이 증가합니다.", "패시브"),
            new UpgradeCard(Haste, "가속", "발사 속도가 증가합니다.", "패시브"),
            new UpgradeCard(Magnet, "자석", "경험치 획득 범위가 넓어집니다.", "패시브"),
            new UpgradeCard(Boots, "질주", "이동 속도가 증가합니다.", "패시브"),
            new UpgradeCard(Fusion, "빙염 폭발", "화염과 빙결을 융합해 강력한 폭발을 일으킵니다.", "융합"),
            new UpgradeCard(Mastery, "숙련", "기본 피해량이 조금 증가합니다.", "보조"),
            new UpgradeCard(Precision, "정밀 조준", "발사 속도가 조금 증가합니다.", "보조"),
            new UpgradeCard(Rhythm, "전투 감각", "이동 속도가 조금 증가합니다.", "보조"),
        };

        private readonly Random random;
        private readonly MvpConfigSet config;
        private readonly List<UpgradeCard> choices = new List<UpgradeCard>(3);
        private readonly Dictionary<string, int> active = new Dictionary<string, int>();
        private readonly Dictionary<string, int> passive = new Dictionary<string, int>();
        private readonly Dictionary<string, int> support = new Dictionary<string, int>();
        private readonly HashSet<string> blocked = new HashSet<string>();
        private int basicUpgrades;

        public MvpProgression(int seed = 1, MvpConfigSet config = null)
        {
            random = new Random(seed);
            this.config = config ?? MvpConfigSet.Default;
            this.config.ValidateAll();
            Reset();
        }

        private MvpProgressionConfig Config => config.progression;

        public int Level { get; private set; }
        public int Xp { get; private set; }
        public int NextXp { get; private set; }
        public int PendingChoices { get; private set; }
        public int MaxActiveSlots => Config.maxActiveSlots;
        public int MaxPassiveSlots => Config.maxPassiveSlots;
        public int ChoiceCount => Config.choiceCount;
        public IReadOnlyList<UpgradeCard> Choices => choices;
        public int ActiveCount => 1 + active.Count;
        public int PassiveCount => passive.Count;
        public IReadOnlyList<string> ActiveLabels => Labels(active, "바운스 볼");
        public IReadOnlyList<string> PassiveLabels => Labels(passive, null);
        public bool IsChoosing => PendingChoices > 0;

        /// <summary>Returns a value snapshot; callers cannot mutate retained progression stats.</summary>
        public CombatStats Stats
        {
            get
            {
                RefreshConfiguration();
                return BuildStats();
            }
        }

        public void Reset()
        {
            ValidateLiveConfig();
            Level = 1;
            Xp = 0;
            PendingChoices = 0;
            NextXp = Threshold(Level);
            choices.Clear();
            active.Clear();
            passive.Clear();
            support.Clear();
            blocked.Clear();
            basicUpgrades = 0;
        }

        /// <summary>Re-reads the SO while retaining earned cards and retained XP.</summary>
        public void RefreshConfiguration()
        {
            ValidateLiveConfig();
            NextXp = Threshold(Level);
            ProcessRetainedXp();
            if (PendingChoices > 0)
            {
                RefreshChoices();
            }
        }

        public void AddXp(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Xp = amount > int.MaxValue - Xp ? int.MaxValue : Xp + amount;
            RefreshConfiguration();
        }

        public bool Choose(int index)
        {
            RefreshConfiguration();
            if (PendingChoices <= 0 || index < 0 || index >= choices.Count)
            {
                return false;
            }

            if (!TryApplyCard(choices[index].Id))
            {
                return false;
            }

            PendingChoices--;
            choices.Clear();
            if (PendingChoices > 0)
            {
                BuildChoices();
            }

            return true;
        }

        public IReadOnlyList<UpgradeCard> EligibleCards()
        {
            ValidateLiveConfig();
            var result = new List<UpgradeCard>();
            for (var i = 0; i < Cards.Length; i++)
            {
                if (!IsFallback(Cards[i]) && IsEligible(Cards[i]))
                {
                    result.Add(Cards[i]);
                }
            }

            if (result.Count < ChoiceCount)
            {
                for (var i = 0; i < Cards.Length; i++)
                {
                    if (IsFallback(Cards[i]) && IsEligible(Cards[i]))
                    {
                        result.Add(Cards[i]);
                    }
                }
            }

            return result;
        }

        public bool TryApplyCard(string id)
        {
            RefreshConfiguration();
            var card = FindCard(id);
            if (card == null || !IsEligible(card))
            {
                return false;
            }

            Apply(card);
            return true;
        }

        private void ValidateLiveConfig()
        {
            Config.Validate();
            config.ball.Validate();
        }

        private void ProcessRetainedXp()
        {
            for (var i = 0; Xp >= NextXp && i < MaxRefreshLevels; i++)
            {
                Xp -= NextXp;
                if (Level == int.MaxValue)
                {
                    Xp += NextXp;
                    break;
                }

                Level++;
                if (PendingChoices < int.MaxValue)
                {
                    PendingChoices++;
                }

                NextXp = Threshold(Level);
            }
        }

        private int Threshold(int level)
        {
            var value = (long)Config.initialXp + (long)(Math.Max(1, level) - 1) * Config.xpGrowth;
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }

        private void RefreshChoices()
        {
            var eligible = EligibleCards();
            var expectedCount = Math.Min(ChoiceCount, eligible.Count);
            var ids = new HashSet<string>();
            var valid = choices.Count == expectedCount;
            for (var i = 0; valid && i < choices.Count; i++)
            {
                valid = ids.Add(choices[i].Id) && IsEligible(choices[i]);
            }

            if (!valid)
            {
                BuildChoices(new List<UpgradeCard>(eligible));
            }
        }

        private void BuildChoices()
        {
            BuildChoices(new List<UpgradeCard>(EligibleCards()));
        }

        private void BuildChoices(List<UpgradeCard> eligible)
        {
            for (var i = eligible.Count - 1; i > 0; i--)
            {
                var target = random.Next(i + 1);
                var temporary = eligible[i];
                eligible[i] = eligible[target];
                eligible[target] = temporary;
            }

            choices.Clear();
            for (var i = 0; i < ChoiceCount && i < eligible.Count; i++)
            {
                choices.Add(eligible[i]);
            }
        }

        private bool IsEligible(UpgradeCard card)
        {
            switch (card.Id)
            {
                case Ball:
                    return basicUpgrades < Config.maxBasicUpgrades;
                case Fire:
                case Ice:
                case Storm:
                    return IsSkillEligible(card.Id);
                case Power:
                case Haste:
                case Magnet:
                case Boots:
                    return IsPassiveEligible(card.Id);
                case Fusion:
                    return IsFusionEligible();
                case Mastery:
                case Precision:
                case Rhythm:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsFusionEligible()
        {
            if (active.ContainsKey(Fusion))
            {
                return active[Fusion] < Config.maxSkillLevel;
            }

            return LevelOf(active, Fire) >= Config.fusionRequiredLevel
                && LevelOf(active, Ice) >= Config.fusionRequiredLevel;
        }

        private bool IsSkillEligible(string id)
        {
            if (blocked.Contains(id))
            {
                return false;
            }

            return active.ContainsKey(id)
                ? active[id] < Config.maxSkillLevel
                : ActiveCount < Config.maxActiveSlots;
        }

        private bool IsPassiveEligible(string id)
        {
            return passive.ContainsKey(id)
                ? passive[id] < Config.maxSkillLevel
                : PassiveCount < Config.maxPassiveSlots;
        }

        private void Apply(UpgradeCard card)
        {
            switch (card.Id)
            {
                case Ball:
                    basicUpgrades++;
                    break;
                case Fire:
                case Ice:
                case Storm:
                    Increase(active, card.Id);
                    break;
                case Fusion:
                    ApplyFusion();
                    break;
                case Power:
                case Haste:
                case Magnet:
                case Boots:
                    Increase(passive, card.Id);
                    break;
                default:
                    Increase(support, card.Id);
                    break;
            }
        }

        private void ApplyFusion()
        {
            if (active.ContainsKey(Fusion))
            {
                Increase(active, Fusion);
                return;
            }

            active.Remove(Fire);
            active.Remove(Ice);
            blocked.Add(Fire);
            blocked.Add(Ice);
            active[Fusion] = 1;
        }

        private CombatStats BuildStats()
        {
            var stats = new CombatStats
            {
                BallCount = SaturatingAdd(Math.Max(1, config.ball.startingCount), basicUpgrades),
                DamageMultiplier = 1f,
                FireRateMultiplier = 1f,
                MoveSpeedMultiplier = 1f,
                PickupRadius = 1f,
                FireLevel = LevelOf(active, Fire),
                IceLevel = LevelOf(active, Ice),
                StormLevel = LevelOf(active, Storm),
                FusionLevel = LevelOf(active, Fusion),
            };

            stats.DamageMultiplier += LevelOf(passive, Power) * Config.passiveDamageBonus;
            stats.DamageMultiplier += LevelOf(support, Mastery) * Config.masteryDamageBonus;
            stats.FireRateMultiplier += LevelOf(passive, Haste) * Config.passiveFireRateBonus;
            stats.FireRateMultiplier += LevelOf(support, Precision) * Config.precisionFireRateBonus;
            stats.PickupRadius += LevelOf(passive, Magnet) * Config.passivePickupRadiusBonus;
            stats.MoveSpeedMultiplier += LevelOf(passive, Boots) * Config.passiveMoveSpeedBonus;
            stats.MoveSpeedMultiplier += LevelOf(support, Rhythm) * Config.rhythmMoveSpeedBonus;
            return stats;
        }

        private static UpgradeCard FindCard(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var i = 0; i < Cards.Length; i++)
            {
                if (Cards[i].Id == id)
                {
                    return Cards[i];
                }
            }

            return null;
        }

        private static bool IsFallback(UpgradeCard card)
        {
            return card.Id == Mastery || card.Id == Precision || card.Id == Rhythm;
        }

        private static void Increase(Dictionary<string, int> levels, string id)
        {
            levels[id] = LevelOf(levels, id) + 1;
        }

        private static int LevelOf(Dictionary<string, int> levels, string id)
        {
            int level;
            return levels.TryGetValue(id, out level) ? level : 0;
        }

        private static int SaturatingAdd(int left, int right)
        {
            return right > int.MaxValue - left ? int.MaxValue : left + right;
        }

        private static IReadOnlyList<string> Labels(Dictionary<string, int> levels, string initial)
        {
            var result = new List<string>();
            if (initial != null)
            {
                result.Add(initial);
            }

            for (var i = 0; i < Cards.Length; i++)
            {
                if (levels.ContainsKey(Cards[i].Id))
                {
                    result.Add(Cards[i].Title);
                }
            }

            return result;
        }
    }
}
