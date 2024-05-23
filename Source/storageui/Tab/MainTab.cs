using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using storageui.Helper;
using System.Linq;
using System;

namespace storageui.Tab
{
    public class MainTabWindow_StorageUI : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero; // Store the scroll position
        private List<(string, List<(Thing, int)>)> itemsInStorage; // Store the items in storage
        private Dictionary<ThingDef, int> currentIndices = new Dictionary<ThingDef, int>(); // Store the current index for each item
        private string currentFilter = "All"; // store the current filter
        private string oldSearchQuery = ""; // store the previous search query
        private string currentSearchQuery = ""; // store the current search query

        public override Vector2 RequestedTabSize
        {
            get
            {
                float width = 400f; // Set the width of the window here
                float height = Screen.height - 35f; // Set the height to the screen height minus a small margin
                return new Vector2(width, height);
            }
        }

        public override void PreOpen() // Called before the window is opened
        {
            base.PreOpen();
            UpdateItemsInStorage();
        }
        public override void WindowUpdate() // Called every frame
        {
            base.WindowUpdate();
            UpdateItemsInStorage();
        }
        private void UpdateItemsInStorage()
        {
            itemsInStorage = StorageHelper.GetItemsInStorage(); // Get the items in storage
            ApplyFilter(); // Apply the filter to the items
        }

        public override void DoWindowContents(Rect inRect) // Draw the window contents
        {
            // Title label
            Widgets.Label(new Rect(0, 0, inRect.width, 30), "Items in Storage");
            // Search bar
            Widgets.Label(new Rect(0, 20, 60, 30), "Search:");
            string newSearchQuery = Widgets.TextField(new Rect(60, 20, inRect.width - 170, 30), currentSearchQuery); 
            if (newSearchQuery != oldSearchQuery) // Only update the items in storage if the search query has changed
            {
                currentSearchQuery = newSearchQuery;
                oldSearchQuery = newSearchQuery;
                UpdateItemsInStorage(); // Update the items in storage
            }

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
            Rect outRect = new Rect(0, 60, inRect.width, inRect.height - 80); 
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
                // Group items by their def and quality, then display them
                foreach (var groupedItems in items.GroupBy(i => new { i.Item1.def, Quality = i.Item1.TryGetQuality(out QualityCategory qc) ? qc : (QualityCategory?)null }))
                {
                    ThingDef itemDef = groupedItems.Key.def; 
                    QualityCategory? quality = groupedItems.Key.Quality; 
                    int totalCount = groupedItems.Sum(i => i.Item2); 
                    // Draw the item's icon
                    Widgets.ThingIcon(new Rect(20, y, 30, 30), itemDef);
                    // Check if the item has a quality
                    string label; 
                    string color = "ffffff"; 
                    if (quality.HasValue) 
                    {
                        // Adjust the label's position to be next to the icon and display the quality
                        label = $"<color=white>{itemDef.label.CapitalizeFirst()} Quality:</color> "; 
                        switch (quality.Value)
                        {
                            case QualityCategory.Awful: 
                                color = ColorUtility.ToHtmlStringRGB(new Color(255 / 255f, 0 / 255f, 0 / 255f)); 
                                break;
                            case QualityCategory.Poor:
                                color = ColorUtility.ToHtmlStringRGB(new Color(255 / 255f, 166 / 255f, 0 / 255f));
                                break;
                            case QualityCategory.Normal:
                                color = ColorUtility.ToHtmlStringRGB(new Color(255 / 255f, 255 / 255f, 255 / 255f));
                                break;
                            case QualityCategory.Good:
                                color = ColorUtility.ToHtmlStringRGB(new Color(0 / 255f, 255 / 255f, 0 / 255f));
                                break;
                            case QualityCategory.Excellent:
                                color = ColorUtility.ToHtmlStringRGB(new Color(0 / 255f, 0 / 255f, 255 / 255f));
                                break;
                            case QualityCategory.Masterwork:
                                color = ColorUtility.ToHtmlStringRGB(new Color(239/255f, 178/255f, 255/255f));
                                break;
                            case QualityCategory.Legendary:
                                color = ColorUtility.ToHtmlStringRGB(new Color(255/255f, 235/255f, 4/255f));
                                break;
                        }
                        label += $"<color=#{color}>{quality.Value}</color>";
                    }
                    else
                    {
                        // Adjust the label's position to be next to the icon
                        label = $"<color=#{color}>{itemDef.label.CapitalizeFirst()} x{totalCount}</color>";
                    }
                    Rect labelRect = new Rect(60, y, viewRect.width, 30);
                    GUI.Label(labelRect, label, new GUIStyle() { richText = true });

                    // Reset the color back to white
                    GUI.color = Color.white;

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
            if (!string.IsNullOrEmpty(currentFilter) || !string.IsNullOrEmpty(currentSearchQuery))
            {
                itemsInStorage = itemsInStorage
                    .Select(s => (s.Item1, s.Item2.Where(i => IsItemInCategory(i.Item1) && i.Item1.LabelCapNoCount.IndexOf(currentSearchQuery, StringComparison.OrdinalIgnoreCase) != -1).ToList()))
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