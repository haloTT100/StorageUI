using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using storageui.Helper;
using System.Linq;

namespace storageui.Tab
{
    public class StorageUIWindow : Window
    {
        private Vector2 scrollPosition = Vector2.zero;
        private List<(string, List<(Thing, int)>)> itemsInStorage;

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public StorageUIWindow()
        {
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;

            // Collect items when the window is created
            itemsInStorage = StorageHelper.GetItemsInStorage();
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title label
            Widgets.Label(new Rect(0, 0, inRect.width, 30), "Items in Storage");

            // Scroll view to display items
            Rect outRect = new Rect(0, 40, inRect.width, inRect.height - 80);
            int totalStorages = itemsInStorage.Count;
            Rect viewRect = new Rect(0, 0, inRect.width - 16, 
                itemsInStorage.SelectMany(x => x.Item2).GroupBy(i => i.Item1.def).Count() * 30 + totalStorages * 30 + 40); // Change this line

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
                        Thing firstThing = groupedItems.First().Item1;
                        Find.CameraDriver.JumpToCurrentMapLoc(firstThing.Position);
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

        public override void PostClose()
        {
            base.PostClose();
            Log.Message("Storage UI window closed");
            // Additional cleanup or logic here if needed
        }
    }

    public class MainTabWindow_StorageUI : MainTabWindow
    {
        public override void PreOpen()
        {
            base.PreOpen();
            Log.Message("Storage UI tab clicked");
            Find.WindowStack.Add(new StorageUIWindow());
        }

        public override void DoWindowContents(Rect canvas)
        {
            // You can leave this empty if you do not want to draw anything here
        }

        public override Vector2 RequestedTabSize => new Vector2(0f, 0f); // Hide the default tab window
    }
}