using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed partial class JsonLoadoutRuleFileProvider
    {
        private readonly string _filePath;
        private readonly string _fallbackFilePath;

        public JsonLoadoutRuleFileProvider(string filePath)
            : this(filePath, string.Empty)
        {
        }

        public JsonLoadoutRuleFileProvider(string filePath, string fallbackFilePath)
        {
            _filePath = filePath;
            _fallbackFilePath = fallbackFilePath ?? string.Empty;
        }

        public LoadoutRuleFileLoadResult Load()
        {
            List<string> messages = new List<string>();
            List<string> warnings = new List<string>();
            LoadoutRuleFileModel fileModel = null;

            if (!File.Exists(_filePath))
            {
                warnings.Add("Loadout rule file was not found at '" + _filePath + "'.");
                TryLoadFallback(messages, warnings, "Primary rule file was missing", out fileModel);
            }
            else
            {
                try
                {
                    string rawJson = Json5TextNormalizer.Normalize(File.ReadAllText(_filePath, Encoding.UTF8));
                    fileModel = ParseRuleFile(rawJson);
                }
                catch (Exception exception)
                {
                    warnings.Add("Failed to parse loadout rule file '" + _filePath + "'. " + exception.Message);
                    TryLoadFallback(messages, warnings, "Primary rule file could not be parsed", out fileModel);
                }
            }

            if (fileModel == null)
            {
                warnings.Add("Falling back to built-in default rules for this session.");
                fileModel = CreateDefaultModel();
            }

            return new LoadoutRuleFileLoadResult(ConvertToDefinitions(fileModel, messages), messages.ToArray(), warnings.ToArray());
        }

        private void TryLoadFallback(List<string> messages, List<string> warnings, string reason, out LoadoutRuleFileModel fileModel)
        {
            fileModel = null;

            if (string.IsNullOrEmpty(_fallbackFilePath) || !File.Exists(_fallbackFilePath))
            {
                warnings.Add(
                    "Fallback full-pool rule file was not found at '" + _fallbackFilePath + "'. " +
                    "Run deploy_mod.py again, or copy the repository default RandomLoadout rules into the game config directory.");
                return;
            }

            try
            {
                string fallbackRawJson = Json5TextNormalizer.Normalize(File.ReadAllText(_fallbackFilePath, Encoding.UTF8));
                fileModel = ParseRuleFile(fallbackRawJson);
                messages.Add(reason + ", so RandomLoadout loaded fallback rules from '" + _fallbackFilePath + "'.");
            }
            catch (Exception exception)
            {
                warnings.Add(
                    "Failed to parse fallback loadout rule file '" + _fallbackFilePath + "'. Falling back to built-in default. " +
                    exception.Message);
            }
        }

        private static LoadoutRuleFileModel CreateDefaultModel()
        {
            return new LoadoutRuleFileModel
            {
                Rules = new[]
                {
                    new LoadoutRuleFileRuleModel
                    {
                        Enabled = true,
                        Mode = "specific",
                        Category = "gun",
                        Count = 1,
                        Id = 541,
                        Pool = new string[0],
                    },
                    new LoadoutRuleFileRuleModel
                    {
                        Enabled = true,
                        Mode = "specific",
                        Category = "passive",
                        Count = 1,
                        Id = 118,
                        Pool = new string[0],
                    },
                },
            };
        }
    }
}
