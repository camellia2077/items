namespace RandomLoadout.Core
{
    public sealed class GrantCommandParseResult
    {
        private GrantCommandParseResult(bool succeeded, GrantCommandRequest request, string errorMessage, string errorCode)
        {
            Succeeded = succeeded;
            Request = request;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        public bool Succeeded { get; private set; }

        public GrantCommandRequest Request { get; private set; }

        public string ErrorMessage { get; private set; }

        public string ErrorCode { get; private set; }

        public static GrantCommandParseResult Success(GrantCommandRequest request)
        {
            return new GrantCommandParseResult(true, request, string.Empty, string.Empty);
        }

        public static GrantCommandParseResult Failure(string errorCode, string errorMessage)
        {
            return new GrantCommandParseResult(false, null, errorMessage, errorCode);
        }
    }
}
