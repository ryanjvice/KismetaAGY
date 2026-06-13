using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Views
{
    /// <summary>
    /// Snapshot of everything visible to all players: board state, phase, and each
    /// player's public view. Constructed from a GameSession; never holds a live reference
    /// to mutable state. Safe to pass to UI, network, or AI without exposing Hand contents.
    /// </summary>
    public sealed class GamePublicView
    {
        public string SessionId { get; }
        public GameMode Mode { get; }
        public Season CurrentSeason { get; }
        public int CurrentStepIndex { get; }
        public string CurrentStepName { get; }
        public bool CardLockActive { get; }
        public bool IsOver { get; }
        public int? WinnerPlayerId { get; }

        public int RoundNumber { get; }
        public ZodiacSign CosmicAgeSign { get; }
        public int CommonDeckCount { get; }
        public int CommonDiscardCount { get; }

        public IReadOnlyList<PublicPlayerView> Players { get; }

        private GamePublicView(GameSession session)
        {
            SessionId = session.SessionId;
            Mode = session.Mode;
            CurrentSeason = session.Phase.CurrentSeason;
            CurrentStepIndex = session.Phase.CurrentStepIndex;
            CurrentStepName = session.Phase.CurrentStep.Name;
            CardLockActive = session.CardLockActive;
            IsOver = session.IsOver;
            WinnerPlayerId = session.WinnerPlayerId;

            RoundNumber = session.Board.RoundNumber;
            CosmicAgeSign = session.Board.CosmicAgeSign;
            CommonDeckCount = session.Board.CommonDeckCount;
            CommonDiscardCount = session.Board.CommonDiscardCount;

            var players = new List<PublicPlayerView>(session.Players.Count);
            foreach (var p in session.Players)
                players.Add(PublicPlayerView.From(p));
            Players = players;
        }

        public static GamePublicView From(GameSession session) => new(session);
    }
}
