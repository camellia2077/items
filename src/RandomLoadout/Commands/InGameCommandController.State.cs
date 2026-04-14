using System;
using System.Collections.Generic;
using RandomLoadout.Core;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private enum PanelPage
        {
            Command,
            Pickups,
            Characters,
            Currency,
        }

        private enum CharacterActionMode
        {
            SwitchOnly,
            Unlock,
        }

        private enum PickupBrowserFilter
        {
            All,
            Gun,
            Passive,
            Active,
        }

        private const KeyCode ToggleKey = KeyCode.F7;
        private const string InputControlName = "RandomLoadoutCommandInput";
        private const string PickupSearchControlName = "RandomLoadoutPickupSearch";
        private const float StatusDurationSeconds = 4f;
        private const float PanelWidth = 612f;
        private const float BasePanelHeight = 200f;
        private const float PickupBrowserPanelHeight = 428f;
        private const float CharacterPanelBaseHeaderHeight = 126f;
        private const float CharacterPanelFooterHeight = 26f;
        private const float CurrencyPanelHeight = 208f;
        private const float PanelBottomMargin = 92f;
        private const float StatusMaxWidth = 560f;
        private const float StatusMinHeight = 40f;
        private const float StatusGap = 14f;
        private const float ButtonWidth = 92f;
        private const float ButtonGap = 8f;
        private const float CurrencyMenuButtonWidth = 108f;
        private const float PickupMenuButtonWidth = 108f;
        private const float CurrencyActionButtonWidth = 180f;
        private const float CharacterButtonWidth = 108f;
        private const int CharacterButtonsPerRow = 5;
        private const float CharacterModeButtonWidth = 180f;
        private const float CharacterPageRefreshIntervalSeconds = 0.2f;
        private const float PickupFilterButtonWidth = 88f;
        private const float PickupRowHeight = 48f;
        private const float PickupIconSize = 32f;
        private const float PickupGrantButtonWidth = 72f;

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
        private static readonly PickupBrowserEntry[] EmptyPickupBrowserEntries = new PickupBrowserEntry[0];

        private readonly GrantCommandParser _parser = new GrantCommandParser();
        private readonly GrantCommandService _commandService;
        private readonly PlayerDebugCommandService _playerDebugCommandService;
        private readonly FoyerCharacterSwitchService _foyerCharacterSwitchService;
        private readonly RapidFireToggleService _rapidFireToggleService;
        private readonly Func<EtgPickupCatalogEntry[]> _pickupCatalogProvider;
        private readonly Func<PickupAliasRegistry> _aliasRegistryProvider;

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _wrappedHintStyle;
        private GUIStyle _textFieldStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _statusSuccessStyle;
        private GUIStyle _statusErrorStyle;
        private GUIStyle _pickupRowStyle;
        private GUIStyle _pickupRowButtonStyle;
        private GUIStyle _pickupPrimaryTextStyle;
        private GUIStyle _pickupSecondaryTextStyle;
        private GUIStyle _pickupFilterButtonStyle;
        private GUIStyle _pickupFilterActiveButtonStyle;
        private GUIStyle _pickupIconFallbackStyle;

        private bool _isVisible;
        private bool _focusInputField;
        private bool _focusPickupSearchField;
        private PanelPage _currentPage;
        private string _lastGuiLanguageCode = string.Empty;
        private string _inputText = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _statusIsError;
        private float _statusExpiresAt;
        private string _lastCharacterAvailabilityLog = string.Empty;
        private CharacterActionMode _characterActionMode = CharacterActionMode.SwitchOnly;
        private FoyerCharacterOption[] _cachedCharacterOptions = EmptyCharacterOptions;
        private string _cachedCharacterAvailability = string.Empty;
        private float _nextCharacterPageRefreshAt;
        private PickupBrowserEntry[] _cachedPickupEntries = EmptyPickupBrowserEntries;
        private PickupBrowserFilter _pickupBrowserFilter = PickupBrowserFilter.All;
        private string _pickupSearchText = string.Empty;
        private Vector2 _pickupScrollPosition = Vector2.zero;
        private readonly Dictionary<int, PickupIconData> _pickupIconCache = new Dictionary<int, PickupIconData>();
    }
}
