// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using BepInEx.Logging;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private void OpenCurrencyPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.Currency;
            _focusInputField = false;
            _currencyPageFocusedControlId = "currency.max_health";
            _isPickupShortcutConfigurationMode = false;
            CancelPickupShortcutCapture();

            if (logger != null)
            {
                logger.LogInfo(EtgGameplayDashboardLog.Command("Pickups menu opened."));
            }
        }

        private void DrawCurrencyPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect backButtonRect = GetSecondaryPageBackButtonRect(panelRect);
            Rect targetButtonRect = new Rect(backButtonRect.x - ButtonGap - ButtonWidth, panelRect.y + 12f, ButtonWidth, 30f);
            Rect shortcutConfigurationButtonRect = new Rect(
                targetButtonRect.x - ButtonGap - 148f,
                panelRect.y + 12f,
                148f,
                30f);
            GUIStyle targetButtonStyle = _characterSwitchTarget == CharacterSwitchTarget.SecondaryPlayer
                ? _enabledButtonStyle
                : _buttonStyle;
            if (GUI.Button(targetButtonRect, GetCharacterSwitchTargetButtonLabel(), GetControllerButtonStyle("currency.target", targetButtonStyle)))
            {
                ToggleCharacterSwitchTarget(logger);
            }
            if (GUI.Button(backButtonRect, GuiText.Get("gui.common.back"), GetControllerButtonStyle("currency.back", _buttonStyle)))
            {
                HandleCurrencyBack();
                return;
            }
            if (GUI.Button(
                shortcutConfigurationButtonRect,
                _isPickupShortcutConfigurationMode
                    ? GetPickupShortcutExitConfigurationButtonLabel()
                    : GetPickupShortcutConfigurationButtonLabel(),
                _buttonStyle))
            {
                TogglePickupShortcutConfigurationMode();
            }

            GUI.Label(
                new Rect(
                    panelRect.x + 14f,
                    panelRect.y + 12f,
                    shortcutConfigurationButtonRect.x - panelRect.x - 24f,
                    24f),
                GetLocalizedFallback("gui.command.currency.title", "Pickups", "拾取物"),
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                GetLocalizedFallback("gui.command.currency.hint.choose", "Choose a pickup to add.", "选择要增加的拾取物。"),
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                GetLocalizedFallback("gui.command.currency.hint.run_only", "Applies to the current character or current run only.", "只影响当前角色或当前这一局。") + " " + GetPickupBrowserTargetHint(),
                _hintStyle);

            const float rowHeight = 38f;
            const float rowGap = 8f;
            float top = panelRect.y + 92f;
            Rect contentRect = new Rect(panelRect.x + 14f, top, panelRect.width - 28f, panelRect.height - (top - panelRect.y) - 14f);
            DrawPickupActionRows(contentRect, top, rowHeight, rowGap, BuildCurrencyPickupRows(player, logger));
        }

        private void HandleCurrencyBack()
        {
            CancelPickupShortcutCapture();

            if (_isPickupShortcutConfigurationMode)
            {
                _isPickupShortcutConfigurationMode = false;
                _currencyPageFocusedControlId = "currency.max_health";
                return;
            }

            _isPickupShortcutConfigurationMode = false;
            _currentPage = PanelPage.Command;
            _focusInputField = true;
        }

        private static ControllerFocusEntry[] GetCurrencyPageFocusEntries()
        {
            return CurrencyPageFocusEntries;
        }

        private void ExecuteCurrencyPageFocusedControl(PlayerController player, ManualLogSource logger)
        {
            if (_isPickupShortcutConfigurationMode)
            {
                return;
            }

            switch (_currencyPageFocusedControlId)
            {
                case "currency.back":
                    HandleCurrencyBack();
                    return;
                case "currency.target":
                    ToggleCharacterSwitchTarget(logger);
                    return;
                case "currency.max_health":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddMaxHealth(targetPlayer, logger); });
                    return;
                case "currency.armor":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddArmor(targetPlayer, logger); });
                    return;
                case "currency.cell_key":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddCellKey(targetPlayer, logger); });
                    return;
                case "currency.blank":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddBlank(targetPlayer, logger); });
                    return;
                case "currency.key":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddKey(targetPlayer, logger); });
                    return;
                case "currency.rat_key":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddRatKey(targetPlayer, logger); });
                    return;
                case "currency.casings":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddCurrency(targetPlayer, logger); });
                    return;
                case "currency.clear_casings":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteClearCurrency(targetPlayer, logger); });
                    return;
                case "currency.hegemony":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddMetaCurrency(targetPlayer, logger); });
                    return;
                case "currency.clear_hegemony":
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteClearMetaCurrency(targetPlayer, logger); });
                    return;
                default:
                    return;
            }
        }

        private PickupActionRowDefinition[] BuildCurrencyPickupRows(PlayerController player, ManualLogSource logger)
        {
            if (_isPickupShortcutConfigurationMode)
            {
                return BuildCurrencyShortcutRows();
            }

            string actionLabel = GetLocalizedFallback("gui.command.currency.button.spawn", "Spawn", "生成");
            return new[]
            {
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteHealthPickup,
                    GetLocalizedFallback("gui.command.currency.label.max_health", "Max HP (+1)", "血量上限（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.max_health", actionLabel, delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddMaxHealth(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteArmorPickup,
                    GetLocalizedFallback("gui.command.currency.label.armor", "Armor (+1)", "护甲（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.armor", actionLabel, delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddArmor(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteBlankPickup,
                    GetLocalizedFallback("gui.command.currency.label.blank", "Blank (+1)", "空响弹（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.blank", actionLabel, delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddBlank(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteCasingsPickup,
                    GetLocalizedFallback("gui.command.currency.label.casings", "Casings (+100)", "弹壳（+100）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.casings", actionLabel, delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddCurrency(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("currency.clear_casings", GetLocalizedFallback("gui.command.currency.button.clear", "Clear", "清除"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteClearCurrency(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteHegemonyPickup,
                    GetLocalizedFallback("gui.command.currency.label.hegemony", "Hegemony", "霸权币"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.hegemony", actionLabel, delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddMetaCurrency(targetPlayer, logger); }); }, _buttonStyle),
                        new PickupActionButtonDefinition("currency.clear_hegemony", GetLocalizedFallback("gui.command.currency.button.clear", "Clear", "清除"), delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteClearMetaCurrency(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteKeyPickup,
                    GetLocalizedFallback("gui.command.currency.label.key", "Key (+1)", "钥匙（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.key", actionLabel, delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddKey(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    GameUiAtlasSpriteRatRewardKeyPickup,
                    GetLocalizedFallback("gui.command.currency.label.rat_key", "Rat Key (+1)", "老鼠钥匙（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.rat_key", actionLabel, delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddRatKey(targetPlayer, logger); }); }, _buttonStyle),
                    }),
                new PickupActionRowDefinition(
                    PlayerDebugCommandService.FindCellKeyPickupId(),
                    GetLocalizedFallback("gui.command.currency.label.cell_key", "Cell Key (+1)", "牢房钥匙（+1）"),
                    new[]
                    {
                        new PickupActionButtonDefinition("currency.cell_key", actionLabel, delegate { ExecuteForSelectedPickupTargets(player, delegate(PlayerController targetPlayer) { ExecuteAddCellKey(targetPlayer, logger); }); }, _buttonStyle),
                    }),
            };
        }

    }
}
