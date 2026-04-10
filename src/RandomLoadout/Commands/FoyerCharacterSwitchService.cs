using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class FoyerCharacterSwitchService
    {
        private const float PendingSelectionTimeoutSeconds = 5f;
        private static readonly string[] KnownCharacterLabels =
        {
            "Marine",
            "Hunter",
            "Pilot",
            "Convict",
            "Robot",
            "Bullet",
            "Paradox",
            "Gunslinger",
        };

        private FoyerCharacterSelectFlag _pendingSelectionFlag;
        private float _pendingSelectionStartedAt;

        public FoyerCharacterOption[] GetCharacterOptions()
        {
            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                ClearPendingSelection();
                return new FoyerCharacterOption[0];
            }

            RefreshPendingSelectionState(foyer);

            FoyerCharacterSelectFlag[] flags = GetCharacterFlagsForFoyer(foyer);
            List<FoyerCharacterOption> options = new List<FoyerCharacterOption>();
            string selectedLabel = GetSelectedLabel(foyer);
            for (int i = 0; i < KnownCharacterLabels.Length; i++)
            {
                string label = KnownCharacterLabels[i];
                FoyerCharacterSelectFlag flag = FindFlagForLabel(flags, label);
                bool isSelected = !string.IsNullOrEmpty(selectedLabel) &&
                    string.Equals(selectedLabel, label, StringComparison.OrdinalIgnoreCase);
                bool isPending = (object)_pendingSelectionFlag != null &&
                    (object)_pendingSelectionFlag == (object)flag;
                bool isSelectable = !_pendingSelectionFlag &&
                    (isSelected || ((object)flag != null && flag.CanBeSelected()));
                options.Add(new FoyerCharacterOption(label, isSelectable, isSelected, isPending, flag, IsUnlockableCharacter(label)));
            }

            options.Sort(CompareOptions);
            return options.ToArray();
        }

        public string GetAvailabilityStatus()
        {
            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return "Character switching is only available in the Breach.";
            }

            FoyerCharacterOption[] options = GetCharacterOptions();
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

        public GrantCommandExecutionResult SwitchCharacter(FoyerCharacterOption option)
        {
            return SwitchCharacterOnly(option);
        }

        public GrantCommandExecutionResult UnlockCharacter(FoyerCharacterOption option)
        {
            if (option == null)
            {
                return new GrantCommandExecutionResult(false, "The selected character option was no longer available.");
            }

            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return new GrantCommandExecutionResult(false, "Character unlocking is only available in the Breach.");
            }

            if (string.Equals(option.Label, "Robot", StringComparison.OrdinalIgnoreCase))
            {
                // Robot is intentionally excluded from unlock mode.
                // In this panel, Robot follows switch-only behavior for reliability.
                return new GrantCommandExecutionResult(false, "Robot is switch-only in this panel and cannot be unlocked here.");
            }

            if (!IsUnlockableCharacter(option.Label))
            {
                return new GrantCommandExecutionResult(false, option.Label + " does not require manual unlock.");
            }

            FoyerCharacterSelectFlag refreshedFlag = FindFlagForLabel(GetCharacterFlagsForFoyer(foyer), option.Label);
            if ((object)refreshedFlag != null && refreshedFlag.CanBeSelected())
            {
                return new GrantCommandExecutionResult(true, option.Label + " is already unlocked.");
            }

            string unlockFailureMessage;
            if (!TryUnlockCharacter(option, out unlockFailureMessage))
            {
                return new GrantCommandExecutionResult(
                    false,
                    !string.IsNullOrEmpty(unlockFailureMessage)
                        ? unlockFailureMessage
                        : option.Label + " could not be unlocked.");
            }

            return new GrantCommandExecutionResult(
                true,
                "Unlocked " + option.Label + ". Reopen Characters or restart the game to refresh availability.");
        }

        public GrantCommandExecutionResult SwitchCharacterOnly(FoyerCharacterOption option)
        {
            if (option == null)
            {
                return new GrantCommandExecutionResult(false, "The selected character option was no longer available.");
            }

            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return new GrantCommandExecutionResult(false, "Character switching is only available in the Breach.");
            }

            RefreshPendingSelectionState(foyer);

            if ((object)_pendingSelectionFlag != null)
            {
                return new GrantCommandExecutionResult(false, "Character selection is already in progress.");
            }

            if (Foyer.IsCurrentlyPlayingCharacterSelect)
            {
                return new GrantCommandExecutionResult(false, "Character selection is already in progress.");
            }

            if (option.IsSelected ||
                ((object)option.Flag != null && (object)foyer.CurrentSelectedCharacterFlag == (object)option.Flag))
            {
                return new GrantCommandExecutionResult(false, option.Label + " is already selected.");
            }

            // Switch-only mode must avoid the native character-select callback flow,
            // because that flow can trigger currency costs for some selections.
            string forceSwitchFailureMessage;
            if (TryForceSwitchCharacterInBreach(foyer, option.Label, out forceSwitchFailureMessage))
            {
                return new GrantCommandExecutionResult(true, "Switched character to " + option.Label + " (switch-only mode).");
            }

            return new GrantCommandExecutionResult(
                false,
                !string.IsNullOrEmpty(forceSwitchFailureMessage)
                    ? forceSwitchFailureMessage
                    : "Force switch failed.");
        }
    }
}
