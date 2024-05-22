using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using storageui.Helper;
using System.Linq;

namespace storageui.Tab
{
    public class MainTabWindow_StorageUI : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private List<(string, List<(Thing, int)>)> itemsInStorage;
        private Dictionary<ThingDef, int> currentIndices = new Dictionary<ThingDef, int>();
        private string currentFilter = "All"; // New field to store the current filter

        public override Vector2 RequestedTabSize
        {
            get
            {
                float width = 400f; // Set the width of the window here
                float height = Screen.height - 35f; // Set the height to the screen height minus a small margin
                return new Vector2(width, height);
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            UpdateItemsInStorage();
        }
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            UpdateItemsInStorage();
        }
        private void UpdateItemsInStorage()
        {
            itemsInStorage = StorageHelper.GetItemsInStorage();
            ApplyFilter(); // New method to apply the filter to the items
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title label
            Widgets.Label(new Rect(0, 0, inRect.width, 30), "Items in Storage");

            // Filter button
            if (Widgets.ButtonText(new Rect(inRect.width - 100, 0, 100, 30), "Filter"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Clear filter", () => currentFilter = ""),
                    new FloatMenuOption("Filter by...", () =>
                    {
                        List<FloatMenuOption> filterOptions = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("Weapons", () => currentFilter = "Weapons"),
                            new FloatMenuOption("Apparel", () => currentFilter = "Apparel"),
                            new FloatMenuOption("Food", () => currentFilter = "Food"),
                            new FloatMenuOption("Medicine", () => currentFilter = "Medicine"),
                            new FloatMenuOption("Organs and Implants", () => currentFilter = "Organs and Implants"),
                            new FloatMenuOption("Resources", () => currentFilter = "Resources"),
                            // Add more categories here
                        };
                        Find.WindowStack.Add(new FloatMenu(filterOptions));
                    })
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Scroll view to display items
            Rect outRect = new Rect(0, 40, inRect.width, inRect.height - 80);
            int totalLines = itemsInStorage.Count; // One line for each storage label
            foreach (var storage in itemsInStorage)
            {
                totalLines += storage.Item2.GroupBy(i => i.Item1.def).Count(); // One line for each grouped item
                totalLines += storage.Item2.GroupBy(i => i.Item1.TryGetQuality(out QualityCategory qc) ? qc : (QualityCategory?)null).Count(); // One line for each quality category within a grouped item
            }
            float viewHeight = totalLines * 30 + 40;
            Rect viewRect = new Rect(0, 0, inRect.width - 16, viewHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0;
            foreach ((string storageLabel, List<(Thing, int)> items) in itemsInStorage)
            {
                Widgets.Label(new Rect(0, y, viewRect.width, 30), storageLabel);
                y += 30;
                foreach (var groupedItems in items.GroupBy(i => new { i.Item1.def, Quality = i.Item1.TryGetQuality(out QualityCategory qc) ? qc : (QualityCategory?)null }))
                {
                    ThingDef itemDef = groupedItems.Key.def;
                    QualityCategory? quality = groupedItems.Key.Quality;
                    int totalCount = groupedItems.Sum(i => i.Item2);
                    // Draw the item's icon
                    Widgets.ThingIcon(new Rect(20, y, 30, 30), itemDef);
                    // Check if the item has a quality
                    string label;
                    if (quality.HasValue)
                    {
                        // Adjust the label's position to be next to the icon and display the quality
                        label = $"{itemDef.label.CapitalizeFirst()} Quality: {quality.Value}";
                    }
                    else
                    {
                        // Adjust the label's position to be next to the icon
                        label = $"{itemDef.label.CapitalizeFirst()} x{totalCount}";
                    }
                    Rect labelRect = new Rect(60, y, viewRect.width, 30);
                    Widgets.Label(labelRect, label);

                    // Highlight the item when the mouse hovers over it
                    if (Mouse.IsOver(labelRect))
                    {
                        Widgets.DrawHighlight(labelRect);
                    }
                    
                    // Pan the camera to the item when it is clicked
                    if (Widgets.ButtonInvisible(labelRect))
                    {
                        if (!currentIndices.ContainsKey(itemDef))
                        {
                            currentIndices[itemDef] = 0;
                        }

                        List<Thing> things = groupedItems.Select(i => i.Item1).ToList();
                        Thing thingToSelect = things[currentIndices[itemDef]];
                        Find.CameraDriver.JumpToCurrentMapLoc(thingToSelect.Position);
                        Find.Selector.ClearSelection(); // Clear the current selection
                        Find.Selector.Select(thingToSelect); // Select the item

                        // Increment the index for this item, wrapping around to 0 if it's at the end of the list
                        currentIndices[itemDef] = (currentIndices[itemDef] + 1) % things.Count;
                    }

                    y += 30;
                }
            }

            Widgets.EndScrollView();
            // Close the window if the escape key is pressed
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Close();
            }
        }

        private void ApplyFilter()
        {
            if (!string.IsNullOrEmpty(currentFilter))
            {
                itemsInStorage = itemsInStorage
                    .Select(s => (s.Item1, s.Item2.Where(i => IsItemInCategory(i.Item1)).ToList()))
                    .Where(s => s.Item2.Count > 0)
                    .ToList();
            }
        }

        private bool IsItemInCategory(Thing item)
        {
            switch (currentFilter)
            {
                case "Weapons":
                    return item.def.IsWeapon;
                case "Apparel":
                    return item.def.IsApparel;
                case "Food":
                    return item.def.IsNutritionGivingIngestible;
                case "Medicine":
                    return item.def.IsMedicine;
                case "Resources":
                    return item.def.IsStuff;
                case "Organs and Implants":
                    return item.def.category == ThingCategory.Item && item.def.thingCategories.Any(tc => tc.defName == "BodyPartsNatural" || tc.defName == "BodyPartsArtificial");
                // Add more cases here for other categories
                default:
                    return true; // If the filter doesn't match any known category, don't filter the item
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            Log.Message("Storage UI window closed");
            // Additional cleanup or logic here if needed
        }
    }
}