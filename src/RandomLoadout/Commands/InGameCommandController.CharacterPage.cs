using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void DrawCharacterPage(Rect panelRect, FoyerCharacterOption[] characterOptions, string availabilityMessage, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            Rect modeButtonRect = new Rect(backButtonRect.x - ButtonGap - CharacterModeButtonWidth, panelRect.y + 12f, CharacterModeButtonWidth, 30f);
            if (GUI.Button(modeButtonRect, GetCharacterModeButtonLabel(), _buttonStyle))
            {
                ToggleCharacterActionMode(logger);
            }

            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                ResetCharacterPageCache();
                return;
            }

            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - CharacterModeButtonWidth - ButtonWidth - 32f, 24f), GuiText.Get("gui.characters.title"), _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.characters.hint.apply_mode"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GetCharacterModeHint(),
                _hintStyle);
            float availabilityHeight = GetCharacterAvailabilityHeight(availabilityMessage, panelRect.width);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 80f, panelRect.width - 28f, availabilityHeight),
                availabilityMessage,
                _wrappedHintStyle);

            if (characterOptions.Length == 0)
            {
                return;
            }

            DrawCharacterButtons(panelRect, characterOptions, logger, 80f + availabilityHeight + 4f);
        }

        private void DrawCharacterButtons(Rect panelRect, FoyerCharacterOption[] characterOptions, ManualLogSource logger, float topOffset)
        {
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + topOffset, panelRect.width - 28f, 20f),
                GuiText.Get("gui.characters.select"),
                _hintStyle);

            for (int i = 0; i < characterOptions.Length; i++)
            {
                FoyerCharacterOption option = characterOptions[i];
                int row = i / CharacterButtonsPerRow;
                int column = i % CharacterButtonsPerRow;
                float buttonX = panelRect.x + 14f + (column * (CharacterButtonWidth + ButtonGap));
                float buttonY = panelRect.y + topOffset + 24f + (row * (34f + ButtonGap));
                Rect buttonRect = new Rect(buttonX, buttonY, CharacterButtonWidth, 34f);

                bool wasEnabled = GUI.enabled;
                GUI.enabled = !option.IsPending;
                string localizedLabel = GuiText.GetCharacterLabel(option.Label);
                string buttonLabel = option.IsSelected ? localizedLabel + " *" : localizedLabel;
                if (option.IsLocked && option.CanUnlock)
                {
                    buttonLabel = localizedLabel + " ?";
                }
                if (option.IsPending)
                {
                    buttonLabel = localizedLabel + " ...";
                }

                if (GUI.Button(buttonRect, buttonLabel, _buttonStyle))
                {
                    ExecuteSwitchCharacter(option, logger);
                }

                GUI.enabled = wasEnabled;
            }
        }

        private void DrawStatusOverlay(float panelHeight)
        {
            if (string.IsNullOrEmpty(_statusMessage) || Time.unscaledTime > _statusExpiresAt)
            {
                return;
            }

            Rect panelRect = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                Screen.height - PanelBottomMargin - panelHeight,
                PanelWidth,
                panelHeight);
            float statusWidth = Mathf.Min(StatusMaxWidth, Screen.width - 24f);
            GUIStyle style = _statusIsError ? _statusErrorStyle : _statusSuccessStyle;
            float statusHeight = Mathf.Max(StatusMinHeight, style.CalcHeight(new GUIContent(_statusMessage), statusWidth));
            Rect statusRect = new Rect(
                (Screen.width - statusWidth) * 0.5f,
                panelRect.y - StatusGap - statusHeight,
                statusWidth,
                statusHeight);

            GUI.Box(statusRect, _statusMessage, style);
        }

        private float GetPanelHeight(FoyerCharacterOption[] characterOptions, string characterAvailability)
        {
            if (_currentPage != PanelPage.Characters)
            {
                return BasePanelHeight;
            }

            int buttonCount = characterOptions != null ? characterOptions.Length : 0;
            int rows = buttonCount > 0 ? ((buttonCount + CharacterButtonsPerRow - 1) / CharacterButtonsPerRow) : 0;
            return GetCharacterHeaderHeight(characterAvailability) +
                   (rows * (34f + ButtonGap)) +
                   CharacterPanelFooterHeight;
        }

        private float GetCharacterHeaderHeight(string availabilityMessage)
        {
            return CharacterPanelBaseHeaderHeight + GetCharacterAvailabilityHeight(availabilityMessage, PanelWidth);
        }

        private float GetCharacterAvailabilityHeight(string availabilityMessage, float panelWidth)
        {
            return _wrappedHintStyle != null
                ? _wrappedHintStyle.CalcHeight(new GUIContent(availabilityMessage ?? string.Empty), panelWidth - 28f)
                : 40f;
        }
    }
}
