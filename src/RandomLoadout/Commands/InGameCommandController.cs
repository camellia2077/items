using BepInEx.Logging;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed class InGameCommandController
    {
        private const KeyCode ToggleKey = KeyCode.F7;
        private const string InputControlName = "RandomLoadoutCommandInput";
        private const float StatusDurationSeconds = 4f;
        private const float PanelWidth = 500f;
        private const float PanelHeight = 158f;
        private const float PanelBottomMargin = 92f;
        private const float StatusWidth = 440f;
        private const float StatusHeight = 40f;
        private const float StatusGap = 14f;
        private const float ButtonWidth = 92f;
        private const float ButtonGap = 8f;

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

        private readonly GrantCommandParser _parser = new GrantCommandParser();
        private readonly GrantCommandService _commandService;

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _statusSuccessStyle;
        private GUIStyle _statusErrorStyle;

        private bool _isVisible;
        private bool _focusInputField;
        private string _inputText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _statusIsError;
        private float _statusExpiresAt;

        public InGameCommandController(GrantCommandService commandService)
        {
            _commandService = commandService;
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
            DrawStatusOverlay();
            if (!_isVisible)
            {
                return;
            }

            Rect panelRect = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                Screen.height - PanelBottomMargin - PanelHeight,
                PanelWidth,
                PanelHeight);

            GUI.Box(panelRect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(panelRect.x + 14f, panelRect.y + 12f, panelRect.width - 28f, 24f), "RandomLoadout Command", _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                "Use: gun/passive/active/item <name>",
                _hintStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 58f, panelRect.width - 28f, 20f),
                "Press F7 to open or close the command panel.",
                _hintStyle);

            GUI.SetNextControlName(InputControlName);
            float textFieldWidth = panelRect.width - 42f - (ButtonWidth * 2f) - ButtonGap;
            const float controlHeight = 34f;
            Rect textFieldRect = new Rect(panelRect.x + 14f, panelRect.y + 86f, textFieldWidth, controlHeight);
            Rect grantButtonRect = new Rect(textFieldRect.xMax + 12f, textFieldRect.y, ButtonWidth, controlHeight);
            Rect randomButtonRect = new Rect(grantButtonRect.xMax + ButtonGap, textFieldRect.y, ButtonWidth, controlHeight);
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

            if (shouldSubmit)
            {
                Submit(player, logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 128f, panelRect.width - 28f, 20f),
                "Enter to grant, press F7 again to close.",
                _hintStyle);
        }

        private void Toggle()
        {
            _isVisible = !_isVisible;
            _focusInputField = _isVisible;
            if (!_isVisible)
            {
                _inputText = string.Empty;
            }
        }

        private void Close()
        {
            _isVisible = false;
            _inputText = string.Empty;
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

        private void ShowStatus(string message, bool isError)
        {
            _statusMessage = message;
            _statusIsError = isError;
            _statusExpiresAt = Time.unscaledTime + StatusDurationSeconds;
        }

        private void DrawStatusOverlay()
        {
            if (string.IsNullOrEmpty(_statusMessage) || Time.unscaledTime > _statusExpiresAt)
            {
                return;
            }

            Rect panelRect = new Rect(
                (Screen.width - PanelWidth) * 0.5f,
                Screen.height - PanelBottomMargin - PanelHeight,
                PanelWidth,
                PanelHeight);
            Rect statusRect = new Rect(
                (Screen.width - StatusWidth) * 0.5f,
                panelRect.y - StatusGap - StatusHeight,
                StatusWidth,
                StatusHeight);

            GUI.Box(statusRect, _statusMessage, _statusIsError ? _statusErrorStyle : _statusSuccessStyle);
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
