using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NovelGraph.Editor
{
    [CreateAssetMenu(fileName = "NovelgraphSettings", menuName = "Scriptable Objects/NovelgraphSettings")]
    public class NovelgraphSettings : ScriptableObject
    {
        public const string customSettingsPath = "Assets/Novelify/Resources/NovelifySettings.asset";

        [SerializeField]
        public Color inputPortColor = Color.cyan;

        internal static NovelgraphSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<NovelgraphSettings>(customSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<NovelgraphSettings>();
                AssetDatabase.CreateAsset(settings, customSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
}
