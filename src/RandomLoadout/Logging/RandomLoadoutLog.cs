namespace RandomLoadout
{
    internal static class RandomLoadoutLog
    {
        public const string Prefix = "[RandomLoadout]";

        public static string Format(string scope, string message)
        {
            string scopedPrefix = string.IsNullOrEmpty(scope) ? Prefix : Prefix + "[" + scope + "]";
            if (string.IsNullOrEmpty(message))
            {
                return scopedPrefix;
            }

            return scopedPrefix + " " + message;
        }

        public static string Init(string message)
        {
            return Format("Init", message);
        }

        public static string Run(string message)
        {
            return Format("Run", message);
        }

        public static string BossRush(string message)
        {
            return Format("BossRush", message);
        }

        public static string Grant(string message)
        {
            return Format("Grant", message);
        }

        public static string Command(string message)
        {
            return Format("Command", message);
        }

        public static string Alias(string message)
        {
            return Format("Alias", message);
        }
    }
}
