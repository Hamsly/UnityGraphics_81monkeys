using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{

    public abstract class Node
    {
        public Vector2Int Pos;

        public Node(Vector2Int pos)
        {
            Pos = pos;
        }
    }

    public class QuadTree
    {
        private Vector2Int topLeft;
        private Vector2Int botRight;

        private Node n;

        private QuadTree topLeftTree;
        private QuadTree topRightTree;
        private QuadTree botLeftTree;
        private QuadTree botRightTree;

        public QuadTree(Vector2Int topL, Vector2Int botR)
        {
            topLeftTree  = null;
            topRightTree = null;
            botLeftTree  = null;
            botRightTree = null;
            topLeft = topL;
            botRight = botR;
        }

        // Check if current quadtree contains the point
        public bool Contains(Vector2Int p)
        {
            return (p.x >= topLeft.x &&
                    p.x <= botRight.x &&
                    p.y >= topLeft.y &&
                    p.y <= botRight.y);
        }

        // Insert a node into the quadtree
        public void Insert(Node node)
        {
            if (node == null)
                return;

            // Current quad cannot contain it
            if (!Contains(node.Pos))
                return;

            // We are at a quad of unit area
            // We cannot subdivide this quad further
            if (Mathf.Abs(topLeft.x - botRight.x) <= 1 &&
                Mathf.Abs(topLeft.y - botRight.y) <= 1)
            {
                n ??= node;
                return;
            }

            if ((topLeft.x + botRight.x) / 2 >= node.Pos.x)
            {
                // Indicates topLeftTree
                if ((topLeft.y + botRight.y) / 2 >= node.Pos.y)
                {
                    topLeftTree ??= new QuadTree(
                        new Vector2Int(topLeft.x, topLeft.y),
                        new Vector2Int((topLeft.x + botRight.x) / 2,
                            (topLeft.y + botRight.y) / 2));
                    topLeftTree.Insert(node);
                }

                // Indicates botLeftTree
                else
                {
                    botLeftTree ??= new QuadTree(
                        new Vector2Int(topLeft.x,
                            (topLeft.y + botRight.y) / 2),
                        new Vector2Int((topLeft.x + botRight.x) / 2,
                            botRight.y));
                    botLeftTree.Insert(node);
                }
            }
            else
            {
                // Indicates topRightTree
                if ((topLeft.y + botRight.y) / 2 >= node.Pos.y)
                {
                    topRightTree ??= new QuadTree(
                        new Vector2Int((topLeft.x + botRight.x) / 2,
                            topLeft.y),
                        new Vector2Int(botRight.x,
                            (topLeft.y + botRight.y) / 2));
                    topRightTree.Insert(node);
                }

                // Indicates botRightTree
                else
                {
                    botRightTree ??= new QuadTree(
                        new Vector2Int((topLeft.x + botRight.x) / 2,
                            (topLeft.y + botRight.y) / 2),
                        new Vector2Int(botRight.x, botRight.y));
                    botRightTree.Insert(node);
                }
            }
        }

        // Find a node in a quadtree
        public Node Search(Vector2Int p)
        {
            // Current quad cannot contain it
            if (!Contains(p))
                return null;

            // We are at a quad of unit length
            // We cannot subdivide this quad further
            if (n != null)
                return n;

            if ((topLeft.x + botRight.x) / 2 >= p.x)
            {
                // Indicates topLeftTree
                return (topLeft.y + botRight.y) / 2 >= p.y ? topLeftTree?.Search(p) : botLeftTree?.Search(p);
            }
            else
            {
                // Indicates topRightTree
                return (topLeft.y + botRight.y) / 2 >= p.y ? topRightTree?.Search(p) : botRightTree?.Search(p);
            }
        }
    }
}
