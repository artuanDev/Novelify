using System;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace NovelGraph.Editor
{
    public class NovelGraphEditorWindow : EditorWindow
    {
        public static void Open(NovelGraphAsset target)
        {
            NovelGraphEditorWindow[] windows = Resources.FindObjectsOfTypeAll<NovelGraphEditorWindow>();

            foreach (var w in windows)
            {
                if (w.currentGraph == target)
                {
                    w.Focus();
                    return;
                }
            }

            NovelGraphEditorWindow window = CreateWindow<NovelGraphEditorWindow>(typeof(NovelGraphEditorWindow), typeof(SceneView));
            window.titleContent = new GUIContent($"{target.name}", EditorGUIUtility.ObjectContent(null, typeof(NovelGraphAsset)).image);
            window.Load(target);
        }

        [SerializeField]
        private NovelGraphAsset m_currentGraph;

        [SerializeField]
        private SerializedObject m_serializedObject;

        [SerializeField]
        private NovelGraphView m_currentView;

        public NovelGraphAsset currentGraph => m_currentGraph;

        private void OnEnable()
        {
            if (m_currentGraph != null)
            {
                DrawGraph();
            }
        }

        private void OnGUI()
        {
            if(m_currentGraph != null)
            {
                if (EditorUtility.IsDirty(m_currentGraph))
                {
                    this.hasUnsavedChanges = true;
                }
                else
                {
                    this.hasUnsavedChanges = false;
                }
            }
        }

        public void Load(NovelGraphAsset target)
        {
            m_currentGraph = target;
            DrawGraph();
        }

        public void DrawGraph()
        {
            m_serializedObject = new SerializedObject(m_currentGraph);
            m_currentView = new NovelGraphView(m_serializedObject, this);
            m_currentView.graphViewChanged += OnChange;
            rootVisualElement.Add(m_currentView);
        }

        private GraphViewChange OnChange(GraphViewChange graphViewChange)
        {
            EditorUtility.SetDirty(m_currentGraph);
            return graphViewChange;
        }
    }
}
