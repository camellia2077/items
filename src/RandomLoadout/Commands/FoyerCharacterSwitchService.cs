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
                return GuiText.Get("gui.characters.availability.breach_only");
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

            return GuiText.Get("gui.characters.availability.summary", availableCount, lockedCount);
        }

        public GrantCommandExecutionResult SwitchCharacter(FoyerCharacterOption option)
        {
            return SwitchCharacterOnly(option);
        }

        public GrantCommandExecutionResult UnlockCharacter(FoyerCharacterOption option)
        {
            if (option == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.characters.option_missing");
            }

            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.characters.breach_only_unlock");
            }

            if (string.Equals(option.Label, "Robot", StringComparison.OrdinalIgnoreCase))
            {
                // Robot is intentionally excluded from unlock mode.
                // In this panel, Robot follows switch-only behavior for reliability.
                return GrantCommandExecutionResult.Localized(false, "result.characters.robot_unlock_forbidden");
            }

            if (!IsUnlockableCharacter(option.Label))
            {
                return CreateCharacterResult(false, "result.characters.no_manual_unlock", option.Label);
            }

            FoyerCharacterSelectFlag refreshedFlag = FindFlagForLabel(GetCharacterFlagsForFoyer(foyer), option.Label);
            if ((object)refreshedFlag != null && refreshedFlag.CanBeSelected())
            {
                return CreateCharacterResult(true, "result.characters.already_unlocked", option.Label);
            }

            string unlockFailureMessage;
            if (!TryUnlockCharacter(option, out unlockFailureMessage))
            {
                return new GrantCommandExecutionResult(
                    false,
                    !string.IsNullOrEmpty(unlockFailureMessage)
                        ? unlockFailureMessage
                        : GuiText.Get("result.characters.unlock_failed", GuiText.GetCharacterLabel(option.Label)),
                    !string.IsNullOrEmpty(unlockFailureMessage)
                        ? unlockFailureMessage
                        : GuiText.GetEnglish("result.characters.unlock_failed", GuiText.GetEnglishCharacterLabel(option.Label)));
            }

            return CreateCharacterResult(true, "result.characters.unlock_success", option.Label);
        }

        public GrantCommandExecutionResult SwitchCharacterOnly(FoyerCharacterOption option)
        {
            if (option == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.characters.option_missing");
            }

            Foyer foyer = GetActiveFoyer();
            if ((object)foyer == null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.characters.breach_only_switch");
            }

            RefreshPendingSelectionState(foyer);

            if ((object)_pendingSelectionFlag != null)
            {
                return GrantCommandExecutionResult.Localized(false, "result.characters.selection_in_progress");
            }

            if (Foyer.IsCurrentlyPlayingCharacterSelect)
            {
                return GrantCommandExecutionResult.Localized(false, "result.characters.selection_in_progress");
            }

            if (option.IsSelected ||
                ((object)option.Flag != null && (object)foyer.CurrentSelectedCharacterFlag == (object)option.Flag))
            {
                return CreateCharacterResult(false, "result.characters.already_selected", option.Label);
            }

            // Switch-only mode must avoid the native character-select callback flow,
            // because that flow can trigger currency costs for some selections.
            string forceSwitchFailureMessage;
            if (TryForceSwitchCharacterInBreach(foyer, option.Label, out forceSwitchFailureMessage))
            {
                return CreateCharacterResult(true, "result.characters.switch_success", option.Label);
            }

            return new GrantCommandExecutionResult(
                false,
                !string.IsNullOrEmpty(forceSwitchFailureMessage)
                    ? forceSwitchFailureMessage
                    : GuiText.Get("result.characters.force_switch_failed"),
                !string.IsNullOrEmpty(forceSwitchFailureMessage)
                    ? forceSwitchFailureMessage
                    : GuiText.GetEnglish("result.characters.force_switch_failed"));
        }

        private static GrantCommandExecutionResult CreateCharacterResult(bool succeeded, string key, string characterLabel)
        {
            return new GrantCommandExecutionResult(
                succeeded,
                GuiText.Get(key, GuiText.GetCharacterLabel(characterLabel)),
                GuiText.GetEnglish(key, GuiText.GetEnglishCharacterLabel(characterLabel)));
        }
    }
}
