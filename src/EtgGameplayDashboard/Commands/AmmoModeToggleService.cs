// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

namespace EtgGameplayDashboard
{
    internal enum AmmoMode
    {
        Off,
        NoConsume,
        InfiniteReserve,
    }

    internal sealed class AmmoModeToggleService
    {
        private readonly System.Action<AmmoMode> _persistMode;
        private AmmoMode _mode;
        private readonly TrackedGunState _primaryGunState = new TrackedGunState();
        private readonly TrackedGunState _secondaryGunState = new TrackedGunState();

        public AmmoModeToggleService(AmmoMode initiallyEnabledMode, System.Action<AmmoMode> persistMode)
        {
            _mode = initiallyEnabledMode;
            _persistMode = persistMode;
        }

        public AmmoMode Mode
        {
            get { return _mode; }
        }

        public GrantCommandExecutionResult Toggle()
        {
            if (_mode == AmmoMode.Off)
            {
                _mode = AmmoMode.InfiniteReserve;
                PersistMode();
                return GrantCommandExecutionResult.Localized(true, "result.ammo_mode.infinite_reserve.success");
            }

            if (_mode == AmmoMode.InfiniteReserve)
            {
                _mode = AmmoMode.NoConsume;
                PersistMode();
                return GrantCommandExecutionResult.Localized(true, "result.ammo_mode.no_consume.success");
            }

            _mode = AmmoMode.Off;
            ClearTrackedState();
            PersistMode();
            return GrantCommandExecutionResult.Localized(true, "result.ammo_mode.off.success");
        }

        private void PersistMode()
        {
            if (_persistMode != null)
            {
                _persistMode(_mode);
            }
        }

        public void Update(PlayerController player)
        {
            if (_mode == AmmoMode.Off || (object)player == null)
            {
                return;
            }

            UpdateTrackedGun(_primaryGunState, player.CurrentGun);

            // A dual-wield synergy fires CurrentSecondaryGun independently. It has its own
            // reserve and clip state, so it must be locked separately from CurrentGun.
            Gun secondaryGun = player.inventory != null && player.inventory.DualWielding
                ? player.CurrentSecondaryGun
                : null;
            UpdateTrackedGun(_secondaryGunState, secondaryGun);
        }

        public void Reset()
        {
            _mode = AmmoMode.Off;
            ClearTrackedState();
        }

        private void UpdateTrackedGun(TrackedGunState state, Gun gun)
        {
            if (!IsGunUsable(gun) || gun.InfiniteAmmo)
            {
                ClearTrackedGunState(state);
                return;
            }

            if (!ReferenceEquals(state.Gun, gun))
            {
                ClearTrackedGunState(state);
                state.Gun = gun;
                state.Ammo = gun.CurrentAmmo;
                state.ClipShotsRemaining = gun.ClipShotsRemaining;
            }
            else
            {
                RestoreTrackedGunState(state);
            }

            if (_mode == AmmoMode.NoConsume)
            {
                SyncLockedAmmoBehaviour(state);
            }
            else
            {
                RemoveLockedAmmoBehaviour(gun);
            }
        }

        private void ClearTrackedState()
        {
            ClearTrackedGunState(_primaryGunState);
            ClearTrackedGunState(_secondaryGunState);
        }

        private void RestoreTrackedGunState(TrackedGunState state)
        {
            Gun gun = state.Gun;
            if (state.Ammo >= 0 && gun.CurrentAmmo < state.Ammo)
            {
                gun.CurrentAmmo = state.Ammo;
            }
            else if (gun.CurrentAmmo > state.Ammo)
            {
                state.Ammo = gun.CurrentAmmo;
            }

            if (_mode != AmmoMode.NoConsume)
            {
                return;
            }

            if (state.ClipShotsRemaining >= 0 && gun.ClipShotsRemaining < state.ClipShotsRemaining)
            {
                gun.ClipShotsRemaining = state.ClipShotsRemaining;
            }
            else if (gun.ClipShotsRemaining > state.ClipShotsRemaining)
            {
                state.ClipShotsRemaining = gun.ClipShotsRemaining;
            }
        }

        private void SyncLockedAmmoBehaviour(TrackedGunState state)
        {
            Gun gun = state.Gun;
            if (!IsGunUsable(gun))
            {
                return;
            }

            LockedAmmoComponent behaviour;
            if (!TryGetLockedAmmoBehaviour(gun, out behaviour))
            {
                return;
            }

            if ((object)behaviour == null)
            {
                if (!IsUnityObjectAlive(gun.gameObject))
                {
                    return;
                }

                behaviour = gun.gameObject.AddComponent<LockedAmmoComponent>();
            }

            behaviour.SetLockedState(state.Ammo, state.ClipShotsRemaining);
        }

        private static void ClearTrackedGunState(TrackedGunState state)
        {
            RemoveLockedAmmoBehaviour(state.Gun);
            state.Gun = null;
            state.Ammo = 0;
            state.ClipShotsRemaining = 0;
        }

        private static void RemoveLockedAmmoBehaviour(Gun gun)
        {
            if (!IsGunUsable(gun))
            {
                return;
            }

            LockedAmmoComponent behaviour;
            if (!TryGetLockedAmmoBehaviour(gun, out behaviour))
            {
                return;
            }

            if ((object)behaviour != null)
            {
                UnityEngine.Object.Destroy(behaviour);
            }
        }

        private static bool TryGetLockedAmmoBehaviour(Gun gun, out LockedAmmoComponent behaviour)
        {
            behaviour = null;
            if (!IsGunUsable(gun))
            {
                return false;
            }

            try
            {
                behaviour = gun.GetComponent<LockedAmmoComponent>();
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static bool IsGunUsable(Gun gun)
        {
            return IsUnityObjectAlive(gun) && IsUnityObjectAlive(gun.gameObject);
        }

        private static bool IsUnityObjectAlive(UnityEngine.Object unityObject)
        {
            return (object)unityObject != null && unityObject != null;
        }

        private sealed class TrackedGunState
        {
            public Gun Gun;
            public int Ammo;
            public int ClipShotsRemaining;
        }
    }
}
