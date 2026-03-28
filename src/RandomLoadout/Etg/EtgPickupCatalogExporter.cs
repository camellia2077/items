using System;
using System.IO;
using System.Text;

namespace RandomLoadout
{
    internal sealed class EtgPickupCatalogExporter
    {
        private readonly string _textOutputPath;
        private readonly string _jsonOutputPath;
        private readonly string _groupedJsonOutputPath;
        private readonly string _rulePoolOutputPath;

        public EtgPickupCatalogExporter(string textOutputPath, string jsonOutputPath, string groupedJsonOutputPath, string rulePoolOutputPath)
        {
            _textOutputPath = textOutputPath;
            _jsonOutputPath = jsonOutputPath;
            _groupedJsonOutputPath = groupedJsonOutputPath;
            _rulePoolOutputPath = rulePoolOutputPath;
        }

        public EtgPickupCatalogExportResult Export(EtgPickupResolver pickupResolver)
        {
            if (pickupResolver == null)
            {
                return new EtgPickupCatalogExportResult(false, _textOutputPath, _jsonOutputPath, _groupedJsonOutputPath, _rulePoolOutputPath, 0, "The pickup resolver was not available.");
            }

            try
            {
                EtgPickupCatalogEntry[] entries = pickupResolver.GetGrantablePickupCatalog();
                if (entries.Length == 0)
                {
                    return new EtgPickupCatalogExportResult(false, _textOutputPath, _jsonOutputPath, _groupedJsonOutputPath, _rulePoolOutputPath, 0, "No supported pickups were available for export yet.");
                }

                EnsureDirectoryExists(_textOutputPath);
                EnsureDirectoryExists(_jsonOutputPath);
                EnsureDirectoryExists(_groupedJsonOutputPath);
                EnsureDirectoryExists(_rulePoolOutputPath);

                WriteTextCatalog(entries);
                WriteJsonCatalog(entries);
                WriteGroupedJsonCatalog(entries);
                WriteFullRulePool(entries);

                return new EtgPickupCatalogExportResult(true, _textOutputPath, _jsonOutputPath, _groupedJsonOutputPath, _rulePoolOutputPath, entries.Length, string.Empty);
            }
            catch (Exception exception)
            {
                return new EtgPickupCatalogExportResult(false, _textOutputPath, _jsonOutputPath, _groupedJsonOutputPath, _rulePoolOutputPath, 0, exception.Message);
            }
        }

        private void WriteTextCatalog(EtgPickupCatalogEntry[] entries)
        {
            using (StreamWriter writer = new StreamWriter(_textOutputPath, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("# RandomLoadout grantable pickup catalog");
                writer.WriteLine("# Generated (UTC): " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine(
                    "# Format: Category<TAB>ID<TAB>DisplayName<TAB>InternalName<TAB>EncounterGuid<TAB>Quality<TAB>PurchasePrice<TAB>CanBeDropped<TAB>CanBeSold<TAB>SuppressInInventory<TAB>PrimaryDisplayName<TAB>ShortDescription<TAB>LongDescription<TAB>ContentSource<TAB>ForcedPositionInAmmonomicon<TAB>GunClass<TAB>Ammo<TAB>CanGainAmmo<TAB>InfiniteAmmo<TAB>ReloadTime<TAB>ActiveNumberOfUses<TAB>ActiveTimeCooldown<TAB>ActiveDamageCooldown<TAB>ActiveRoomCooldown");

                for (int i = 0; i < entries.Length; i++)
                {
                    EtgPickupCatalogEntry entry = entries[i];
                    WriteField(writer, entry.Category.ToString());
                    WriteField(writer, entry.PickupId.ToString());
                    WriteField(writer, entry.DisplayName);
                    WriteField(writer, entry.InternalName);
                    WriteField(writer, entry.EncounterGuid);
                    WriteField(writer, entry.Quality);
                    WriteField(writer, entry.PurchasePrice.ToString());
                    WriteField(writer, entry.CanBeDropped.ToString());
                    WriteField(writer, entry.CanBeSold.ToString());
                    WriteField(writer, entry.SuppressInInventory.ToString());
                    WriteField(writer, entry.PrimaryDisplayName);
                    WriteField(writer, entry.ShortDescription);
                    WriteField(writer, entry.LongDescription);
                    WriteField(writer, entry.ContentSource);
                    WriteField(writer, entry.ForcedPositionInAmmonomicon.ToString());
                    WriteField(writer, entry.GunClass);
                    WriteField(writer, entry.Ammo.ToString());
                    WriteField(writer, entry.CanGainAmmo.ToString());
                    WriteField(writer, entry.InfiniteAmmo.ToString());
                    WriteField(writer, entry.ReloadTime.ToString("0.###"));
                    WriteField(writer, entry.ActiveNumberOfUses.ToString());
                    WriteField(writer, entry.ActiveTimeCooldown.ToString("0.###"));
                    WriteField(writer, entry.ActiveDamageCooldown.ToString("0.###"));
                    writer.WriteLine(Sanitize(entry.ActiveRoomCooldown.ToString()));
                }
            }
        }

        private void WriteJsonCatalog(EtgPickupCatalogEntry[] entries)
        {
            using (StreamWriter writer = new StreamWriter(_jsonOutputPath, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("{");
                writer.WriteLine("  \"generatedUtc\": \"" + EscapeJson(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")) + "\",");
                writer.WriteLine("  \"entryCount\": " + entries.Length + ",");
                writer.WriteLine("  \"pickups\": [");

                for (int i = 0; i < entries.Length; i++)
                {
                    EtgPickupCatalogEntry entry = entries[i];
                    writer.WriteLine("    {");
                    writer.WriteLine("      \"category\": \"" + EscapeJson(entry.Category.ToString()) + "\",");
                    writer.WriteLine("      \"pickupId\": " + entry.PickupId + ",");
                    writer.WriteLine("      \"displayName\": \"" + EscapeJson(entry.DisplayName) + "\",");
                    writer.WriteLine("      \"internalName\": \"" + EscapeJson(entry.InternalName) + "\",");
                    writer.WriteLine("      \"encounterGuid\": \"" + EscapeJson(entry.EncounterGuid) + "\",");
                    writer.WriteLine("      \"quality\": \"" + EscapeJson(entry.Quality) + "\",");
                    writer.WriteLine("      \"purchasePrice\": " + entry.PurchasePrice + ",");
                    writer.WriteLine("      \"canBeDropped\": " + ToJsonBoolean(entry.CanBeDropped) + ",");
                    writer.WriteLine("      \"canBeSold\": " + ToJsonBoolean(entry.CanBeSold) + ",");
                    writer.WriteLine("      \"suppressInInventory\": " + ToJsonBoolean(entry.SuppressInInventory) + ",");
                    writer.WriteLine("      \"primaryDisplayName\": \"" + EscapeJson(entry.PrimaryDisplayName) + "\",");
                    writer.WriteLine("      \"shortDescription\": \"" + EscapeJson(entry.ShortDescription) + "\",");
                    writer.WriteLine("      \"longDescription\": \"" + EscapeJson(entry.LongDescription) + "\",");
                    writer.WriteLine("      \"contentSource\": \"" + EscapeJson(entry.ContentSource) + "\",");
                    writer.WriteLine("      \"forcedPositionInAmmonomicon\": " + entry.ForcedPositionInAmmonomicon + ",");
                    writer.WriteLine("      \"gunClass\": \"" + EscapeJson(entry.GunClass) + "\",");
                    writer.WriteLine("      \"ammo\": " + entry.Ammo + ",");
                    writer.WriteLine("      \"canGainAmmo\": " + ToJsonBoolean(entry.CanGainAmmo) + ",");
                    writer.WriteLine("      \"infiniteAmmo\": " + ToJsonBoolean(entry.InfiniteAmmo) + ",");
                    writer.WriteLine("      \"reloadTime\": " + ToJsonFloat(entry.ReloadTime) + ",");
                    writer.WriteLine("      \"activeNumberOfUses\": " + entry.ActiveNumberOfUses + ",");
                    writer.WriteLine("      \"activeTimeCooldown\": " + ToJsonFloat(entry.ActiveTimeCooldown) + ",");
                    writer.WriteLine("      \"activeDamageCooldown\": " + ToJsonFloat(entry.ActiveDamageCooldown) + ",");
                    writer.WriteLine("      \"activeRoomCooldown\": " + entry.ActiveRoomCooldown);
                    writer.Write("    }");
                    writer.WriteLine(i < entries.Length - 1 ? "," : string.Empty);
                }

                writer.WriteLine("  ]");
                writer.WriteLine("}");
            }
        }

        private void WriteGroupedJsonCatalog(EtgPickupCatalogEntry[] entries)
        {
            using (StreamWriter writer = new StreamWriter(_groupedJsonOutputPath, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("{");
                writer.WriteLine("  \"generatedUtc\": \"" + EscapeJson(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")) + "\",");
                writer.WriteLine("  \"entryCount\": " + entries.Length + ",");
                writer.WriteLine("  \"categories\": {");
                WriteCategoryGroup(writer, entries, "Gun", 4, true);
                WriteCategoryGroup(writer, entries, "Passive", 4, true);
                WriteCategoryGroup(writer, entries, "Active", 4, false);
                writer.WriteLine("  }");
                writer.WriteLine("}");
            }
        }

        private void WriteFullRulePool(EtgPickupCatalogEntry[] entries)
        {
            int gunCount = CountCategory(entries, "Gun");
            int passiveCount = CountCategory(entries, "Passive");
            int activeCount = CountCategory(entries, "Active");

            using (StreamWriter writer = new StreamWriter(_rulePoolOutputPath, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("{");
                writer.WriteLine("  \"generatedUtc\": \"" + EscapeJson(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")) + "\",");
                writer.WriteLine("  \"description\": \"Auto-generated full pickup rule pool. Copy the rules you want into RandomLoadout.rules.json if you want to activate them.\",");
                writer.WriteLine("  \"entryCount\": " + entries.Length + ",");
                writer.WriteLine("  \"categoryCounts\": {");
                writer.WriteLine("    \"gun\": " + gunCount + ",");
                writer.WriteLine("    \"passive\": " + passiveCount + ",");
                writer.WriteLine("    \"active\": " + activeCount);
                writer.WriteLine("  },");
                writer.WriteLine("  \"rules\": [");
                WriteRandomRule(writer, entries, "Gun", "gun", true);
                WriteRandomRule(writer, entries, "Passive", "passive", true);
                WriteRandomRule(writer, entries, "Active", "active", false);
                writer.WriteLine("  ],");
                writer.WriteLine("  \"reference\": {");
                WriteReferenceCategory(writer, entries, "Gun", 4, true);
                WriteReferenceCategory(writer, entries, "Passive", 4, true);
                WriteReferenceCategory(writer, entries, "Active", 4, false);
                writer.WriteLine("  }");
                writer.WriteLine("}");
            }
        }

        private static void WriteCategoryGroup(StreamWriter writer, EtgPickupCatalogEntry[] entries, string categoryName, int indentSpaces, bool trailingComma)
        {
            string indent = new string(' ', indentSpaces);
            string itemIndent = new string(' ', indentSpaces + 2);
            writer.WriteLine(indent + "\"" + EscapeJson(categoryName) + "\": [");

            bool wroteAny = false;
            for (int i = 0; i < entries.Length; i++)
            {
                EtgPickupCatalogEntry entry = entries[i];
                if (!string.Equals(entry.Category.ToString(), categoryName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (wroteAny)
                {
                    writer.WriteLine(itemIndent + ",");
                }

                WriteJsonPickupObject(writer, entry, itemIndent);
                wroteAny = true;
            }

            writer.Write(indent + "]");
            writer.WriteLine(trailingComma ? "," : string.Empty);
        }

        private static void WriteRandomRule(StreamWriter writer, EtgPickupCatalogEntry[] entries, string categoryName, string categoryValue, bool trailingComma)
        {
            writer.WriteLine("    {");
            writer.WriteLine("      \"enabled\": true,");
            writer.WriteLine("      \"mode\": \"random\",");
            writer.WriteLine("      \"category\": \"" + categoryValue + "\",");
            writer.WriteLine("      \"count\": 1,");
            writer.WriteLine("      \"poolIds\": [");

            bool wroteAny = false;
            for (int i = 0; i < entries.Length; i++)
            {
                EtgPickupCatalogEntry entry = entries[i];
                if (!string.Equals(entry.Category.ToString(), categoryName, StringComparison.Ordinal))
                {
                    continue;
                }

                writer.Write("        " + entry.PickupId);
                wroteAny = true;

                if (HasLaterCategoryEntry(entries, i + 1, categoryName))
                {
                    writer.Write(",");
                }

                writer.WriteLine();
            }

            if (!wroteAny)
            {
                writer.WriteLine("      ");
            }

            writer.WriteLine("      ]");
            writer.Write("    }");
            writer.WriteLine(trailingComma ? "," : string.Empty);
        }

        private static void WriteReferenceCategory(StreamWriter writer, EtgPickupCatalogEntry[] entries, string categoryName, int indentSpaces, bool trailingComma)
        {
            string indent = new string(' ', indentSpaces);
            string itemIndent = new string(' ', indentSpaces + 2);
            writer.WriteLine(indent + "\"" + EscapeJson(categoryName) + "\": [");

            bool wroteAny = false;
            for (int i = 0; i < entries.Length; i++)
            {
                EtgPickupCatalogEntry entry = entries[i];
                if (!string.Equals(entry.Category.ToString(), categoryName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (wroteAny)
                {
                    writer.WriteLine(itemIndent + ",");
                }

                WriteReferencePickupObject(writer, entry, itemIndent);
                wroteAny = true;
            }

            writer.Write(indent + "]");
            writer.WriteLine(trailingComma ? "," : string.Empty);
        }

        private static void WriteJsonPickupObject(StreamWriter writer, EtgPickupCatalogEntry entry, string indent)
        {
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "  \"category\": \"" + EscapeJson(entry.Category.ToString()) + "\",");
            writer.WriteLine(indent + "  \"pickupId\": " + entry.PickupId + ",");
            writer.WriteLine(indent + "  \"displayName\": \"" + EscapeJson(entry.DisplayName) + "\",");
            writer.WriteLine(indent + "  \"internalName\": \"" + EscapeJson(entry.InternalName) + "\",");
            writer.WriteLine(indent + "  \"encounterGuid\": \"" + EscapeJson(entry.EncounterGuid) + "\",");
            writer.WriteLine(indent + "  \"quality\": \"" + EscapeJson(entry.Quality) + "\",");
            writer.WriteLine(indent + "  \"purchasePrice\": " + entry.PurchasePrice + ",");
            writer.WriteLine(indent + "  \"canBeDropped\": " + ToJsonBoolean(entry.CanBeDropped) + ",");
            writer.WriteLine(indent + "  \"canBeSold\": " + ToJsonBoolean(entry.CanBeSold) + ",");
            writer.WriteLine(indent + "  \"suppressInInventory\": " + ToJsonBoolean(entry.SuppressInInventory) + ",");
            writer.WriteLine(indent + "  \"primaryDisplayName\": \"" + EscapeJson(entry.PrimaryDisplayName) + "\",");
            writer.WriteLine(indent + "  \"shortDescription\": \"" + EscapeJson(entry.ShortDescription) + "\",");
            writer.WriteLine(indent + "  \"longDescription\": \"" + EscapeJson(entry.LongDescription) + "\",");
            writer.WriteLine(indent + "  \"contentSource\": \"" + EscapeJson(entry.ContentSource) + "\",");
            writer.WriteLine(indent + "  \"forcedPositionInAmmonomicon\": " + entry.ForcedPositionInAmmonomicon + ",");
            writer.WriteLine(indent + "  \"gunClass\": \"" + EscapeJson(entry.GunClass) + "\",");
            writer.WriteLine(indent + "  \"ammo\": " + entry.Ammo + ",");
            writer.WriteLine(indent + "  \"canGainAmmo\": " + ToJsonBoolean(entry.CanGainAmmo) + ",");
            writer.WriteLine(indent + "  \"infiniteAmmo\": " + ToJsonBoolean(entry.InfiniteAmmo) + ",");
            writer.WriteLine(indent + "  \"reloadTime\": " + ToJsonFloat(entry.ReloadTime) + ",");
            writer.WriteLine(indent + "  \"activeNumberOfUses\": " + entry.ActiveNumberOfUses + ",");
            writer.WriteLine(indent + "  \"activeTimeCooldown\": " + ToJsonFloat(entry.ActiveTimeCooldown) + ",");
            writer.WriteLine(indent + "  \"activeDamageCooldown\": " + ToJsonFloat(entry.ActiveDamageCooldown) + ",");
            writer.WriteLine(indent + "  \"activeRoomCooldown\": " + entry.ActiveRoomCooldown);
            writer.Write(indent + "}");
        }

        private static void WriteReferencePickupObject(StreamWriter writer, EtgPickupCatalogEntry entry, string indent)
        {
            writer.WriteLine(indent + "{");
            writer.WriteLine(indent + "  \"pickupId\": " + entry.PickupId + ",");
            writer.WriteLine(indent + "  \"displayName\": \"" + EscapeJson(entry.DisplayName) + "\",");
            writer.WriteLine(indent + "  \"internalName\": \"" + EscapeJson(entry.InternalName) + "\",");
            writer.WriteLine(indent + "  \"quality\": \"" + EscapeJson(entry.Quality) + "\",");
            writer.WriteLine(indent + "  \"encounterGuid\": \"" + EscapeJson(entry.EncounterGuid) + "\"");
            writer.Write(indent + "}");
        }

        private static int CountCategory(EtgPickupCatalogEntry[] entries, string categoryName)
        {
            int count = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].Category.ToString(), categoryName, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasLaterCategoryEntry(EtgPickupCatalogEntry[] entries, int startIndex, string categoryName)
        {
            for (int i = startIndex; i < entries.Length; i++)
            {
                if (string.Equals(entries[i].Category.ToString(), categoryName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace("\r", " ").Replace("\n", " ").Trim();
        }

        private static void WriteField(StreamWriter writer, string value)
        {
            writer.Write(Sanitize(value));
            writer.Write('\t');
        }

        private static void EnsureDirectoryExists(string outputPath)
        {
            string directoryPath = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string ToJsonBoolean(bool value)
        {
            return value ? "true" : "false";
        }

        private static string ToJsonFloat(float value)
        {
            return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
