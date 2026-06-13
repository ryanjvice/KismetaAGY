namespace Kismeta.Core.Commands
{
    public enum CommandStatus
    {
        Ok = 0,
        NotImplemented,
        Invalid,
        Rejected
    }

    public readonly struct CommandResult
    {
        public readonly CommandStatus Status;
        public readonly string? Message;

        private CommandResult(CommandStatus status, string? message)
        {
            Status = status;
            Message = message;
        }

        public bool IsOk => Status == CommandStatus.Ok;

        public static CommandResult Ok(string? message = null) =>
            new(CommandStatus.Ok, message);

        public static CommandResult NotImplemented(string commandName) =>
            new(CommandStatus.NotImplemented, $"Handler not yet implemented for: {commandName}");

        public static CommandResult Invalid(string reason) =>
            new(CommandStatus.Invalid, reason);

        public static CommandResult Rejected(string reason) =>
            new(CommandStatus.Rejected, reason);
    }
}
