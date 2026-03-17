#if TMP
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

namespace LocalizationPackageExtensions
{
    [AddComponentMenu("Localization Extensions/Localize TMP_Font")]
    public class LocalizeTMP_Font : LocalizedAssetBehaviour<TMP_FontAsset, LocalizedTmpFont>
    {
        public TMP_Text tmp;

        void Reset()
        {
            tmp = GetComponent<TMP_Text>();
        }

        protected override void UpdateAsset(TMP_FontAsset localizedFont)
        {
            tmp.font = localizedFont;
        }
    }
}
#endif