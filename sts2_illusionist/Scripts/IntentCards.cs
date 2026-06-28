using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Illusionist.Scripts;

/// <summary>
/// The intent system, unified by card TEXT: any card whose description refers to an enemy's 意图
/// (intent) belongs to it — reactive cards (反击/致盲/抗衡/预见), manipulation cards (逆转/虚晃/催化),
/// and bridges like 破坏. This text rule auto-includes every current and future intent card with no
/// per-card marker, and (by design) catches 挑衅 too — its text mentions the attack 意图 even though its
/// Strength buff isn't a "true" intent change. Used by intent-flow outputs that scale with how much of
/// the intent system you've engaged this combat (e.g. 清算 / Reckoning).
/// </summary>
public static class IntentCards
{
    /// <summary>True if the card's description text refers to an enemy intent (意图 / intent / intend).</summary>
    public static bool MentionsIntent(CardModel card)
    {
        string text;
        try
        {
            text = card.Description.GetRawText();
        }
        catch
        {
            return false;
        }

        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        // "意图" for 简体中文; "intent"/"intend" for English ("attack intent", "intends to attack").
        return text.Contains("意图", StringComparison.Ordinal)
            || text.Contains("intent", StringComparison.OrdinalIgnoreCase)
            || text.Contains("intend", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>How many intent cards <paramref name="player"/> has finished playing this combat.</summary>
    public static int PlayedThisCombat(Player player)
    {
        return CombatManager.Instance.History.CardPlaysFinished
            .Count(e => e.CardPlay.Card.Owner == player && MentionsIntent(e.CardPlay.Card));
    }
}
