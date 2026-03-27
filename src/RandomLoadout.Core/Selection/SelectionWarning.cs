using System;

namespace RandomLoadout.Core
{
    public sealed class SelectionWarning
    {
        public SelectionWarning(PickupCategory? category, string code, string message)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("A warning code is required.", "code");
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("A warning message is required.", "message");
            }

            Category = category;
            Code = code;
            Message = message;
        }

        public PickupCategory? Category { get; private set; }

        public string Code { get; private set; }

        public string Message { get; private set; }
    }
}
