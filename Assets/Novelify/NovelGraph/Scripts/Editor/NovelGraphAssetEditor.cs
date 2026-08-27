using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections;
using System.Collections.Generic;

namespace NovelGraph.Editor
{
    [CustomEditor(typeof(NovelGraphAsset))]
    public class NovelGraphAssetEditor : UnityEditor.Editor
    {
        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int index)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceId);

            if(asset.GetType() == typeof(NovelGraphAsset))
            {
                NovelGraphEditorWindow.Open((NovelGraphAsset)asset);
                return true;
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            if(GUILayout.Button("Open Novel Graph"))
            {
                NovelGraphEditorWindow.Open((NovelGraphAsset)target);
            }
        }
    }
}
