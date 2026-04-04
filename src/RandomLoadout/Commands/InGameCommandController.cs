using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class InGameCommandController
    {
        private enum PanelPage
        {
            Command,
            Characters,
        }

        private enum CharacterActionMode
        {
            SwitchOnly,
            Unlock,
        }

        private const KeyCode ToggleKey = KeyCode.F7;
        private const string InputControlName = "RandomLoadoutCommandInput";
        private const float StatusDurationSeconds = 4f;
        private const float PanelWidth = 612f;
        private const float BasePanelHeight = 200f;
        private const float CharacterPanelBaseHeaderHeight = 126f;
        private const float CharacterPanelFooterHeight = 26f;
        private const float PanelBottomMargin = 92f;
        private const float StatusMaxWidth = 560f;
        private const float StatusMinHeight = 40f;
        private const float StatusGap = 14f;
        private const float ButtonWidth = 92f;
        private const float ButtonGap = 8f;
        private const float CharacterButtonWidth = 108f;
        private const int CharacterButtonsPerRow = 5;
        private const float CharacterModeButtonWidth = 180f;
        private const float CharacterPageRefreshIntervalSeconds = 0.2f;

        private static readonly Color PanelBackgroundColor = new Color(0.07f, 0.08f, 0.10f, 0.88f);
        private static readonly Color PanelBorderColor = new Color(0.69f, 0.54f, 0.28f, 0.96f);
        private static readonly Color InputBackgroundColor = new Color(0.11f, 0.12f, 0.15f, 0.96f);
        private static readonly Color ButtonBackgroundColor = new Color(0.19f, 0.16f, 0.11f, 0.96f);
        private static readonly Color ButtonHoverColor = new Color(0.27f, 0.22f, 0.15f, 0.98f);
        private static readonly Color ButtonActiveColor = new Color(0.34f, 0.27f, 0.17f, 1f);
        private static readonly Color PrimaryTextColor = new Color(0.90f, 0.87f, 0.79f, 1f);
        private static readonly Color SecondaryTextColor = new Color(0.65f, 0.62f, 0.54f, 1f);
        private static readonly Color SuccessBackgroundColor = new Color(0.23f, 0.31f, 0.22f, 0.95f);
        private static readonly Color ErrorBackgroundColor = new Color(0.44f, 0.24f, 0.21f, 0.95f);
        private static readonly FoyerCharacterOption[] EmptyCharacterOptions = new FoyerCharacterOption[0];

        private readonly GrantCommandParser _parser = new GrantCommandParser();
        private readonly GrantCommandService _commandService;
        private readonly PlayerDebugCommandService _playerDebugCommandService;
        private readonly FoyerCharacterSwitchService _foyerCharacterSwitchService;
        private readonly RapidFireToggleService _rapidFireToggleService;

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _wrappedHintStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _statusSuccessStyle;
        private GUIStyle _statusErrorStyle;

        private bool _isVisible;
        private bool _focusInputField;
        private PanelPage _currentPage;
        private string _inputText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _statusIsError;
        private float _statusExpiresAt;
        private string _lastCharacterAvailabilityLog = string.Empty;
        private CharacterActionMode _characterActionMode = CharacterActionMode.SwitchOnly;
        private FoyerCharacterOption[] _cachedCharacterOptions = EmptyCharacterOptions;
        private string _cachedCharacterAvailability = "Character switching is only available in the Breach.";
        private float _nextCharacterPageRefreshAt;

        public InGameCommandController(
            GrantCommandService commandService,
            PlayerDebugCommandService playerDebugCommandService,
            FoyerCharacterSwitchService foyerCharacterSwitchService,
            RapidFireToggleService rapidFireToggleService)
        {
            _commandService = commandService;
            _playerDebugCommandService = playerDebugCommandService;
            _foyerCharacterSwitchService = foyerCharacterSwitchService;
            _rapidFireToggleService = rapidFireToggleService;
        }

        public void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                Toggle();
            }

            if (!_isVisible)
            {
                return;
            }
        }

        public void OnGUI(PlayerController player, ManualLogSource logger)
        {
            EnsureStyles();
            FoyerCharacterOption[] characterOptions = EmptyCharacterOptions;
            string characterAvailability = _cachedCharacterAvailability;
            float panelHeight = BasePanelHeight;
            if (_isVisible && _currentPage == PanelPage.Characters)
            {
                RefreshCharacterPageData(false);
                characterOptions = _cachedCharacterOptions;
                characterAvailability = _cachedCharacterAvailability;
                panelHeight = GetPanelHeight(characterOptions, characterAvailability);
            }

            DrawStatusOverlay(panelHeight);
            if (!_isVisible)
            {
                return;
            }

            Rect panelRect = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                Screen.height - PanelBottomMargin - panelHeight,
                PanelWidth,
                panelHeight);

            GUI.Box(panelRect, GUIContent.none, _panelStyle);
            if (_currentPage == PanelPage.Characters)
            {
                DrawCharacterPage(panelRect, characterOptions, characterAvailability, logger);
                return;
            }

            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - 28f, 24f), "RandomLoadout Command", _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                "Use: gun/passive/active/item <name, alias, or id>",
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                "Press F7 to open or close the command panel.",
                _hintStyle);

            GUI.SetNextControlName(InputControlName);
            float textFieldWidth = panelRect.width - 54f - (ButtonWidth * 4f) - (ButtonGap * 3f);
            const float controlHeight = 34f;
            Rect textFieldRect = new Rect(panelRect.x + 14f, panelRect.y + 86f, textFieldWidth, controlHeight);
            Rect grantButtonRect = new Rect(textFieldRect.xMax + 12f, textFieldRect.y, ButtonWidth, controlHeight);
            Rect randomButtonRect = new Rect(grantButtonRect.xMax + ButtonGap, textFieldRect.y, ButtonWidth, controlHeight);
            Rect charactersButtonRect = new Rect(randomButtonRect.xMax + ButtonGap, textFieldRect.y, ButtonWidth, controlHeight);
            Rect rapidButtonRect = new Rect(charactersButtonRect.xMax + ButtonGap, textFieldRect.y, ButtonWidth, controlHeight);
            Rect debugLabelRect = new Rect(panelRect.x + 14f, panelRect.y + 128f, panelRect.width - 28f, 20f);
            Rect healButtonRect = new Rect(panelRect.x + 14f, panelRect.y + 150f, ButtonWidth, controlHeight);
            Rect armorButtonRect = new Rect(healButtonRect.xMax + ButtonGap, healButtonRect.y, ButtonWidth, controlHeight);
            Rect fullHealButtonRect = new Rect(armorButtonRect.xMax + ButtonGap, healButtonRect.y, ButtonWidth, controlHeight);
            Rect clearCurseButtonRect = new Rect(fullHealButtonRect.xMax + ButtonGap, healButtonRect.y, ButtonWidth, controlHeight);
            Rect blanksButtonRect = new Rect(clearCurseButtonRect.xMax + ButtonGap, healButtonRect.y, ButtonWidth, controlHeight);
            Rect ammoButtonRect = new Rect(blanksButtonRect.xMax + ButtonGap, healButtonRect.y, ButtonWidth, controlHeight);
            _inputText = GUI.TextField(textFieldRect, _inputText, 256, _textFieldStyle);

            if (_focusInputField)
            {
                GUI.FocusControl(InputControlName);
                _focusInputField = false;
            }

            bool shouldSubmit = false;
            Event currentEvent = Event.current;
            if (currentEvent != null &&
                currentEvent.type == EventType.KeyDown &&
                (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
            {
                shouldSubmit = true;
                currentEvent.Use();
            }

            if (GUI.Button(grantButtonRect, "Grant", _buttonStyle))
            {
                shouldSubmit = true;
            }

            if (GUI.Button(randomButtonRect, "Random", _buttonStyle))
            {
                ExecuteRandom(player, logger);
            }

            if (GUI.Button(charactersButtonRect, "Characters", _buttonStyle))
            {
                OpenCharacterPage(logger);
            }

            string rapidButtonLabel = _rapidFireToggleService != null && _rapidFireToggleService.IsEnabled ? "Rapid ON" : "Rapid OFF";
            if (GUI.Button(rapidButtonRect, rapidButtonLabel, _buttonStyle))
            {
                ExecuteToggleRapidFire(player, logger);
            }

            if (GUI.Button(healButtonRect, "+0.5 HP", _buttonStyle))
            {
                ExecuteHealHalfHeart(player, logger);
            }

            if (GUI.Button(armorButtonRect, "+1 Armor", _buttonStyle))
            {
                ExecuteAddArmor(player, logger);
            }

            GUI.Label(debugLabelRect, "Debug actions", _hintStyle);

            if (GUI.Button(fullHealButtonRect, "Full Heal", _buttonStyle))
            {
                ExecuteFullHeal(player, logger);
            }

            if (GUI.Button(clearCurseButtonRect, "Clear Curse", _buttonStyle))
            {
                ExecuteClearCurse(player, logger);
            }

            if (GUI.Button(blanksButtonRect, "Refill Blanks", _buttonStyle))
            {
                ExecuteRefillBlanks(player, logger);
            }

            if (GUI.Button(ammoButtonRect, "Full Ammo", _buttonStyle))
            {
                ExecuteRefillCurrentGunAmmo(player, logger);
            }

            if (shouldSubmit)
            {
                Submit(player, logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 176f, panelRect.width - 28f, 20f),
                "Enter to grant, press F7 again to close.",
                _hintStyle);
        }

        private void Toggle()
        {
            _isVisible = !_isVisible;
            _focusInputField = _isVisible;
            if (!_isVisible)
            {
                _currentPage = PanelPage.Command;
                _inputText = string.Empty;
                ResetCharacterPageCache();
            }
        }

        private void Close()
        {
            _isVisible = false;
            _currentPage = PanelPage.Command;
            _inputText = string.Empty;
            ResetCharacterPageCache();
        }

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

        private void DrawCharacterPage(Rect panelRect, FoyerCharacterOption[] characterOptions, string availabilityMessage, ManualLogSource logger)
        {
            Rect backButtonRect = new Rect(panelRect.x + panelRect.width - ButtonWidth - 14f, panelRect.y + 12f, ButtonWidth, 30f);
            Rect modeButtonRect = new Rect(backButtonRect.x - ButtonGap - CharacterModeButtonWidth, panelRect.y + 12f, CharacterModeButtonWidth, 30f);
            if (GUI.Button(modeButtonRect, GetCharacterModeButtonLabel(), _buttonStyle))
            {
                ToggleCharacterActionMode(logger);
            }

            if (GUI.Button(backButtonRect, "Back", _buttonStyle))
            {
                _currentPage = PanelPage.Command;
                _focusInputField = true;
                ResetCharacterPageCache();
                return;
            }

            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - CharacterModeButtonWidth - ButtonWidth - 32f, 24f), "Breach Characters", _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                "Select a character and apply the selected mode.",
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
                "Select a character",
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
                string buttonLabel = option.IsSelected ? option.Label + " *" : option.Label;
                if (option.IsLocked && option.CanUnlock)
                {
                    buttonLabel = option.Label + " ?";
                }
                if (option.IsPending)
                {
                    buttonLabel = option.Label + " ...";
                }

                if (GUI.Button(buttonRect, buttonLabel, _buttonStyle))
                {
                    ExecuteSwitchCharacter(option, logger);
                }

                GUI.enabled = wasEnabled;
            }
        }

        private void OpenCharacterPage(ManualLogSource logger)
        {
            _currentPage = PanelPage.Characters;
            _focusInputField = false;
            RefreshCharacterPageData(true);

            if (logger == null)
            {
                return;
            }

            if (!string.Equals(_lastCharacterAvailabilityLog, _cachedCharacterAvailability, System.StringComparison.Ordinal))
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

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.normal.background = MakeBorderedTexture(PanelBackgroundColor, PanelBorderColor);
            _panelStyle.border = new RectOffset(2, 2, 2, 2);
            _panelStyle.padding = new RectOffset(12, 12, 12, 12);

            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.normal.textColor = PrimaryTextColor;
            _titleStyle.fontSize = 18;
            _titleStyle.fontStyle = FontStyle.Bold;

            _hintStyle = new GUIStyle(GUI.skin.label);
            _hintStyle.normal.textColor = SecondaryTextColor;
            _hintStyle.fontSize = 14;

            _wrappedHintStyle = new GUIStyle(_hintStyle);
            _wrappedHintStyle.wordWrap = true;

            _textFieldStyle = new GUIStyle(GUI.skin.textField);
            _textFieldStyle.normal.background = MakeTexture(1, 1, InputBackgroundColor);
            _textFieldStyle.focused.background = MakeTexture(1, 1, InputBackgroundColor);
            _textFieldStyle.normal.textColor = PrimaryTextColor;
            _textFieldStyle.focused.textColor = PrimaryTextColor;
            _textFieldStyle.border = new RectOffset(2, 2, 2, 2);
            _textFieldStyle.padding = new RectOffset(10, 10, 7, 7);
            _textFieldStyle.alignment = TextAnchor.MiddleLeft;
            _textFieldStyle.fontSize = 15;

            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.normal.background = MakeTexture(1, 1, ButtonBackgroundColor);
            _buttonStyle.hover.background = MakeTexture(1, 1, ButtonHoverColor);
            _buttonStyle.active.background = MakeTexture(1, 1, ButtonActiveColor);
            _buttonStyle.normal.textColor = PrimaryTextColor;
            _buttonStyle.hover.textColor = PrimaryTextColor;
            _buttonStyle.active.textColor = PrimaryTextColor;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.fontSize = 14;

            _statusStyle = new GUIStyle(GUI.skin.box);
            _statusStyle.normal.textColor = PrimaryTextColor;
            _statusStyle.alignment = TextAnchor.MiddleCenter;
            _statusStyle.fontSize = 14;
            _statusStyle.padding = new RectOffset(10, 10, 6, 6);
            _statusStyle.wordWrap = true;

            _statusSuccessStyle = new GUIStyle(_statusStyle);
            _statusSuccessStyle.normal.background = MakeTexture(1, 1, SuccessBackgroundColor);

            _statusErrorStyle = new GUIStyle(_statusStyle);
            _statusErrorStyle.normal.background = MakeTexture(1, 1, ErrorBackgroundColor);
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.hideFlags = HideFlags.HideAndDontSave;
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeBorderedTexture(Color fillColor, Color borderColor)
        {
            Texture2D texture = new Texture2D(8, 8);
            texture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[64];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    bool isBorderPixel = x == 0 || x == 7 || y == 0 || y == 7;
                    pixels[(y * 8) + x] = isBorderPixel ? borderColor : fillColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
