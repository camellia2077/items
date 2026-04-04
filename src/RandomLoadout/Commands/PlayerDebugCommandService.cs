namespace RandomLoadout
{
    internal sealed class PlayerDebugCommandService
    {
        private const float HalfHeartAmount = 0.5f;
        private const float SingleArmorAmount = 1f;

        public GrantCommandExecutionResult FullHeal(PlayerController player)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return new GrantCommandExecutionResult(false, "The player's health component was not ready.");
            }

            healthHaver.FullHeal();
            return new GrantCommandExecutionResult(true, "Restored the player to full health.");
        }

        public GrantCommandExecutionResult HealHalfHeart(PlayerController player)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return new GrantCommandExecutionResult(false, "The player's health component was not ready.");
            }

            float maxHealth = healthHaver.GetMaxHealth();
            if (maxHealth <= 0f)
            {
                return new GrantCommandExecutionResult(false, "This character does not use heart health.");
            }

            float currentHealth = healthHaver.GetCurrentHealth();
            float missingHealth = maxHealth - currentHealth;
            if (missingHealth <= 0f)
            {
                return new GrantCommandExecutionResult(false, "The player is already at full health.");
            }

            float healAmount = missingHealth < HalfHeartAmount ? missingHealth : HalfHeartAmount;
            healthHaver.ApplyHealing(healAmount);
            return new GrantCommandExecutionResult(true, "Recovered " + healAmount.ToString("0.0") + " heart.");
        }

        public GrantCommandExecutionResult AddArmor(PlayerController player)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            HealthHaver healthHaver = player.healthHaver;
            if ((object)healthHaver == null)
            {
                return new GrantCommandExecutionResult(false, "The player's health component was not ready.");
            }

            float nextArmor = healthHaver.Armor + SingleArmorAmount;
            healthHaver.Armor = nextArmor;
            return new GrantCommandExecutionResult(true, "Added 1 armor.");
        }

        public GrantCommandExecutionResult ClearCurse(PlayerController player)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            PlayerStats stats = player.stats;
            if ((object)stats == null)
            {
                return new GrantCommandExecutionResult(false, "The player's stats component was not ready.");
            }

            float totalCurse = stats.GetStatValue(PlayerStats.StatType.Curse);
            if (totalCurse <= 0f)
            {
                return new GrantCommandExecutionResult(false, "The player has no curse to clear.");
            }

            float currentBaseCurse = stats.GetBaseStatValue(PlayerStats.StatType.Curse);
            stats.SetBaseStatValue(PlayerStats.StatType.Curse, currentBaseCurse - totalCurse, player);
            stats.RecalculateStats(player, true, false);

            float remainingCurse = stats.GetStatValue(PlayerStats.StatType.Curse);
            if (remainingCurse > 0f)
            {
                return new GrantCommandExecutionResult(true, "Reduced curse, but " + remainingCurse.ToString("0.##") + " curse remains.");
            }

            return new GrantCommandExecutionResult(true, "Cleared the player's curse.");
        }

        public GrantCommandExecutionResult RefillBlanks(PlayerController player)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            PlayerStats stats = player.stats;
            if ((object)stats == null)
            {
                return new GrantCommandExecutionResult(false, "The player's stats component was not ready.");
            }

            int targetBlankCount = stats.NumBlanksPerFloor;
            if (targetBlankCount <= 0)
            {
                return new GrantCommandExecutionResult(false, "This character does not have a blank allotment to refill.");
            }

            if (player.Blanks >= targetBlankCount)
            {
                return new GrantCommandExecutionResult(false, "The player's blanks are already full.");
            }

            player.Blanks = targetBlankCount;
            return new GrantCommandExecutionResult(true, "Refilled blanks to " + targetBlankCount + ".");
        }

        public GrantCommandExecutionResult RefillCurrentGunAmmo(PlayerController player)
        {
            if ((object)player == null)
            {
                return new GrantCommandExecutionResult(false, "The player is not ready yet.");
            }

            Gun currentGun = player.CurrentGun;
            if ((object)currentGun == null)
            {
                return new GrantCommandExecutionResult(false, "No equipped gun was available.");
            }

            if (currentGun.InfiniteAmmo)
            {
                return new GrantCommandExecutionResult(false, "The equipped gun already has infinite ammo.");
            }

            int targetAmmo = currentGun.AdjustedMaxAmmo;
            if (targetAmmo <= 0)
            {
                return new GrantCommandExecutionResult(false, "The equipped gun does not support ammo refills.");
            }

            bool changedAmmo = currentGun.CurrentAmmo < targetAmmo;
            bool changedClip = currentGun.ClipShotsRemaining < currentGun.ClipCapacity;
            if (!changedAmmo && !changedClip)
            {
                return new GrantCommandExecutionResult(false, "The equipped gun is already full.");
            }

            currentGun.CurrentAmmo = targetAmmo;
            if (currentGun.ClipCapacity > 0)
            {
                currentGun.ClipShotsRemaining = currentGun.ClipCapacity;
                currentGun.ForceImmediateReload(false);
            }

            string gunLabel = !string.IsNullOrEmpty(currentGun.DisplayName) ? currentGun.DisplayName : currentGun.name;
            return new GrantCommandExecutionResult(true, "Refilled ammo for " + gunLabel + ".");
        }
    }
}
