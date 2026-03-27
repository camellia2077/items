namespace RandomLoadout.Core
{
    public sealed class GrantCommandParseResult
    {
        private GrantCommandParseResult(bool succeeded, GrantCommandRequest request, string errorMessage)
        {
            Succeeded = succeeded;
            Request = request;
            ErrorMessage = errorMessage;
        }

        public bool Succeeded { get; private set; }

        public GrantCommandRequest Request { get; private set; }

        public string ErrorMessage { get; private set; }

        public static GrantCommandParseResult Success(GrantCommandRequest request)
        {
            return new GrantCommandParseResult(true, request, string.Empty);
        }

        public static GrantCommandParseResult Failure(string errorMessage)
        {
            return new GrantCommandParseResult(false, null, errorMessage);
        }
    }
}
