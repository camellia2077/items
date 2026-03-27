using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RandomLoadout
{
    internal sealed class EtgOwnedPickupReader
    {
        public HashSet<int> CollectOwnedPickupIds(PlayerController player)
        {
            HashSet<int> ownedIds = new HashSet<int>();

            if ((object)player == null)
            {
                return ownedIds;
            }

            if ((object)player.inventory != null && player.inventory.AllGuns != null)
            {
                for (int i = 0; i < player.inventory.AllGuns.Count; i++)
                {
                    Gun gun = player.inventory.AllGuns[i];
                    if ((object)gun != null)
                    {
                        ownedIds.Add(gun.PickupObjectId);
                    }
                }
            }

            AddPickupIdsFromMember(ownedIds, player, "passiveItems");

            if (!AddPickupIdsFromMember(ownedIds, player, "activeItems"))
            {
                AddPickupIdsFromMember(ownedIds, player, "CurrentItem");
            }

            return ownedIds;
        }

        private static bool AddPickupIdsFromMember(HashSet<int> ownedIds, object target, string memberName)
        {
            object value = GetMemberValue(target, memberName);
            if (value == null)
            {
                return false;
            }

            AddPickupIdsFromValue(ownedIds, value);
            return true;
        }

        private static void AddPickupIdsFromValue(HashSet<int> ownedIds, object value)
        {
            PickupObject pickup = value as PickupObject;
            if ((object)pickup != null)
            {
                ownedIds.Add(pickup.PickupObjectId);
                return;
            }

            IEnumerable enumerable = value as IEnumerable;
            if (enumerable == null)
            {
                return;
            }

            foreach (object entry in enumerable)
            {
                PickupObject listPickup = entry as PickupObject;
                if ((object)listPickup != null)
                {
                    ownedIds.Add(listPickup.PickupObjectId);
                }
            }
        }

        private static object GetMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            Type type = target.GetType();
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(target, null);
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(target);
            }

            return null;
        }
    }
}
