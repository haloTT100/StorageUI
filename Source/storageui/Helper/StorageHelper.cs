using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace storageui.Helper
{
    public static class StorageHelper
    {
        public static List<(ThingDef, int)> GetItemsInStorage()
        {
            List<(Thing, int)> itemsInStorage = new List<(Thing, int)>();

            // Iterate through all zones on the map
            foreach (Zone zone in Find.CurrentMap.zoneManager.AllZones)
            {
                if (zone is Zone_Stockpile stockpileZone)
                {
                    foreach (Thing thing in stockpileZone.AllContainedThings)
                    {
                        if (thing.def.category == ThingCategory.Item)
                        {
                            itemsInStorage.Add((thing, thing.stackCount));
                        }
                    }
                }
            }

            // Iterate through all buildings on the map
            foreach (Building building in Find.CurrentMap.listerBuildings.allBuildingsColonist)
            {
                if (building is Building_Storage storageBuilding)
                {
                    // Iterate over all cells of the building
                    foreach (IntVec3 cell in storageBuilding.OccupiedRect().Cells)
                    {
                        var thingsInCell = storageBuilding.Map.thingGrid.ThingsListAtFast(cell);
                        foreach (Thing thing in thingsInCell)
                        {
                            if (storageBuilding.settings.AllowedToAccept(thing) && thing.def.category == ThingCategory.Item)
                            {
                                itemsInStorage.Add((thing, thing.stackCount));
                            }
                        }
                    }
                }
            }

            // Group items by their def and sum their counts
            return itemsInStorage
                .GroupBy(x => x.Item1.def)
                .Select(g => (g.Key, g.Sum(x => x.Item2)))
                .ToList();
        }
    }
}
