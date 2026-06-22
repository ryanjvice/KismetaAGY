using Kismeta.Core.Commands;
using Kismeta.Core.Players;

namespace Kismeta.UI
{
    /// <summary>
    /// Submits player commands to the active <see cref="HotSeatController"/>.
    /// Mirrors the submission path used by <c>GameDebugUI</c>.
    /// </summary>
    public sealed class CommandBridge
    {
        private GameLoop? _loop;

        public void Bind(GameLoop loop) => _loop = loop;

        public bool CanSubmit => _loop?.PendingHumanController != null;

        public HotSeatController? PendingController => _loop?.PendingHumanController;

        public int ActivePlayerId => _loop?.ActivePlayerId ?? -1;

        public ActionHint PendingHint => _loop?.PendingHint ?? ActionHint.None;

        /// <summary>Fired after a successful side-effect apply (Card Limits rearrange/craft).</summary>
        public System.Action? OnSideEffectApplied;

        public bool TrySubmit(IGameCommand command)
        {
            var hs = _loop?.PendingHumanController;
            if (hs == null)
                return false;
            hs.SubmitCommand(command);
            return true;
        }

        /// <summary>
        /// Applies rearrange/craft during Card Limits without completing the pending discard request.
        /// </summary>
        public bool TryApplySideEffect(IGameCommand command)
        {
            if (_loop == null || PendingHint != ActionHint.DiscardToLimit)
                return false;

            int pid = ActivePlayerId;
            if (pid < 0) return false;

            bool allowed = command switch
            {
                WinterMoveCardCommand move => move.PlayerId == pid,
                CraftReagentCommand craft => craft.PlayerId == pid,
                _ => false
            };
            if (!allowed) return false;

            var result = _loop.ApplySideEffect(command);
            if (result.IsOk)
                OnSideEffectApplied?.Invoke();
            return result.IsOk;
        }

        public bool TrySubmitPass()
        {
            var hs = PendingController;
            if (hs == null) return false;

            int pid = hs.Slot.Index;
            IGameCommand cmd = PendingHint switch
            {
                ActionHint.SummerAction => new PassCrucibleActionCommand(pid),
                ActionHint.WinterAction => new PassActionCommand(pid),
                ActionHint.AutumnAction => new PassCrucibleActionCommand(pid),
                _ => new PassActionCommand(pid)
            };
            hs.SubmitCommand(cmd);
            return true;
        }

        public void SubmitPass()
        {
            TrySubmitPass();
        }
    }
}
