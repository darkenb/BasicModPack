using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace BasicModPack
{
    // While building, a line beside the cursor: how many of the held building you carry, and for a dragged belt line how many
    // pieces it takes.
    public static class BeltCounter
    {
        public static ConfigEntry<bool> Enabled;
        static Text label;

        public static string Line(string item, int carried, int placing) =>
            placing > 0 ? Lang.F("{0}: {1} to place, {2} carried", item, placing, carried) + (placing > carried ? Lang.F(" ({0} short)", placing - carried) : "") : Lang.F("{0}: {1} carried", item, carried);

        public static void Tick()
        {
            string text = Enabled.Value ? Current() : null;
            if (text == null) { Hide(); return; }
            if (label == null) Build();
            label.text = text;
            var parent = (RectTransform)label.transform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Input.mousePosition, UIRoot.instance.overlayCanvas.worldCamera, out var local))
                label.rectTransform.localPosition = local + new Vector2(24f, -24f);
            label.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            if (label != null) label.gameObject.SetActive(false);
        }

        static string Current()
        {
            Player player = GameMain.mainPlayer;
            var build = player?.controller?.actionBuild;
            if (build == null || player.controller.cmd.mode != 1 || player.package == null) return null;
            ItemProto item = LDB.items.Select(player.controller.cmd.refId);
            if (item == null) return null;
            int carried = player.package.GetItemCount(item.ID) + (player.inhandItemId == item.ID ? player.inhandItemCount : 0);
            int placing = 0;
            if (build.activeTool is BuildTool_Path path && path.buildPreviews != null)
                foreach (var bp in path.buildPreviews) if (bp.item == item && bp.coverObjId == 0) placing++;
            return Line(item.name, carried, placing);
        }

        static void Build()
        {
            var layer = UIRoot.instance.uiGame.generatorWindow.transform.parent;
            label = Ui.NewText(Ui.ValueTpl, layer, Vector2.zero, new Vector2(320f, 22f), TextAnchor.UpperLeft, "");
            label.name = "bug-build-counter";
            label.transform.SetAsLastSibling();
            label.color = Ui.Ink;
            label.fontSize = 14;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        }
    }
}
