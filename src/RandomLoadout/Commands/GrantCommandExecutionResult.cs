namespace RandomLoadout
{
    internal sealed class GrantCommandExecutionResult
    {
        public GrantCommandExecutionResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message;
        }

        public bool Succeeded { get; private set; }

        public string Message { get; private set; }
    }
}
