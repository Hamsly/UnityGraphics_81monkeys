using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;


namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class ShadowRealm2D : MonoBehaviour
    {
        public static ShadowRealm2D Instance;

        [SerializeField] private Camera shadowCamera;

        public int IterationsPerFrame = 100;
        [Min(1)]public int UnitSize = 16;

        public Rect WorldRect;

        [SerializeField] private QuadTree<ShadowCaster2D> shadowTreeA;
        [SerializeField] private QuadTree<ShadowCaster2D> shadowTreeB;
        [SerializeField] private QuadTree<ShadowCaster2D> staticShadowTree;

        public static event Action OnInit;

        private bool currentShadowTree = false;
        private bool prevShadowTree = true;

        [Space]
        [SerializeField] private float DebugOffset = 0.1f;
        [SerializeField] private bool DebugStaticShadows = false;
        [SerializeField] private bool DebugDynamicShadows = false;
        [SerializeField] private bool DebugDynamicBuildingShadows = false;

        public bool HasDoneInit { get; private set; } = false;

        public Camera ShadowCamera => shadowCamera;

        private void OnEnable()
        {
            Instance = this;

            if (WorldRect.width != 0 && WorldRect.height != 0)
            {
                InitShadow2DWorld(WorldRect);
            }
        }

        public ShadowRealm2D()
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
                staticShadowTree.DrawGizmo(DebugOffset);
            }

            if (DebugDynamicShadows)
            {
                QuadTree<ShadowCaster2D> workingTree = !currentShadowTree ? shadowTreeA : shadowTreeB;
                workingTree.DrawGizmo(DebugOffset);
            }

            if (DebugDynamicBuildingShadows)
            {
                QuadTree<ShadowCaster2D> workingTree = currentShadowTree ? shadowTreeA : shadowTreeB;
                workingTree.DrawGizmo(DebugOffset);
            }

        }
    }
}
