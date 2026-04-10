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
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.Message));
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.Message));
            }
        }

        private string GetCharacterModeButtonLabel()
        {
            return _characterActionMode == CharacterActionMode.Unlock
                ? "Mode: Unlock"
                : "Mode: Switch Only";
        }

        private string GetCharacterModeHint()
        {
            return _characterActionMode == CharacterActionMode.Unlock
                ? "Unlock mode: click a character to unlock it (Robot excluded)."
                : "Switch-only mode: click a character to switch immediately.";
        }

        private void ToggleCharacterActionMode(ManualLogSource logger)
        {
            _characterActionMode = _characterActionMode == CharacterActionMode.Unlock
                ? CharacterActionMode.SwitchOnly
                : CharacterActionMode.Unlock;

            string modeMessage = _characterActionMode == CharacterActionMode.Unlock
                ? "Character mode changed to Unlock."
                : "Character mode changed to Switch Only.";
            ShowStatus(modeMessage, false);

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command(modeMessage));
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
                _cachedCharacterAvailability = "Character switching is unavailable.";
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
                return "Character switching is only available in the Breach.";
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

            return "Found " + availableCount + " available characters and " + lockedCount + " locked hidden characters.";
        }

        private void ResetCharacterPageCache()
        {
            _cachedCharacterOptions = EmptyCharacterOptions;
            _cachedCharacterAvailability = "Character switching is only available in the Breach.";
            _nextCharacterPageRefreshAt = 0f;
        }
    }
}
