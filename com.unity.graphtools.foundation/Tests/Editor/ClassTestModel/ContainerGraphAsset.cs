using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class ContainerGraphAsset : GraphAsset
    {
        protected override Type GraphModelType => typeof(ClassGraphModel);
    }
}
