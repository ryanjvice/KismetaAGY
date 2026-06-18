using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;

namespace Kismeta.Core.Players
{
    public enum CeremonyStep
    {
        RoundOpen,
        AgeOpening,
        SpringIntro,
        SummerIntro,
        AutumnIntro,
        WinterIntro,
        AgeClosing,
    }

    public readonly struct CeremonyResult
    {
        public IGameCommand? Command { get; }

        public CeremonyResult(IGameCommand? command) => Command = command;

        public static CeremonyResult Empty => new(null);
    }

    /// <summary>
    /// Blocks <see cref="GameLoop"/> at ceremony screens until the UI calls Complete.
    /// </summary>
    public sealed class CeremonyGate
    {
        private TaskCompletionSource<CeremonyResult>? _tcs;

        public CeremonyStep? ActiveStep { get; private set; }

        public async Task<CeremonyResult> WaitAsync(CeremonyStep step, CancellationToken ct)
        {
            ActiveStep = step;
            _tcs = new TaskCompletionSource<CeremonyResult>();
            try
            {
                ct.ThrowIfCancellationRequested();
                using var reg = ct.Register(() => _tcs.TrySetCanceled());
                return await _tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                ActiveStep = null;
                _tcs = null;
            }
        }

        public void Complete() => CompleteWithCommand(null);

        public void CompleteWithCommand(IGameCommand? command) =>
            _tcs?.TrySetResult(new CeremonyResult(command));
    }
}
