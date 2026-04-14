using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenCharacterPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.Characters;
            _focusInputField = false;
            RefreshCharacterPageData(true);

            if (logger == null)
            {
                return;
            }

            if (!string.Equals(_lastCharacterAvailabilityLog, _cachedCharacterAvailability, StringComparison.Ordinal))
            {
                _lastCharacterAvailabilityLog = _cachedCharacterAvailability;
                logger.LogInfo(RandomLoadoutLog.Command("Character page opened. " + _cachedCharacterAvailability));
            }
        }

        private void ExecuteSwitchCharacter(FoyerCharacterOption option, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _characterActionMode == CharacterActionMode.Unlock
                ? _foyerCharacterSwitchService.UnlockCharacter(option)
                : _foyerCharacterSwitchService.SwitchCharacterOnly(option);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            RefreshCharacterPageData(true);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
            }
        }

        private string GetCharacterModeButtonLabel()
        {
            return _characterActionMode == CharacterActionMode.Unlock
                ? GuiText.Get("gui.characters.mode.unlock")
                : GuiText.Get("gui.characters.mode.switch_only");
        }

        private string GetCharacterModeHint()
        {
            return _characterActionMode == CharacterActionMode.Unlock
                ? GuiText.Get("gui.characters.hint.unlock")
                : GuiText.Get("gui.characters.hint.switch_only");
        }

        private void ToggleCharacterActionMode(ManualLogSource logger)
        {
            _characterActionMode = _characterActionMode == CharacterActionMode.Unlock
                ? CharacterActionMode.SwitchOnly
                : CharacterActionMode.Unlock;

            string modeMessage = _characterActionMode == CharacterActionMode.Unlock
                ? GuiText.Get("result.characters.mode_changed_unlock")
                : GuiText.Get("result.characters.mode_changed_switch_only");
            string logMessage = _characterActionMode == CharacterActionMode.Unlock
                ? GuiText.GetEnglish("result.characters.mode_changed_unlock")
                : GuiText.GetEnglish("result.characters.mode_changed_switch_only");
            ShowStatus(modeMessage, false);

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(logMessage));
            }
        }

        private void ShowStatus(string message, bool isError)
        {
            _statusMessage = message;
            _statusIsError = isError;
            _statusExpiresAt = Time.unscaledTime + StatusDurationSeconds;
        }

        private void RefreshCharacterPageData(bool forceRefresh)
        {
            if (_foyerCharacterSwitchService == null)
            {
                _cachedCharacterOptions = EmptyCharacterOptions;
                _cachedCharacterAvailability = GuiText.Get("gui.characters.availability.unavailable");
                return;
            }

            if (!forceRefresh && Time.unscaledTime < _nextCharacterPageRefreshAt)
            {
                return;
            }

            FoyerCharacterOption[] options = _foyerCharacterSwitchService.GetCharacterOptions();
            _cachedCharacterOptions = options ?? EmptyCharacterOptions;
            _cachedCharacterAvailability = BuildCharacterAvailabilityStatus(_cachedCharacterOptions);
            _nextCharacterPageRefreshAt = Time.unscaledTime + CharacterPageRefreshIntervalSeconds;
        }

        private static string BuildCharacterAvailabilityStatus(FoyerCharacterOption[] options)
        {
            if (options == null || options.Length == 0)
            {
                return GuiText.Get("gui.characters.availability.breach_only");
            }

            int availableCount = 0;
            int lockedCount = 0;
            for (int i = 0; i < options.Length; i++)
            {
                FoyerCharacterOption option = options[i];
                if (option.IsSelected || option.IsSelectable)
                {
                    availableCount++;
                }
                else if (option.CanUnlock)
                {
                    lockedCount++;
                }
            }

            return GuiText.Get("gui.characters.availability.summary", availableCount, lockedCount);
        }

        private void ResetCharacterPageCache()
        {
            _cachedCharacterOptions = EmptyCharacterOptions;
            _cachedCharacterAvailability = GuiText.Get("gui.characters.availability.breach_only");
            _nextCharacterPageRefreshAt = 0f;
        }
    }
}
