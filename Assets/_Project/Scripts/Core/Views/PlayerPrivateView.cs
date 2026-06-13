using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;

namespace Kismeta.Core.Views
{
    /// <summary>
    /// Extends GamePublicView with the requesting player's Hand card IDs.
    /// This is the only view that includes Hand contents; it must never be sent
    /// to other players in a networked game.
    /// </summary>
    public sealed class PlayerPrivateView
    {
        public GamePublicView Public { get; }
        public int PlayerId { get; }
        public IReadOnlyList<string> Hand { get; }
        /// <summary>The 4 activation formulas for this player's assigned Codex (always visible to the owner).</summary>
        public IReadOnlyList<CodexFormulaDefinition> CodexFormulas { get; }

        private PlayerPrivateView(GameSession session, int playerId)
        {
            Public   = GamePublicView.From(session);
            PlayerId = playerId;

            // Find the requesting player's Hand and codex.
            var hand          = new List<string>();
            var codexFormulas = new List<CodexFormulaDefinition>();
            foreach (var p in session.Players)
            {
                if (p.PlayerId != playerId) continue;
                hand.AddRange(p.Hand);

                var codexDb = session.Rules?.CodexDatabase;
                if (codexDb != null && p.AssignedCodex != CodexVariant.None)
                    codexFormulas.AddRange(codexDb.GetEntriesForCodex(p.AssignedCodex));
                break;
            }
            Hand         = hand;
            CodexFormulas = codexFormulas;
        }

        /// <param name="playerId">The player requesting their private view.</param>
        public static PlayerPrivateView From(GameSession session, int playerId) =>
            new(session, playerId);
    }
}
