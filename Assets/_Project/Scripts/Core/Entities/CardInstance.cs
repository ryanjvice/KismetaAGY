using Kismeta.Core.Domain;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// A single physical card in play. Wraps a CardDefinition with runtime state:
    /// which zone it occupies, who owns it, and per-instance flags.
    /// </summary>
    public sealed class CardInstance
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public CardZone Zone { get; private set; }
        /// <summary>Player index (0-based) who owns this card. -1 = in common deck/discard.</summary>
        public int OwnerId { get; private set; }
        /// <summary>Adept cards can be turned face-down by The Tower fate; costs 1 Salt to refresh.</summary>
        public bool IsFaceDown { get; private set; }
        /// <summary>Crucible cards that have been Fired are marked here until Tempered.</summary>
        public bool IsFired { get; private set; }
        /// <summary>Crucible cards seized via a failed Gambit.</summary>
        public bool IsArrested { get; private set; }
        /// <summary>Human-readable summary set when a Fate resolves; shown in Active Effects.</summary>
        public string? FateResolutionNote { get; private set; }

        public CardInstance(string instanceId, string definitionId, CardZone initialZone, int ownerId)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            Zone = initialZone;
            OwnerId = ownerId;
        }

        public void MoveTo(CardZone zone, int newOwnerId = -1)
        {
            Zone = zone;
            if (newOwnerId >= 0)
                OwnerId = newOwnerId;
        }

        public void SetFaceDown(bool faceDown) => IsFaceDown = faceDown;
        public void SetFired(bool fired) => IsFired = fired;
        public void SetArrested(bool arrested) => IsArrested = arrested;
        public void SetFateResolutionNote(string? note) => FateResolutionNote = note;
    }
}
