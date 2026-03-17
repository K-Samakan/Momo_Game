using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Networking;

namespace LocalizationPackageExtensionsEditor
{
    public static class GoogleTranslatorAPI
    {
        static string APIKey => LocalizationPackageExtensionsProjectSettings.instance.googleAPIKey;

        public static void Request(LocaleIdentifier sourceLocaleId, LocaleIdentifier targetLocaleId, string text,
            Action<bool, string> result)
        {
            var formData = new List<IMultipartFormSection>
            {
                new MultipartFormDataSection("Content-Type", "application/json; charset=utf-8"),
                new MultipartFormDataSection("source", sourceLocaleId.Code),
                new MultipartFormDataSection("target", targetLocaleId.Code),
                new MultipartFormDataSection("format", "text"),
                new MultipartFormDataSection("q", text)
            };

            var uri = $"https://translation.googleapis.com/language/translate/v2?key={APIKey}";

            var webRequest = UnityWebRequest.Post(uri, formData);

            var async = webRequest.SendWebRequest();
            async.completed += operation =>
            {
                if (webRequest.result is UnityWebRequest.Result.ProtocolError or UnityWebRequest.Result.ConnectionError)
                {
                    Debug.LogError(webRequest.error);
                    result.Invoke(false, string.Empty);

                    return;
                }

                var data = JObject.Parse(webRequest.downloadHandler.text);
                var translatedText = data["data"]["translations"][0]["translatedText"].ToString();

                result.Invoke(true, translatedText);
            };
        }
    }
}