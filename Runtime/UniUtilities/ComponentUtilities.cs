using UnityEngine;
using UnityEngine.UI;

namespace UniCore.Utilities
{
    public static class ComponentUtilities
    {
        public static void SetNativeSizeWithMaxSize(this Image image, float maxSize)
        {
            if (image.sprite == null) return;
            var rect = image.rectTransform;
            var sizeDelta = image.sprite.rect;
            var ratio = sizeDelta.width / sizeDelta.height;
            if (ratio > 1)
            {
                sizeDelta.width = maxSize;
                sizeDelta.height = maxSize / ratio;
            }
            else
            {
                sizeDelta.height = maxSize;
                sizeDelta.width = maxSize * ratio;
            }

            rect.anchorMax = rect.anchorMin;
            rect.sizeDelta = new Vector2(sizeDelta.width, sizeDelta.height);
            image.SetAllDirty();
        }
    }
}