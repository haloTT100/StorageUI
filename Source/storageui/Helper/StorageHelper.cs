using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

namespace storageui.Helper
{
    public static class StorageHelper
    {
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
                    // Iterate over all cells of the building
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
                    if (itemsInBuilding.Count > 0)
                    {
                        stockpilesWithItems.Add((storageBuilding.LabelCap, itemsInBuilding));
                    }
                }
            }

            // Group items by their def and sum their counts
            return stockpilesWithItems;
        }
    }
}