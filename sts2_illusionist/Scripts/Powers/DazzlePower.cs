using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Illusionist.Scripts.Monsters;

using STS2RitsuLib.Interop.AutoRegistration;
namespace Illusionist.Scripts.Powers;

/// <summary>
/// 炫目 (DazzleIllusionist) power. At the start of each of your turns, gain Block equal to your current number
/// of mirror clones (复制品) MULTIPLIED by this power's stacks (one per 炫目 played) times 4 - so 1 Dazzle
/// with 7 mirrors = 28 Block. Power-sourced (Unpowered), unmodified by Dexterity.
/// <para>The icon shows the ACTUAL Block you'll gain (mirrors × stacks × 4) via a dynamic
/// <see cref="DisplayAmount" />, refreshed with <see cref="InvokeDisplayAmountChanged" /> whenever the
/// mirror count changes - not the static stack count.</para>
/// </summary>
[RegisterPower]
public sealed class DazzlePower : IllusionistPower
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Dynamic: the Block you'll gain this turn (mirrors × stacks × 4), not the static stack count.
    public override int DisplayAmount => MirrorClone.CountAlive(base.Owner.Player) * base.Amount * 4;

    // Dynamic hover-tip: show the total Block gained (mirrors × stacks × 4). The base HoverTips getter adds
    // {Amount}; we add {BlockPerMirror} so the description can show the per-mirror Block live.
    public override LocString Description
    {
        get
        {
            LocString loc = base.Description;
            loc.Add("TotalBlock", MirrorClone.CountAlive(base.Owner.Player) * base.Amount * 4);
            return loc;
        }
    }

    private sealed class Data
    {
        public int LastSeenMirrors = -1;
    }

    protected override object InitInternalData() => new Data();

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player.Creature)
        {
            return;
        }

        RefreshDisplay();
        int block = MirrorClone.CountAlive(player) * base.Amount * 4;
        if (block > 0)
        {
            await CreatureCmd.GainBlock(base.Owner, block, ValueProp.Unpowered, null);
        }
    }

    // Copy cards and mirror-destroying cards are owner card plays -> refresh here. Relic Copy (combat
    // start) and post-enemy-turn mirror death are caught by AfterPlayerTurnStart. Only re-renders when the
    // mirror count actually changed (cheap LastSeenMirrors guard).
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == base.Owner.Player)
        {
            RefreshDisplay();
        }

        return Task.CompletedTask;
    }

    private void RefreshDisplay()
    {
        int current = MirrorClone.CountAlive(base.Owner.Player);
        Data data = GetInternalData<Data>();
        if (current != data.LastSeenMirrors)
        {
            data.LastSeenMirrors = current;
            InvokeDisplayAmountChanged();
        }
    }
}
