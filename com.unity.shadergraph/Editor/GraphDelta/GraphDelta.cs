using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphDelta : IGraphHandler
    {
        internal readonly GraphStorage m_data;

        public GraphDelta()
        {
            m_data = new GraphStorage();
        }

        /*
        public INodeRef AddNode(string id)
        {
            return m_data.AddNode(id);
        }
        */

        public INodeReader GetNode(string id)
        {
            return m_data.GetNode(id);
        }

        public IEnumerable<INodeReader> GetNodes()
        {
            return m_data.GetNodes();
        }

        /*
        internal void RemoveNode(string id)
        {
            m_data.RemoveNode(id);
        }

        public void RemoveNode(INodeRef node)
        {
            node.Remove();
        }

        public bool TryMakeConnection(IPortRef output, IPortRef input)
        {
            return m_data.TryConnectPorts(output, input);
        }
        */
    }
}
