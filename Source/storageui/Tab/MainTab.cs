using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using storageui.Helper;

namespace storageui.Tab
{
    public class StorageUIWindow : Window
    {
        private Vector2 scrollPosition = Vector2.zero;
        private List<(ThingDef, int)> itemsInStorage;

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
            Rect viewRect = new Rect(0, 0, inRect.width - 16, itemsInStorage.Count * 30);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

            float y = 0;
            foreach ((ThingDef itemDef, int count) in itemsInStorage) // Change this line
            {
                Widgets.Label(new Rect(0, y, viewRect.width, 30), $"{itemDef.label.CapitalizeFirst()} x{count}");
                y += 30;
            }

            Widgets.EndScrollView();
            // Close the window if the escape key is pressed
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                this.Close();
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