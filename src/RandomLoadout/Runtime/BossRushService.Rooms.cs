using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Dungeonator;
using UnityEngine;

namespace RandomLoadout
{
    internal sealed partial class BossRushService
    {
        private static void FindBossRushRooms(Dungeon dungeon, out RoomHandler bossRoom, out RoomHandler stagingRoom, out string scanSummary)
        {
            bossRoom = null;
            stagingRoom = null;
            scanSummary = "dungeon unavailable";
            List<RoomHandler> rooms = dungeon != null && dungeon.data != null ? dungeon.data.rooms : null;
            if (rooms != null)
            {
                int totalRooms = rooms.Count;
                stagingRoom = FindBossFoyerRoom(rooms);
                for (int index = 0; index < rooms.Count; index++)
                {
                    RoomHandler room = rooms[index];
                    if ((object)room == null || room.area == null)
                    {
                        continue;
                    }

                    if (room.area.PrototypeRoomCategory == PrototypeDungeonRoom.RoomCategory.BOSS)
                    {
                        bossRoom = room;
                        break;
                    }
                }

                if ((object)bossRoom != null)
                {
                    if ((object)stagingRoom == null)
                    {
                        stagingRoom = FindBossStagingRoom(bossRoom);
                    }

                    scanSummary = stagingRoom != null
                        ? "matched RoomCategory.BOSS in " + totalRooms + " rooms; using staging room " + GetRoomDebugLabel(stagingRoom)
                        : "matched RoomCategory.BOSS in " + totalRooms + " rooms; no staging room found";
                    return;
                }

                for (int index = 0; index < rooms.Count; index++)
                {
                    RoomHandler room = rooms[index];
                    if ((object)room == null)
                    {
                        continue;
                    }

                    if (HasBossTriggerZones(room))
                    {
                        bossRoom = room;
                        if ((object)stagingRoom == null)
                        {
                            stagingRoom = FindBossStagingRoom(bossRoom);
                        }
                        scanSummary = stagingRoom != null
                            ? "matched bossTriggerZones in " + totalRooms + " rooms; using staging room " + GetRoomDebugLabel(stagingRoom)
                            : "matched bossTriggerZones in " + totalRooms + " rooms; no staging room found";
                        return;
                    }
                }

                for (int index = 0; index < rooms.Count; index++)
                {
                    RoomHandler room = rooms[index];
                    if ((object)room == null)
                    {
                        continue;
                    }

                    string roomName = room.GetRoomName();
                    if (!string.IsNullOrEmpty(roomName) &&
                        roomName.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        bossRoom = room;
                        if ((object)stagingRoom == null)
                        {
                            stagingRoom = FindBossStagingRoom(bossRoom);
                        }
                        scanSummary = stagingRoom != null
                            ? "matched room name \"" + roomName + "\" in " + totalRooms + " rooms; using staging room " + GetRoomDebugLabel(stagingRoom)
                            : "matched room name \"" + roomName + "\" in " + totalRooms + " rooms; no staging room found";
                        return;
                    }
                }

                scanSummary = "no boss-room match across " + totalRooms + " rooms";
            }
        }

        private static RoomHandler FindBossFoyerRoom(List<RoomHandler> rooms)
        {
            if (rooms == null)
            {
                return null;
            }

            for (int index = 0; index < rooms.Count; index++)
            {
                RoomHandler room = rooms[index];
                if (IsNamedBossFoyer(room))
                {
                    return room;
                }
            }

            return null;
        }

        private static RoomHandler FindBossStagingRoom(RoomHandler bossRoom)
        {
            if ((object)bossRoom == null || bossRoom.connectedRooms == null || bossRoom.connectedRooms.Count == 0)
            {
                return null;
            }

            for (int index = 0; index < bossRoom.connectedRooms.Count; index++)
            {
                RoomHandler adjacentRoom = bossRoom.connectedRooms[index];
                if ((object)adjacentRoom == null || adjacentRoom.area == null)
                {
                    continue;
                }

                PrototypeDungeonRoom.RoomCategory category = adjacentRoom.area.PrototypeRoomCategory;
                if (category == PrototypeDungeonRoom.RoomCategory.BOSS)
                {
                    continue;
                }

                if (IsNamedBossFoyer(adjacentRoom))
                {
                    return adjacentRoom;
                }
            }

            return null;
        }

        private static bool IsFloorReadyForBossRush(PlayerController player, Dungeon dungeon, out string readinessSummary)
        {
            if ((object)player == null)
            {
                readinessSummary = "PrimaryPlayer is unavailable";
                return false;
            }

            if ((object)dungeon == null || dungeon.data == null)
            {
                readinessSummary = "Dungeon data is unavailable";
                return false;
            }

            if ((object)player.CurrentRoom == null || player.CurrentRoom.area == null)
            {
                readinessSummary = "Player current room is not ready";
                return false;
            }

            if (GameManager.IsBossIntro)
            {
                readinessSummary = "Boss intro is already active";
                return false;
            }

            PrototypeDungeonRoom.RoomCategory category = player.CurrentRoom.area.PrototypeRoomCategory;
            readinessSummary =
                "CurrentRoom=" +
                GetRoomDebugLabel(player.CurrentRoom) +
                ", Category=" +
                category +
                ", Input=" +
                DescribeInputState(player);
            return true;
        }

        private static bool HasBossTriggerZones(RoomHandler room)
        {
            object value = ReadFieldValue(room, BossTriggerZonesFieldName);
            ICollection collection = value as ICollection;
            return collection != null && collection.Count > 0;
        }

        private IEnumerator ManualTeleportToRoom_CR(PlayerController player, RoomHandler targetRoom)
        {
            if ((object)player == null || (object)targetRoom == null)
            {
                yield break;
            }

            IntVector2? targetCell = targetRoom.GetRandomAvailableCell(new IntVector2?(new IntVector2(2, 2)), new CellTypes?(CellTypes.FLOOR), false, null);
            if (!targetCell.HasValue)
            {
                targetCell = targetRoom.GetBestRewardLocation(new IntVector2(2, 2), RoomHandler.RewardLocationStyle.PlayerCenter, true);
            }

            if (!targetCell.HasValue)
            {
                LogWarning("Manual boss-room teleport could not find a valid target cell for " + GetRoomDebugLabel(targetRoom) + ".");
                yield break;
            }

            targetRoom.EnsureUpstreamLocksUnlocked();
            Vector2 targetPoint = targetCell.Value.ToCenterVector2();

            LogInfo("Manual boss-room teleport target resolved. Room=" + GetRoomDebugLabel(targetRoom) + ", Cell=" + targetCell.Value + ".");
            player.ForceStopDodgeRoll();
            player.DoVibration(Vibration.Time.Normal, Vibration.Strength.Medium);
            player.specRigidbody.Velocity = Vector2.zero;
            if ((object)player.knockbackDoer != null)
            {
                player.knockbackDoer.TriggerTemporaryKnockbackInvulnerability(1f);
            }

            if ((object)player.healthHaver != null)
            {
                player.healthHaver.IsVulnerable = false;
            }

            yield return new WaitForSeconds(ManualTeleportPrepSeconds);

            player.transform.position = targetPoint;
            player.specRigidbody.Reinitialize();
            player.specRigidbody.RecheckTriggers = true;
            player.WarpFollowersToPlayer();
            player.WarpCompanionsToPlayer(false);

            yield return null;
            if ((object)player.specRigidbody != null)
            {
                PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(player.specRigidbody, null, false);
            }

            if ((object)player.healthHaver != null)
            {
                player.healthHaver.IsVulnerable = true;
            }

            RestorePlayerInputAfterBossRushTeleport(player, targetRoom);
        }

        private static string DescribeInputState(PlayerController player)
        {
            if ((object)player == null)
            {
                return "<null>";
            }

            try
            {
                PropertyInfo property = player.GetType().GetProperty("CurrentInputState", InstanceFlags);
                if (property != null)
                {
                    object value = property.GetValue(player, null);
                    return value != null ? value.ToString() : "<null>";
                }
            }
            catch
            {
            }

            return "<unknown>";
        }

        private static object ReadFieldValue(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            FieldInfo field = target.GetType().GetField(fieldName, InstanceFlags);
            return field != null ? field.GetValue(target) : null;
        }

#pragma warning disable 0618
        private static string GetCurrentSceneName()
        {
            return Application.loadedLevelName ?? string.Empty;
        }
#pragma warning restore 0618

        private static bool IsInCharacterSelectHub()
        {
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager != null && gameManager.IsFoyer)
            {
                return true;
            }

            return IsCharacterSelectScene(GetCurrentSceneName());
        }

        private static bool IsInCharacterSelectHubState(string sceneName)
        {
            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager != null && gameManager.IsFoyer)
            {
                return true;
            }

            return IsCharacterSelectScene(sceneName);
        }

        private static bool IsCharacterSelectScene(string sceneName)
        {
            return string.Equals(sceneName, CharacterSelectSceneName, StringComparison.Ordinal) ||
                   string.Equals(sceneName, LegacyCharacterSelectSceneName, StringComparison.Ordinal);
        }

        private static string GetRoomDebugLabel(RoomHandler room)
        {
            if ((object)room == null)
            {
                return "<null>";
            }

            string roomName = room.GetRoomName();
            return !string.IsNullOrEmpty(roomName) ? roomName : "<unnamed>";
        }

        private void RestorePlayerInputAfterBossRushTeleport(PlayerController player, RoomHandler targetRoom)
        {
            if ((object)player == null)
            {
                return;
            }

            string inputBeforeRestore = DescribeInputState(player);
            bool hadInputOverride = player.IsInputOverridden;
            if (hadInputOverride)
            {
                player.ClearAllInputOverrides();
            }

            // Boss Rush reaches the floor by custom scene load + room warp instead of the
            // game's normal room-to-room traversal. In that path ETG sometimes leaves the
            // player in NoInput after the warp completes, which blocks both movement and
            // firing even though the player is already standing in the boss foyer.
            // Restore vanilla-like control ownership here so the player can walk into the
            // boss room and let the intro trigger naturally.
            if (player.CurrentInputState == PlayerInputState.NoInput)
            {
                player.CurrentInputState = PlayerInputState.AllInput;
            }

            GameManager gameManager = GameManager.Instance;
            if ((object)gameManager != null && (object)gameManager.MainCameraController != null)
            {
                gameManager.MainCameraController.SetManualControl(false, true);
            }

            LogInfo(
                "Restored player input after Boss Rush teleport. Room=" +
                GetRoomDebugLabel(targetRoom) +
                ", Before=" +
                inputBeforeRestore +
                ", After=" +
                DescribeInputState(player) +
                ", ClearedOverrides=" +
                hadInputOverride +
                ".");
        }

        private static bool IsNamedBossFoyer(RoomHandler room)
        {
            if ((object)room == null || room.area == null)
            {
                return false;
            }

            PrototypeDungeonRoom.RoomCategory category = room.area.PrototypeRoomCategory;
            if (category == PrototypeDungeonRoom.RoomCategory.SECRET ||
                category == PrototypeDungeonRoom.RoomCategory.REWARD ||
                category == PrototypeDungeonRoom.RoomCategory.EXIT)
            {
                return false;
            }

            string roomName = room.GetRoomName() ?? string.Empty;
            string lowerName = roomName.ToLowerInvariant();

            // Keep this intentionally strict. Broader name matches such as "entrance"
            // can incorrectly select shop entrances or other non-boss utility rooms.
            // ETG community mods commonly rely on the explicit "Boss Foyer" naming
            // convention, so prefer that exact semantic and otherwise fall back to
            // the boss room itself.
            if (lowerName.IndexOf("boss foyer", StringComparison.Ordinal) >= 0 ||
                lowerName.IndexOf("boss_foyer", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            return false;
        }
    }
}
