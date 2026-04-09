using System.Collections.Generic;

namespace RandomLoadout
{
    internal sealed class RapidFireToggleService
    {
        private sealed class ModuleOverrideState
        {
            public ModuleOverrideState(ProjectileModule.ShootStyle originalShootStyle, Gun ownerGun)
            {
                OriginalShootStyle = originalShootStyle;
                OwnerGun = ownerGun;
            }

            public ProjectileModule.ShootStyle OriginalShootStyle { get; private set; }

            public Gun OwnerGun { get; private set; }
        }

        private readonly Dictionary<ProjectileModule, ModuleOverrideState> _moduleOverrides = new Dictionary<ProjectileModule, ModuleOverrideState>();
        private readonly HashSet<Gun> _enabledGuns = new HashSet<Gun>();

        public GrantCommandExecutionResult Toggle(PlayerController player)
        {
            Gun currentGun = GetCurrentGun(player);
            if ((object)currentGun == null)
            {
                return new GrantCommandExecutionResult(false, "No current gun is equipped.");
            }

            return IsEnabledFor(currentGun) ? Disable(currentGun) : Enable(currentGun);
        }

        public bool IsEnabledFor(PlayerController player)
        {
            Gun currentGun = GetCurrentGun(player);
            return (object)currentGun != null && IsEnabledFor(currentGun);
        }

        public void Update(PlayerController player)
        {
            Gun currentGun = GetCurrentGun(player);
            if ((object)currentGun == null || !IsEnabledFor(currentGun))
            {
                return;
            }

            ApplyToGun(currentGun);
        }

        public void Reset()
        {
            RestoreOverrides();
            _enabledGuns.Clear();
        }

        private GrantCommandExecutionResult Enable(Gun gun)
        {
            if ((object)gun == null)
            {
                return new GrantCommandExecutionResult(false, "No current gun is equipped.");
            }

            _enabledGuns.Add(gun);
            int convertedModuleCount = ApplyToGun(gun);
            if (convertedModuleCount > 0)
            {
                return new GrantCommandExecutionResult(
                    true,
                    "Hold-rapid enabled for current gun. Converted " + convertedModuleCount + " semi-auto module(s) to automatic.");
            }

            return new GrantCommandExecutionResult(
                true,
                "Hold-rapid enabled for current gun. No semi-auto modules were available.");
        }

        private GrantCommandExecutionResult Disable(Gun gun)
        {
            if ((object)gun == null)
            {
                return new GrantCommandExecutionResult(false, "No current gun is equipped.");
            }

            int restoredModuleCount = RestoreOverridesForGun(gun);
            _enabledGuns.Remove(gun);
            if (restoredModuleCount > 0)
            {
                return new GrantCommandExecutionResult(
                    true,
                    "Hold-rapid disabled for current gun. Restored " + restoredModuleCount + " module(s) to original shoot styles.");
            }

            return new GrantCommandExecutionResult(true, "Hold-rapid disabled for current gun.");
        }

        private int ApplyToGun(Gun gun)
        {
            if ((object)gun == null)
            {
                return 0;
            }

            int convertedModuleCount = 0;
            if (ApplyToGunModules(gun, ref convertedModuleCount))
            {
                gun.RegenerateCache();
            }

            return convertedModuleCount;
        }

        private bool ApplyToGunModules(Gun gun, ref int convertedModuleCount)
        {
            bool changedGun = false;
            List<ProjectileModule> modules = CollectModules(gun);
            for (int i = 0; i < modules.Count; i++)
            {
                ProjectileModule module = modules[i];
                if ((object)module == null || _moduleOverrides.ContainsKey(module))
                {
                    continue;
                }

                if (module.shootStyle != ProjectileModule.ShootStyle.SemiAutomatic)
                {
                    continue;
                }

                // Keep rapid-fire opt-in on a per-gun basis. This avoids forcing every carried
                // ammo-using weapon into a high fire rate, which can empty magazines or total
                // ammo reserves much faster than the player expects.
                _moduleOverrides.Add(module, new ModuleOverrideState(module.shootStyle, gun));
                module.shootStyle = ProjectileModule.ShootStyle.Automatic;
                convertedModuleCount++;
                changedGun = true;
            }

            return changedGun;
        }

        private int RestoreOverrides()
        {
            if (_moduleOverrides.Count == 0)
            {
                return 0;
            }

            int restoredModuleCount = 0;
            HashSet<Gun> gunsToRegenerate = new HashSet<Gun>();
            foreach (KeyValuePair<ProjectileModule, ModuleOverrideState> pair in _moduleOverrides)
            {
                ProjectileModule module = pair.Key;
                ModuleOverrideState overrideState = pair.Value;
                if ((object)module != null)
                {
                    module.shootStyle = overrideState.OriginalShootStyle;
                    restoredModuleCount++;
                }

                if ((object)overrideState.OwnerGun != null)
                {
                    gunsToRegenerate.Add(overrideState.OwnerGun);
                }
            }

            foreach (Gun gun in gunsToRegenerate)
            {
                if ((object)gun != null)
                {
                    gun.RegenerateCache();
                }
            }

            _moduleOverrides.Clear();
            return restoredModuleCount;
        }

        private int RestoreOverridesForGun(Gun gun)
        {
            if (_moduleOverrides.Count == 0 || (object)gun == null)
            {
                return 0;
            }

            int restoredModuleCount = 0;
            List<ProjectileModule> modulesToRemove = new List<ProjectileModule>();
            foreach (KeyValuePair<ProjectileModule, ModuleOverrideState> pair in _moduleOverrides)
            {
                ModuleOverrideState overrideState = pair.Value;
                if (!ReferenceEquals(overrideState.OwnerGun, gun))
                {
                    continue;
                }

                ProjectileModule module = pair.Key;
                if ((object)module != null)
                {
                    module.shootStyle = overrideState.OriginalShootStyle;
                    restoredModuleCount++;
                }

                modulesToRemove.Add(module);
            }

            for (int i = 0; i < modulesToRemove.Count; i++)
            {
                _moduleOverrides.Remove(modulesToRemove[i]);
            }

            if (restoredModuleCount > 0)
            {
                gun.RegenerateCache();
            }

            return restoredModuleCount;
        }

        private bool IsEnabledFor(Gun gun)
        {
            return (object)gun != null && _enabledGuns.Contains(gun);
        }

        private static Gun GetCurrentGun(PlayerController player)
        {
            return (object)player != null ? player.CurrentGun : null;
        }

        private static List<ProjectileModule> CollectModules(Gun gun)
        {
            List<ProjectileModule> modules = new List<ProjectileModule>();
            if ((object)gun == null)
            {
                return modules;
            }

            AddModule(modules, gun.DefaultModule);
            AddVolleyModules(modules, gun.Volley);
            AddVolleyModules(modules, gun.RawSourceVolley);
            AddVolleyModules(modules, gun.OptionalReloadVolley);
            return modules;
        }

        private static void AddVolleyModules(List<ProjectileModule> modules, ProjectileVolleyData volley)
        {
            if ((object)volley == null || volley.projectiles == null)
            {
                return;
            }

            for (int i = 0; i < volley.projectiles.Count; i++)
            {
                AddModule(modules, volley.projectiles[i]);
            }
        }

        private static void AddModule(List<ProjectileModule> modules, ProjectileModule module)
        {
            if ((object)module == null || modules.Contains(module))
            {
                return;
            }

            modules.Add(module);
        }
    }
}
