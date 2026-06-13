using System.Collections.Generic;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Read-only context passed to IPlayerController when requesting an action.
    /// Contains the requesting player's private view plus the full public view,
    /// and an ActionHint so the controller knows what kind of command is expected.
    /// </summary>
    public sealed class GameContext
    {
        public GamePublicView        PublicView    { get; }
        public PlayerPrivateView     PrivateView   { get; }
        public int                   ActivePlayerId { get; }
        public ActionHint            Hint           { get; }
        public ICrucibleCodexDatabase? CodexDatabase { get; }
        public ICardDatabase?          CardDatabase  { get; }

        /// <summary>
        /// When Hint == AdeptDecision: the instance ID of the Adept card being offered.
        /// When Hint == FateMoonDecision / FateLoversDecision etc.: the Fate card's instance ID.
        /// </summary>
        public string? PendingCardId { get; }

        /// <summary>
        /// When Hint == FateMoonDecision: the 4 card instance IDs drawn by The Moon.
        /// The player must keep exactly 2 of these; the rest return to the bottom of the deck.
        /// </summary>
        public IReadOnlyList<string>? MoonDrawnCardIds { get; }

        /// <summary>
        /// When Hint == AdeptDecision: instance IDs of Adept cards currently in the player's
        /// Arcanum. If this list is at the limit the player must specify a swap-out target.
        /// </summary>
        public IReadOnlyList<string>? ArcanumAdeptIds { get; }

        public GameContext(GamePublicView publicView, PlayerPrivateView privateView,
            int activePlayerId, ActionHint hint = ActionHint.None, string? pendingCardId = null,
            IReadOnlyList<string>? moonDrawnCardIds = null,
            IReadOnlyList<string>? arcanumAdeptIds = null,
            ICrucibleCodexDatabase? codexDatabase = null,
            ICardDatabase? cardDatabase = null)
        {
            PublicView       = publicView;
            PrivateView      = privateView;
            ActivePlayerId   = activePlayerId;
            Hint             = hint;
            PendingCardId    = pendingCardId;
            MoonDrawnCardIds = moonDrawnCardIds;
            ArcanumAdeptIds  = arcanumAdeptIds;
            CodexDatabase    = codexDatabase;
            CardDatabase     = cardDatabase;
        }
    }
}
