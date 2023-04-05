using System;
using System.Collections;
using System.Collections.Generic;
using Auios.QuadTree;


namespace UnityEngine.Experimental.Rendering.Universal
{
    public class ShadowRealm2D : MonoBehaviour
    {
        public static ShadowRealm2D Instance;

        [SerializeField] private Camera shadowCamera;

        public int IterationsPerFrame = 100;
        [Min(1)]public int UnitSize = 16;

        public Rect WorldRect;

        private QuadTree<ShadowCaster2D> shadowTreeA;
        private QuadTree<ShadowCaster2D> shadowTreeB;
        private QuadTree<ShadowCaster2D> staticShadowTree;

        public static event Action OnInit;

        private bool currentShadowTree = false;
        private bool prevShadowTree = true;


        [Space]
        [SerializeField] private float DebugOffset = 0.1f;
        [SerializeField] private bool DebugStaticShadows = false;
        [SerializeField] private bool DebugDynamicShadows = false;
        [SerializeField] private bool DebugDynamicBuildingShadows = false;

        public bool HasDoneInit { get; private set; } = false;

        public Camera ShadowCamera
        {
            get => shadowCamera;
            set => shadowCamera = value;
        }

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

        public void InitShadow2DWorld(Camera shadowCamera, Bounds bounds)
        {
            Rect rect = new Rect(bounds.min, bounds.size);
            InitShadow2DWorld(rect);
        }

        private class ShadowCaster2DBounds : IQuadTreeObjectBounds<ShadowCaster2D>
        {
            public Rect GetRect(ShadowCaster2D obj) => obj.Bounds;
            public float GetTop(ShadowCaster2D obj) => obj.Bounds.yMax;
            public float GetBottom(ShadowCaster2D obj) => obj.Bounds.yMin;
            public float GetLeft(ShadowCaster2D obj) => obj.Bounds.xMin;
            public float GetRight(ShadowCaster2D obj) => obj.Bounds.xMax;
        }

        public void InitShadow2DWorld(Rect rect)
        {
            float size = Mathf.Min(rect.width, rect.height);
            int targetLevel = 0;
            for (int i = 0; size > UnitSize; i++)
            {
                targetLevel += 1;
                size *= 0.5f;
            }
            int maxShadows = 10;

            var bounds = new ShadowCaster2DBounds();
            shadowTreeA ??= new QuadTree<ShadowCaster2D>(rect.position,rect.size,bounds,maxShadows,targetLevel);
            shadowTreeB ??= new QuadTree<ShadowCaster2D>(rect.position,rect.size,bounds,maxShadows,targetLevel);
            staticShadowTree ??= new QuadTree<ShadowCaster2D>(rect.position,rect.size,bounds,maxShadows,targetLevel);

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
            var workingTree = currentShadowTree ? shadowTreeA : shadowTreeB;
            workingTree.SoftClear();

            int iteration = 0;

            var casters = ShadowCasterGroup2DManager.GetDynamicShadows();
            var count = casters.Count;
            for (int i = 0; i < count; i++)
            {
                var caster = casters[i];

                workingTree.Insert(caster);

                if (iteration++ > maxIterations)
                {
                    iteration = 0;
                    yield return null;

                    i -= Mathf.Max(0, count - casters.Count);
                    count = casters.Count;
                }
            }

            currentShadowTree = !currentShadowTree;
        }

        public void GetShadowCasters(ref List<ShadowCaster2D> casters, Rect rect)
        {
            staticShadowTree?.FindObjects(ref casters,rect);
            //staticShadowTree.GetAll(casters);

            QuadTree<ShadowCaster2D> currentTree = !currentShadowTree ? shadowTreeA : shadowTreeB;
            currentTree?.FindObjects(ref casters,rect);

            casters.RemoveAll(w => w == null || !w.isActiveAndEnabled);

            casters.Sort((a, b) => Mathf.Clamp(a.GetShadowGroup() - b.GetShadowGroup(),-1,1));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (WorldRect.width > 0 && WorldRect.height > 0)
            {
                InitShadow2DWorld(WorldRect);
            }
        }

        private void OnDrawGizmos()
        {
            if (DebugStaticShadows)
            {
                staticShadowTree?.DrawGizmo(DebugOffset);
            }

            if (DebugDynamicShadows)
            {
                QuadTree<ShadowCaster2D> workingTree = !currentShadowTree ? shadowTreeA : shadowTreeB;
                workingTree?.DrawGizmo(DebugOffset);
            }

            if (DebugDynamicBuildingShadows)
            {
                QuadTree<ShadowCaster2D> workingTree = currentShadowTree ? shadowTreeA : shadowTreeB;
                workingTree?.DrawGizmo(DebugOffset);
            }
        }
#endif
    }
}
