using BepInEx.Logging;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class InGameCommandController
    {
        private void DrawCommandPage(Rect panelRect, PlayerController player, ManualLogSource logger)
        {
            Rect currencyMenuButtonRect = new Rect(panelRect.x + panelRect.width - CurrencyMenuButtonWidth - 14f, panelRect.y + 12f, CurrencyMenuButtonWidth, 30f);
            Rect pickupMenuButtonRect = new Rect(currencyMenuButtonRect.x - ButtonGap - PickupMenuButtonWidth, panelRect.y + 12f, PickupMenuButtonWidth, 30f);
            if (GUI.Button(pickupMenuButtonRect, "Pickups", _buttonStyle))
            {
                OpenPickupPage(logger);
            }

            if (GUI.Button(currencyMenuButtonRect, "Currency", _buttonStyle))
            {
                OpenCurrencyPage(logger);
            }

            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 12f, pickupMenuButtonRect.x - panelRect.x - 28f, 24f),
                "RandomLoadout Command",
                _titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 14f, panelRect.y + 40f, panelRect.width - 28f, 20f),
                "Use: <name, alias, or id> or gun/passive/active/item <value>",
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

            string rapidButtonLabel = _rapidFireToggleService != null && _rapidFireToggleService.IsEnabledFor(player) ? "Rapid ON" : "Rapid OFF";
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
    }
}
