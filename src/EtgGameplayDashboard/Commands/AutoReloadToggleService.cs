// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using System.Collections.Generic;

namespace EtgGameplayDashboard
{
    internal enum AutoReloadMode
    {
        Off,
        Instant,
        Animated,
    }

    internal sealed class AutoReloadToggleService
    {
        private readonly HashSet<Gun> _reloadRequestedGuns = new HashSet<Gun>();
        private readonly Action<AutoReloadMode> _persistMode;
        private AutoReloadMode _mode;

        public AutoReloadToggleService(AutoReloadMode initiallyEnabledMode, Action<AutoReloadMode> persistMode)
        {
            _mode = initiallyEnabledMode;
            _persistMode = persistMode;
        }

        public GrantCommandExecutionResult Toggle()
        {
            if (_mode == AutoReloadMode.Off)
            {
                _mode = AutoReloadMode.Instant;
                PersistMode();
                return GrantCommandExecutionResult.Localized(true, "result.auto_reload.instant.success");
            }

            if (_mode == AutoReloadMode.Instant)
            {
                _mode = AutoReloadMode.Animated;
                _reloadRequestedGuns.Clear();
                PersistMode();
                return GrantCommandExecutionResult.Localized(true, "result.auto_reload.animated.success");
            }

            _mode = AutoReloadMode.Off;
            _reloadRequestedGuns.Clear();
            PersistMode();
            return GrantCommandExecutionResult.Localized(true, "result.auto_reload.off.success");
        }

        public AutoReloadMode Mode
        {
            get { return _mode; }
        }

        public void Update(PlayerController player)
        {
            if (_mode == AutoReloadMode.Off)
            {
                return;
            }

            Gun currentGun = GetCurrentGun(player);
            if ((object)currentGun == null)
            {
                return;
            }

            Gun secondaryGun = GetCurrentSecondaryGun(player);
            if ((object)secondaryGun != null)
            {
                UpdateDualWieldingGuns(currentGun, secondaryGun);
                return;
            }

            UpdateSingleGun(currentGun);
        }

        private void UpdateSingleGun(Gun currentGun)
        {
            if (currentGun.ClipShotsRemaining > 0)
            {
                _reloadRequestedGuns.Remove(currentGun);
                return;
            }

            if (!ShouldReload(currentGun))
            {
                return;
            }

            if (_reloadRequestedGuns.Contains(currentGun))
            {
                if (currentGun.IsReloading)
                {
                    return;
                }

                _reloadRequestedGuns.Remove(currentGun);
            }

            if (_mode == AutoReloadMode.Instant)
            {
                currentGun.ForceImmediateReload(false);
                _reloadRequestedGuns.Add(currentGun);
                return;
            }

            if (_mode == AutoReloadMode.Animated && !currentGun.IsReloading && currentGun.Reload())
            {
                _reloadRequestedGuns.Add(currentGun);
            }
        }

        private void UpdateDualWieldingGuns(Gun currentGun, Gun secondaryGun)
        {
            bool currentGunEmpty = currentGun.ClipShotsRemaining <= 0;
            bool secondaryGunEmpty = secondaryGun.ClipShotsRemaining <= 0;
            if (!currentGunEmpty && !secondaryGunEmpty)
            {
                _reloadRequestedGuns.Remove(currentGun);
                _reloadRequestedGuns.Remove(secondaryGun);
                return;
            }

            if (!CanReloadEmptyGun(currentGun, currentGunEmpty) && !CanReloadEmptyGun(secondaryGun, secondaryGunEmpty))
            {
                return;
            }

            if (_reloadRequestedGuns.Contains(currentGun) || _reloadRequestedGuns.Contains(secondaryGun))
            {
                if (currentGun.IsReloading || secondaryGun.IsReloading)
                {
                    return;
                }

                _reloadRequestedGuns.Remove(currentGun);
                _reloadRequestedGuns.Remove(secondaryGun);
            }

            // Vanilla PlayerController handles one Reload input as one operation: it calls Reload
            // for CurrentGun and then CurrentSecondaryGun. Do the same here whenever either dual-
            // wield clip is empty. Reloading only the empty slot would not match manual reload's
            // ammo result, and would incorrectly leave the two guns in separate reload states.
            if (_mode == AutoReloadMode.Instant)
            {
                currentGun.ForceImmediateReload(false);
                secondaryGun.ForceImmediateReload(false);
                MarkReloadRequested(currentGun, secondaryGun);
                return;
            }

            if (_mode == AutoReloadMode.Animated)
            {
                bool currentGunReloadStarted = currentGun.Reload();
                bool secondaryGunReloadStarted = secondaryGun.Reload();
                if (currentGunReloadStarted || secondaryGunReloadStarted)
                {
                    MarkReloadRequested(currentGun, secondaryGun);
                }
            }
        }

        public void Reset()
        {
            _mode = AutoReloadMode.Off;
            _reloadRequestedGuns.Clear();
        }

        public void Disable()
        {
            _mode = AutoReloadMode.Off;
            _reloadRequestedGuns.Clear();
        }

        private void PersistMode()
        {
            if (_persistMode != null)
            {
                _persistMode(_mode);
            }
        }

        private static bool ShouldReload(Gun gun)
        {
            if ((object)gun == null || gun.ClipCapacity <= 0)
            {
                return false;
            }

            return gun.InfiniteAmmo || gun.CurrentAmmo > 0;
        }

        private static bool CanReloadEmptyGun(Gun gun, bool isEmpty)
        {
            return isEmpty && ShouldReload(gun);
        }

        private void MarkReloadRequested(Gun currentGun, Gun secondaryGun)
        {
            _reloadRequestedGuns.Add(currentGun);
            _reloadRequestedGuns.Add(secondaryGun);
        }

        private static Gun GetCurrentGun(PlayerController player)
        {
            return (object)player != null ? player.CurrentGun : null;
        }

        private static Gun GetCurrentSecondaryGun(PlayerController player)
        {
            return (object)player != null && player.inventory != null && player.inventory.DualWielding
                ? player.CurrentSecondaryGun
                : null;
        }
    }
}
