using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Illusionist.Scripts.Afflictions;

/// <summary>
/// Prevents the afflicted card from changing until it is played.
/// </summary>
[RegisterAffliction]
public sealed class Frozen : ModAfflictionTemplate
{
    private const string OverlayScenePath =
        "res://illusionist/scenes/afflictions/frozen_overlay.tscn";

    public override bool HasExtraCardText => true;

    // RitsuLib supplies this scene through IModAfflictionAssetOverrides; no visual Harmony patch.
    public override AfflictionAssetProfile AssetProfile => new(OverlayScenePath);

    public static bool IsAppliedTo(CardModel card) => card.Affliction is Frozen;

    public override bool TryModifyKeywordsInCombat(CardModel card, ISet<CardKeyword> keywords)
    {
        if (!ReferenceEquals(card, Card))
        {
            return false;
        }

        return keywords.Add(CardKeyword.Retain);
    }

    public override Task OnPlay(PlayerChoiceContext choiceContext, Creature? target)
    {
        CardCmd.ClearAffliction(Card);
        return Task.CompletedTask;
    }
}
