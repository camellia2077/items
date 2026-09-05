// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private const string RoomEnemyRewindShortcutId = "room.rewind";
        private const string CurrencyShortcutMaxHealthId = "currency.max_health";
        private const string CurrencyShortcutArmorId = "currency.armor";
        private const string CurrencyShortcutBlankId = "currency.blank";
        private const string CurrencyShortcutCasingsId = "currency.casings";
        private const string CurrencyShortcutHegemonyId = "currency.hegemony";
        private const string CurrencyShortcutKeyId = "currency.key";
        private const string CurrencyShortcutRatKeyId = "currency.rat_key";
        private const string CurrencyShortcutCellKeyId = "currency.cell_key";

        private static string GetPickupShortcutConfigurationButtonLabel()
        {
            return GetLocalizedFallback("gui.pickups.button.configure_shortcuts", "Set Shortcuts", "设置快捷键");
        }

        private static string GetPickupShortcutExitConfigurationButtonLabel()
        {
            return GetLocalizedFallback("gui.pickups.button.exit_shortcuts", "Exit Editing", "退出编辑");
        }

        private void TogglePickupShortcutConfigurationMode()
        {
            CancelPickupShortcutCapture();
            _isPickupShortcutConfigurationMode = !_isPickupShortcutConfigurationMode;
            ShowStatus(
                GetLocalizedFallback("result.pickups.shortcut.mode_enabled", "Shortcut setup mode enabled.", "已进入快捷键设置模式。"),
                false);
        }

        private void HandlePickupShortcutCapture()
        {
            if (!_isCapturingPickupShortcut || string.IsNullOrEmpty(_pickupShortcutCaptureTargetId) || Event.current == null || Event.current.type != EventType.KeyDown)
            {
                return;
            }

            KeyCode keyCode = Event.current.keyCode;
            Event.current.Use();
            if (keyCode == KeyCode.Escape)
            {
                CancelPickupShortcutCapture();
                return;
            }

            if (IsReservedPickupShortcutKey(keyCode, _pickupShortcutCaptureTargetId))
            {
                ShowStatus(
                    GetLocalizedFallback("result.pickups.shortcut.reserved", "That key is reserved by the command panel.", "该按键由控制面板占用。"),
                    true);
                return;
            }

            string replacedTargetId;
            if (!_keyboardShortcutRegistry.Set(_pickupShortcutCaptureTargetId, keyCode, out replacedTargetId))
            {
                ShowStatus(
                    GetLocalizedFallback("result.pickups.shortcut.invalid", "Only keyboard keys can be used for pickup shortcuts.", "物品快捷键只能使用键盘按键。"),
                    true);
                return;
            }

            PersistKeyboardShortcuts();
            _isCapturingPickupShortcut = false;
            _pickupShortcutCaptureTargetId = string.Empty;
            string shortcutName = GetPickupShortcutDisplayName(keyCode);
            ShowStatus(
                GetLocalizedFormattedFallback("result.pickups.shortcut.set", "Pickup shortcut set to {0}.", "物品快捷键已设置为 {0}。", shortcutName),
                false);
            if (!string.IsNullOrEmpty(replacedTargetId))
            {
                ShowStatus(
                    GetLocalizedFormattedFallback("result.pickups.shortcut.replaced", "{0} is now assigned to this pickup; the previous assignment was cleared.", "{0} 已分配给当前物品，原快捷键已清除。", shortcutName),
                    false);
            }
        }

        private void TryHandlePickupShortcut()
        {
            if (_isVisible || _keyboardShortcutRegistry == null || _pickupCatalogProvider == null)
            {
                return;
            }

            KeyValuePair<string, KeyCode>[] bindings = _keyboardShortcutRegistry.GetBindings();
            for (int index = 0; index < bindings.Length; index++)
            {
                KeyValuePair<string, KeyCode> binding = bindings[index];
                if (IsReservedPickupShortcutKey(binding.Value) || !Input.GetKeyDown(binding.Value))
                {
                    continue;
                }

                if (string.Equals(binding.Key, RoomEnemyRewindShortcutId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (binding.Key.StartsWith("currency.", StringComparison.Ordinal))
                {
                    ExecuteCurrencyShortcut(binding.Key);
                    return;
                }

                int pickupId;
                if (!int.TryParse(binding.Key, out pickupId))
                {
                    continue;
                }

                EtgPickupCatalogEntry pickupEntry = FindPickupCatalogEntry(pickupId);
                if (pickupEntry == null)
                {
                    ShowStatus(
                        GetLocalizedFallback("result.pickups.shortcut.missing", "The pickup assigned to this shortcut is unavailable.", "该快捷键绑定的物品当前不可用。"),
                        true);
                    LogPickupShortcutMessage("Pickup shortcut target was unavailable. PickupId=" + pickupId + ".");
                    return;
                }

                GrantCommandExecutionResult executionResult = _commandService.ExecuteCatalogEntry(
                    GetSelectedCommandTargetPlayer(),
                    pickupEntry);
                ShowStatus(executionResult.Message, !executionResult.Succeeded);
                LogPickupShortcutMessage(
                    "Pickup keyboard shortcut activated. Key=" + binding.Value +
                    ", PickupId=" + pickupId +
                    ", Succeeded=" + executionResult.Succeeded + ".");
                return;
            }
        }

        private EtgPickupCatalogEntry FindPickupCatalogEntry(int pickupId)
        {
            EtgPickupCatalogEntry[] catalogEntries = _pickupCatalogProvider();
            if (catalogEntries == null)
            {
                return null;
            }

            for (int index = 0; index < catalogEntries.Length; index++)
            {
                EtgPickupCatalogEntry entry = catalogEntries[index];
                if (entry != null && entry.PickupId == pickupId)
                {
                    return entry;
                }
            }

            return null;
        }

        private void LogPickupShortcutMessage(string message)
        {
            if (_inputLogHandler != null)
            {
                _inputLogHandler("[PickupShortcut] " + message);
            }
        }

        private void BeginPickupShortcutCapture(PickupBrowserEntry entry)
        {
            if (entry == null || entry.CatalogEntry == null)
            {
                return;
            }

            BeginPickupShortcutCapture(entry.CatalogEntry.PickupId);
        }

        private void BeginPickupShortcutCapture(int pickupId)
        {
            BeginPickupShortcutCapture(pickupId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private void BeginPickupShortcutCapture(string targetId)
        {
            _isCapturingPickupShortcut = true;
            _pickupShortcutCaptureTargetId = targetId;
            ShowStatus(
                GetLocalizedFallback("result.pickups.shortcut.waiting", "Press a keyboard key to assign it. Esc cancels.", "请按下要绑定的键盘按键，Esc 取消。"),
                false);
        }

        private void BeginRoomEnemyRewindShortcutCapture()
        {
            BeginPickupShortcutCapture(RoomEnemyRewindShortcutId);
        }

        private void ClearRoomEnemyRewindShortcut()
        {
            CancelPickupShortcutCapture();
            KeyCode currentKey;
            if (_keyboardShortcutRegistry.TryGetKey(RoomEnemyRewindShortcutId, out currentKey) &&
                currentKey == RoomEnemyRewindShortcutKey)
            {
                return;
            }

            string replacedTargetId;
            if (_keyboardShortcutRegistry.Set(RoomEnemyRewindShortcutId, RoomEnemyRewindShortcutKey, out replacedTargetId))
            {
                PersistKeyboardShortcuts();
                ShowStatus(
                    GetLocalizedFallback("result.room.rewind.shortcut.cleared", "Room rewind shortcut reset to C.", "房间回溯快捷键已重置为 C。"),
                    false);
            }
        }

        private string GetRoomEnemyRewindShortcutButtonLabel()
        {
            if (_isCapturingPickupShortcut &&
                string.Equals(_pickupShortcutCaptureTargetId, RoomEnemyRewindShortcutId, StringComparison.Ordinal))
            {
                return GetLocalizedFallback("gui.pickups.button.shortcut_waiting", "Press key...", "请按键…");
            }

            KeyCode keyCode;
            return _keyboardShortcutRegistry.TryGetKey(RoomEnemyRewindShortcutId, out keyCode)
                ? GetPickupShortcutDisplayName(keyCode)
                : GetPickupShortcutNoneDisplayName();
        }

        private void CancelPickupShortcutCapture()
        {
            _isCapturingPickupShortcut = false;
            _pickupShortcutCaptureTargetId = string.Empty;
        }

        private void ClearPickupShortcut(PickupBrowserEntry entry)
        {
            if (entry == null || entry.CatalogEntry == null)
            {
                return;
            }

            ClearPickupShortcut(entry.CatalogEntry.PickupId);
        }

        private void ClearPickupShortcut(int pickupId)
        {
            ClearPickupShortcut(pickupId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private void ClearPickupShortcut(string targetId)
        {
            CancelPickupShortcutCapture();
            if (_keyboardShortcutRegistry.Clear(targetId))
            {
                PersistKeyboardShortcuts();
                ShowStatus(
                    GetLocalizedFallback("result.pickups.shortcut.cleared", "Pickup shortcut cleared.", "物品快捷键已清除。"),
                    false);
            }
        }

        private string GetPickupShortcutButtonLabel(PickupBrowserEntry entry)
        {
            int pickupId = entry != null && entry.CatalogEntry != null ? entry.CatalogEntry.PickupId : -1;
            return GetPickupShortcutButtonLabel(pickupId);
        }

        private string GetPickupShortcutButtonLabel(int pickupId)
        {
            return GetPickupShortcutButtonLabel(pickupId.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private string GetPickupShortcutButtonLabel(string targetId)
        {
            if (string.Equals(_pickupShortcutCaptureTargetId, targetId, StringComparison.Ordinal))
            {
                return GetLocalizedFallback("gui.pickups.button.shortcut_waiting", "Press key...", "请按键…");
            }

            KeyCode keyCode;
            string keyName = _keyboardShortcutRegistry.TryGetKey(targetId, out keyCode)
                ? GetPickupShortcutDisplayName(keyCode)
                : GetPickupShortcutNoneDisplayName();
            return GetLocalizedFormattedFallback("gui.pickups.button.shortcut", "Key: {0}", "键：{0}", keyName);
        }

        private static string GetPickupShortcutNoneDisplayName()
        {
            string localizedNone = GuiText.Get("gui.pickups.button.shortcut_none");
            if (string.Equals(localizedNone, "gui.pickups.button.shortcut_none", StringComparison.Ordinal))
            {
                return string.Equals(GuiText.CurrentLanguageCode, "zh-CN", StringComparison.OrdinalIgnoreCase) ? "无" : "None";
            }

            if (localizedNone.StartsWith("键：", StringComparison.Ordinal))
            {
                return localizedNone.Substring(2).Trim();
            }

            if (localizedNone.StartsWith("Key:", StringComparison.OrdinalIgnoreCase))
            {
                return localizedNone.Substring(4).Trim();
            }

            return localizedNone;
        }

        private static string GetPickupShortcutDisplayName(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.LeftShift:
                    return GetLocalizedFallback("gui.pickups.key.left_shift", "Left Shift", "左 Shift");
                case KeyCode.RightShift:
                    return GetLocalizedFallback("gui.pickups.key.right_shift", "Right Shift", "右 Shift");
                case KeyCode.LeftControl:
                    return GetLocalizedFallback("gui.pickups.key.left_control", "Left Ctrl", "左 Ctrl");
                case KeyCode.RightControl:
                    return GetLocalizedFallback("gui.pickups.key.right_control", "Right Ctrl", "右 Ctrl");
                case KeyCode.LeftAlt:
                    return GetLocalizedFallback("gui.pickups.key.left_alt", "Left Alt", "左 Alt");
                case KeyCode.RightAlt:
                    return GetLocalizedFallback("gui.pickups.key.right_alt", "Right Alt", "右 Alt");
                case KeyCode.Comma:
                    return GetLocalizedFallback("gui.pickups.key.comma", ",", ",");
                case KeyCode.Period:
                    return GetLocalizedFallback("gui.pickups.key.period", ".", ".");
                case KeyCode.Slash:
                    return GetLocalizedFallback("gui.pickups.key.slash", "/", "/");
                case KeyCode.LeftBracket:
                    return GetLocalizedFallback("gui.pickups.key.left_bracket", "[", "[");
                case KeyCode.RightBracket:
                    return GetLocalizedFallback("gui.pickups.key.right_bracket", "]", "]");
                case KeyCode.Semicolon:
                    return GetLocalizedFallback("gui.pickups.key.semicolon", ";", ";");
                case KeyCode.Quote:
                    return GetLocalizedFallback("gui.pickups.key.quote", "'", "'");
                case KeyCode.None:
                    return GetLocalizedFallback("gui.pickups.button.shortcut_none", "None", "无");
                default:
                    return keyCode.ToString();
            }
        }

        private bool IsReservedPickupShortcutKey(KeyCode keyCode)
        {
            return IsReservedPickupShortcutKey(keyCode, string.Empty);
        }

        private bool IsReservedPickupShortcutKey(KeyCode keyCode, string targetId)
        {
            return keyCode == KeyCode.None ||
                keyCode == GetToggleKey() ||
                (keyCode == GetRoomEnemyRewindKey() && !string.Equals(targetId, RoomEnemyRewindShortcutId, StringComparison.Ordinal)) ||
                keyCode == KeyCode.LeftArrow ||
                keyCode == KeyCode.RightArrow ||
                keyCode == KeyCode.UpArrow ||
                keyCode == KeyCode.DownArrow ||
                keyCode == KeyCode.Insert ||
                keyCode == KeyCode.Delete;
        }

        private void PersistKeyboardShortcuts()
        {
            if (_keyboardShortcutConfigSetter != null)
            {
                _keyboardShortcutConfigSetter(_keyboardShortcutRegistry.Serialize());
            }

        }

        private PickupActionRowDefinition[] BuildCurrencyShortcutRows()
        {
            string clearLabel = GuiText.Get("gui.pickups.button.shortcut_clear");
            GUIStyle shortcutStyle = _buttonStyle;
            return new[]
            {
                CreateCurrencyShortcutRow(GameUiAtlasSpriteHealthPickup, GetLocalizedFallback("gui.command.currency.label.max_health", "Max HP (+1)", "血量上限（+1）"), CurrencyShortcutMaxHealthId, "max_health", shortcutStyle, clearLabel),
                CreateCurrencyShortcutRow(GameUiAtlasSpriteArmorPickup, GetLocalizedFallback("gui.command.currency.label.armor", "Armor (+1)", "护甲（+1）"), CurrencyShortcutArmorId, "armor", shortcutStyle, clearLabel),
                CreateCurrencyShortcutRow(GameUiAtlasSpriteBlankPickup, GetLocalizedFallback("gui.command.currency.label.blank", "Blank (+1)", "空响弹（+1）"), CurrencyShortcutBlankId, "blank", shortcutStyle, clearLabel),
                CreateCurrencyShortcutRow(GameUiAtlasSpriteCasingsPickup, GetLocalizedFallback("gui.command.currency.label.casings", "Casings (+100)", "弹壳（+100）"), CurrencyShortcutCasingsId, "casings", shortcutStyle, clearLabel),
                CreateCurrencyShortcutRow(GameUiAtlasSpriteHegemonyPickup, GetLocalizedFallback("gui.command.currency.label.hegemony", "Hegemony", "霸权币"), CurrencyShortcutHegemonyId, "hegemony", shortcutStyle, clearLabel),
                CreateCurrencyShortcutRow(GameUiAtlasSpriteKeyPickup, GetLocalizedFallback("gui.command.currency.label.key", "Key (+1)", "钥匙（+1）"), CurrencyShortcutKeyId, "key", shortcutStyle, clearLabel),
                CreateCurrencyShortcutRow(GameUiAtlasSpriteRatRewardKeyPickup, GetLocalizedFallback("gui.command.currency.label.rat_key", "Rat Key (+1)", "老鼠钥匙（+1）"), CurrencyShortcutRatKeyId, "rat_key", shortcutStyle, clearLabel),
                CreateCurrencyShortcutRow(PlayerDebugCommandService.FindCellKeyPickupId(), GetLocalizedFallback("gui.command.currency.label.cell_key", "Cell Key (+1)", "牢房钥匙（+1）"), CurrencyShortcutCellKeyId, "cell_key", shortcutStyle, clearLabel),
            };
        }

        private PickupActionRowDefinition CreateCurrencyShortcutRow(string spriteName, string label, string actionId, string actionName, GUIStyle shortcutStyle, string clearLabel)
        {
            return new PickupActionRowDefinition(
                spriteName,
                label,
                new[]
                {
                    new PickupActionButtonDefinition(
                        "currency.shortcut." + actionName,
                        GetCurrencyShortcutButtonLabel(actionId),
                        delegate { BeginPickupShortcutCapture(actionId); },
                        shortcutStyle),
                    new PickupActionButtonDefinition(
                        "currency.shortcut.clear." + actionName,
                        clearLabel,
                        delegate { ClearPickupShortcut(actionId); },
                        _buttonStyle),
                });
        }

        private PickupActionRowDefinition CreateCurrencyShortcutRow(int pickupId, string label, string actionId, string actionName, GUIStyle shortcutStyle, string clearLabel)
        {
            PickupActionRowDefinition row = CreateCurrencyShortcutRow(string.Empty, label, actionId, actionName, shortcutStyle, clearLabel);
            return new PickupActionRowDefinition(pickupId, row.Label, row.Actions);
        }

        private string GetCurrencyShortcutButtonLabel(string actionId)
        {
            if (_isCapturingPickupShortcut && string.Equals(_pickupShortcutCaptureTargetId, actionId, StringComparison.Ordinal))
            {
                return GetLocalizedFallback("gui.pickups.button.shortcut_waiting", "Press key...", "请按键…");
            }

            KeyCode keyCode;
            string keyName = _keyboardShortcutRegistry.TryGetKey(actionId, out keyCode)
                ? GetPickupShortcutDisplayName(keyCode)
                : GetPickupShortcutNoneDisplayName();
            return GetLocalizedFormattedFallback("gui.pickups.button.shortcut", "Key: {0}", "键：{0}", keyName);
        }

        private PickupActionRowDefinition[] BuildPlayerPickupShortcutRows()
        {
            string clearLabel = GuiText.Get("gui.pickups.button.shortcut_clear");
            return new[]
            {
                CreatePlayerPickupShortcutRow(GameUiAtlasSpriteHealthPickup, GetLocalizedFallback("gui.command.label.health", "Health", "血量"), CurrencyShortcutMaxHealthId, clearLabel),
                CreatePlayerPickupShortcutRow(GameUiAtlasSpriteArmorPickup, GetLocalizedFallback("gui.command.label.armor", "Armor", "护甲"), CurrencyShortcutArmorId, clearLabel),
                CreatePlayerPickupShortcutRow(GameUiAtlasSpriteBlankPickup, GetLocalizedFallback("gui.command.label.blank", "Blank", "空响弹"), CurrencyShortcutBlankId, clearLabel),
                CreatePlayerPickupShortcutRow(GameUiAtlasSpriteCasingsPickup, GetLocalizedFallback("gui.command.label.casings", "Casings", "弹壳"), CurrencyShortcutCasingsId, clearLabel),
                CreatePlayerPickupShortcutRow(GameUiAtlasSpriteHegemonyPickup, GetLocalizedFallback("gui.command.currency.label.hegemony", "Hegemony", "霸权币"), CurrencyShortcutHegemonyId, clearLabel),
                CreatePlayerPickupShortcutRow(GameUiAtlasSpriteKeyPickup, GetLocalizedFallback("gui.command.label.key", "Key", "钥匙"), CurrencyShortcutKeyId, clearLabel),
                CreatePlayerPickupShortcutRow(GameUiAtlasSpriteRatRewardKeyPickup, GetLocalizedFallback("gui.command.label.rat_key", "Rat Key", "老鼠钥匙"), CurrencyShortcutRatKeyId, clearLabel),
                CreatePlayerPickupShortcutRow(PlayerDebugCommandService.FindCellKeyPickupId(), GetLocalizedFallback("gui.command.label.cell_key", "Cell Key", "牢房钥匙"), CurrencyShortcutCellKeyId, clearLabel),
            };
        }

        private PickupActionRowDefinition CreatePlayerPickupShortcutRow(string spriteName, string label, string targetId, string clearLabel)
        {
            return new PickupActionRowDefinition(
                spriteName,
                label,
                new[]
                {
                    new PickupActionButtonDefinition(
                        "player.pickups.shortcut." + targetId,
                        GetPickupShortcutButtonLabel(targetId),
                        delegate { BeginPickupShortcutCapture(targetId); },
                        _buttonStyle),
                    new PickupActionButtonDefinition(
                        "player.pickups.shortcut.clear." + targetId,
                        clearLabel,
                        delegate { ClearPickupShortcut(targetId); },
                        _buttonStyle),
                });
        }

        private PickupActionRowDefinition CreatePlayerPickupShortcutRow(int pickupId, string label, string targetId, string clearLabel)
        {
            PickupActionRowDefinition row = CreatePlayerPickupShortcutRow(string.Empty, label, targetId, clearLabel);
            return new PickupActionRowDefinition(pickupId, row.Label, row.Actions);
        }


        private void ExecuteCurrencyShortcut(string actionId)
        {
            PlayerController player = GetCurrentPlayer();
            switch (actionId)
            {
                case CurrencyShortcutMaxHealthId:
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddMaxHealth(targetPlayer, null); });
                    return;
                case CurrencyShortcutArmorId:
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddArmor(targetPlayer, null); });
                    return;
                case CurrencyShortcutBlankId:
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddBlank(targetPlayer, null); });
                    return;
                case CurrencyShortcutKeyId:
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddKey(targetPlayer, null); });
                    return;
                case CurrencyShortcutRatKeyId:
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddRatKey(targetPlayer, null); });
                    return;
                case CurrencyShortcutCasingsId:
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddCurrency(targetPlayer, null); });
                    return;
                case CurrencyShortcutHegemonyId:
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddMetaCurrency(targetPlayer, null); });
                    return;
                case CurrencyShortcutCellKeyId:
                    ExecuteForSelectedPickupTargets(player, delegate (PlayerController targetPlayer) { ExecuteAddCellKey(targetPlayer, null); });
                    return;
                default:
                    return;
            }
        }
    }
}
