using System.Collections.Generic;
using System.Threading.Tasks;

namespace MegaCrit.Sts2.Core.Models.Cards
{
    public sealed class FixtureStrike : CardModel
    {
        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(6m, ValueProp.Move)
        };

        public FixtureStrike() : base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) { }

        protected override async Task OnPlay(Context context, CardPlay play)
        {
            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).Targeting(play.Target).Execute(context);
        }

        protected override void OnUpgrade()
        {
            UpgradeEnergyCostBy(-1);
            base.DynamicVars.Damage.UpgradeValueBy(3m);
        }
    }
}

namespace MegaCrit.Sts2.Core.Models.Relics
{
    public sealed class FixtureRelic : RelicModel
    {
        public override RelicRarity Rarity => RelicRarity.Common;
        public override async Task BeforeCombatStart() { await CreatureCmd.GainBlock(base.Owner.Creature, 4, null); }
    }
}

namespace MegaCrit.Sts2.Core.Models.Potions
{
    public sealed class FixturePotion : PotionModel
    {
        public override PotionRarity Rarity => PotionRarity.Common;
        protected override async Task OnUse(Context context, Creature target) { await PowerCmd.Apply<WeakPower>(context, target, 1); }
    }
}

namespace MegaCrit.Sts2.Core.Models.Powers
{
    public sealed class FixturePower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override async Task AfterCardPlayed(Context context, CardPlay play) { await CreatureCmd.GainBlock(base.Owner, 1, null); }
    }
}

namespace MegaCrit.Sts2.Core.Rooms
{
    public sealed class FixtureRoom : AbstractRoom
    {
        public override RoomType RoomType => RoomType.Event;
        public override Task Enter() { return Task.CompletedTask; }
    }
}

namespace MegaCrit.Sts2.Core.Models.Events
{
    public sealed class FixtureAncient : AncientEventModel
    {
        protected override IReadOnlyList<EventOption> GenerateInitialOptions() { return new EventOption[] { RelicOption<FixtureRelic>() }; }
    }
}

namespace MegaCrit.Sts2.Core.Models.Monsters
{
    public sealed class FixtureMonster : MonsterModel
    {
        private const string STRIKE_MOVE = "STRIKE";
        private const string HEX_MOVE = "HEX";
        public override decimal MinHp => AscensionHelper.IsToughEnemies() ? 44m : 40m;
        public override decimal MaxHp => AscensionHelper.IsToughEnemies() ? 48m : 44m;
        private decimal StrikeDamage => AscensionHelper.IsDeadlyEnemies() ? 9m : 8m;

        protected override MonsterMoveStateMachine GenerateMoveStateMachine()
        {
            var strike = new MoveState(STRIKE_MOVE, Strike, new SingleAttackIntent(StrikeDamage));
            var hex = new MoveState(HEX_MOVE, Hex, new DebuffIntent());
            var random = new RandomBranchState("RAND");
            random.AddBranch(strike, MoveRepeatType.CannotRepeat);
            random.AddBranch(hex, 2, MoveRepeatType.CannotRepeat);
            var conditional = new ConditionalBranchState("CHECK");
            conditional.AddState(hex, () => Creature.Hp < Creature.MaxHp / 2);
            conditional.AddState(random, () => true);
            strike.FollowUpState = conditional;
            hex.FollowUpState = random;
            return random.AsMachine(this);
        }

        private async Task Strike(Context context)
        {
            await DamageCmd.Attack(StrikeDamage).TargetingAllOpponents(CombatState).Execute(context);
        }

        private async Task Hex(Context context)
        {
            await PowerCmd.Apply<VulnerablePower>(context, CombatState.Players, 2, Creature, null);
        }
    }
}
