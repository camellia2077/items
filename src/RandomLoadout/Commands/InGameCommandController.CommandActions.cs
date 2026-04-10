using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void Submit(PlayerController player, ManualLogSource logger)
        {
            GrantCommandParseResult parseResult = _parser.Parse(_inputText);
            if (!parseResult.Succeeded)
            {
                ShowStatus(parseResult.ErrorMessage, true);
                logger.LogWarning(RandomLoadoutLog.Command(parseResult.ErrorMessage));
                return;
            }

            GrantCommandExecutionResult executionResult = _commandService.Execute(player, parseResult.Request);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (executionResult.Succeeded)
            {
                logger.LogInfo(RandomLoadoutLog.Command(executionResult.Message));
                _inputText = string.Empty;
                _focusInputField = true;
            }
            else
            {
                logger.LogWarning(RandomLoadoutLog.Command(executionResult.Message));
            }
        }

        private void ExecuteRandom(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _commandService.ExecuteRandom(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteHealHalfHeart(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.HealHalfHeart(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteAddArmor(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddArmor(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteFullHeal(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.FullHeal(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteClearCurse(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.ClearCurse(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteRefillBlanks(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.RefillBlanks(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteRefillCurrentGunAmmo(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.RefillCurrentGunAmmo(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteAddKey(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddKey(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteAddCurrency(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddCurrency(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteAddMetaCurrency(PlayerController player, ManualLogSource logger)
        {
            GrantCommandExecutionResult executionResult = _playerDebugCommandService.AddMetaCurrency(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

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

        private void ExecuteToggleRapidFire(PlayerController player, ManualLogSource logger)
        {
            if (_rapidFireToggleService == null)
            {
                const string unavailableMessage = "Rapid fire service is unavailable.";
                ShowStatus(unavailableMessage, true);
                if (logger != null)
                {
                    logger.LogWarning(RandomLoadoutLog.Command(unavailableMessage));
                }

                return;
            }

            GrantCommandExecutionResult executionResult = _rapidFireToggleService.Toggle(player);
            ShowStatus(executionResult.Message, !executionResult.Succeeded);

            if (logger == null)
            {
                return;
            }

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
    }
}
