using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace LocalizationPackageExtensions
{
    [AddComponentMenu("Localization Extensions/Localize Legacy Font")]
    public class LocalizeLegacyFont : LocalizedAssetBehaviour<Font, LocalizedFont>
    {
        public Text text;
        public TextMesh textMesh;

        void Reset()
        {
            text = GetComponent<Text>();
            textMesh = GetComponent<TextMesh>();
        }

        protected override void UpdateAsset(Font localizedFont)
        {
            if (text)text.font = localizedFont;
            if (textMesh) textMesh.font = localizedFont;
        }
    }
}