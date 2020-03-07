using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Searcher;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Build.Editor
{
    [CustomEditor(typeof(BuildPipelineScriptedImporter))]
    sealed class BuildPipelineScriptedImporterEditor : ScriptedImporterEditor
    {
        ReorderableList m_BuildStepsList;
        bool m_IsModified;
        TextField m_RunStepTextInput;
        Label m_CustomInspectorHeader;

        public override bool showImportedObject { get; } = false;

        public override void OnEnable()
        {
            BuildPipeline.AssetChanged += OnBuildPipelineImported;
            Refresh();
            base.OnEnable();
        }

        void OnBuildPipelineImported(BuildPipeline pipeline)
        {
            Refresh();
        }

        public override void OnDisable()
        {
            BuildPipeline.AssetChanged -= OnBuildPipelineImported;
            base.OnDisable();
        }

        protected override void OnHeaderGUI()
        {
            // Intentional
            //base.OnHeaderGUI();
        }

        public override bool HasModified()
        {
            return m_IsModified;
        }

        protected override void Apply()
        {
            Save();
            m_IsModified = false;
            base.Apply();
            Restore();
        }

        protected override void ResetValues()
        {
            Restore();
            m_IsModified = false;
            base.ResetValues();
        }

        void Save()
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return;
            }

            pipeline.SerializeToPath(importer.assetPath);
        }

        void Restore()
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return;
            }

            BuildPipeline.DeserializeFromPath(pipeline, importer.assetPath);
            SetRunStepValue(pipeline);
            Refresh();
        }

        void Refresh()
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return;
            }

            if (m_CustomInspectorHeader != null)
                SetCustomInspectorHeader();

            m_BuildStepsList = new ReorderableList(pipeline.BuildSteps, typeof(IBuildStep), true, true, true, true);
            m_BuildStepsList.headerHeight = 3;
            m_BuildStepsList.onAddDropdownCallback = AddDropdownCallbackDelegate;
            m_BuildStepsList.drawElementCallback = ElementCallbackDelegate;
            m_BuildStepsList.drawHeaderCallback = HeaderCallbackDelegate;
            m_BuildStepsList.onReorderCallback = ReorderCallbackDelegate;
            m_BuildStepsList.onRemoveCallback = RemoveCallbackDelegate;
            m_BuildStepsList.drawFooterCallback = FooterCallbackDelegate;
            m_BuildStepsList.drawNoneElementCallback = DrawNoneElementCallback;
            m_BuildStepsList.elementHeightCallback = ElementHeightCallbackDelegate;
        }

        static string GetBuildStepDisplayName(Type type)
        {
            var name = BuildStep.GetName(type);
            var category = BuildStep.GetCategory(type);
            return !string.IsNullOrEmpty(category) ? $"{category}/{name}" : name;
        }

        static string GetRunStepDisplayName(Type type)
        {
            var name = RunStep.GetName(type);
            var category = RunStep.GetCategory(type);
            return !string.IsNullOrEmpty(category) ? $"{category}/{name}" : name;
        }

        bool AddStep(SearcherItem item)
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return false;
            }

            if (item is TypeSearcherItem typeItem)
            {
                if (TypeConstruction.TryConstruct<IBuildStep>(typeItem.Type, out var step))
                {
                    pipeline.BuildSteps.Add(step);
                    m_IsModified = true;
                    return true;
                }
            }
            return false;
        }

        void AddDropdownCallbackDelegate(Rect buttonRect, ReorderableList list)
        {
            var databases = new[]
            {
                TypeSearcherDatabase.GetBuildStepsDatabase(new HashSet<Type>(BuildStep.GetAvailableTypes(type => !BuildStep.GetIsShown(type))), GetBuildStepDisplayName),
            };

            var searcher = new Searcher(databases, new AddTypeSearcherAdapter("Add Build Step"));
            var editorWindow = EditorWindow.focusedWindow;
            SearcherWindow.Show(
                editorWindow,
                searcher,
                AddStep,
                buttonRect.min + Vector2.up * 35.0f,
                a => { },
                new SearcherWindow.Alignment(SearcherWindow.Alignment.Vertical.Top,
                    SearcherWindow.Alignment.Horizontal.Left)
            );
        }

        void HandleDragDrop(Rect rect, int index)
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return;
            }

            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.ContextClick:

                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!rect.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (IBuildStep step in DragAndDrop.objectReferences)
                        {
                            pipeline.BuildSteps.Insert(index, step);
                            m_IsModified = true;
                        }
                    }
                    break;
            }
        }

        void DrawNoneElementCallback(Rect rect)
        {
            ReorderableList.defaultBehaviours.DrawNoneElement(rect, false);
            HandleDragDrop(rect, 0);
        }

        void FooterCallbackDelegate(Rect rect)
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return;
            }

            ReorderableList.defaultBehaviours.DrawFooter(rect, m_BuildStepsList);
            HandleDragDrop(rect, pipeline.BuildSteps.Count);
        }

        void ElementCallbackDelegate(Rect rect, int index, bool isActive, bool isFocused)
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return;
            }

            var step = pipeline.BuildSteps[index];
            var labelRect = rect;
            if (!BuildPipeline.ValidateBuildStepPosition(pipeline.BuildSteps, index, out var reasons))
            {
                labelRect = new Rect(rect.x, rect.y, rect.width, m_BuildStepsList.elementHeight);
                for (var i = 0; i < reasons.Length; i++)
                {
                    EditorGUI.HelpBox(new Rect(rect.x, rect.y + (m_BuildStepsList.elementHeight + ReorderableList.Defaults.padding) * (i + 1), rect.width, m_BuildStepsList.elementHeight), reasons[i], MessageType.Error);
                }
            }

            if (step is BuildPipeline buildPipeline)
            {
                GUI.Label(labelRect, buildPipeline.name + " (Build Pipeline Asset)");
            }
            else if (step is BuildStep buildStep)
            {
                GUI.Label(labelRect, buildStep.Name);
            }

            HandleDragDrop(rect, index);
        }

        float ElementHeightCallbackDelegate(int index)
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return m_BuildStepsList.elementHeight;
            }

            BuildPipeline.ValidateBuildStepPosition(pipeline.BuildSteps, index, out var reasons);
            return m_BuildStepsList.elementHeight + (reasons != null ? (m_BuildStepsList.elementHeight + ReorderableList.Defaults.padding) * reasons.Length : 0f);
        }

        void ReorderCallbackDelegate(ReorderableList list)
        {
            m_IsModified = true;
        }

        void HeaderCallbackDelegate(Rect rect)
        {
            //GUI.Label(rect, new GUIContent("Build Steps"));
            HandleDragDrop(rect, 0);
        }

        void RemoveCallbackDelegate(ReorderableList list)
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return;
            }

            pipeline.BuildSteps.RemoveAt(list.index);
            m_IsModified = true;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = Assets.LoadVisualTreeAsset("BuildPipelineCustomInspector").CloneTree();
            root.AddStyleSheetAndVariant("BuildPipelineCustomInspector");
            m_CustomInspectorHeader = root.Q<Label>(className: "InspectorHeader__Label");
            root.Q<VisualElement>("BuildSteps__IMGUIContainer").Add(new IMGUIContainer(m_BuildStepsList.DoLayoutList));
            root.Q<VisualElement>("ApplyRevertButtons").Add(new IMGUIContainer(ApplyRevertGUI));
            root.Q<Button>("RunStep__SelectButton").clickable.clickedWithEventInfo += OnRunStepSelectorClicked;
            m_RunStepTextInput = root.Q<TextField>("RunStep__RunStepTypeName");
            SetRunStepValue();
            SetCustomInspectorHeader();
            return root;
        }

        void OnRunStepSelectorClicked(EventBase @event)
        {
            SearcherWindow.Show(
                EditorWindow.focusedWindow,
                new Searcher(
                    TypeSearcherDatabase.GetRunStepDatabase(new HashSet<Type>(RunStep.GetAvailableTypes(type => !RunStep.GetIsShown(type))), GetRunStepDisplayName),
                    new AddTypeSearcherAdapter("Select Run Script")),
                UpdateRunStep,
                @event.originalMousePosition + Vector2.up * 35.0f,
                a => { },
                new SearcherWindow.Alignment(SearcherWindow.Alignment.Vertical.Top,
                                             SearcherWindow.Alignment.Horizontal.Left)
            );
        }

        bool UpdateRunStep(SearcherItem item)
        {
            var pipeline = assetTarget as BuildPipeline;
            var importer = target as BuildPipelineScriptedImporter;
            if (null == pipeline || null == importer)
            {
                return false;
            }

            if (item is TypeSearcherItem typeItem)
            {
                if (TypeConstruction.TryConstruct<RunStep>(typeItem.Type, out var step))
                {
                    pipeline.RunStep = step;
                    SetRunStepValue(pipeline);
                    m_IsModified = true;
                    return true;
                }
            }
            return false;
        }

        void SetRunStepValue(BuildPipeline pipeline = null)
        {
            var step = (pipeline ?? assetTarget as BuildPipeline)?.RunStep as RunStep;
            m_RunStepTextInput.value = step?.Name ?? string.Empty;
        }

        void SetCustomInspectorHeader()
            => m_CustomInspectorHeader.text = $"{(assetTarget as BuildPipeline)?.name} (Build Pipeline Asset)";
    }
}
