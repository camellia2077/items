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

        public bool IsEnabled { get; private set; }

        public GrantCommandExecutionResult Toggle(PlayerController player)
        {
            return IsEnabled ? Disable() : Enable(player);
        }

        public void Update(PlayerController player)
        {
            if (!IsEnabled || (object)player == null)
            {
                return;
            }

            ApplyToPlayerGuns(player);
        }

        public void Reset()
        {
            RestoreOverrides();
            IsEnabled = false;
        }

        private GrantCommandExecutionResult Enable(PlayerController player)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            IsEnabled = true;
            int convertedModuleCount = ApplyToPlayerGuns(player);
            if (convertedModuleCount > 0)
            {
                return new GrantCommandExecutionResult(
                    true,
                    "Hold-rapid enabled. Converted " + convertedModuleCount + " semi-auto module(s) to automatic while enabled.");
            }

            return new GrantCommandExecutionResult(
                true,
                "Hold-rapid enabled. No semi-auto modules were available in current guns.");
        }

        private GrantCommandExecutionResult Disable()
        {
            int restoredModuleCount = RestoreOverrides();
            IsEnabled = false;
            if (restoredModuleCount > 0)
            {
                return new GrantCommandExecutionResult(
                    true,
                    "Hold-rapid disabled. Restored " + restoredModuleCount + " module(s) to original shoot styles.");
            }

            return new GrantCommandExecutionResult(true, "Hold-rapid disabled.");
        }

        private int ApplyToPlayerGuns(PlayerController player)
        {
            GunInventory inventory = player.inventory;
            if ((object)inventory == null || inventory.AllGuns == null)
            {
                return 0;
            }

            int convertedModuleCount = 0;
            for (int i = 0; i < inventory.AllGuns.Count; i++)
            {
                Gun gun = inventory.AllGuns[i];
                if ((object)gun == null)
                {
                    continue;
                }

                if (ApplyToGun(gun, ref convertedModuleCount))
                {
                    gun.RegenerateCache();
                }
            }

            return convertedModuleCount;
        }

        private bool ApplyToGun(Gun gun, ref int convertedModuleCount)
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
