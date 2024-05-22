using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using UnityEngine;
using LWM.DeepStorage;

namespace storageui.Helper
{
    public static class StorageHelper
    {
        private static readonly Type DSStorageGroupUtilityType;
        private static readonly MethodInfo GetStorageGroupForMethod;
        private static readonly MethodInfo GetCompForMethod;

        static StorageHelper()
        {
            // Find the LWM.DeepStorage.DSStorageGroupUtility type
            DSStorageGroupUtilityType = Type.GetType("LWM.DeepStorage.DSStorageGroupUtility, LWM.DeepStorage");
            if (DSStorageGroupUtilityType != null)
            {
                // Get the GetStorageGroupFor method info
                GetStorageGroupForMethod = DSStorageGroupUtilityType.GetMethod("GetSTorageGroupFor", BindingFlags.Static | BindingFlags.NonPublic);
                // Get the GetCompFor method info
                GetCompForMethod = DSStorageGroupUtilityType.GetMethod("GetCompFor", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        public static List<(string, List<(Thing, int)>)> GetItemsInStorage()
        {
            List<(string, List<(Thing, int)>)> stockpilesWithItems = new List<(string, List<(Thing, int)>)>();

            // Iterate through all zones on the map
            foreach (Zone zone in Find.CurrentMap.zoneManager.AllZones)
            {
                if (zone is Zone_Stockpile stockpileZone)
                {
                    var itemsInStockpile = new List<(Thing, int)>();
                    foreach (Thing thing in stockpileZone.AllContainedThings)
                    {
                        if (thing.def.category == ThingCategory.Item)
                        {
                            itemsInStockpile.Add((thing, thing.stackCount));
                        }
                    }
                    if (itemsInStockpile.Count > 0)
                    {
                        stockpilesWithItems.Add((stockpileZone.label, itemsInStockpile));
                    }
                }
            }

            // Iterate through all buildings on the map
            foreach (Building building in Find.CurrentMap.listerBuildings.allBuildingsColonist)
            {
                if (building is Building_Storage storageBuilding)
                {
                    var itemsInBuilding = new List<(Thing, int)>();

                    // Check if the building is part of a storage group
                    if (building is IStorageGroupMember storageGroupMember)
                    {
                        var storageGroup = GetStorageGroupFor(building);
                        if (storageGroup != null)
                        {
                            foreach (var groupedBuilding in GetGroupMembers(storageGroup))
                            {
                                var comp = GetCompFor((IStorageGroupMember)groupedBuilding);
                                if (comp != null)
                                {
                                    var storageBuildingComp = groupedBuilding as Building_Storage;
                                    if (storageBuildingComp != null)
                                    {
                                        foreach (IntVec3 cell in groupedBuilding.OccupiedRect().Cells)
                                        {
                                            var thingsInCell = groupedBuilding.Map.thingGrid.ThingsListAtFast(cell);
                                            foreach (Thing thing in thingsInCell)
                                            {
                                                if (storageBuildingComp.settings.AllowedToAccept(thing) && thing.def.category == ThingCategory.Item)
                                                {
                                                    itemsInBuilding.Add((thing, thing.stackCount));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If not part of a group, just iterate over its cells
                            foreach (IntVec3 cell in storageBuilding.OccupiedRect().Cells)
                            {
                                var thingsInCell = storageBuilding.Map.thingGrid.ThingsListAtFast(cell);
                                foreach (Thing thing in thingsInCell)
                                {
                                    if (storageBuilding.settings.AllowedToAccept(thing) && thing.def.category == ThingCategory.Item)
                                    {
                                        itemsInBuilding.Add((thing, thing.stackCount));
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // If not part of a group, just iterate over its cells
                        foreach (IntVec3 cell in storageBuilding.OccupiedRect().Cells)
                        {
                            var thingsInCell = storageBuilding.Map.thingGrid.ThingsListAtFast(cell);
                            foreach (Thing thing in thingsInCell)
                            {
                                if (storageBuilding.settings.AllowedToAccept(thing) && thing.def.category == ThingCategory.Item)
                                {
                                    itemsInBuilding.Add((thing, thing.stackCount));
                                }
                            }
                        }
                    }

                    if (itemsInBuilding.Count > 0)
                    {
                        stockpilesWithItems.Add((storageBuilding.LabelCap, itemsInBuilding));
                    }
                }
            }

            // Group items by their def and sum their counts
            return stockpilesWithItems;
        }

        private static StorageGroup GetStorageGroupFor(Thing t)
        {
            if (GetStorageGroupForMethod != null)
            {
                return GetStorageGroupForMethod.Invoke(null, new object[] { t }) as StorageGroup;
            }
            return null;
        }

        private static CompDeepStorage GetCompFor(IStorageGroupMember member)
        {
            if (GetCompForMethod != null)
            {
                return GetCompForMethod.Invoke(null, new object[] { member }) as CompDeepStorage;
            }
            return null;
        }

        private static IEnumerable<ThingWithComps> GetGroupMembers(StorageGroup storageGroup)
        {
            var membersField = storageGroup.GetType().GetField("members", BindingFlags.Instance | BindingFlags.NonPublic);
            if (membersField != null)
            {
                return membersField.GetValue(storageGroup) as IEnumerable<ThingWithComps>;
            }
            return Enumerable.Empty<ThingWithComps>();
        }
    }
}
