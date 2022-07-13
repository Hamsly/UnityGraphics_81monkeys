using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;


namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class Shadow2DWorldManager : MonoBehaviour
    {
        public static Shadow2DWorldManager Instance;

        public int IterationsPerFrame = 100;
        [Min(1)]public int UnitSize = 16;

        public Rect WorldRect;

        private QuadTree<ShadowCaster2D> shadowTreeA;
        private QuadTree<ShadowCaster2D> shadowTreeB;
        private QuadTree<ShadowCaster2D> staticShadowTree;

        public static event Action OnInit;

        private bool currentShadowTree = false;
        private bool prevShadowTree = true;


        public bool DebugStaticShadows = false;
        public bool DebugDynamicShadows = false;
        public bool DebugDynamicBuildingShadows = false;

        public bool HasDoneInit { get; private set; } = false;

        private void Awake()
        {
            if (WorldRect.width != 0 && WorldRect.height != 0)
            {
                InitShadow2DWorld(WorldRect);
            }

        }

        private void OnValidate()
        {
            InitShadow2DWorld(WorldRect);
        }

        public Shadow2DWorldManager()
        {
            HasDoneInit = false;

            Instance = this;
        }


        public void InitShadow2DWorld(Rect rect)
        {
            shadowTreeA = new QuadTree<ShadowCaster2D>(rect,UnitSize);
            shadowTreeB = new QuadTree<ShadowCaster2D>(rect,UnitSize);
            staticShadowTree = new QuadTree<ShadowCaster2D>(rect,UnitSize);

            HasDoneInit = true;

            OnInit?.Invoke();
        }

        private void Update()
        {
            if (prevShadowTree != currentShadowTree)
            {
                prevShadowTree = currentShadowTree;
                StartCoroutine(GetDynamicShadows(IterationsPerFrame));
            }
        }

        public void RegisterStaticShadow(ShadowCaster2D caster)
        {
            staticShadowTree.Insert(caster);
        }

        public void ClearStaticShadows()
        {
            staticShadowTree.Clear();
        }

        IEnumerator GetDynamicShadows(int maxIterations)
        {
            QuadTree<ShadowCaster2D> workingTree = currentShadowTree ? shadowTreeA : shadowTreeB;
            workingTree.Clear();

            int iteration = 0;

            List<ShadowCaster2D> casters = ShadowCasterGroup2DManager.GetDynamicShadows();
            foreach (var caster in casters)
            {
                if (iteration++ > maxIterations)
                {
                    iteration = 0;
                    yield return null;
                }

                workingTree.Insert(caster);
            }

            currentShadowTree = !currentShadowTree;
        }

        public void GetShadowCasters(ref List<ShadowCaster2D> casters, Rect rect)
        {
            staticShadowTree?.GetNodes(ref casters,rect);

            QuadTree<ShadowCaster2D> currentTree = !currentShadowTree ? shadowTreeA : shadowTreeB;
            currentTree?.GetNodes(ref casters,rect);

            casters.Sort((a, b) => Mathf.Clamp(a.GetShadowGroup() - b.GetShadowGroup(),-1,1));
        }

        private void OnDrawGizmos()
        {
            if (DebugStaticShadows)
            {
                staticShadowTree.DrawGizmo(1);
            }

            if (DebugDynamicShadows)
            {
                QuadTree<ShadowCaster2D> workingTree = currentShadowTree ? shadowTreeA : shadowTreeB;
                workingTree.DrawGizmo(1);
            }

            if (DebugDynamicBuildingShadows)
            {
                QuadTree<ShadowCaster2D> workingTree = !currentShadowTree ? shadowTreeA : shadowTreeB;
                workingTree.DrawGizmo(1);
            }

        }
    }
}
