using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenPickupPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.Pickups;
            _focusInputField = false;
            _focusPickupSearchField = true;
            RefreshPickupBrowserData();

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Pickup browser opened."));
            }
        }

        private void DrawPickupPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, "Back", _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                _focusPickupSearchField = false;
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - ButtonWidth - 32f, 24f),
                "Pickup Browser",
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                "Search by alias, internal name, display name, or ID.",
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                "Click a row or Grant to use the selected pickup. Icons reuse the game's live sprite data.",
                _hintStyle);

            GUI.SetNextControlName(PickupSearchControlName);
            Rect searchRect = new Rect(panelRect.x + 14f, panelRect.y + 86f, panelRect.width - 28f, 32f);
            _pickupSearchText = GUI.TextField(searchRect, _pickupSearchText, 128, _textFieldStyle);
            if (_focusPickupSearchField)
            {
                GUI.FocusControl(PickupSearchControlName);
                _focusPickupSearchField = false;
            }

            float filtersTop = searchRect.yMax + 10f;
            DrawPickupFilterButtons(panelRect.x + 14f, filtersTop);

            float listTop = filtersTop + 38f;
            Rect listRect = new Rect(panelRect.x + 14f, listTop, panelRect.width - 28f, panelRect.height - (listTop - panelRect.y) - 14f);
            DrawPickupResults(listRect, player, logger);
        }

        private void DrawPickupFilterButtons(float left, float top)
        {
            DrawPickupFilterButton(new Rect(left, top, PickupFilterButtonWidth, 30f), PickupBrowserFilter.All, "All");
            DrawPickupFilterButton(new Rect(left + PickupFilterButtonWidth + ButtonGap, top, PickupFilterButtonWidth, 30f), PickupBrowserFilter.Gun, "Gun");
            DrawPickupFilterButton(new Rect(left + (PickupFilterButtonWidth + ButtonGap) * 2f, top, PickupFilterButtonWidth, 30f), PickupBrowserFilter.Passive, "Passive");
            DrawPickupFilterButton(new Rect(left + (PickupFilterButtonWidth + ButtonGap) * 3f, top, PickupFilterButtonWidth, 30f), PickupBrowserFilter.Active, "Active");
        }

        private void DrawPickupFilterButton(Rect rect, PickupBrowserFilter filter, string label)
        {
            GUIStyle style = _pickupBrowserFilter == filter ? _pickupFilterActiveButtonStyle : _pickupFilterButtonStyle;
            if (GUI.Button(rect, label, style))
            {
                _pickupBrowserFilter = filter;
                _pickupScrollPosition = Vector2.zero;
                _focusPickupSearchField = true;
            }
        }

        private void DrawPickupResults(Rect listRect, PlayerController player, ManualLogSource logger)
        {
            PickupBrowserEntry[] matches = GetFilteredPickupEntries();
            if (matches.Length == 0)
            {
                GUI.Box(listRect, GUIContent.none, _pickupRowStyle);
                GUI.Label(
                    new Rect(listRect.x + 12f, listRect.y + 12f, listRect.width - 24f, listRect.height - 24f),
                    "No pickups matched the current search.",
                    _wrappedHintStyle);
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - 18f, (matches.Length * PickupRowHeight) + 4f);
            _pickupScrollPosition = GUI.BeginScrollView(listRect, _pickupScrollPosition, viewRect);
            for (int i = 0; i < matches.Length; i++)
            {
                DrawPickupRow(new Rect(0f, 2f + (i * PickupRowHeight), viewRect.width, PickupRowHeight - 4f), matches[i], player, logger);
            }

            GUI.EndScrollView();
        }

        private void DrawPickupRow(Rect rowRect, PickupBrowserEntry entry, PlayerController player, ManualLogSource logger)
        {
            GUI.Box(rowRect, GUIContent.none, _pickupRowStyle);

            Rect rowButtonRect = new Rect(rowRect.x, rowRect.y, rowRect.width - PickupGrantButtonWidth - ButtonGap, rowRect.height);
            if (GUI.Button(rowButtonRect, GUIContent.none, _pickupRowButtonStyle))
            {
                ExecutePickupBrowserGrant(entry, player, logger);
            }

            Rect iconRect = new Rect(rowRect.x + 8f, rowRect.y + ((rowRect.height - PickupIconSize) * 0.5f), PickupIconSize, PickupIconSize);
            DrawPickupIcon(iconRect, entry);

            float textLeft = iconRect.xMax + 8f;
            float textWidth = rowRect.width - PickupGrantButtonWidth - 32f - PickupIconSize - 16f;
            GUI.Label(
                new Rect(textLeft, rowRect.y + 5f, textWidth, 20f),
                entry.DisplayName,
                _pickupPrimaryTextStyle);
            GUI.Label(
                new Rect(textLeft, rowRect.y + 24f, textWidth, 18f),
                entry.MetadataLine,
                _pickupSecondaryTextStyle);

            Rect grantButtonRect = new Rect(rowRect.x + rowRect.width - PickupGrantButtonWidth - 8f, rowRect.y + 8f, PickupGrantButtonWidth, rowRect.height - 16f);
            if (GUI.Button(grantButtonRect, "Grant", _buttonStyle))
            {
                ExecutePickupBrowserGrant(entry, player, logger);
            }
        }

        private void DrawPickupIcon(Rect iconRect, PickupBrowserEntry entry)
        {
            PickupIconData iconData;
            if (TryGetPickupIcon(entry.CatalogEntry.PickupId, out iconData))
            {
                GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                return;
            }

            GUI.Box(iconRect, entry.IconFallbackLabel, _pickupIconFallbackStyle);
        }

        private void ExecutePickupBrowserGrant(PickupBrowserEntry entry, PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _commandService.ExecuteCatalogEntry(player, entry.CatalogEntry);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            _inputText = entry.CommandText;

            if (executionResult.Succeeded)
            {
                if (logger != null)
                {
                    logger.LogInfo(RandomLoadoutLog.Command(executionResult.Message));
                }

                _focusPickupSearchField = true;
            }
            else if (logger != null)
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.Message));
            }
        }

        private void RefreshPickupBrowserData()
        {
            if (_cachedPickupEntries.Length > 0 || _pickupCatalogProvider == null)
            {
                return;
            }

            EtgPickupCatalogEntry[] catalogEntries = _pickupCatalogProvider() ?? new EtgPickupCatalogEntry[0];
            PickupAliasRegistry aliasRegistry = _aliasRegistryProvider != null ? _aliasRegistryProvider() : PickupAliasRegistry.Empty;
            Dictionary<int, List<string>> aliasesByPickupId = BuildAliasesByPickupId(aliasRegistry);
            List<PickupBrowserEntry> browserEntries = new List<PickupBrowserEntry>(catalogEntries.Length);
            for (int index = 0; index < catalogEntries.Length; index++)
            {
                EtgPickupCatalogEntry entry = catalogEntries[index];
                if (entry == null)
                {
                    continue;
                }

                List<string> aliases;
                aliasesByPickupId.TryGetValue(entry.PickupId, out aliases);
                browserEntries.Add(new PickupBrowserEntry(entry, aliases));
            }

            _cachedPickupEntries = browserEntries.ToArray();
        }

        private void ResetPickupBrowserState()
        {
            _cachedPickupEntries = EmptyPickupBrowserEntries;
            _pickupSearchText = string.Empty;
            _pickupBrowserFilter = PickupBrowserFilter.All;
            _pickupScrollPosition = Vector2.zero;
            _focusPickupSearchField = false;
            _pickupIconCache.Clear();
        }

        private bool TryGetPickupIcon(int pickupId, out PickupIconData iconData)
        {
            if (_pickupIconCache.TryGetValue(pickupId, out iconData))
            {
                return iconData.Texture != null;
            }

            PickupObject pickup = PickupObjectDatabase.GetById(pickupId);
            iconData = CreatePickupIconData(pickup);
            _pickupIconCache[pickupId] = iconData;
            return iconData.Texture != null;
        }

        private static PickupIconData CreatePickupIconData(PickupObject pickup)
        {
            // Reuse the game's live pickup sprite data so the browser does not need its own icon bundle.
            if ((object)pickup == null || (object)pickup.sprite == null)
            {
                return PickupIconData.Empty;
            }

            tk2dSpriteDefinition definition = pickup.sprite.CurrentSprite;
            if (definition == null || definition.material == null || definition.uvs == null || definition.uvs.Length == 0)
            {
                return PickupIconData.Empty;
            }

            Texture texture = definition.material.mainTexture;
            if (texture == null)
            {
                return PickupIconData.Empty;
            }

            float minX = definition.uvs[0].x;
            float minY = definition.uvs[0].y;
            float maxX = minX;
            float maxY = minY;
            for (int index = 1; index < definition.uvs.Length; index++)
            {
                Vector2 uv = definition.uvs[index];
                minX = Mathf.Min(minX, uv.x);
                minY = Mathf.Min(minY, uv.y);
                maxX = Mathf.Max(maxX, uv.x);
                maxY = Mathf.Max(maxY, uv.y);
            }

            return new PickupIconData(texture, Rect.MinMaxRect(minX, minY, maxX, maxY));
        }

        private static string NormalizeLookupValue(string rawValue)
        {
            if (string.IsNullOrEmpty(rawValue))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(rawValue.Length);
            for (int index = 0; index < rawValue.Length; index++)
            {
                char current = rawValue[index];
                if (char.IsLetterOrDigit(current))
                {
                    builder.Append(char.ToLowerInvariant(current));
                }
            }

            return builder.ToString();
        }

        private PickupBrowserEntry[] GetFilteredPickupEntries()
        {
            if (_cachedPickupEntries.Length == 0)
            {
                return EmptyPickupBrowserEntries;
            }

            string normalizedSearch = NormalizeLookupValue(_pickupSearchText);
            List<PickupBrowserEntry> matches = new List<PickupBrowserEntry>();
            for (int index = 0; index < _cachedPickupEntries.Length; index++)
            {
                PickupBrowserEntry entry = _cachedPickupEntries[index];
                if (!MatchesPickupFilter(entry.CatalogEntry.Category))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(normalizedSearch) &&
                    entry.SearchText.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                matches.Add(entry);
            }

            return matches.ToArray();
        }

        private bool MatchesPickupFilter(PickupCategory category)
        {
            switch (_pickupBrowserFilter)
            {
                case PickupBrowserFilter.All:
                    return true;
                case PickupBrowserFilter.Gun:
                    return category == PickupCategory.Gun;
                case PickupBrowserFilter.Passive:
                    return category == PickupCategory.Passive;
                case PickupBrowserFilter.Active:
                    return category == PickupCategory.Active;
                default:
                    return true;
            }
        }

        private static Dictionary<int, List<string>> BuildAliasesByPickupId(PickupAliasRegistry aliasRegistry)
        {
            Dictionary<int, List<string>> aliasesByPickupId = new Dictionary<int, List<string>>();
            PickupAliasRegistry effectiveRegistry = aliasRegistry ?? PickupAliasRegistry.Empty;
            for (int index = 0; index < effectiveRegistry.Entries.Length; index++)
            {
                PickupAliasEntry entry = effectiveRegistry.Entries[index];
                List<string> aliases;
                if (!aliasesByPickupId.TryGetValue(entry.PickupId, out aliases))
                {
                    aliases = new List<string>();
                    aliasesByPickupId.Add(entry.PickupId, aliases);
                }

                aliases.Add(entry.Alias);
            }

            return aliasesByPickupId;
        }
    }
}
