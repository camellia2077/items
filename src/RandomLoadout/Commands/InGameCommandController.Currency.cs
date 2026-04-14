using System;
using System.Collections.Generic;
using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void OpenCurrencyPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.Currency;
            _focusInputField = false;

            if (logger != null)
            {
                logger.LogInfo(RandomLoadoutLog.Command("Currency menu opened."));
            }
        }

        private void DrawCurrencyPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                return;
            }

            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - ButtonWidth - 32f, 24f), GuiText.Get("gui.currency.title"), _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.currency.hint.choose"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GuiText.Get("gui.currency.hint.run_only"),
                _hintStyle);

            Rect addKeyButtonRect = new Rect(panelRect.x + 14f, panelRect.y + 92f, CurrencyActionButtonWidth, 34f);
            Rect addCurrencyButtonRect = new Rect(addKeyButtonRect.xMax + ButtonGap, addKeyButtonRect.y, CurrencyActionButtonWidth, 34f);
            Rect addMetaCurrencyButtonRect = new Rect(panelRect.x + 14f, addKeyButtonRect.yMax + ButtonGap, CurrencyActionButtonWidth, 34f);
            if (GUI.Button(addKeyButtonRect, GuiText.Get("gui.currency.button.key"), _buttonStyle))
            {
                ExecuteAddKey(player, logger);
            }

            if (GUI.Button(addCurrencyButtonRect, GuiText.Get("gui.currency.button.casings"), _buttonStyle))
            {
                // Dungeon run currency (casings).
                ExecuteAddCurrency(player, logger);
            }

            if (GUI.Button(addMetaCurrencyButtonRect, GuiText.Get("gui.currency.button.hegemony"), _buttonStyle))
            {
                // Breach hub meta currency (hegemony credits).
                ExecuteAddMetaCurrency(player, logger);
            }
        }
    }
}
