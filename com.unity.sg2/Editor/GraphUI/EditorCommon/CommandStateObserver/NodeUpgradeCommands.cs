using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class DismissNodeUpgradeCommand : UndoableCommand
    {
        public readonly GraphDataNodeModel NodeModel;

        public DismissNodeUpgradeCommand(GraphDataNodeModel nodeModel)
        {
            NodeModel = nodeModel;
            UndoString = $"Dismiss {nodeModel.DisplayTitle} Node Upgrade Flag";
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            DismissNodeUpgradeCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphModelState, command);
            }

            using var graphUpdater = graphModelState.UpdateScope;

            command.NodeModel.optedOutOfUpgrade = true;
            graphUpdater.MarkChanged(command.NodeModel);
        }
    }

    class UpgradeNodeCommand : UndoableCommand
    {
        public readonly GraphDataNodeModel NodeModel;

        public UpgradeNodeCommand(GraphDataNodeModel nodeModel)
        {
            NodeModel = nodeModel;
            UndoString = $"Upgrade {nodeModel.DisplayTitle} Node";
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            UpgradeNodeCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphModelState, command);
            }

            using var graphUpdater = graphModelState.UpdateScope;

            Debug.LogWarning($"UNIMPLEMENTED: Upgrade {command.NodeModel}"); // TODO
            graphUpdater.MarkChanged(command.NodeModel);
        }
    }
}
