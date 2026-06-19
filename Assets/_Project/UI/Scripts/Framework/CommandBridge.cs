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

        public bool TrySubmit(IGameCommand command)
        {
            var hs = _loop?.PendingHumanController;
            if (hs == null)
                return false;
            hs.SubmitCommand(command);
            return true;
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
