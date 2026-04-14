namespace RandomLoadout
{
    internal sealed class GrantCommandExecutionResult
    {
        public GrantCommandExecutionResult(bool succeeded, string message)
            : this(succeeded, message, message)
        {
        }

        public GrantCommandExecutionResult(bool succeeded, string message, string logMessage)
        {
            Succeeded = succeeded;
            Message = message;
            LogMessage = logMessage;
        }

        public bool Succeeded { get; private set; }

        public string Message { get; private set; }

        public string LogMessage { get; private set; }

        public static GrantCommandExecutionResult Localized(bool succeeded, string key)
        {
            return Localized(succeeded, key, new object[0]);
        }

        public static GrantCommandExecutionResult Localized(bool succeeded, string key, params object[] args)
        {
            return new GrantCommandExecutionResult(
                succeeded,
                GuiText.Get(key, args),
                GuiText.GetEnglish(key, args));
        }
    }
}
