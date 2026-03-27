using System;

namespace RandomLoadout.Core
{
    public sealed class GrantCommandParser
    {
        public GrantCommandParseResult Parse(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return GrantCommandParseResult.Failure("Enter a command like 'gun AK-47' or 'item Scope'.");
            }

            string trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
            {
                return GrantCommandParseResult.Failure("Enter a command like 'gun AK-47' or 'item Scope'.");
            }

            string[] parts = trimmedInput.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return GrantCommandParseResult.Failure("Enter a command like 'gun AK-47' or 'item Scope'.");
            }

            GrantCommandTarget target;
            if (!TryParseTarget(parts[0], out target))
            {
                return GrantCommandParseResult.Failure("Unknown command target. Use gun, passive, active, or item.");
            }

            if (parts.Length < 2 || string.IsNullOrEmpty(parts[1].Trim()))
            {
                return GrantCommandParseResult.Failure("Enter the pickup name after the command target.");
            }

            return GrantCommandParseResult.Success(new GrantCommandRequest(target, parts[1].Trim()));
        }

        private static bool TryParseTarget(string rawTarget, out GrantCommandTarget target)
        {
            string normalized = rawTarget != null ? rawTarget.Trim().ToLowerInvariant() : string.Empty;
            switch (normalized)
            {
                case "gun":
                    target = GrantCommandTarget.Gun;
                    return true;
                case "passive":
                    target = GrantCommandTarget.Passive;
                    return true;
                case "active":
                    target = GrantCommandTarget.Active;
                    return true;
                case "item":
                case "any":
                    target = GrantCommandTarget.Any;
                    return true;
                default:
                    target = GrantCommandTarget.Any;
                    return false;
            }
        }
    }
}
