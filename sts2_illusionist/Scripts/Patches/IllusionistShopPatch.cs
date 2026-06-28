using System;
using System.Collections;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Illusionist.Scripts.Patches;

/// <summary>
/// Give the Illusionist its own Spine body in the shop. The merchant scene is per-character
/// (<c>merchant/characters/&lt;id&gt;_merchant</c>, redirected to Necrobinder's), so without this the
/// shop shows the Necrobinder figure. <see cref="NMerchantRoom"/>'s <c>AfterRoomIsLoaded</c> builds one
/// <see cref="NMerchantCharacter"/> per player from <c>players[i].Character.MerchantAnimPath</c>; we
/// postfix it, pair each visual to its player by index, and for the Illusionist hide the borrowed spine
/// (the merchant node's first child) and overlay our own.
///
/// <para>Reuses the REST skeleton (a relaxed pose suits a shopkeeper) — no separate art needed. Swap
/// <see cref="ShopSkel"/> to a dedicated skeleton later if desired. Tune
/// <see cref="TargetHeightPx"/> / <see cref="YOffset"/> in-game.</para>
/// </summary>
[HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
public static class IllusionistShopPatch
{
    private static readonly string ShopSkel = SpineBody.RestSkel;
    private const float TargetHeightPx = 360f;
    private const float YOffset = 0f;
    private const string NodeName = "IllusionistShopSpine";

    private static void Postfix(NMerchantRoom __instance)
    {
        try
        {
            if (AccessTools.Field(typeof(NMerchantRoom), "_players").GetValue(__instance) is not IList players)
            {
                return;
            }
            System.Collections.Generic.IReadOnlyList<NMerchantCharacter> visuals = __instance.PlayerVisuals;

            int count = Math.Min(players.Count, visuals.Count);
            for (int i = 0; i < count; i++)
            {
                if (players[i] is not Player player || player.Character is not global::Illusionist.Scripts.Characters.Illusionist)
                {
                    continue;
                }

                Node2D merchant = visuals[i];
                if (!GodotObject.IsInstanceValid(merchant) || merchant.GetNodeOrNull(NodeName) != null)
                {
                    continue;
                }

                Node2D? sprite = SpineBody.CreateSprite(ShopSkel, alpha: 1f, NodeName);
                if (sprite == null)
                {
                    continue; // Spine unavailable — leave the borrowed merchant body.
                }

                // The borrowed spine is the merchant node's first child; hide it and anchor to its spot.
                Node2D? borrowed = (merchant.GetChildCount() > 0) ? merchant.GetChild(0) as Node2D : null;
                Vector2 anchor = borrowed?.Position ?? Vector2.Zero;
                if (borrowed != null)
                {
                    borrowed.Visible = false;
                }

                merchant.AddChild(sprite);
                SpineBody.Place(sprite, ShopSkel, anchor, TargetHeightPx, Mathf.Abs(merchant.GlobalScale.Y), YOffset);
                SpineBody.Play(sprite, ShopSkel);   // start idle AFTER it's in the tree
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[illusionist] Shop: spine apply failed: {ex}");
        }
    }
}
