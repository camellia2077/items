using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using RandomLoadout.Core;

namespace RandomLoadout
{
    internal sealed class EtgPickupResolver
    {
        private const int FallbackScanLimit = 2048;

        public EtgPickupResolveResult Resolve(PickupCategory category, string pickupName)
        {
            return ResolveInternal(category, pickupName, false);
        }

        public EtgPickupResolveResult ResolveAny(string pickupName)
        {
            return ResolveInternal(null, pickupName, true);
        }

        public EtgPickupResolveResult ResolveRandomGrantable(int seed)
        {
            List<PickupObject> candidates = new List<PickupObject>();
            foreach (PickupObject pickup in EnumeratePickups())
            {
                if ((object)pickup == null)
                {
                    continue;
                }

                PickupCategory? category = GetPickupCategory(pickup);
                if (!category.HasValue)
                {
                    continue;
                }

                candidates.Add(pickup);
            }

            if (candidates.Count == 0)
            {
                return Failure(null, "RandomPickupUnavailable", "No supported pickups were available for random grant.");
            }

            Random random = new Random(seed);
            PickupObject match = candidates[random.Next(candidates.Count)];
            PickupCategory? resolvedCategory = GetPickupCategory(match);
            return new EtgPickupResolveResult(true, resolvedCategory, match.PickupObjectId, GetPickupLabel(match), null);
        }

        private static EtgPickupResolveResult ResolveInternal(PickupCategory? category, string pickupName, bool allowAnyCategory)
        {
            if (string.IsNullOrEmpty(pickupName))
            {
                return Failure(category, "SpecificNameNotFound", "The configured specific pickup name was empty.");
            }

            List<PickupObject> matches = new List<PickupObject>();
            foreach (PickupObject pickup in EnumeratePickups())
            {
                if ((object)pickup == null)
                {
                    continue;
                }

                if (!string.Equals(GetPickupLabel(pickup), pickupName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!allowAnyCategory && category.HasValue && !MatchesCategory(category.Value, pickup))
                {
                    continue;
                }

                matches.Add(pickup);
            }

            if (matches.Count == 0)
            {
                return Failure(category, "SpecificNameNotFound", "No pickup matched the configured specific pickup name '" + pickupName + "'.");
            }

            if (matches.Count > 1)
            {
                return Failure(category, "SpecificNameAmbiguous", "Multiple pickups matched the configured specific pickup name '" + pickupName + "'.");
            }

            PickupObject match = matches[0];
            PickupCategory? resolvedCategory = GetPickupCategory(match);
            if (!resolvedCategory.HasValue)
            {
                return Failure(category, "SpecificCategoryMismatch", "The configured specific pickup '" + pickupName + "' was not a supported grantable category.");
            }

            if (!allowAnyCategory && category.HasValue && resolvedCategory.Value != category.Value)
            {
                return Failure(category, "SpecificCategoryMismatch", "The configured specific pickup '" + pickupName + "' did not match the expected category.");
            }

            return new EtgPickupResolveResult(true, resolvedCategory, match.PickupObjectId, GetPickupLabel(match), null);
        }

        private static EtgPickupResolveResult Failure(PickupCategory? category, string code, string message)
        {
            return new EtgPickupResolveResult(false, category, 0, string.Empty, new SelectionWarning(category, code, message));
        }

        private static IEnumerable<PickupObject> EnumeratePickups()
        {
            HashSet<int> seenIds = new HashSet<int>();
            IEnumerable objects = GetDatabaseObjects();
            if (objects != null)
            {
                foreach (object entry in objects)
                {
                    PickupObject pickup = entry as PickupObject;
                    if ((object)pickup == null || !seenIds.Add(pickup.PickupObjectId))
                    {
                        continue;
                    }

                    yield return pickup;
                }

                yield break;
            }

            for (int pickupId = 0; pickupId < FallbackScanLimit; pickupId++)
            {
                PickupObject pickup = PickupObjectDatabase.GetById(pickupId);
                if ((object)pickup == null || !seenIds.Add(pickup.PickupObjectId))
                {
                    continue;
                }

                yield return pickup;
            }
        }

        private static IEnumerable GetDatabaseObjects()
        {
            object database = GetStaticMemberValue(typeof(PickupObjectDatabase), "Instance");
            if (database == null)
            {
                return null;
            }

            object objects = GetInstanceMemberValue(database, "Objects");
            if (objects == null)
            {
                objects = GetInstanceMemberValue(database, "objects");
            }

            return objects as IEnumerable;
        }

        private static object GetStaticMemberValue(Type type, string memberName)
        {
            PropertyInfo property = type.GetProperty(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
            {
                return property.GetValue(null, null);
            }

            FieldInfo field = type.GetField(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(null);
            }

            return null;
        }

        private static object GetInstanceMemberValue(object target, string memberName)
        {
            if (target == null)
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

        private static bool MatchesCategory(PickupCategory category, PickupObject pickup)
        {
            PickupCategory? resolvedCategory = GetPickupCategory(pickup);
            return resolvedCategory.HasValue && resolvedCategory.Value == category;
        }

        private static PickupCategory? GetPickupCategory(PickupObject pickup)
        {
            if (pickup is Gun)
            {
                return PickupCategory.Gun;
            }

            if (pickup is PassiveItem)
            {
                return PickupCategory.Passive;
            }

            if (pickup is PlayerItem)
            {
                return PickupCategory.Active;
            }

            return null;
        }

        private static string GetPickupLabel(PickupObject pickup)
        {
            if ((object)pickup == null)
            {
                return "<null>";
            }

            if (pickup.encounterTrackable != null &&
                pickup.encounterTrackable.journalData != null &&
                !string.IsNullOrEmpty(pickup.encounterTrackable.journalData.PrimaryDisplayName))
            {
                return pickup.encounterTrackable.journalData.PrimaryDisplayName;
            }

            return pickup.name;
        }
    }
}
