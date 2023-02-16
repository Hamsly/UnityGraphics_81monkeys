using System;
using System.Collections.Generic;



namespace UnityEngine.Experimental.Rendering.Universal
{
    public interface IQuadTreeNode
    {
        public Rect Bounds { get; }
    }

    public class QuadTree<T> where T : IQuadTreeNode
    {
        private Rect bounds;

        private List<T> nodes;

        private bool canSubDivide;

        private QuadTree<T>[] subTrees;

        private int unitSize;

        private bool didSplit = false;

        private const int MAX_NODES = 10;

        private const int NONE = -1;
        private const int TOP_RIGHT = 0;
        private const int TOP_LEFT = 1;
        private const int BOT_RIGHT = 2;
        private const int BOT_LEFT = 3;

#if UNITY_EDITOR
        private float accessed = 0;
#endif

        public QuadTree(Rect bounds,int unitSize = 1)
        {
            subTrees = null;
            this.bounds = bounds;
            this.unitSize = unitSize;

            canSubDivide = bounds.width > unitSize && bounds.height > unitSize;
            nodes = new List<T>();
            subTrees = new QuadTree<T>[4];
        }

        /// <summary>
        /// Clear the Quadtree
        /// </summary>
        public void Clear()
        {
            nodes.Clear();
            subTrees = new QuadTree<T>[4];
        }

        /// <summary>
        /// Check if the Quad Tree contains the rect
        /// </summary>
        /// <param name="rect">The rect to check against</param>
        /// <returns>An int denoting the quadrant the rect fits within, -1 if the rect fits in no quadrant</returns>
        private int GetQuadrant(Rect rect)
        {
            var index = NONE;
            float xMidpoint = bounds.x + (bounds.width / 2);
            float yMidpoint = bounds.y + (bounds.height / 2);

            // Is node on top
            bool topQuadrant = (rect.y < yMidpoint && rect.y + rect.height < yMidpoint);
            // Is node on bottom
            bool bottomQuadrant = (rect.y > yMidpoint);

            // Node is on the left
            if (rect.x < xMidpoint && rect.x + rect.width < xMidpoint)
            {
                if (topQuadrant)
                {
                    index = TOP_LEFT;
                }
                else if (bottomQuadrant)
                {
                    index = BOT_LEFT;
                }
            }
            // Node is on the right
            else if (rect.x > xMidpoint)
            {
                if (topQuadrant)
                {
                    index = TOP_RIGHT;
                }
                else if (bottomQuadrant)
                {
                    index = BOT_RIGHT;
                }
            }

            return index;
        }

        /// <summary>
        /// Split the tree and by creating the sub trees
        /// </summary>
        private void Split()
        {
            var subWidth = bounds.width / 2;
            var subHeight = bounds.height / 2;
            var x = bounds.x;
            var y = bounds.y;

            didSplit = true;

            subTrees[TOP_RIGHT] = new QuadTree<T>( new Rect(x + subWidth, y, subWidth, subHeight), unitSize);
            subTrees[TOP_LEFT]  = new QuadTree<T>( new Rect(x, y, subWidth, subHeight), unitSize);
            subTrees[BOT_LEFT]  = new QuadTree<T>( new Rect(x, y + subHeight, subWidth, subHeight), unitSize);
            subTrees[BOT_RIGHT] = new QuadTree<T>( new Rect(x + subWidth, y + subHeight, subWidth, subHeight), unitSize);
        }

        /// <summary>
        /// Insert a node into the Quad Tree
        /// </summary>
        /// <param name="node">The node to insert</param>
        public void Insert(T node)
        {
            if(node == null) return;

            if (subTrees[0] != null)
            {
                var quadrant = GetQuadrant(node.Bounds);

                if (quadrant != NONE)
                {
                    subTrees[quadrant].Insert(node);
                    return;
                }
            }
            nodes.Add(node);

            if (nodes.Count <= MAX_NODES || !canSubDivide) return;

            if (!didSplit)
            {
                Split();
            }

            int i = 0;
            while (i < nodes.Count)
            {
                var quadrant = NONE;
                if (nodes[i] != null)
                {
                    quadrant = GetQuadrant(nodes[i].Bounds);
                }
                else
                {
                    nodes.RemoveAt(i);
                    continue;
                }

                if (quadrant != NONE)
                {
                    subTrees[quadrant].Insert(nodes[i]);
                    nodes.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public void Remove(T node)
        {
            for( int i = 0; i < nodes.Count; i++)
            {
                T n = nodes[i];
                if (n.Equals(node))
                {
                    nodes.RemoveAt(i);
                    return;
                }
            }

            var quadrant = GetQuadrant(node.Bounds);
            if (quadrant != NONE && didSplit)
            {
                subTrees[quadrant].Remove(node);
            }
        }

        /// <summary>
        /// Populate the given list with all nodes that are close to the given rectangle
        /// </summary>
        /// <param name="nodeList">The list to populate</param>
        /// <param name="searchRect">The rect to search the quad tree with</param>
        public void GetNodes(ref List<T> nodeList, Rect searchRect)
        {
            if(!searchRect.Overlaps(bounds)) return;

#if UNITY_EDITOR
            accessed += 1;
#endif
            if (didSplit)
            {
                foreach (var tree in subTrees)
                {
                    tree.GetNodes(ref nodeList, searchRect);
                }
            }

           nodeList.AddRange(nodes);
        }

        public void DrawGizmo(float buffer = 0)
        {

#if UNITY_EDITOR
            Gizmos.color = Color.Lerp(Color.yellow , Color.cyan,accessed);
            accessed = 0;
#endif

            var p1 = new Vector2(bounds.xMin + buffer, bounds.yMin + buffer);
            var p2 = new Vector2(bounds.xMin + buffer, bounds.yMax - buffer);
            var p3 = new Vector2(bounds.xMax - buffer, bounds.yMin + buffer);
            var p4 = new Vector2(bounds.xMax - buffer, bounds.yMax - buffer);

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p4);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p1, p3);

            if (subTrees[0] == null) return;
            subTrees[0].DrawGizmo(buffer);
            subTrees[1].DrawGizmo(buffer);
            subTrees[2].DrawGizmo(buffer);
            subTrees[3].DrawGizmo(buffer);
        }
    }
}
