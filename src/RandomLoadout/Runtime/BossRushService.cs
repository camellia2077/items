using System;
using System.Reflection;
using BepInEx.Logging;
using Dungeonator;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class BossRushService
    {
        private const string CharacterSelectSceneName = "tt_foyer";
        private const string LegacyCharacterSelectSceneName = "tt_breach";
        private const float ReturnToCharacterSelectDelaySeconds = 0.5f;
        private const int MaxTeleportFrames = 180;
        private const int RequiredReadyFrames = 20;
        private const float ManualTeleportPrepSeconds = 0.05f;
        private const string BossTriggerZonesFieldName = "bossTriggerZones";
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BossRushEncounter[] Encounters =
        {
            new BossRushEncounter("keep", "tt_castle"),
            new BossRushEncounter("proper", "tt5"),
            new BossRushEncounter("mines", "tt_mines"),
            new BossRushEncounter("hollow", "tt_catacombs"),
            new BossRushEncounter("forge", "tt_forge"),
            new BossRushEncounter("hell", "tt_bullethell"),
        };

        private readonly ManualLogSource _logger;
        private BossRushState _state;
        private int _currentEncounterIndex;
        private RoomHandler _currentBossRoom;
        private Coroutine _activeCoroutine;
        private bool _hasClaimedRewardThisEncounter;
        private bool _hasAttemptedPlayerBootstrapThisFloor;
        private bool _hasStartedSession;
        private GameObject _selectedPlayerPrefab;
        private string _selectedCharacterLabel;

        public BossRushService(ManualLogSource logger)
        {
            _logger = logger;
            _state = BossRushState.Idle;
            _currentEncounterIndex = -1;
            Instance = this;
        }

        public static BossRushService Instance { get; private set; }

        public event Action<GrantCommandExecutionResult> StatusRaised;

        public BossRushState State
        {
            get { return _state; }
        }

        public bool IsActive
        {
            get { return _state != BossRushState.Idle; }
        }

        public bool ShouldSuppressAutomaticLoadout
        {
            get { return IsActive; }
        }

        public GrantCommandExecutionResult Start()
        {
            if (IsActive)
            {
                return GrantCommandExecutionResult.Localized(false, "result.boss_rush.start.already_active");
            }

            if (!IsInCharacterSelectHub())
            {
                return GrantCommandExecutionResult.Localized(false, "result.boss_rush.start.breach_only");
            }

            _currentEncounterIndex = 0;
            _currentBossRoom = null;
            _hasClaimedRewardThisEncounter = false;
            _hasAttemptedPlayerBootstrapThisFloor = false;
            CaptureSelectedCharacterProfile();
            BeginBossRushSession();
            _state = BossRushState.Starting;
            StopActiveCoroutine();
            LoadEncounter(GetCurrentEncounter());
            return GrantCommandExecutionResult.Localized(true, "result.boss_rush.start.success");
        }

        public GrantCommandExecutionResult ReturnToCharacterSelect()
        {
            if (!IsActive)
            {
                return GrantCommandExecutionResult.Localized(false, "result.boss_rush.abort.idle");
            }

            BeginReturnToCharacterSelect();
            return GrantCommandExecutionResult.Localized(true, "result.boss_rush.abort.success");
        }

        public string GetCurrentFloorLabel()
        {
            BossRushEncounter encounter = GetCurrentEncounter();
            return encounter != null
                ? GuiText.Get("label.boss_rush.floor." + encounter.FloorKey)
                : GuiText.Get("label.boss_rush.floor.none");
        }

        public string GetStateLabel()
        {
            return GuiText.Get("label.boss_rush.state." + GetStateToken(_state));
        }

        public string GetAvailabilityMessage()
        {
            if (IsActive)
            {
                return GuiText.Get("gui.boss_rush.status.active", GetCurrentFloorLabel(), GetStateLabel());
            }

            return IsInCharacterSelectHub()
                ? GuiText.Get("gui.boss_rush.status.ready")
                : GuiText.Get("gui.boss_rush.unavailable.breach_only");
        }

        internal void NotifyLevelLoaded()
        {
            if (!IsActive)
            {
                return;
            }

            string sceneName = GetCurrentSceneName();
            LogInfo("Level load notification received. Scene=" + sceneName + ", State=" + _state + ".");
            if (TryResetAfterCharacterSelectHubReturn(sceneName, "level load notification"))
            {
                return;
            }

            if (TryAdvanceEncounterForObservedScene(sceneName, "level load notification"))
            {
                LogInfo("Advanced Boss Rush progression after natural floor transition. Scene=" + sceneName + ".");
            }

            BossRushEncounter encounter = GetCurrentEncounter();
            if (encounter == null || !string.Equals(sceneName, encounter.SceneName, StringComparison.Ordinal))
            {
                return;
            }

            _state = BossRushState.TeleportingToBoss;
            StartActiveCoroutine(PrepareFloorAndTeleportToBossRoom_CR());
        }

        internal void NotifySceneObserved(string sceneName, bool sceneChanged)
        {
            if (!IsActive)
            {
                return;
            }

            string normalizedSceneName = sceneName ?? string.Empty;
            if (TryResetAfterCharacterSelectHubReturn(normalizedSceneName, sceneChanged ? "scene change" : "poll"))
            {
                return;
            }

            BossRushEncounter encounter = GetCurrentEncounter();
            if (encounter == null || !string.Equals(normalizedSceneName, encounter.SceneName, StringComparison.Ordinal))
            {
                if (TryAdvanceEncounterForObservedScene(normalizedSceneName, sceneChanged ? "scene change" : "poll"))
                {
                    LogInfo(
                        "Observed next Boss Rush floor via " +
                        (sceneChanged ? "scene change" : "poll") +
                        ". Scene=" +
                        normalizedSceneName +
                        ", State=" +
                        _state +
                        ".");
                }

                encounter = GetCurrentEncounter();
                if (encounter == null || !string.Equals(normalizedSceneName, encounter.SceneName, StringComparison.Ordinal))
                {
                    return;
                }
            }

            if (_state == BossRushState.LoadingFloor || _state == BossRushState.Starting)
            {
                LogInfo(
                    "Observed active Boss Rush scene via " +
                    (sceneChanged ? "scene change" : "poll") +
                    ". Scene=" +
                    normalizedSceneName +
                    ", State=" +
                    _state +
                    ". Starting floor preparation fallback.");
                _state = BossRushState.TeleportingToBoss;
                StartActiveCoroutine(PrepareFloorAndTeleportToBossRoom_CR());
            }
        }

        internal void NotifyBossRewardSpawned(RoomHandler bossRoom)
        {
            if (!IsActive)
            {
                return;
            }

            if (_state != BossRushState.InEncounter &&
                _state != BossRushState.TeleportingToBoss &&
                _state != BossRushState.LoadingFloor)
            {
                return;
            }

            _currentBossRoom = bossRoom ?? _currentBossRoom;
            _hasClaimedRewardThisEncounter = false;
            _state = BossRushState.AwaitingRewardClaim;
            LogInfo("Boss clear reward spawned for " + GetCurrentFloorLabel() + ".");
        }

        internal void NotifyRewardClaimed(PlayerController player)
        {
            if (!IsActive || _state != BossRushState.AwaitingRewardClaim || _hasClaimedRewardThisEncounter)
            {
                return;
            }

            if ((object)player == null)
            {
                return;
            }

            if ((object)_currentBossRoom != null && (object)player.CurrentRoom != (object)_currentBossRoom)
            {
                return;
            }

            _hasClaimedRewardThisEncounter = true;
            _state = BossRushState.Transitioning;
            LogInfo(
                "Boss reward claimed for " +
                GetCurrentFloorLabel() +
                ". Waiting for the player to take the vanilla exit elevator instead of forcing a custom floor load.");

            // Let the player use the vanilla post-boss elevator so the base game keeps
            // ownership of reward-room cleanup, camera flow, and floor transition timing.
            // Boss Rush resumes when we observe the next configured floor scene load.
            StopActiveCoroutine();
        }

        internal bool TryHandleGameOver(string deathSource)
        {
            if (!IsActive)
            {
                return false;
            }

            LogWarning("Intercepted game over during Boss Rush. Source=" + (deathSource ?? string.Empty));
            RaiseStatus(GrantCommandExecutionResult.Localized(false, "result.boss_rush.failed"));
            BeginReturnToCharacterSelect();
            return true;
        }

        internal bool TryHandlePauseMenuExitRequest()
        {
            if (!IsActive)
            {
                return false;
            }

            LogInfo("Intercepted pause-menu exit request during Boss Rush. Returning to character select instead.");
            RaiseStatus(GrantCommandExecutionResult.Localized(true, "result.boss_rush.abort.success"));
            BeginReturnToCharacterSelect();
            return true;
        }

        public void Dispose()
        {
            StopActiveCoroutine();
            Reset();
            if ((object)Instance == (object)this)
            {
                Instance = null;
            }
        }

        private bool TryResetAfterCharacterSelectHubReturn(string sceneName, string source)
        {
            if (!IsInCharacterSelectHubState(sceneName))
            {
                return false;
            }

            if (_state == BossRushState.ReturningToCharacterSelect)
            {
                LogInfo(
                    "Observed character select hub via " +
                    source +
                    ". Scene=" +
                    (sceneName ?? string.Empty) +
                    ", IsFoyer=" +
                    ((object)GameManager.Instance != null && GameManager.Instance.IsFoyer) +
                    ". Resetting Boss Rush state.");
                Reset();
                return true;
            }

            if (_state != BossRushState.Idle)
            {
                LogWarning(
                    "Boss Rush is still active while already in the character select hub. Source=" +
                    source +
                    ", Scene=" +
                    (sceneName ?? string.Empty) +
                    ", State=" +
                    _state +
                    ". Forcing reset.");
                Reset();
                return true;
            }

            return false;
        }
    }
}
