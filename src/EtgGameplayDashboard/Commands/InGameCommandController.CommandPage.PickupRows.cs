// Copyright (C) 2026 camellia2077
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU GPLv3 or later.

using System;
using UnityEngine;

namespace EtgGameplayDashboard
{
    internal sealed partial class InGameCommandController
    {
        private struct PickupActionButtonDefinition
        {
            public PickupActionButtonDefinition(string controlId, string label, Action onClick, GUIStyle style)
            {
                ControlId = controlId ?? string.Empty;
                Label = label ?? string.Empty;
                OnClick = onClick;
                Style = style;
            }

            public readonly string ControlId;
            public readonly string Label;
            public readonly Action OnClick;
            public readonly GUIStyle Style;
        }

        private struct PickupActionRowDefinition
        {
            public PickupActionRowDefinition(string spriteName, string label, PickupActionButtonDefinition[] actions)
            {
                SpriteName = spriteName ?? string.Empty;
                PickupId = -1;
                Label = label ?? string.Empty;
                Actions = actions ?? EmptyPickupActionButtons;
            }

            public PickupActionRowDefinition(int pickupId, string label, PickupActionButtonDefinition[] actions)
            {
                SpriteName = string.Empty;
                PickupId = pickupId;
                Label = label ?? string.Empty;
                Actions = actions ?? EmptyPickupActionButtons;
            }

            public readonly string SpriteName;
            public readonly int PickupId;
            public readonly string Label;
            public readonly PickupActionButtonDefinition[] Actions;
        }

        private static readonly PickupActionButtonDefinition[] EmptyPickupActionButtons = new PickupActionButtonDefinition[0];

        private void DrawPickupActionRows(Rect contentRect, float top, float rowHeight, float rowGap, PickupActionRowDefinition[] rows)
        {
            if (rows == null)
            {
                return;
            }

            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                Rect rowRect = new Rect(contentRect.x, top + ((rowHeight + rowGap) * rowIndex), contentRect.width, rowHeight);
                DrawPickupActionRow(rowRect, rows[rowIndex]);
            }
        }

        private void DrawPickupActionRow(Rect rowRect, PickupActionRowDefinition row)
        {
            GUI.Box(rowRect, GUIContent.none, _pickupRowStyle);

            const float iconSize = 30f;
            const float rowPadding = 8f;
            // Reserve enough room for the longest current label, but do not hold the unused
            // portion of the old fixed label column on short labels such as "Casings".
            // That recovered space is needed by the longer English action labels.
            float labelWidth = Mathf.Max(
                100f,
                _pickupPrimaryTextStyle.CalcSize(new GUIContent(row.Label)).x + 10f);
            Rect iconRect = new Rect(rowRect.x + rowPadding, rowRect.y + ((rowRect.height - iconSize) * 0.5f), iconSize, iconSize);
                DrawPickupActionIcon(iconRect, row.SpriteName, row.PickupId);

            GUI.Label(
                new Rect(iconRect.xMax + 10f, rowRect.y + 5f, labelWidth, rowRect.height - 10f),
                row.Label,
                _pickupPrimaryTextStyle);

            PickupActionButtonDefinition[] actions = row.Actions ?? EmptyPickupActionButtons;
            if (actions.Length == 0)
            {
                return;
            }

            float actionAreaRight = rowRect.x + rowRect.width - rowPadding;
            float actionAreaLeft = iconRect.xMax + 10f + labelWidth + 4f;
            float actionGap = 8f;
            float actionAreaWidth = actionAreaRight - actionAreaLeft;
            float availableActionWidth = actionAreaWidth - (actionGap * (actions.Length - 1));
            float[] actionButtonWidths = GetPickupActionButtonWidths(actions, availableActionWidth);
            float actionButtonHeight = rowRect.height - 8f;
            float actionX = actionAreaLeft;
            for (int actionIndex = 0; actionIndex < actions.Length; actionIndex++)
            {
                PickupActionButtonDefinition action = actions[actionIndex];
                Rect actionButtonRect = new Rect(
                    actionX,
                    rowRect.y + 4f,
                    actionButtonWidths[actionIndex],
                    actionButtonHeight);
                GUIStyle actionStyle = action.Style ?? _buttonStyle;
                if (!DrawControllerButton(actionButtonRect, action.ControlId, action.Label, actionStyle))
                {
                    actionX += actionButtonWidths[actionIndex] + actionGap;
                    continue;
                }

                if (action.OnClick != null)
                {
                    action.OnClick();
                }

                actionX += actionButtonWidths[actionIndex] + actionGap;
            }
        }

        private float[] GetPickupActionButtonWidths(PickupActionButtonDefinition[] actions, float availableActionWidth)
        {
            const float minimumButtonWidth = 76f;
            const float horizontalTextPadding = 20f;
            float[] widths = new float[actions.Length];
            float requestedWidth = 0f;
            for (int actionIndex = 0; actionIndex < actions.Length; actionIndex++)
            {
                PickupActionButtonDefinition action = actions[actionIndex];
                GUIStyle actionStyle = action.Style ?? _buttonStyle;
                float textWidth = actionStyle.CalcSize(new GUIContent(action.Label)).x;
                widths[actionIndex] = Mathf.Max(minimumButtonWidth, textWidth + horizontalTextPadding);
                requestedWidth += widths[actionIndex];
            }

            float minimumRequestedWidth = minimumButtonWidth * actions.Length;
            if (availableActionWidth <= minimumRequestedWidth)
            {
                float equalWidth = availableActionWidth / actions.Length;
                for (int actionIndex = 0; actionIndex < widths.Length; actionIndex++)
                {
                    widths[actionIndex] = equalWidth;
                }

                return widths;
            }

            // English "Consume: OFF" needs more width than its neighboring +100, Clear, and
            // Spawn buttons. Preserve the per-label proportions even when the preferred total
            // is wider than the available area; falling back to equal widths clips its opening
            // letters, which is exactly what happened in the four-button Casings row.
            float availableExtraWidth = availableActionWidth - minimumRequestedWidth;
            float requestedExtraWidth = requestedWidth - minimumRequestedWidth;
            float widthScale = requestedExtraWidth > 0f
                ? Mathf.Min(1f, availableExtraWidth / requestedExtraWidth)
                : 0f;
            for (int actionIndex = 0; actionIndex < widths.Length; actionIndex++)
            {
                float requestedExtraForButton = widths[actionIndex] - minimumButtonWidth;
                widths[actionIndex] = minimumButtonWidth + (requestedExtraForButton * widthScale);
            }

            if (requestedWidth < availableActionWidth)
            {
                float extraWidthPerButton = (availableActionWidth - requestedWidth) / actions.Length;
                for (int actionIndex = 0; actionIndex < widths.Length; actionIndex++)
                {
                    widths[actionIndex] += extraWidthPerButton;
                }
            }

            return widths;
        }

        private void DrawPickupActionIcon(Rect iconRect, string spriteName, int pickupId = -1)
        {
            GUI.Box(iconRect, GUIContent.none, _pickupIconBackgroundStyle);

            PickupIconData iconData;
            if (pickupId >= 0 && TryGetPickupIcon(pickupId, out iconData))
            {
                GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                return;
            }

            if (TryGetGameUiAtlasIcon(spriteName, out iconData))
            {
                GUI.DrawTextureWithTexCoords(iconRect, iconData.Texture, iconData.TextureCoords, true);
                return;
            }

            GUI.Box(iconRect, "?", _pickupIconFallbackStyle);
        }
    }
}
