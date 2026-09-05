// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using EtgGameplayDashboard.Core;

namespace EtgGameplayDashboard
{
    internal sealed class PickupBrowserEntry
    {
        public PickupBrowserEntry(
            EtgPickupCatalogEntry catalogEntry,
            IList<string> aliases,
            Func<int, string> pickupGameplayNameProvider)
        {
            CatalogEntry = catalogEntry;
            string gameplayDisplayName = pickupGameplayNameProvider != null && catalogEntry != null
                ? pickupGameplayNameProvider(catalogEntry.PickupId)
                : string.Empty;
            DisplayName = !string.IsNullOrEmpty(gameplayDisplayName)
                ? gameplayDisplayName
                : ResolveDisplayName(catalogEntry);
            Aliases = aliases != null ? ToArray(aliases) : new string[0];
            PreferredInput = Aliases.Length > 0
                ? Aliases[0]
                : (!string.IsNullOrEmpty(catalogEntry.InternalName)
                    ? catalogEntry.InternalName.ToLowerInvariant()
                    : catalogEntry.PickupId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            CommandText = BuildCommandText(catalogEntry.Category, PreferredInput);
            MetadataLine = BuildMetadataLine(catalogEntry, Aliases, PreferredInput);
            SearchText = BuildSearchText(catalogEntry, gameplayDisplayName, Aliases, PreferredInput);
            IconFallbackLabel = GetCategoryInitial(catalogEntry.Category);
        }

        public EtgPickupCatalogEntry CatalogEntry { get; private set; }
        public string DisplayName { get; private set; }
        public string[] Aliases { get; private set; }
        public string PreferredInput { get; private set; }
        public string CommandText { get; private set; }
        public string MetadataLine { get; private set; }
        public string SearchText { get; private set; }
        public string IconFallbackLabel { get; private set; }

        private static string[] ToArray(IList<string> aliases)
        {
            string[] values = new string[aliases.Count];
            for (int index = 0; index < aliases.Count; index++) values[index] = aliases[index] ?? string.Empty;
            return values;
        }

        private static string ResolveDisplayName(EtgPickupCatalogEntry entry)
        {
            if (entry == null) return string.Empty;
            if (string.Equals(GuiText.CurrentLanguageCode, "en", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(entry.EnglishDisplayName)) return entry.EnglishDisplayName;
            if (!string.IsNullOrEmpty(entry.DisplayName)) return entry.DisplayName;
            return !string.IsNullOrEmpty(entry.EnglishDisplayName) ? entry.EnglishDisplayName : entry.InternalName;
        }

        private static string BuildCommandText(PickupCategory category, string preferredInput)
        {
            switch (category)
            {
                case PickupCategory.Gun: return "gun " + preferredInput;
                case PickupCategory.Passive: return "passive " + preferredInput;
                case PickupCategory.Active: return "active " + preferredInput;
                default: return preferredInput;
            }
        }

        private static string BuildMetadataLine(EtgPickupCatalogEntry entry, string[] aliases, string preferredInput)
        {
            string metadata = GuiText.GetCategoryLabel(entry.Category) + " | " + GuiText.Get("gui.pickups.metadata.id") + " " + entry.PickupId + " | " + preferredInput;
            if (aliases.Length > 1) metadata += " | " + GuiText.Get("gui.pickups.metadata.aliases") + ": " + string.Join(", ", aliases);
            else if (aliases.Length == 1 && !string.Equals(aliases[0], preferredInput, StringComparison.OrdinalIgnoreCase)) metadata += " | " + GuiText.Get("gui.pickups.metadata.alias") + ": " + aliases[0];
            else if (!string.IsNullOrEmpty(entry.InternalName) && !string.Equals(entry.InternalName, preferredInput, StringComparison.OrdinalIgnoreCase)) metadata += " | " + entry.InternalName;
            return metadata;
        }

        private static string BuildSearchText(EtgPickupCatalogEntry entry, string gameplayDisplayName, string[] aliases, string preferredInput)
        {
            // The browser can render the name resolved from the game's active language. Include
            // that same name in the index so, for example, a Chinese in-game item name can be
            // searched even when the shipped catalog's fallback labels are English.
            string rawValue = gameplayDisplayName + "|" + entry.DisplayName + "|" + entry.EnglishDisplayName + "|" + entry.InternalName + "|" + entry.PickupId + "|" + preferredInput + "|" + string.Join("|", aliases);
            System.Text.StringBuilder builder = new System.Text.StringBuilder(rawValue.Length);
            for (int index = 0; index < rawValue.Length; index++)
            {
                char current = rawValue[index];
                if (char.IsLetterOrDigit(current)) builder.Append(char.ToLowerInvariant(current));
            }

            return builder.ToString();
        }

        private static string GetCategoryInitial(PickupCategory category)
        {
            switch (category)
            {
                case PickupCategory.Gun: return "G";
                case PickupCategory.Passive: return "P";
                case PickupCategory.Active: return "A";
                default: return "?";
            }
        }
    }
}
