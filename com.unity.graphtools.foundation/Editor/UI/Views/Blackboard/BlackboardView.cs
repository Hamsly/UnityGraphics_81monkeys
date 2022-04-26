using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The blackboard view.
    /// </summary>
    public class BlackboardView : RootView, IDragSource
    {
        static List<ModelView> s_UIList = new List<ModelView>();

        public new static readonly string ussClassName = "blackboard-view";

        BlackboardGraphLoadedObserver m_LoadedObserver;
        ModelViewUpdater m_UpdateObserver;
        DeclarationHighlighter m_DeclarationHighlighter;

        IBlackboardGraphModel m_BlackboardGraphModel;

        public ViewSelection ViewSelection { get; }

        public Blackboard Blackboard { get; private set; }

        public BlackboardViewModel BlackboardViewModel => (BlackboardViewModel)Model;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardView"/> class.
        /// </summary>
        /// <param name="window">The <see cref="EditorWindow"/> containing this view.</param>
        /// <param name="parentGraphView">The <see cref="GraphView"/> linked to this blackboard.</param>
        public BlackboardView(EditorWindow window, GraphView parentGraphView)
            : base(window, parentGraphView.GraphTool)
        {
            Model = new BlackboardViewModel(parentGraphView, GraphTool.HighlighterState);

            this.AddStylesheet("BlackboardView.uss");
            AddToClassList(ussClassName);

            BlackboardCommandsRegistrar.RegisterCommands(this, GraphTool);

            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);

            ViewSelection = new BlackboardViewSelection(this, BlackboardViewModel);
            ViewSelection.AttachToView();
        }

        /// <inheritdoc />
        protected override void RegisterObservers()
        {
            if (m_LoadedObserver == null)
            {
                m_LoadedObserver = new BlackboardGraphLoadedObserver(GraphTool.ToolState, BlackboardViewModel.ViewState, BlackboardViewModel.SelectionState);
                GraphTool.ObserverManager.RegisterObserver(m_LoadedObserver);
            }

            if (m_UpdateObserver == null)
            {
                m_UpdateObserver = new ModelViewUpdater(this, BlackboardViewModel.GraphModelState, BlackboardViewModel.SelectionState, BlackboardViewModel.ViewState, GraphTool.ToolState, GraphTool.HighlighterState);
                GraphTool.ObserverManager.RegisterObserver(m_UpdateObserver);
            }

            if (m_DeclarationHighlighter == null)
            {
                m_DeclarationHighlighter = new DeclarationHighlighter(GraphTool.ToolState, BlackboardViewModel.SelectionState, GraphTool.HighlighterState);
                GraphTool.ObserverManager.RegisterObserver(m_DeclarationHighlighter);
            }
        }

        /// <inheritdoc />
        protected override void UnregisterObservers()
        {
            if (m_LoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_LoadedObserver);
                m_LoadedObserver = null;
            }

            if (m_UpdateObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                m_UpdateObserver = null;
            }

            if (m_DeclarationHighlighter != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_DeclarationHighlighter);
                m_DeclarationHighlighter = null;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IGraphElementModel> GetSelection()
        {
            return ViewSelection.GetSelection();
        }

        /// <summary>
        /// Rebuilds the whole blackboard UI.
        /// </summary>
        public override void BuildUI()
        {
            if (Blackboard != null)
            {
                Blackboard.RemoveFromHierarchy();
                Blackboard.RemoveFromRootView();
                Blackboard = null;
            }

            m_BlackboardGraphModel = GraphTool?.ToolState.BlackboardGraphModel;
            if (m_BlackboardGraphModel == null)
            {
                return;
            }

            Blackboard = ModelViewFactory.CreateUI<Blackboard>(this, m_BlackboardGraphModel);
            if (Blackboard != null)
            {
                Blackboard.ScrollView.scrollOffset = BlackboardViewModel.ViewState.ScrollOffset;
                Blackboard.ScrollView.horizontalScroller.slider.RegisterCallback<ChangeEvent<float>>(OnHorizontalScroll);
                Blackboard.ScrollView.verticalScroller.slider.RegisterCallback<ChangeEvent<float>>(OnVerticalScroll);
                Blackboard.AddToRootView(this);
                Add(Blackboard);
            }
        }

        void OnHorizontalScroll(ChangeEvent<float> e)
        {
            using (var updater = BlackboardViewModel.ViewState.UpdateScope)
            {
                updater.SetScrollOffset(new Vector2(e.newValue, Blackboard.ScrollView.verticalScroller.slider.value));
            }
        }

        void OnVerticalScroll(ChangeEvent<float> e)
        {
            using (var updater = BlackboardViewModel.ViewState.UpdateScope)
            {
                updater.SetScrollOffset(new Vector2(Blackboard.ScrollView.horizontalScroller.slider.value, e.newValue));
            }
        }

        /// <inheritdoc />
        public override void UpdateFromModel()
        {
            if (panel == null)
                return;

            using (var selectionObservation = m_UpdateObserver.ObserveState(BlackboardViewModel.SelectionState))
            using (var graphModelObservation = m_UpdateObserver.ObserveState(BlackboardViewModel.GraphModelState))
            using (var blackboardObservation = m_UpdateObserver.ObserveState(BlackboardViewModel.ViewState))
            using (var highlighterObservation = m_UpdateObserver.ObserveState(BlackboardViewModel.HighlighterState))
            {
                if (graphModelObservation.UpdateType == UpdateType.Complete ||
                    blackboardObservation.UpdateType == UpdateType.Complete)
                {
                    // Another GraphModel loaded, or big changes in the GraphModel.
                    BuildUI();
                }
                else
                {
                    List<IGraphElementModel> deletedModels = new List<IGraphElementModel>();

                    IGraphElementModel renamedModel = null;

                    // Update variable/groups model UI.
                    if (graphModelObservation.UpdateType == UpdateType.Partial)
                    {
                        var gvChangeSet = BlackboardViewModel.GraphModelState.GetAggregatedChangeset(graphModelObservation.LastObservedVersion);

                        if (gvChangeSet != null)
                        {
                            deletedModels = gvChangeSet.DeletedModels.ToList();

                            // Adding/removing a variable/group should mark the parent group as changed.
                            // Updating the parent will add/remove the variable UI.
                            foreach (var model in gvChangeSet.ChangedModels)
                            {
                                model.GetAllViews(this, null, s_UIList);
                            }

                            if (gvChangeSet?.RenamedModel != null)
                            {
                                renamedModel = gvChangeSet?.RenamedModel;
                            }
                        }
                    }

                    // Update collapsed state.
                    if (blackboardObservation.UpdateType == UpdateType.Partial)
                    {
                        var changeset = BlackboardViewModel.ViewState.GetAggregatedChangeset(blackboardObservation.LastObservedVersion);
                        foreach (var guidString in changeset.ChangedModels)
                        {
                            var guid = new SerializableGUID(guidString);
                            if (BlackboardViewModel.GraphModelState.GraphModel.TryGetModelFromGuid(guid, out var model))
                            {
                                model.GetAllViews(this, null, s_UIList);
                            }
                        }
                    }

                    // Update selection.
                    if (selectionObservation.UpdateType != UpdateType.None)
                    {
                        var selChangeSet = BlackboardViewModel.SelectionState.GetAggregatedChangeset(selectionObservation.LastObservedVersion);
                        if (selChangeSet != null)
                        {
                            foreach (var changedModel in selChangeSet.ChangedModels)
                            {
                                if (changedModel is IGroupItemModel groupItemModel)
                                {
                                    groupItemModel.GetAllViews(this, null, s_UIList);
                                }
                            }
                        }
                    }

                    // Update highlighting.
                    if (highlighterObservation.UpdateType == UpdateType.Complete)
                    {
                        foreach (var declaration in GraphTool.ToolState.GraphModel.VariableDeclarations)
                        {
                            declaration.GetAllViews(this, null, s_UIList);
                        }
                    }
                    else if (highlighterObservation.UpdateType == UpdateType.Partial)
                    {
                        var changedModels = GraphTool.HighlighterState.GetAggregatedChangeset(highlighterObservation.LastObservedVersion);
                        foreach (var declaration in changedModels.ChangedModels)
                        {
                            declaration.GetAllViews(this, null, s_UIList);
                        }
                    }

                    foreach (var ui in s_UIList.Distinct())
                    {
                        // Check ui.View != null because ui.UpdateFromModel can remove other ui from the view.
                        if (ui != null && ui.RootView != null && !deletedModels.Contains(ui.Model))
                        {
                            ui.UpdateFromModel();
                        }
                    }

                    s_UIList.Clear();

                    if (renamedModel != null)
                    {
                        renamedModel.GetAllViews(this, null, s_UIList);
                        foreach (var ui in s_UIList)
                        {
                            ui.ActivateRename();
                        }
                    }

                    s_UIList.Clear();

                    if (Blackboard != null)
                    {
                        Blackboard.ScrollView.scrollOffset = BlackboardViewModel.ViewState.ScrollOffset;
                    }
                }
            }
        }

        /// <summary>
        /// Extends the current selection by adding <see cref="row"/> and all elements between current selection and <see cref="row"/>.
        /// If the current selection is empty, only selects <see cref="row"/>.
        /// </summary>
        /// <param name="row">The element to extend the selection to.</param>
        public void ExtendSelection(BlackboardElement row)
        {
            var lastSelectedItem = GetSelection().OfType<IGroupItemModel>().LastOrDefault();
            if (lastSelectedItem == null || GetSelection().Contains(row.Model))
            {
                Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Toggle, row.GraphElementModel));
                return;
            }

            List<IGraphElementModel> flattenedList = new List<IGraphElementModel>();
            foreach (var section in Blackboard.PartList.GetPart(Blackboard.blackboardContentPartName).Root.Children().OfType<IModelViewContainer>())
            {
                RecurseGetFlattenedList(flattenedList, section);
            }

            int firstIndex = flattenedList.IndexOf(lastSelectedItem);
            int secondIndex = flattenedList.IndexOf((IGroupItemModel)row.Model);

            if (firstIndex == -1 || secondIndex == -1)
                return;

            List<IGraphElementModel> selectedList = new List<IGraphElementModel>();

            if (firstIndex < secondIndex)
                selectedList.AddRange(flattenedList.GetRange(firstIndex, secondIndex - firstIndex).OfType<IVariableDeclarationModel>());
            else
                selectedList.AddRange(flattenedList.GetRange(secondIndex, firstIndex - secondIndex).OfType<IVariableDeclarationModel>());

            selectedList.Add(row.Model as IGraphElementModel);

            Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, selectedList));

            static void RecurseGetFlattenedList(List<IGraphElementModel> flattenedList, IModelViewContainer container)
            {
                foreach (var element in container.ModelViews)
                {
                    flattenedList.Add(element.Model as IGraphElementModel);

                    if (element is IModelViewContainer subContainer)
                        RecurseGetFlattenedList(flattenedList, subContainer);
                }
            }
        }

        internal IGroupItemModel CreateGroupFromSelection(IGroupItemModel model)
        {
            var selectedItems = GetSelection().OfType<IGroupItemModel>().Where(t => t.GetSection() == model.GetSection()).ToList();
            selectedItems.Add(model);

            selectedItems.Sort(GroupItemOrderComparer.Default);

            int index = model.ParentGroup.Items.IndexOfInternal(selectedItems.First(t => !selectedItems.Contains(t.ParentGroup)));

            while (selectedItems.Contains(model.ParentGroup)) // make sure whe don't move to a group within the selection
            {
                model = model.ParentGroup;
            }

            //Look up for the next non selected item after me
            IGroupItemModel prevItem = null;
            for (int i = index - 1; i >= 0; --i)
            {
                prevItem = model.ParentGroup.Items[i];
                if (!selectedItems.Contains(prevItem))
                    break;
            }

            Dispatch(new BlackboardGroupCreateCommand(model.ParentGroup, prevItem, null, selectedItems));
            return model;
        }

        /// <summary>
        /// Handles the <see cref="KeyDownEvent"/>.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnRenameKeyDown(KeyDownEvent e)
        {
            var lastSelectedItem = GetSelection().LastOrDefault(x => x.IsRenamable());
            if (ModelView.IsRenameKey(e) && lastSelectedItem.IsRenamable())
            {
                var uiList = new List<ModelView>();
                lastSelectedItem.GetAllViews(this, null, uiList);
                if (uiList.Any(ui => ui?.Rename() ?? false))
                {
                    e.StopPropagation();
                }
            }
        }
    }
}
