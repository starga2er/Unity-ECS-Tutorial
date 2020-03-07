using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Properties;
using Unity.Properties.Editor;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Searcher;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Build.Editor
{
    using BuildConfigurationElement = HierarchicalComponentContainerElement<BuildConfiguration, IBuildComponent, IBuildComponent>;

    [CustomEditor(typeof(BuildConfigurationScriptedImporter))]
    sealed class BuildConfigurationScriptedImporterEditor : ScriptedImporterEditor
    {
        static class ClassNames
        {
            public const string BaseClassName = nameof(BuildConfiguration);
            public const string Dependencies = BaseClassName + "__asset-dependencies";
            public const string Header = BaseClassName + "__asset-header";
            public const string HeaderLabel = BaseClassName + "__asset-header-label";
            public const string BuildAction = BaseClassName + "__build-action";
            public const string BuildDropdown = BaseClassName + "__build-dropdown";
            public const string AddComponent = BaseClassName + "__add-component-button";
        }

        struct BuildAction
        {
            public string Name;
            public Action<BuildConfiguration> Action;
        }

        static readonly BuildAction k_Build = new BuildAction
        {
            Name = "Build",
            Action = bs =>
            {
                var result = bs.Build();
                result.LogResult();
            }
        };

        static readonly BuildAction k_BuildAndRun = new BuildAction
        {
            Name = "Build and Run",
            Action = (bs) =>
            {
                var buildResult = bs.Build();
                buildResult.LogResult();
                if (buildResult.Failed)
                {
                    return;
                }

                using (var runResult = bs.Run())
                {
                    runResult.LogResult();
                }
            }
        };

        static readonly BuildAction k_Run = new BuildAction
        {
            Name = "Run",
            Action = (bs) =>
            {
                using (var result = bs.Run())
                {
                    result.LogResult();
                }
            }
        };

        // Needed because properties don't handle root collections well.
        class DependenciesWrapper
        {
            public List<BuildConfiguration> Dependencies;
        }

        const string k_CurrentActionKey = "BuildAction-CurrentAction";

        bool m_IsModified, m_LastEditState;
        BindableElement m_BuildConfigurationRoot;
        readonly DependenciesWrapper m_DependenciesWrapper = new DependenciesWrapper();

        protected override bool needsApplyRevert { get; } = true;
        public override bool showImportedObject { get; } = false;
        BuildAction CurrentBuildAction => BuildActions[CurrentActionIndex];

        static List<BuildAction> BuildActions { get; } = new List<BuildAction>
        {
            k_Build,
            k_BuildAndRun,
            k_Run,
        };

        static int CurrentActionIndex
        {
            get => EditorPrefs.HasKey(k_CurrentActionKey) ? EditorPrefs.GetInt(k_CurrentActionKey) : BuildActions.IndexOf(k_BuildAndRun);
            set => EditorPrefs.SetInt(k_CurrentActionKey, value);
        }

        public override void OnEnable()
        {
            BuildConfiguration.AssetChanged += OnBuildConfigurationImported;
            base.OnEnable();
        }

        void OnBuildConfigurationImported(BuildConfiguration obj)
        {
            if (null != m_BuildConfigurationRoot)
            {
                Refresh(m_BuildConfigurationRoot);
            }
        }

        public override void OnDisable()
        {
            BuildConfiguration.AssetChanged -= OnBuildConfigurationImported;
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
            Revert();
        }

        protected override void ResetValues()
        {
            Revert();
            m_IsModified = false;
            base.ResetValues();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            m_BuildConfigurationRoot = new BindableElement();
            m_BuildConfigurationRoot.AddStyleSheetAndVariant(ClassNames.BaseClassName);
            Refresh(m_BuildConfigurationRoot);

            root.contentContainer.Add(m_BuildConfigurationRoot);
            root.contentContainer.Add(new IMGUIContainer(ApplyRevertGUI));
            return root;
        }

        void Refresh(BindableElement root)
        {
            root.Clear();

            var config = assetTarget as BuildConfiguration;
            if (null == config)
            {
                return;
            }

            m_LastEditState = AssetDatabase.IsOpenForEdit(config);
            var openedForEditUpdater = UIUpdaters.MakeBinding(config, root);
            openedForEditUpdater.OnPreUpdate += updater =>
            {
                if (!updater.Source)
                {
                    return;
                }
                m_LastEditState = AssetDatabase.IsOpenForEdit(updater.Source);
            };
            root.binding = openedForEditUpdater;

            RefreshHeader(root, config);
            RefreshDependencies(root, config);
            RefreshComponents(root, config);
        }

        void RefreshHeader(BindableElement root, BuildConfiguration config)
        {
            var headerRoot = new VisualElement();
            headerRoot.AddToClassList(ClassNames.Header);
            root.Add(headerRoot);

            // Refresh Name Label
            var nameLabel = new Label(config.name);
            nameLabel.AddToClassList(ClassNames.HeaderLabel);
            headerRoot.Add(nameLabel);

            var labelUpdater = UIUpdaters.MakeBinding(config, nameLabel);
            labelUpdater.OnUpdate += (binding) =>
            {
                if (binding.Source != null && binding.Source)
                {
                    binding.Element.text = binding.Source.name;
                }
            };
            nameLabel.binding = labelUpdater;

            // Refresh Build&Run Button
            var dropdownButton = new VisualElement();
            dropdownButton.style.flexDirection = FlexDirection.Row;
            dropdownButton.style.justifyContent = Justify.FlexEnd;
            nameLabel.Add(dropdownButton);

            var dropdownActionButton = new Button { text = BuildActions[CurrentActionIndex].Name };
            dropdownActionButton.AddToClassList(ClassNames.BuildAction);
            dropdownActionButton.clickable = new Clickable(() => CurrentBuildAction.Action(config));
            dropdownActionButton.SetEnabled(true);
            dropdownButton.Add(dropdownActionButton);

            var actionUpdater = UIUpdaters.MakeBinding(this, dropdownActionButton);
            actionUpdater.OnUpdate += (binding) =>
            {
                if (binding.Source != null && binding.Source)
                {
                    binding.Element.text = CurrentBuildAction.Name;
                }
            };
            dropdownActionButton.binding = actionUpdater;

            var dropdownActionPopup = new PopupField<BuildAction>(BuildActions, CurrentActionIndex, a => string.Empty, a => a.Name);
            dropdownActionPopup.AddToClassList(ClassNames.BuildDropdown);
            dropdownActionPopup.RegisterValueChangedCallback(evt =>
            {
                CurrentActionIndex = BuildActions.IndexOf(evt.newValue);
                dropdownActionButton.clickable = new Clickable(() => CurrentBuildAction.Action(config));
            });
            dropdownButton.Add(dropdownActionPopup);

            // Refresh Asset Field
            var assetField = new ObjectField { objectType = typeof(BuildConfiguration) };
            assetField.Q<VisualElement>(className: "unity-object-field__selector").SetEnabled(false);
            assetField.SetValueWithoutNotify(assetTarget);
            headerRoot.Add(assetField);

            var assetUpdater = UIUpdaters.MakeBinding(config, assetField);
            assetField.SetEnabled(m_LastEditState);
            assetUpdater.OnPreUpdate += updater => updater.Element.SetEnabled(m_LastEditState);
            assetField.binding = assetUpdater;
        }

        void RefreshDependencies(BindableElement root, BuildConfiguration config)
        {
            m_DependenciesWrapper.Dependencies = FilterDependencies(config, config.Dependencies).ToList();

            var dependencyElement = new PropertyElement();
            dependencyElement.AddToClassList(ClassNames.BaseClassName);
            dependencyElement.SetTarget(m_DependenciesWrapper);
            dependencyElement.OnChanged += element =>
            {
                config.Dependencies.Clear();
                config.Dependencies.AddRange(FilterDependencies(config, m_DependenciesWrapper.Dependencies));
                Refresh(root);
                m_IsModified = true;
            };
            dependencyElement.SetEnabled(m_LastEditState);
            root.Add(dependencyElement);

            var foldout = dependencyElement.Q<Foldout>();
            foldout.AddToClassList(ClassNames.Dependencies);
            foldout.Q<Toggle>().AddToClassList(BuildConfigurationElement.ClassNames.Header);
            foldout.contentContainer.AddToClassList(BuildConfigurationElement.ClassNames.Fields);

            var dependencyUpdater = UIUpdaters.MakeBinding(config, dependencyElement);
            dependencyUpdater.OnPreUpdate += updater => updater.Element.SetEnabled(m_LastEditState);
            dependencyElement.binding = dependencyUpdater;
        }

        IEnumerable<BuildConfiguration> FilterDependencies(BuildConfiguration config, IEnumerable<BuildConfiguration> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (dependency == null || !dependency || dependency == config || dependency.HasDependency(config))
                    yield return null;
                else
                    yield return dependency;
            }
        }

        void RefreshComponents(BindableElement root, BuildConfiguration config)
        {
            // Refresh Components
            var componentRoot = new BindableElement();
            var components = config.GetComponents();
            foreach (var component in components)
            {
                componentRoot.Add(GetComponentElement(config, component));
            }
            componentRoot.SetEnabled(m_LastEditState);
            root.Add(componentRoot);

            var componentUpdater = UIUpdaters.MakeBinding(config, componentRoot);
            componentUpdater.OnUpdate += updater => updater.Element.SetEnabled(m_LastEditState);
            componentRoot.binding = componentUpdater;

            // Refresh Add Component Button
            var addComponentButton = new Button();
            addComponentButton.AddToClassList(ClassNames.AddComponent);
            addComponentButton.RegisterCallback<MouseUpEvent>(evt =>
            {
                var databases = new[]
                {
                    TypeSearcherDatabase.GetBuildConfigurationDatabase(new HashSet<Type>(BuildConfiguration.GetAvailableTypes(type => !IsShown(type)).Concat(components.Select(c => c.GetType()))))
                };

                var searcher = new Searcher(databases, new AddTypeSearcherAdapter("Add Component"));
                var editorWindow = EditorWindow.focusedWindow;
                var button = evt.target as Button;

                SearcherWindow.Show(editorWindow, searcher, AddType,
                    button.worldBound.min + Vector2.up * 15.0f, a => { },
                    new SearcherWindow.Alignment(SearcherWindow.Alignment.Vertical.Top, SearcherWindow.Alignment.Horizontal.Left));
            });
            addComponentButton.SetEnabled(m_LastEditState);
            root.contentContainer.Add(addComponentButton);

            var addComponentButtonUpdater = UIUpdaters.MakeBinding(config, addComponentButton);
            addComponentButtonUpdater.OnPreUpdate += updater => updater.Element.SetEnabled(m_LastEditState);
            addComponentButton.binding = addComponentButtonUpdater;
        }

        void Revert()
        {
            var config = assetTarget as BuildConfiguration;
            var importer = target as BuildConfigurationScriptedImporter;
            if (null == config || null == importer)
            {
                return;
            }

            BuildConfiguration.DeserializeFromPath(config, importer.assetPath);
            Refresh(m_BuildConfigurationRoot);
        }

        void Save()
        {
            var config = assetTarget as BuildConfiguration;
            var importer = target as BuildConfigurationScriptedImporter;
            if (null == config || null == importer)
            {
                return;
            }

            config.SerializeToPath(importer.assetPath);
        }

        static bool IsShown(Type t) => t.GetCustomAttribute<HideInInspector>() == null;

        bool AddType(SearcherItem arg)
        {
            if (!(arg is TypeSearcherItem typeItem))
            {
                return false;
            }

            var type = typeItem.Type;
            var config = assetTarget as BuildConfiguration;
            if (null == config)
            {
                return false;
            }

            config.SetComponent(type, TypeConstruction.Construct<IBuildComponent>(type));
            Refresh(m_BuildConfigurationRoot);
            m_IsModified = true;
            return true;

        }

        VisualElement GetComponentElement(BuildConfiguration container, object component)
        {
            var componentType = component.GetType();
            var element = (VisualElement)Activator.CreateInstance(typeof(HierarchicalComponentContainerElement<,,>)
                .MakeGenericType(typeof(BuildConfiguration), typeof(IBuildComponent), componentType), container, component);

            if (element is IChangeHandler changeHandler)
            {
                changeHandler.OnChanged += () => { m_IsModified = true; };
            }

            return element;
        }
    }
}
