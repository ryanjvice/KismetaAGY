using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using UnityEngine;

namespace Kismeta.UI.Chronicle
{
    public readonly struct StoneRacePoint
    {
        public int RoundNumber { get; }
        public int PlayerId { get; }
        public StonePosition Position { get; }

        public StoneRacePoint(int roundNumber, int playerId, StonePosition position)
        {
            RoundNumber = roundNumber;
            PlayerId = playerId;
            Position = position;
        }
    }

    public sealed class ContestRecord
    {
        public int DuelWins;
        public int DuelLosses;
        public int GambitWins;
        public int GambitLosses;
        public int OppWins;
        public int OppLosses;

        public int TotalContests =>
            DuelWins + DuelLosses + GambitWins + GambitLosses + OppWins + OppLosses;
    }

    /// <summary>
    /// Subscribes to session events and records stone race + contest stats for end screens.
    /// </summary>
    public sealed class GameChronicle : MonoBehaviour
    {
        readonly List<StoneRacePoint> _stoneRace = new();
        readonly Dictionary<int, ContestRecord> _contests = new();
        int _reagentsForged;
        GameSession? _session;

        public IReadOnlyList<StoneRacePoint> StoneRace => _stoneRace;
        public int ReagentsForged => _reagentsForged;

        public ContestRecord GetContestRecord(int playerId)
        {
            if (!_contests.TryGetValue(playerId, out var rec))
            {
                rec = new ContestRecord();
                _contests[playerId] = rec;
            }
            return rec;
        }

        public int TotalContestCount()
        {
            int total = 0;
            foreach (var kv in _contests)
                total += kv.Value.TotalContests;
            return total / 2;
        }

        public void Reset()
        {
            _stoneRace.Clear();
            _contests.Clear();
            _reagentsForged = 0;
            _session = null;
        }

        public void Attach(GameSession session)
        {
            Detach(_session);
            Reset();
            _session = session;
            RecordInitialPositions(session);
            session.OnEvent += OnEvent;
        }

        public void Detach(GameSession? session)
        {
            if (session != null)
                session.OnEvent -= OnEvent;
            if (ReferenceEquals(_session, session))
                _session = null;
        }

        void OnEvent(IGameEvent evt)
        {
            int round = _session?.Board.RoundNumber ?? 1;

            switch (evt)
            {
                case StoneFiredEvent e:
                    _reagentsForged++;
                    AddStonePoint(round, e.PlayerId, e.NewPosition);
                    break;
                case StoneTemperedEvent e:
                    AddStonePoint(round, e.PlayerId, e.NewPosition);
                    break;
                case DuelResolvedEvent e:
                    RecordContestWin(e.WinnerId, e.AttackerId, e.DefenderId, ContestKind.Duel);
                    break;
                case GambitResolvedEvent e:
                    RecordContestWin(e.WinnerId, e.AttackerId, e.DefenderId, ContestKind.Gambit);
                    break;
                case OppositionResolvedEvent e:
                {
                    int winner = e.LoserId == e.AttackerId ? e.DefenderId : e.AttackerId;
                    RecordContestWin(winner, e.AttackerId, e.DefenderId, ContestKind.Opposition);
                    break;
                }
                case AgeTransitedEvent e:
                    if (_session != null)
                    {
                        foreach (var p in _session.Players)
                            AddStonePoint(e.NewRoundNumber, p.PlayerId, p.StonePosition);
                    }
                    break;
            }
        }

        void RecordInitialPositions(GameSession session)
        {
            int round = Mathf.Max(1, session.Board.RoundNumber);
            foreach (var p in session.Players)
                AddStonePoint(round, p.PlayerId, p.StonePosition);
        }

        void AddStonePoint(int roundNumber, int playerId, StonePosition position) =>
            _stoneRace.Add(new StoneRacePoint(roundNumber, playerId, position));

        enum ContestKind { Duel, Gambit, Opposition }

        void RecordContestWin(int winnerId, int attackerId, int defenderId, ContestKind kind)
        {
            int loserId = winnerId == attackerId ? defenderId : attackerId;
            var winner = GetContestRecord(winnerId);
            var loser = GetContestRecord(loserId);
            switch (kind)
            {
                case ContestKind.Duel:
                    winner.DuelWins++;
                    loser.DuelLosses++;
                    break;
                case ContestKind.Gambit:
                    winner.GambitWins++;
                    loser.GambitLosses++;
                    break;
                case ContestKind.Opposition:
                    winner.OppWins++;
                    loser.OppLosses++;
                    break;
            }
        }
    }
}
