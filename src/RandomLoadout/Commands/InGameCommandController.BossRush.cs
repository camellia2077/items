using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenBossRushPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.BossRush;
            _focusInputField = false;

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Boss Rush page opened."));
            }
        }

        private void DrawBossRushPage(Rect panelRect, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                return;
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - ButtonWidth - 32f, 24f),
                GuiText.Get("gui.boss_rush.title"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.boss_rush.hint.breach"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.boss_rush.hint.active"),
                _hintStyle);

            string availabilityMessage = _bossRushService != null
                ? _bossRushService.GetAvailabilityMessage()
                : GuiText.Get("gui.boss_rush.unavailable.breach_only");
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 88f, panelRect.width - 28f, 36f),
                availabilityMessage,
                _wrappedHintStyle);

            string floorLabel = _bossRushService != null ? _bossRushService.GetCurrentFloorLabel() : GuiText.Get("label.boss_rush.floor.none");
            string stateLabel = _bossRushService != null ? _bossRushService.GetStateLabel() : GuiText.Get("label.boss_rush.state.idle");
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 130f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.boss_rush.status.floor", floorLabel),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 150f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.boss_rush.status.state", stateLabel),
                _hintStyle);

            Rect startButtonRect = new Rect(panelRect.x + 14f, panelRect.y + 182f, BossRushActionButtonWidth, 34f);
            Rect returnButtonRect = new Rect(startButtonRect.xMax + ButtonGap, startButtonRect.y, BossRushActionButtonWidth, 34f);

            bool startEnabled = _bossRushService != null && !_bossRushService.IsActive;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = startEnabled;
            if (GUI.Button(startButtonRect, GuiText.Get("gui.boss_rush.button.start"), _buttonStyle))
            {
                ExecuteStartBossRush(logger);
            }

            GUI.enabled = _bossRushService != null && _bossRushService.IsActive;
            if (GUI.Button(returnButtonRect, GuiText.Get("gui.boss_rush.button.return"), _buttonStyle))
            {
                ExecuteAbortBossRush(logger);
            }

            GUI.enabled = previousEnabled;
        }

        private void ExecuteStartBossRush(ManualLogSource logger)
        {
            if (_bossRushService == null)
            {
                ShowStatus(GuiText.Get("gui.boss_rush.unavailable.breach_only"), true);
                return;
            }

            GrantCommandExecutionResult executionResult = _bossRushService.Start();
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            if (logger != null)
            {
                if (executionResult.Succeeded)
                {
                    logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                }
                else
                {
                    logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
                }
            }
        }

        private void ExecuteAbortBossRush(ManualLogSource logger)
        {
            if (_bossRushService == null)
            {
                ShowStatus(GuiText.Get("gui.boss_rush.unavailable.breach_only"), true);
                return;
            }

            GrantCommandExecutionResult executionResult = _bossRushService.ReturnToCharacterSelect();
            ShowStatus(executionResult.Message, !executionResult.Succeeded);
            if (logger != null)
            {
                if (executionResult.Succeeded)
                {
                    logger.LogInfo(RandomLoadoutLog.Command(executionResult.LogMessage));
                }
                else
                {
                    logger.LogWarning(RandomLoadoutLog.Command(executionResult.LogMessage));
                }
            }
        }

        private void OnBossRushStatusRaised(GrantCommandExecutionResult result)
        {
            if (result != null)
            {
                ShowStatus(result.Message, !result.Succeeded);
            }
        }
    }
}
