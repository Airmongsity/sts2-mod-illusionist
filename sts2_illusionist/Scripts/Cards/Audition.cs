using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Illusionist.Scripts.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Random;
using STS2RitsuLib.Interop.AutoRegistration;

namespace Illusionist.Scripts.Cards;

/// <summary>
/// 试镜 (Audition) — 1 cost Skill, Event rarity (upgraded: Innate). Look at three random cards from other
/// playable characters, choose one, then choose a card in hand and turn it into that card FOR GOOD:
/// the deck card is replaced too, not just the combat copy.
///
/// <para>This is the run-level half of the borrowed-character content. Deckbuilding happens over the
/// 49 floors exactly like everyone else's — the player picks, one card at a time, and coherence comes
/// out of those picks rather than out of a generator. MOD characters are picked up automatically
/// through <see cref="OtherCharacterCardSelection"/>.</para>
///
/// <para>Combat cards are throwaway copies (<c>PlayerCombatState.AfterCombatEnd</c> clears every
/// combat pile), so the permanent edit has to hit the matching card in <see cref="PileType.Deck"/> —
/// <see cref="CardCmd.Transform"/> supports that as a first-class case. The hand copy is transformed
/// as well so the new card is usable in this fight, and the chosen hand card is first resolved down
/// its 幻化 revert chain so a transmuted card still matches the deck card it started as.</para>
///
/// <para>Event rarity rather than Uncommon: card rewards and the merchant both skip
/// Basic/Ancient/Event, so this stays out of the roll and the reward pool holds at the base
/// characters' 85. The card itself is unchanged and still works wherever it is granted.
/// The borrowed-character line was retired as a system — these cards are kept as working
/// models rather than deleted, but they no longer dilute the rolls.</para>
/// </summary>
[RegisterCard(typeof(IllusionistCardPool), StableEntryStem = "AUDITION")]
public sealed class AuditionIllusionist : IllusionistCard
{
    /// <summary>FromChooseACardScreen rejects more than three options.</summary>
    private const int OfferSize = 3;

    /// <summary>How far to walk the 幻化 revert chain; far past any reachable stack depth.</summary>
    private const int MaxRevertDepth = 100;

    /// <summary>
    /// Exhaust is the brake on the whole borrowed-character line. Without it this is repeatable every
    /// fight for 1 energy and a run ends up as someone else's deck wearing her relic; with it a run
    /// converts a few key cards and the deck stays hers.
    /// </summary>
    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[]
    {
        CardKeyword.Exhaust,
    };

    public AuditionIllusionist()
        : base(1, CardType.Skill, CardRarity.Event, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player owner = base.Owner;
        Rng rng = IllusionistRng.RunRng(owner);

        // Keep each display card paired with the canonical template it came from: RunState.CreateCard
        // asserts its argument is canonical, and the card the screen hands back is a mutable display
        // copy. Passing that back in throws MutableModelException and strands the selection screen in
        // Select Mode, which is what froze the game.
        List<(CardModel Template, CardModel Display)> offer = BuildOffer(owner, rng);
        if (offer.Count == 0)
        {
            return;
        }

        CardModel? picked = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            offer.Select(entry => entry.Display).ToList(),
            owner,
            canSkip: true);
        if (picked == null)
        {
            return;
        }

        CardModel template = offer.First(entry => ReferenceEquals(entry.Display, picked)).Template;

        CardModel? handCard = (await CardSelectCmd.FromHand(
            choiceContext,
            owner,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
            IsValidHandTarget,
            this)).FirstOrDefault();
        if (handCard == null)
        {
            return;
        }

        // Permanent half: replace the deck card this hand card came from.
        CardModel original = ResolveOriginal(handCard, owner);
        CardModel? deckCard = PileType.Deck.GetPile(owner).Cards
            .FirstOrDefault(candidate => candidate.Id == original.Id && candidate.IsTransformable);
        if (deckCard != null)
        {
            await CardCmd.Transform(deckCard, owner.RunState.CreateCard(template, owner));
        }

        // Combat half: make it usable in this fight too. This one must come from the COMBAT scope —
        // a RunState card is not registered with the CombatState and the engine refuses to play it
        // ("<card> must be added to a CombatState before playing it"). The deck half above is the
        // mirror image: that one has to be run-scoped or it would vanish when the fight ends.
        await CardCmd.Transform(handCard, base.CardScope!.CreateCard(template, owner));
    }

    /// <summary>
    /// A hand card is only a valid target if the permanent half can actually land on it: Eternal cards
    /// cannot be transformed while they sit in the deck, and Token cards have no deck card at all, so
    /// both would silently convert the combat copy and leave the deck untouched.
    /// </summary>
    private static bool IsValidHandTarget(CardModel card)
    {
        return card.IsTransformable && card.IsRemovable && card.Rarity != CardRarity.Token;
    }

    /// <summary>Three random cards drawn from the other playable characters' unlocked pools.</summary>
    private static List<(CardModel Template, CardModel Display)> BuildOffer(Player owner, Rng rng)
    {
        List<CardModel> templates = ModelDb.AllCharacters
            .Where(character => character.IsPlayable && character.Id != owner.Character.Id)
            .SelectMany(character => OtherCharacterCardSelection.GetUnlockedPool(owner, character))
            .ToList();

        return templates
            .OrderBy(_ => rng.NextInt(0, 10000))
            .Take(OfferSize)
            .Select(template => (Template: template, Display: owner.RunState.CreateCard(template, owner)))
            .ToList();
    }

    /// <summary>
    /// Walk the 幻化 chain back to the form the player actually owns, so a card transmuted this fight
    /// still matches the deck card it started as.
    /// </summary>
    private static CardModel ResolveOriginal(CardModel card, Player owner)
    {
        TransmutePower? transmute = owner.Creature.GetPower<TransmutePower>();
        if (transmute == null)
        {
            return card;
        }

        CardModel original = card;
        for (int i = 0; i < MaxRevertDepth; i++)
        {
            if (transmute.GetRevertTarget(original) is not { } previous)
            {
                break;
            }

            original = previous;
        }

        return original;
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
