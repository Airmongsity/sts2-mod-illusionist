using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Illusionist.Scripts.Intents;

/// <summary>
/// Display-only view of an existing Attack intent with some repeats removed.
/// Damage remains delegated to the original intent so dynamic monster values stay live.
/// </summary>
internal sealed class CutInAttackIntent : AttackIntent
{
    private readonly AttackIntent _source;
    private readonly int _removedRepeats;

    public override int Repeats => Math.Max(0, _source.Repeats - _removedRepeats);

    protected override LocString IntentLabelFormat =>
        new("intents", Repeats > 1 ? "FORMAT_DAMAGE_MULTI" : "FORMAT_DAMAGE_SINGLE");

    internal CutInAttackIntent(AttackIntent source, int removedRepeats)
    {
        _source = source;
        _removedRepeats = removedRepeats;
        base.DamageCalc = source.DamageCalc ?? (() => 0m);
    }

    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner)
    {
        return GetSingleDamage(targets, owner) * Repeats;
    }

    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        LocString label = IntentLabelFormat;
        label.Add("Damage", GetSingleDamage(targets, owner));
        label.Add("Repeat", Repeats);
        return label;
    }
}
