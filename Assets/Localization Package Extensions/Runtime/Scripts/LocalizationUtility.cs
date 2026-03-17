#if TMP
using TMPro;
#endif

using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;

namespace LocalizationPackageExtensions
{
    public static class LocalizationUtility
    {
#if TMP
        /// <summary>
        /// Find a localized string from a table and apply it directly to a TMP component.
        /// It can be used for both UI and 3D TextMesh.
        /// This method operates asynchronously.
        /// </summary>
        public static void ApplyLocalizedString(TableReference tableReference, TableEntryReference entryReference, TMP_Text tmp)
        {
            tmp.text = null;
            var async = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableReference, entryReference);
            async.Completed += handle => tmp.text = handle.Result;
        }  
#endif
        /// <summary>
        /// Find a localized string from a table and apply it directly to a Legacy Text component.
        /// This method operates asynchronously.
        /// </summary>
        public static void ApplyLocalizedString(TableReference tableReference, TableEntryReference entryReference, Text text)
        {
            text.text = null;
            var async = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableReference, entryReference);
            async.Completed += handle => text.text = handle.Result;
        }
        /// <summary>
        /// Find a localized string from a table and apply it directly to a Legacy TextMesh component.
        /// This method operates asynchronously.
        /// </summary>
        public static void ApplyLocalizedString(TableReference tableReference, TableEntryReference entryReference, TextMesh textMesh)
        {
            textMesh.text = null;
            var async = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableReference, entryReference);
            async.Completed += handle => textMesh.text = handle.Result;
        }
    }
}