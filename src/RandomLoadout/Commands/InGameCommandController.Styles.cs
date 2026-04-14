using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeBorderedTexture(PanelBackgroundColor, PanelBorderColor);
            _panelStyle.border = new RectOffset(2, 2, 2, 2);
            _panelStyle.padding = new RectOffset(12, 12, 12, 12);

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.normal.textColor = PrimaryTextColor;
            _titleStyle.fontSize = 18;
            _titleStyle.fontStyle = FontStyle.Bold;

            _hintStyle = new GUIStyle(GUI.skin.label);
            _hintStyle.normal.textColor = SecondaryTextColor;
            _hintStyle.fontSize = 14;

            _wrappedHintStyle = new GUIStyle(_hintStyle);
            _wrappedHintStyle.wordWrap = true;

            _textFieldStyle = new GUIStyle(GUI.skin.textField);
            _textFieldStyle.normal.background = MakeTexture(1, 1, InputBackgroundColor);
            _textFieldStyle.focused.background = MakeTexture(1, 1, InputBackgroundColor);
            _textFieldStyle.normal.textColor = PrimaryTextColor;
            _textFieldStyle.focused.textColor = PrimaryTextColor;
            _textFieldStyle.border = new RectOffset(2, 2, 2, 2);
            _textFieldStyle.padding = new RectOffset(10, 10, 7, 7);
            _textFieldStyle.alignment = TextAnchor.MiddleLeft;
            _textFieldStyle.fontSize = 15;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.background = MakeTexture(1, 1, ButtonBackgroundColor);
            _buttonStyle.hover.background = MakeTexture(1, 1, ButtonHoverColor);
            _buttonStyle.active.background = MakeTexture(1, 1, ButtonActiveColor);
            _buttonStyle.normal.textColor = PrimaryTextColor;
            _buttonStyle.hover.textColor = PrimaryTextColor;
            _buttonStyle.active.textColor = PrimaryTextColor;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.fontSize = 14;

            _statusStyle = new GUIStyle(GUI.skin.box);
            _statusStyle.normal.textColor = PrimaryTextColor;
            _statusStyle.alignment = TextAnchor.MiddleCenter;
            _statusStyle.fontSize = 14;
            _statusStyle.padding = new RectOffset(10, 10, 6, 6);
            _statusStyle.wordWrap = true;

            _statusSuccessStyle = new GUIStyle(_statusStyle);
            _statusSuccessStyle.normal.background = MakeTexture(1, 1, SuccessBackgroundColor);

            _statusErrorStyle = new GUIStyle(_statusStyle);
            _statusErrorStyle.normal.background = MakeTexture(1, 1, ErrorBackgroundColor);

            _pickupRowStyle = new GUIStyle(GUI.skin.box);
            _pickupRowStyle.normal.background = MakeTexture(1, 1, new Color(0.10f, 0.11f, 0.14f, 0.92f));
            _pickupRowStyle.border = new RectOffset(1, 1, 1, 1);

            _pickupRowButtonStyle = new GUIStyle(GUI.skin.button);
            _pickupRowButtonStyle.normal.background = MakeTexture(1, 1, new Color(0f, 0f, 0f, 0f));
            _pickupRowButtonStyle.hover.background = MakeTexture(1, 1, new Color(0.18f, 0.16f, 0.11f, 0.30f));
            _pickupRowButtonStyle.active.background = MakeTexture(1, 1, new Color(0.28f, 0.22f, 0.14f, 0.38f));

            _pickupPrimaryTextStyle = new GUIStyle(GUI.skin.label);
            _pickupPrimaryTextStyle.normal.textColor = PrimaryTextColor;
            _pickupPrimaryTextStyle.fontSize = 14;
            _pickupPrimaryTextStyle.fontStyle = FontStyle.Bold;

            _pickupSecondaryTextStyle = new GUIStyle(GUI.skin.label);
            _pickupSecondaryTextStyle.normal.textColor = SecondaryTextColor;
            _pickupSecondaryTextStyle.fontSize = 11;

            _pickupFilterButtonStyle = new GUIStyle(_buttonStyle);
            _pickupFilterButtonStyle.fontSize = 13;

            _pickupFilterActiveButtonStyle = new GUIStyle(_buttonStyle);
            _pickupFilterActiveButtonStyle.normal.background = MakeTexture(1, 1, ButtonActiveColor);
            _pickupFilterActiveButtonStyle.hover.background = MakeTexture(1, 1, ButtonActiveColor);
            _pickupFilterActiveButtonStyle.active.background = MakeTexture(1, 1, ButtonActiveColor);
            _pickupFilterActiveButtonStyle.fontSize = 13;

            _pickupIconFallbackStyle = new GUIStyle(GUI.skin.box);
            _pickupIconFallbackStyle.normal.background = MakeTexture(1, 1, ButtonBackgroundColor);
            _pickupIconFallbackStyle.normal.textColor = PrimaryTextColor;
            _pickupIconFallbackStyle.alignment = TextAnchor.MiddleCenter;
            _pickupIconFallbackStyle.fontStyle = FontStyle.Bold;
        }

        private sealed class PickupBrowserEntry
        {
            public PickupBrowserEntry(EtgPickupCatalogEntry catalogEntry, IList<string> aliases)
            {
                CatalogEntry = catalogEntry;
                DisplayName = !string.IsNullOrEmpty(catalogEntry.DisplayName) ? catalogEntry.DisplayName : catalogEntry.InternalName;
                Aliases = aliases != null ? ToArray(aliases) : new string[0];
                PreferredInput = Aliases.Length > 0
                    ? Aliases[0]
                    : (!string.IsNullOrEmpty(catalogEntry.InternalName)
                        ? catalogEntry.InternalName.ToLowerInvariant()
                        : catalogEntry.PickupId.ToString());
                CommandText = BuildCommandText(catalogEntry.Category, PreferredInput);
                MetadataLine = BuildMetadataLine(catalogEntry, Aliases, PreferredInput);
                SearchText = BuildSearchText(catalogEntry, Aliases, PreferredInput);
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
                for (int index = 0; index < aliases.Count; index++)
                {
                    values[index] = aliases[index] ?? string.Empty;
                }

                return values;
            }

            private static string BuildCommandText(PickupCategory category, string preferredInput)
            {
                switch (category)
                {
                    case PickupCategory.Gun:
                        return "gun " + preferredInput;
                    case PickupCategory.Passive:
                        return "passive " + preferredInput;
                    case PickupCategory.Active:
                        return "active " + preferredInput;
                    default:
                        return preferredInput;
                }
            }

            private static string BuildMetadataLine(EtgPickupCatalogEntry catalogEntry, string[] aliases, string preferredInput)
            {
                string metadata = GuiText.GetCategoryLabel(catalogEntry.Category) + " | " + GuiText.Get("gui.pickups.metadata.id") + " " + catalogEntry.PickupId + " | " + preferredInput;
                if (aliases.Length > 1)
                {
                    metadata += " | " + GuiText.Get("gui.pickups.metadata.aliases") + ": " + string.Join(", ", aliases);
                }
                else if (aliases.Length == 1 && !string.Equals(aliases[0], preferredInput, StringComparison.OrdinalIgnoreCase))
                {
                    metadata += " | " + GuiText.Get("gui.pickups.metadata.alias") + ": " + aliases[0];
                }
                else if (!string.IsNullOrEmpty(catalogEntry.InternalName) &&
                         !string.Equals(catalogEntry.InternalName, preferredInput, StringComparison.OrdinalIgnoreCase))
                {
                    metadata += " | " + catalogEntry.InternalName;
                }

                return metadata;
            }

            private static string BuildSearchText(EtgPickupCatalogEntry catalogEntry, string[] aliases, string preferredInput)
            {
                string combined = catalogEntry.DisplayName + "|" +
                                  catalogEntry.InternalName + "|" +
                                  catalogEntry.PickupId + "|" +
                                  preferredInput + "|" +
                                  string.Join("|", aliases);
                return NormalizeLookupValue(combined);
            }

            private static string GetCategoryInitial(PickupCategory category)
            {
                switch (category)
                {
                    case PickupCategory.Gun:
                        return "G";
                    case PickupCategory.Passive:
                        return "P";
                    case PickupCategory.Active:
                        return "A";
                    default:
                        return "?";
                }
            }
        }

        private struct PickupIconData
        {
            public static readonly PickupIconData Empty = new PickupIconData(null, Rect.zero);

            public PickupIconData(Texture texture, Rect textureCoords)
            {
                Texture = texture;
                TextureCoords = textureCoords;
            }

            public Texture Texture;

            public Rect TextureCoords;
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeBorderedTexture(Color fillColor, Color borderColor)
        {
            Texture2D texture = new Texture2D(8, 8);
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[64];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isBorderPixel = x == 0 || x == 7 || y == 0 || y == 7;
                    pixels[(y * 8) + x] = isBorderPixel ? borderColor : fillColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
