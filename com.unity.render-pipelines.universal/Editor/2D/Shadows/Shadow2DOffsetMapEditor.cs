////////////////////////////////////////////////////////////////////////////
/// Author: Cameron Gomez
/// Date: 22/06/13
/// This editor will help create the offset maps to be used with the 2D lighting
/// system.
////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using PlasticGui;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace UnityEditor.Experimental.Rendering.Universal
{
    internal static class Shadow2DOffsetMapEditorContent
    {
        internal static readonly Texture CursorIconTL = Icon("SOTE_CursorTL");
        internal static readonly Texture CursorIconTR = Icon("SOTE_CursorTR");
        internal static readonly Texture CursorIconBL = Icon("SOTE_CursorBL");
        internal static readonly Texture CursorIconBR = Icon("SOTE_CursorBR");
        internal static readonly Texture CursorIconTarget = Icon("SOTE_CursorTarget");

        internal static readonly Texture ToolIconFree = Icon("SOTE_ToolIconFree");
        internal static readonly Texture ToolIconLine = Icon("SOTE_ToolIconLine");
        internal static readonly Texture ToolIconRect = Icon("SOTE_ToolIconRect");
        internal static readonly Texture ToolIconRemap = Icon("SOTE_ToolIconRemap");
        internal static readonly Texture ToolIconNoise = Icon("SOTE_ToolNoise");
        internal static readonly Texture ToolIconUndo = Icon("SOTE_Undo");

        internal static Texture Icon(string name)
        {
            return Resources.Load<Texture>(name);
        }
    }

    class Shadow2DOffsetMapEditor : EditorWindow
    {
        private float _scaling = 1f;
        private Vector2 _mousePosition;
        private Vector2 _scrollPosition;
        private Vector2 _cursorPos;
        private Vector2Int _cursorSize = Vector2Int.one;
        private Vector2Int _imageCursorPos;
        private Vector2Int _imageCursorPosStart;
        private Vector2 _viewSize;
        private Rect _groupRect;
        [SerializeField] private Sprite _sourceSprite = null;
        [SerializeField] private Texture2D _sourceTexture = null;
        [SerializeField] private Texture2D _workingTexture = null;
        [SerializeField] private Texture2D _previewTexture = null;

        private Hash128 _imageHash;

        private const int BaseY = 21 + HeaderSize;
        private const int HeaderSize = 80;
        private const float MaxGraphSize = 16000.0f;
        private readonly Vector2Int WORK_SPACE_CENTER = new Vector2Int(4096,4096);

        private bool _requireReCenter = true;

        [SerializeField] private bool editX = true;
        [SerializeField] private bool editY = true;

        [SerializeField] private bool lockXTarget = false;
        [SerializeField] private bool lockYTarget = false;

        [SerializeField] private int xLockPosition = -1;
        [SerializeField] private int yLockPosition = -1;

        [SerializeField] private float previewAlpha = 0.5f;
        private Material _previewMaterial;
        private float _previewContrast = 0;

        [SerializeField] private Vector2Int brushOffset = Vector2Int.zero;

        private bool _isMovingRemapTargets = false;
        [SerializeField] private Vector2Int remapTarget1 = Vector2Int.zero;
        [SerializeField] private Vector2Int remapTarget2 = Vector2Int.zero;

        [SerializeField] private Vector2Int noiseAmount = Vector2Int.zero;
        [SerializeField] private float noiseChaos = 1;

        private readonly List<Color[]> _undoBuffer = new List<Color[]>();

        private bool _mouseIsHeld = false;

        [SerializeField] private int selectedTool = 0;


        /// <summary>
        /// Used to open the editor from one of Unity's drop down menus
        /// </summary>
        [MenuItem("Window/Shadow2D/Shadow Offset Map Editor")]
        static void Init()
        {
            var window = GetWindow<Shadow2DOffsetMapEditor>("Shadow2D Offset Map Editor");
            window.position = new Rect(0, 0, 800, 800);
            window.Show();
        }

        private enum  ButtonState
        {
            Up,
            Pressed,
            Down,
            Released
        }

        /// <summary>
        /// Called by unity when the editor is opened
        /// </summary>
        private void Awake()
        {
            SelectInitialTexture();
        }

        /// <summary>
        /// Called by Unity every frame
        /// </summary>
        private void Update()
        {
            if (!hasFocus) return;
            Repaint();

            if (!_requireReCenter) return;
            ResetView();
            _requireReCenter = false;
        }

        /// <summary>
        /// Clear the undo history
        /// </summary>
        private void ClearUndo()
        {
            if(_undoBuffer == null) return;

            _undoBuffer.Clear();
        }

        /// <summary>
        /// Grabs the top most texture from the Undo stack and changes the working image to reflect it
        /// </summary>
        private void HandleUndo()
        {
            if(_undoBuffer == null) return;

            if (_undoBuffer.Count <= 0) return;

            var i = _undoBuffer.Count - 1;
            var textureUndoPixels = _undoBuffer[i];
            _undoBuffer.RemoveAt(i);

            if (textureUndoPixels.Length != _workingTexture.width * _workingTexture.height) return;
            _workingTexture.SetPixels(textureUndoPixels);
            _workingTexture.Apply();
        }

        /// <summary>
        /// Saves the current working texture into a list so it may be reverted to via undo
        /// </summary>
        private void RecordTextureUndo()
        {
            var pixels = _workingTexture.GetPixels();

            var unchanged = true;
            if (_undoBuffer.Count > 0)
            {
                var prevPixels = _undoBuffer[_undoBuffer.Count - 1];
                for (int i = 0; i < pixels.Length; i++)
                {
                    unchanged = (pixels[i] == prevPixels[i]) & unchanged;
                }
            }
            else
            {
                unchanged = false;
            }

            if (unchanged) return;

            _undoBuffer.Add(_workingTexture.GetPixels());

            if (_undoBuffer.Count > 50)
            {
                _undoBuffer.RemoveAt(0);
            }
        }

        /// <summary>
        /// Floors and rounds the given vector2
        /// </summary>
        /// <param name="position"></param>
        /// <param name="snap"></param>
        /// <returns></returns>
        private static Vector2 FloorVector2(Vector2 position,float snap = 1)
        {
            return new Vector2(Mathf.RoundToInt(position.x * snap), Mathf.RoundToInt(position.y * snap)) / snap;
        }

        /// <summary>
        /// Adjusts the given workspace position to a position relative to the working image
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2 GetPositionOnImage(Vector2 position)
        {
            return FloorVector2(position - WORK_SPACE_CENTER);
        }

        private Vector2 brushTargetOffset
        {
            get
            {
                var xx = (lockXTarget)? WORK_SPACE_CENTER.x + xLockPosition : _cursorPos.x + brushOffset.x;
                var yy = (lockYTarget)? WORK_SPACE_CENTER.y + yLockPosition : _cursorPos.y + brushOffset.y;
                return new Vector2(xx, yy) - _cursorPos;
            }
        }

        private Vector2 viewPosition
        {
            get => _scrollPosition / _scaling;
            set => _scrollPosition = (value) * _scaling;
        }

        private Vector2 viewSize
        {
            get => _viewSize;
        }

        private Vector2 viewCenter
        {
            get => viewPosition + (_viewSize * 0.5f);

            set => viewPosition = value - (_viewSize * 0.5f);
        }

        private Material previewMaterial
        {
            get
            {
                if (_previewMaterial == null)
                {
                    _previewMaterial = new Material(Shader.Find("Hidden/Internal-GUITextureClipBlended"));
                }
                return _previewMaterial;

            }
        }

        /// <summary>
        /// On init check if the currently selected game object has an associated sprite and use it as the initial texture.
        /// </summary>
        private void SelectInitialTexture()
        {
            if (_sourceTexture != null)
            {
                return;
            }

            if (Selection.activeObject is GameObject)
            {
                var o = Selection.activeObject as GameObject;

                if (o == null) return;

                SpriteRenderer sr;
                if (o.TryGetComponent(out sr))
                {
                    if (sr.sprite != null)
                    {
                        _sourceTexture = sr.sprite.texture;
                    }
                }
            }
        }


        /// <summary>
        /// Resets the workspace camera's zoom and position to be centered on the current image
        /// </summary>
        private void ResetView()
        {
            if (_sourceTexture != null)
            {
                _scaling = 2;
                viewCenter = WORK_SPACE_CENTER + new Vector2(_sourceTexture.width * 0.5f, _sourceTexture.height * 0.5f);
            }
        }

        private void ResetWorkSpace()
        {
            _requireReCenter = true;
            _imageHash = _sourceTexture.imageContentsHash;

            xLockPosition = -1;
            yLockPosition = -1;

            ClearUndo();
        }

        /// <summary>
        /// Built in Unity event to draw all GUI
        /// </summary>
        private void OnGUI()
        {
            var e = Event.current;
            _mousePosition = (e.mousePosition + _scrollPosition) / _scaling;

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.C)
                {
                    ResetView();
                }
            }

            DrawTopBar(position.width,HeaderSize);

            if (_sourceTexture != null)
            {
                if (_sourceTexture.imageContentsHash != _imageHash)
                {
                    ResetWorkSpace();
                }

                if(_workingTexture != null)
                {
                    DrawWorkspace();
                    var hh = 20;
                    EditorGUI.LabelField(new Rect(e.mousePosition.x, e.mousePosition.y - hh, 200, hh), String.Format("[{0},{1}]", (int)_imageCursorPos.x,(int)_imageCursorPos.y));

                    HandleToolUse();
                }
                else
                {
                    EditorGUILayout.Space(HeaderSize);
                    EditorGUILayout.LabelField("Generate a working texture to begin.");
                }
            }
            else
            {
                EditorGUILayout.Space(HeaderSize);
                EditorGUILayout.LabelField("Select a source texture to begin.");
            }
        }

        /// <summary>
        /// Handles mouse input and hands that information to the currently selected tool
        /// </summary>
        private void HandleToolUse()
        {
            Event e = Event.current;

            ButtonState state = ButtonState.Up;

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _mouseIsHeld = false;
                return;
            }

            if (e.button == 2) return;

            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                state = ButtonState.Down;
                if (!_mouseIsHeld)
                {
                    _mouseIsHeld = true;
                    state = ButtonState.Pressed;
                }
            }

            if (e.type == EventType.MouseUp)
            {
                if (_mouseIsHeld)
                {
                    _mouseIsHeld = false;
                    state = ButtonState.Released;
                }
            }

            if (state == ButtonState.Up)
            {
                //if(selectedTool != 0) _mouseIsHeld = false;
                return;
            }

            switch (selectedTool)
            {
                case 0:
                    FreeDrawTool(e.button,state,e.control);
                    break;

                case 1:
                    LineTool(e.button, state);
                    break;

                case 2:
                    RectTool(e.button, state);
                    break;

                case 3:
                    RemapTool(e.button, state,e.control);
                    break;

                case 4:
                    NoiseTool(e.button, state);
                    break;
            }
        }

        /// <summary>
        /// Handles the free-draw tool, paints a line between the current mouse position and the last mouse position
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonState"></param>
        /// <param name="ctrl"></param>
        private void FreeDrawTool(int button, ButtonState buttonState,bool ctrl)
        {
            if(buttonState != ButtonState.Down && buttonState != ButtonState.Pressed) return;

            if (buttonState == ButtonState.Pressed)
            {
                _imageCursorPosStart = _imageCursorPos;

                RecordTextureUndo();
            }

            if (button == 0 || button == 1)
            {
                foreach (var texturePosition in CalculateLinePoints(_imageCursorPosStart, _imageCursorPos))
                {
                    var minX = Mathf.Min(texturePosition.x, texturePosition.x + _cursorSize.x-1);
                    var minY = Mathf.Min(texturePosition.y, texturePosition.y + _cursorSize.y-1);
                    var maxX = Mathf.Max(texturePosition.x, texturePosition.x + _cursorSize.x-1);
                    var maxY = Mathf.Max(texturePosition.y, texturePosition.y + _cursorSize.y-1);


                    var lx = lockXTarget;
                    var ly = lockYTarget;
                    var lpx = xLockPosition;
                    var lpy = yLockPosition;

                    if (ctrl)
                    {
                        lockXTarget = true;
                        lockYTarget = true;

                        xLockPosition = texturePosition.x + brushOffset.x;
                        yLockPosition = texturePosition.y + brushOffset.y;
                    }


                    for (int texX = minX; texX <= maxX; texX++)
                    {
                        for (int texY = minY; texY <= maxY; texY++)
                        {
                            SetTextureOffset(texX, texY, button == 1);
                        }
                    }

                    lockXTarget = lx;
                    lockYTarget = ly;
                    xLockPosition = lpx;
                    yLockPosition = lpy;
                }


                _workingTexture.Apply();
            }

            _imageCursorPosStart = _imageCursorPos;
        }

        /// <summary>
        /// Draws a line between the current mouse position and the starting mouse position
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonState"></param>
        private void LineTool(int button, ButtonState buttonState)
        {
            if (buttonState == ButtonState.Pressed)
            {
                _imageCursorPosStart = _imageCursorPos;
            }

            if (buttonState == ButtonState.Released)
            {
                if (button == 0 || button == 1)
                {
                    RecordTextureUndo();

                    foreach (var texturePosition in CalculateLinePoints(_imageCursorPosStart, _imageCursorPos))
                    {
                        SetTextureOffset(texturePosition.x, texturePosition.y, button == 1);
                    }


                    _workingTexture.Apply();
                }
            }
        }

        /// <summary>
        /// Draws a rectangle between the current mouse position and the starting mouse position
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonState"></param>
        private void RectTool(int button, ButtonState buttonState)
        {
            if (buttonState == ButtonState.Pressed)
            {
                _imageCursorPosStart = _imageCursorPos;
            }

            if (buttonState == ButtonState.Released)
            {
                if (button == 0 || button == 1)
                {
                    RecordTextureUndo();

                    var minX = Mathf.Min(_imageCursorPos.x, _imageCursorPosStart.x);
                    var minY = Mathf.Min(_imageCursorPos.y, _imageCursorPosStart.y);
                    var maxX = Mathf.Max(_imageCursorPos.x, _imageCursorPosStart.x);
                    var maxY = Mathf.Max(_imageCursorPos.y, _imageCursorPosStart.y);

                    for (int texX = minX; texX <= maxX; texX++)
                    {
                        for (int texY = minY; texY <= maxY; texY++)
                        {
                            SetTextureOffset(texX, texY, button == 1);
                        }
                    }

                    _workingTexture.Apply();
                }
            }
        }

        /// <summary>
        /// Remaps a given line's offsets to the drawn line.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonState"></param>
        /// <param name="ctrl"></param>
        private void RemapTool(int button, ButtonState buttonState, bool ctrl)
        {
            if (buttonState == ButtonState.Pressed)
            {
                _imageCursorPosStart = _imageCursorPos;

                if (ctrl)
                {
                    _isMovingRemapTargets = true;
                    remapTarget1 = _imageCursorPosStart;
                }
            }

            if (_isMovingRemapTargets)
            {
                remapTarget2 = _imageCursorPos;
            }

            if (buttonState != ButtonState.Released && (ctrl || !_isMovingRemapTargets)) return;

            if (_isMovingRemapTargets)
            {
                if (button == 0)
                {
                    _isMovingRemapTargets = false;
                }
            }
            else
            {
                if (button != 0 && button != 1) return;

                RecordTextureUndo();

                var line = CalculateLinePoints(_imageCursorPosStart, _imageCursorPos);
                var lineTarget = CalculateLinePoints(remapTarget1, remapTarget2);

                var lx = lockXTarget;
                var ly = lockYTarget;
                var lpx = xLockPosition;
                var lpy = yLockPosition;

                lockXTarget = true;
                lockYTarget = true;

                for (int i = 0; i < line.Length; i++)
                {
                    var texturePosition = line[i];

                    int ii = Mathf.RoundToInt(((float)i / (line.Length - 1)) * (lineTarget.Length - 1));

                    xLockPosition = lineTarget[ii].x;
                    yLockPosition = lineTarget[ii].y;

                    SetTextureOffset(texturePosition.x, texturePosition.y, button == 1);
                }

                lockXTarget = lx;
                lockYTarget = ly;
                xLockPosition = lpx;
                yLockPosition = lpy;

                _workingTexture.Apply();
            }
        }

        /// <summary>
        /// Applies random noise to the pixels within a rectangle
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonState"></param>
        private void NoiseTool(int button, ButtonState buttonState)
        {
            switch (buttonState)
            {
                case ButtonState.Pressed:
                    _imageCursorPosStart = _imageCursorPos;
                    break;
                case ButtonState.Released:
                {
                    if (button == 0 || button == 1)
                    {
                        RecordTextureUndo();

                        var minX = Mathf.Min(_imageCursorPos.x, _imageCursorPosStart.x);
                        var minY = Mathf.Min(_imageCursorPos.y, _imageCursorPosStart.y);
                        var maxX = Mathf.Max(_imageCursorPos.x, _imageCursorPosStart.x);
                        var maxY = Mathf.Max(_imageCursorPos.y, _imageCursorPosStart.y);

                        for (int texX = minX; texX <= maxX; texX++)
                        {
                            for (int texY = minY; texY <= maxY; texY++)
                            {
                                SetTextureOffsetNoise(texX, texY, noiseAmount,noiseChaos);
                            }
                        }

                        _workingTexture.Apply();
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Sets a single pixel in the offset map
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="reset"></param>
        private void SetTextureOffset(int x, int y, bool reset)
        {
            y = (_workingTexture.height) - (y + 1);

            if (x < 0 || x >= _workingTexture.width || y < 0 || y >= _workingTexture.height) return;

            var offset = GetOffsetFromPoint(x,y);

            var prevCol = _workingTexture.GetPixel(x, y);
            var col = !reset ? OffsetToColor(offset.x, offset.y) : new Color(0.5f, 0, 0.5f, 1);

            if (!editX)
            {
                col.r = prevCol.r;
            }

            if (!editY)
            {
                col.b = prevCol.b;
            }

            _workingTexture.SetPixel(x,y,col);
        }


        private Vector2Int GetOffsetFromPoint(int brushX, int brushY)
        {
            var xLock = xLockPosition;
            var yLock = (_workingTexture.height) - (yLockPosition + 1);
            var xx = ((lockXTarget) ? brushX - xLock : brushOffset.x);
            var yy = ((lockYTarget) ? brushY- yLock : brushOffset.y);

            return new Vector2Int(xx, yy);
        }

        private Color OffsetToColor(int x, int y)
        {
            float r = (128 + (-x % 127)) / 255f;
            float b = (128 + (y % 127)) / 255f;

            int gx = Mathf.Abs(x) / 127;
            int gy = Mathf.Abs(y) / 127;

            int gxy = (gx & 0xf) | ((gy & 0xf) << 4);
            float g = gxy / 255f;

            return new Color(r, g, b, 1);
        }


        /// <summary>
        /// Generates a random value between a min and mav value that can be biased toward the mean value
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="chaos"></param>
        /// <returns></returns>
        private static float RandomNoiseValue(float min, float max, float chaos)
        {
            chaos = Mathf.Clamp01(chaos);

            var mid1 = Mathf.Lerp(min, max, Mathf.Lerp(0.5f, 0.33f, chaos));
            var mid2 = Mathf.Lerp(max, min, Mathf.Lerp(0.5f, 0.33f, chaos));

            float r = Random.value;
            var xx1 = Mathf.Lerp(min, mid1, r);
            var xx2 = Mathf.Lerp(mid1, mid2, r);
            var xx3 = Mathf.Lerp(mid2, max, r);

            var xxx1 = Mathf.Lerp(xx1, xx2, r);
            var xxx2 = Mathf.Lerp(xx2, xx3, r);

            return Mathf.Lerp(xxx1, xxx2,r);
        }

        /// <summary>
        /// Sets a single pixel of the offset map to a random value
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="chaos"></param>
        /// <returns></returns>
        private void SetTextureOffsetNoise(int x, int y,Vector2Int noiseAmount, float chaos)
        {
            y = (_workingTexture.height) - (y + 1);

            if (x < 0 || x >= _workingTexture.width || y < 0 || y >= _workingTexture.height) return;

            var prevCol = _workingTexture.GetPixel(x, y);

            var xx = (int)((prevCol.r * 255f) - 127f);
            var yy = (int)((prevCol.b * 255f) - 127f);

            xx += Mathf.RoundToInt(RandomNoiseValue(-noiseAmount.x, noiseAmount.x, chaos));
            yy += Mathf.RoundToInt(RandomNoiseValue(-noiseAmount.y, noiseAmount.y, chaos));

            var col = new Color((xx + 127f) / 255f,0,(yy + 127f) / 255f,1);
            _workingTexture.SetPixel(x,y,col);
        }

        /// <summary>
        /// Gets a list of pixel positions in a straight line between 2 points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private Vector2Int[] CalculateLinePoints(Vector2Int p1, Vector2Int p2)
        {
            var difX = Mathf.Abs(p2.x - p1.x);
            var difY = Mathf.Abs(p2.y - p1.y);

            var points = new Vector2Int[Mathf.Max(difX,difY) + 1];

            if (points.Length == 1)
            {
                points[0] = Vector2ToInt(p1);
            }
            else
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var p = i / (float)(points.Length - 1);
                    points[i].x = Mathf.RoundToInt(Mathf.Lerp(p1.x, p2.x, p));
                    points[i].y = Mathf.RoundToInt(Mathf.Lerp(p1.y, p2.y, p));
                }
            }

            return points;
        }

        /// <summary>
        /// Updates the preview texture to reflect the working texture augmented with the current preview settings
        /// </summary>
        private void UpdatePreviewTexture()
        {
            if (_previewTexture == null)
            {
                _previewTexture = new Texture2D(_workingTexture.width,_workingTexture.height)
                {
                    filterMode = FilterMode.Point
                };
            }

            if (_workingTexture.width != _previewTexture.width ||
                _workingTexture.height != _previewTexture.height)
            {
                _previewTexture.Resize(_workingTexture.width, _workingTexture.height);
            }

            var pixelsSource = _workingTexture.GetPixels();
            var pixels = new Color[pixelsSource.Length];

            for (int i = 0; i < pixelsSource.Length; i++)
            {
                var col = pixelsSource[i];

                var rr = Mathf.FloorToInt(col.r * 255);
                var bb = Mathf.FloorToInt(col.b * 255);
                if ( rr == 128 && bb == 128)
                {
                    col.a = 0;
                }

                if (_previewContrast > 0)
                {
                    col.r = Mathf.Repeat(((col.r - 0.5f) * (1 + _previewContrast)) + 0.5f,1);
                    col.g = Mathf.Repeat(((col.g) * (1 + _previewContrast)),1);
                    col.b = Mathf.Repeat(((col.b - 0.5f) * (1 + _previewContrast)) + 0.5f,1);
                }

                if (!editX && editY)
                {
                    col.r = col.b;
                    col.g = col.b;
                }

                if (!editY && editX)
                {
                    col.b = col.r;
                    col.g = col.r;
                }

                if (!editX && !editY)
                {
                    col.a = 0;
                }


                pixels[i] = col;
            }

            _previewTexture.SetPixels(pixels);
            _previewTexture.Apply();

        }

        /// <summary>
        /// Draws the GUI above the workspace
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void DrawTopBar(float width,float height)
        {
            Rect givenRect = new Rect(0, 0, width, height);

            GUILayout.BeginArea(givenRect);

            EditorGUI.DrawRect(givenRect,Color.white * 0.6f);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.Space();

            DrawTextureSelector();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            editX = EditorGUILayout.Toggle(new GUIContent("Edit X offsets:"),editX);
            editY = EditorGUILayout.Toggle(new GUIContent("Edit Y offsets:"),editY);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();


            switch (selectedTool)
            {
                case 0:
                case 1:
                case 2:
                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();

                    lockXTarget = EditorGUILayout.Toggle(new GUIContent("Lock offset X:", "Toggle with 'A'"),
                        lockXTarget);
                    lockYTarget = EditorGUILayout.Toggle(new GUIContent("Lock offset Y:", "Toggle with 'S'"),
                        lockYTarget);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space();

                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();

                    brushOffset = EditorGUILayout.Vector2IntField(
                        new GUIContent("Brush offset:", "Use 'Q' and 'W' to set with the cursor"), brushOffset);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();
                    break;

                case 3:
                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();

                    remapTarget1 = EditorGUILayout.Vector2IntField(
                        new GUIContent("Remap Target Start:", "The start point to remap from"), remapTarget1);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space();

                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();

                    remapTarget2 =
                        EditorGUILayout.Vector2IntField(
                            new GUIContent("Brush Target End:", "The end point to remap from"), remapTarget2);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();
                    break;

                case 4:
                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();

                    noiseAmount = EditorGUILayout.Vector2IntField(
                        new GUIContent("Noise Level:", "The outer bounds of the applied noise"), noiseAmount);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space();

                    EditorGUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();

                    noiseChaos = EditorGUILayout.FloatField(
                            new GUIContent("Noise Chaose:", "The end point to remap from"), noiseChaos);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();
                    break;
            }


            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// Draws the GUI for selecting the texture to work on
        /// </summary>
        private void DrawTextureSelector()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.LabelField("Source Texture:");

            //If there is no current shadowOffsetMapDictionary, do no allow the user to select a texture

            var prev = _sourceSprite;
            _sourceSprite = (Sprite)EditorGUILayout.ObjectField(_sourceSprite, typeof(Sprite), true);

            if (prev != _sourceSprite)
            {
                _sourceTexture = null;
                _workingTexture = null;

                if (_sourceSprite != null)
                {
                    _sourceTexture = _sourceSprite.texture;
                    _workingTexture = SpriteGetOffsetTexture(_sourceSprite);
                }

                ResetWorkSpace();
            }


            if (_workingTexture == null && _sourceTexture != null)
            {
                if (GUILayout.Button("Generate Working Texture"))
                {
                    _workingTexture = GenerateNewWorkingTexture(_sourceSprite);

                }
            }
            else if (_workingTexture != null)
            {
                EditorGUILayout.BeginHorizontal();
                previewAlpha = EditorGUILayout.Slider("Preview Alpha:", previewAlpha, 0, 1);
                _previewContrast = EditorGUILayout.Slider("Preview Contrast", _previewContrast,0,10);
                EditorGUILayout.EndHorizontal();
            }


            EditorGUILayout.EndVertical();
        }

        private Texture2D SpriteGetOffsetTexture(Sprite sourceSprite)
        {
            var sourceSpritePath = AssetDatabase.GetAssetPath(sourceSprite);
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(sourceSpritePath);

            if (importer == null) return null;

            var secondarySpriteTextures = importer.secondarySpriteTextures;

            if (secondarySpriteTextures == null) return null;

            foreach (var secTex in secondarySpriteTextures)
            {
                if (secTex.name == "_OffsetMap")
                {
                    return secTex.texture;
                }
            }

            return null;
        }

        private bool SpriteSetOffsetTexture(Sprite sourceSprite,Texture2D offsetTexture)
        {
            var sourceSpritePath = AssetDatabase.GetAssetPath(sourceSprite);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(sourceSpritePath);

            if (importer == null) return false;

            var secondarySpriteTextures = importer.secondarySpriteTextures;

            if (secondarySpriteTextures == null) return false;

            int index = -1;
            for (int i = 0; i < secondarySpriteTextures.Length; i++)
            {
                var secTex = secondarySpriteTextures[i];
                if (secTex.name == "_OffsetMap")
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                var newSecondarySpriteTexture = new SecondarySpriteTexture
                {
                    name = "_OffsetMap",
                    texture = offsetTexture
                };

                var secondarySpriteTextureList = new List<SecondarySpriteTexture>(secondarySpriteTextures);
                secondarySpriteTextureList.Add(newSecondarySpriteTexture);

                importer.secondarySpriteTextures = secondarySpriteTextureList.ToArray();
            }
            else
            {
                secondarySpriteTextures[index].texture = offsetTexture;
                importer.secondarySpriteTextures = secondarySpriteTextures;
            }

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            AssetDatabase.ImportAsset(sourceSpritePath,ImportAssetOptions.ForceUpdate);
            return true;
        }

        private Texture2D GenerateNewWorkingTexture(Sprite sourceSprite)
        {
            if (AssertAssetFolder("Assets/Textures/ShadowOffsetMaps"))
            {
                var sourceTexture = sourceSprite.texture;

                var newTexture = new Texture2D(sourceTexture.width, sourceTexture.height);

                var pixels = newTexture.GetPixels();
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color(0.5f, 0f, 0.5f, 1);
                }

                newTexture.SetPixels(pixels);
                newTexture.filterMode = FilterMode.Point;
                newTexture.wrapMode = TextureWrapMode.Clamp;
                newTexture.Apply();

                var newPath = AssetDatabase.GenerateUniqueAssetPath("Assets/Textures/ShadowOffsetMaps/SOM_" +
                                                                    sourceTexture.name + ".renderTexture");
                AssetDatabase.CreateAsset(newTexture, newPath);

                if (SpriteSetOffsetTexture(sourceSprite, newTexture)) return newTexture;

                EditorUtility.DisplayDialog("Cannot Generate Working Texture!",
                    "Failed to add offset map to this asset.\n\n" +
                    "This can happen with built in assets. " +
                    "Please ensure that the asset you are editing" +
                    " can be opened in the Sprite Editor.",
                    "Ok");

                AssetDatabase.DeleteAsset(newPath);

                return null;
            }

            return null;
        }

        private bool AssertAssetFolder(string path)
        {
            try
            {
                if (AssetDatabase.IsValidFolder(path)) return true;

                List<string> pathElements = new List<string>(path.Split('/'));

                if (pathElements.Count <= 0)
                {
                    throw new Exception("Path must not be empty");
                }

                if (pathElements[0] != "Assets")
                {
                    pathElements.Insert(0,"Assets");
                }

                string currentString = pathElements[0];

                for (int i = 1; i < pathElements.Count; i++)
                {
                    string nextString = pathElements[i];
                    if (!AssetDatabase.IsValidFolder(currentString + "/" + nextString))
                    {
                        AssetDatabase.CreateFolder(currentString, nextString);
                    }
                    currentString += "/" + nextString;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return AssetDatabase.IsValidFolder(path);
        }

        /// <summary>
        /// Draw the workspace for painting textures
        /// </summary>
        private void DrawWorkspaceViewport()
        {
            _viewSize = (position.size / _scaling) - ((Vector2.up * HeaderSize) / _scaling);

            const float unitSize = 1f;

            DrawWorkspaceGrid(16);

            var xx = WORK_SPACE_CENTER.x;
            var yy = WORK_SPACE_CENTER.y;

            EditorGUI.DrawTextureTransparent(new Rect(xx, yy, _sourceTexture.width, _sourceTexture.height),_sourceTexture);


            UpdatePreviewTexture();
            previewMaterial.color = new Color(1, 1, 1, previewAlpha);
            EditorGUI.DrawPreviewTexture(new Rect(xx, yy, _previewTexture.width, _previewTexture.height),_previewTexture,previewMaterial);

            if (selectedTool != 3)
            {
                float aa1 = 0.75f;
                float aa2 = 0.2f;
                EditorGUI.DrawRect(new Rect(xx + xLockPosition, viewPosition.y, unitSize, viewSize.y),
                    new Color(0, 1, 0, lockXTarget ? aa1 : aa2));
                EditorGUI.DrawRect(new Rect(viewPosition.x, yy + yLockPosition, viewSize.x, unitSize),
                    new Color(1, 0, 0, lockYTarget ? aa1 : aa2));
            }

            DrawCursor();
        }

        /// <summary>
        /// Converts a Vector2 to a Vector2Int
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private Vector2Int Vector2ToInt(Vector2 v)
        {
            return new Vector2Int((int)v.x, (int)v.y);
        }

        /// <summary>
        /// Handles the drawing the cursor for the various tools
        /// </summary>
        private void DrawCursor()
        {
            _cursorPos = _mousePosition - ((Vector2.up * HeaderSize) / _scaling);
            _cursorPos.x = Mathf.Floor(_cursorPos.x );
            _cursorPos.y = Mathf.Floor(_cursorPos.y - 0.5f);
            _imageCursorPos = Vector2ToInt(GetPositionOnImage(_cursorPos));

            _cursorSize.x = Mathf.Max(_cursorSize.x, 1);
            _cursorSize.y = Mathf.Max(_cursorSize.y, 1);

            var c = Vector2ToInt(WORK_SPACE_CENTER);
            var p1 = _imageCursorPosStart + c;
            var p2 = _imageCursorPos + c;

            var minX = p2.x;
            var minY = p2.y;
            var maxX = p2.x + 1;
            var maxY = p2.y + 1;

            if (selectedTool == 0)
            {
                p1 = Vector2ToInt(_cursorPos);
                p2 = Vector2ToInt(_cursorPos) + _cursorSize;

                minX = Mathf.Min(p1.x, p2.x);
                minY = Mathf.Min(p1.y, p2.y);
                maxX = Mathf.Max(p1.x, p2.x);
                maxY = Mathf.Max(p1.y, p2.y);
            }

            if (_mouseIsHeld && selectedTool != 0)
            {
                minX = Mathf.Min(p1.x, p2.x);
                minY = Mathf.Min(p1.y, p2.y);
                maxX = Mathf.Max(p1.x, p2.x) + 1;
                maxY = Mathf.Max(p1.y, p2.y) + 1;
            }

            const float cursorAlpha = 0.25f;
            switch (selectedTool)
            {
                case 0:
                    EditorGUI.DrawRect(new Rect(_cursorPos.x,_cursorPos.y,_cursorSize.x,_cursorSize.y),new Color(1,1,1,cursorAlpha));
                    break;

                case 1:
                    if(_mouseIsHeld)
                    {
                        var points = CalculateLinePoints(_imageCursorPosStart + c, _imageCursorPos + c);

                        foreach (var point in points)
                        {
                            EditorGUI.DrawRect(new Rect(point.x,point.y,1,1),new Color(1,1,1,cursorAlpha));
                        }
                    }
                    break;

                case 2:
                case 4:
                    if(_mouseIsHeld)
                    {
                        EditorGUI.DrawRect(new Rect(minX,minY,(maxX - minX),(maxY - minY)),new Color(1,1,1,cursorAlpha));
                    }
                    break;

                case 3:

                    var t1 = remapTarget1 + c;
                    var t2 = remapTarget2 + c;

                    var targetPoints = CalculateLinePoints(t1, t2);

                    for (int i = 0; i < targetPoints.Length; i++)
                    {
                        var col = Color.Lerp(Color.green, Color.red, (float )i / (targetPoints.Length - 1));
                        var point = targetPoints[i];
                        EditorGUI.DrawRect(new Rect(point.x,point.y,_cursorSize.x,_cursorSize.y),col);
                    }

                    GUI.DrawTexture(new Rect(t1.x - 0.5f, t1.y - 0.5f, 2, 2),
                        Shadow2DOffsetMapEditorContent.CursorIconTarget, ScaleMode.ScaleToFit, true);
                    GUI.DrawTexture(new Rect(t2.x - 0.5f, t2.y - 0.5f, 2, 2),
                        Shadow2DOffsetMapEditorContent.CursorIconTarget, ScaleMode.ScaleToFit, true);


                    if(_mouseIsHeld && !_isMovingRemapTargets)
                    {
                        var points = CalculateLinePoints(_imageCursorPosStart + c, t1);

                        foreach (var point in points)
                        {
                            EditorGUI.DrawRect(new Rect(point.x,point.y,_cursorSize.x,_cursorSize.y),new Color(1,1,0,cursorAlpha));
                        }

                        points = CalculateLinePoints(_imageCursorPos + c, t2);

                        foreach (var point in points)
                        {
                            EditorGUI.DrawRect(new Rect(point.x,point.y,_cursorSize.x,_cursorSize.y),new Color(1,1,0,cursorAlpha));
                        }


                        points = CalculateLinePoints(_imageCursorPosStart + c, _imageCursorPos + c);

                        foreach (var point in points)
                        {
                            EditorGUI.DrawRect(new Rect(point.x,point.y,_cursorSize.x,_cursorSize.y),new Color(1,1,1,cursorAlpha));
                        }
                    }
                    break;
            }


            DrawCursorCorner(minX, minY, Shadow2DOffsetMapEditorContent.CursorIconTL);
            DrawCursorCorner(maxX, minY, Shadow2DOffsetMapEditorContent.CursorIconTR);
            DrawCursorCorner(minX, maxY, Shadow2DOffsetMapEditorContent.CursorIconBL);
            DrawCursorCorner(maxX, maxY, Shadow2DOffsetMapEditorContent.CursorIconBR);

            if (selectedTool != 3)
            {
                var targetPoint = _cursorPos + brushTargetOffset + new Vector2(0.5f, 0.5f);

                GUI.DrawTexture(new Rect(targetPoint.x - 1f, targetPoint.y - 1f, 2, 2),
                    Shadow2DOffsetMapEditorContent.CursorIconTarget, ScaleMode.ScaleToFit, true);
            }
        }

        /// <summary>
        /// draw a single corner of the selection cursor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="texture"></param>
        private void DrawCursorCorner(float x, float y,Texture texture)
        {
            GUI.DrawTexture(new Rect(x - 0.5f, y - 0.5f, 1, 1), texture,ScaleMode.ScaleToFit,true);
        }

        /// <summary>
        /// Draw a grid in the background of the workspace
        /// </summary>
        /// <param name="pixelsPerLine"></param>
        private void DrawWorkspaceGrid(int pixelsPerLine)
        {
            var size = FloorVector2(_viewSize);

            if (_scaling > 3)
            {
                pixelsPerLine /= 2;
            }

            if (_scaling > 6)
            {
                pixelsPerLine /= 2;
            }

            const float ww = 1f;
            float aa1 = 0.1f;
            float aa2 = _scaling < 1.5f ? 0 : 0.02f;
            for (int i = 0; i < size.x; i++)
            {
                int xPos = Mathf.FloorToInt(viewPosition.x) + i;

                var aa = xPos %(pixelsPerLine * 4) == 0 ? aa1 : aa2;

                if (xPos % pixelsPerLine == 0)
                {
                    EditorGUI.DrawRect(new Rect(xPos,viewPosition.y,ww,viewSize.y),new Color(1,1,1,aa));
                }
            }

            for (int i = 0; i < size.y; i++)
            {
                int yPos = Mathf.FloorToInt(viewPosition.y) + i;

                var aa = yPos %(pixelsPerLine * 4) == 0 ? aa1 : aa2;

                if (yPos % pixelsPerLine == 0)
                {
                    EditorGUI.DrawRect(new Rect(viewPosition.x,yPos,viewSize.x,ww),new Color(1,1,1,aa));
                }
            }
        }

        /// <summary>
        /// Update the workspace camera's position based on mouse input
        /// </summary>
        /// <param name="e"></param>
        private void ProcessMiddleMouseDrag(Event e)
        {
            if (e == null || e.type != EventType.MouseDrag)
                return;
            if (e.button == 2)
            {
                _scrollPosition -= e.delta;
                e.Use();
            }
        }

        /// <summary>
        /// Update the workspace camera's zoom level based on mouse wheel input
        /// </summary>
        /// <param name="e"></param>
        private void ProcessScrollWheel(Event e)
        {
            if (e == null || e.type != EventType.ScrollWheel)
                return;
            if (e.control)
            {
                float shiftMultiplier = e.shift ? 8 : 4;

                _scaling = Mathf.Clamp(_scaling - e.delta.y * 0.01f * shiftMultiplier, 1, 16f);

                e.Use();
            }
        }

        /// <summary>
        /// Update lock position values using keyboard input
        /// </summary>
        /// <param name="e"></param>
        private void ProcessCoordLocks(Event e)
        {
            if (e.isKey && e.type == EventType.KeyDown && e.control)
            {
                switch(e.keyCode)
                {
                    case KeyCode.A:
                        lockXTarget = !lockXTarget;
                        break;

                    case KeyCode.S:
                        lockYTarget = !lockYTarget;
                        break;

                    case KeyCode.Q:
                        xLockPosition = Mathf.FloorToInt(_imageCursorPos.x);
                        break;

                    case KeyCode.W:
                        yLockPosition = Mathf.FloorToInt(_imageCursorPos.y);
                        break;

                    case KeyCode.UpArrow:
                        brushOffset.y -= 1;
                        break;

                    case KeyCode.DownArrow:
                        brushOffset.y += 1;
                        break;

                    case KeyCode.LeftArrow:
                        brushOffset.x -= 1;
                        break;

                    case KeyCode.RightArrow:
                        brushOffset.x += 1;
                        break;

                    case KeyCode.Alpha1:
                        selectedTool = 0;
                        break;

                    case KeyCode.Alpha2:
                        selectedTool = 1;
                        break;

                    case KeyCode.Alpha3:
                        selectedTool = 2;
                        break;

                    default:
                        return;
                }

                e.Use();
            }
        }

        /// <summary>
        /// Draw the tool bar that exists at the top of the workspace
        /// </summary>
        private void DrawToolBar()
        {
            GUILayout.BeginArea(new Rect(4, 2, position.width, 80));

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            selectedTool = GUILayout.SelectionGrid(selectedTool, new[]
            {
                new GUIContent(Shadow2DOffsetMapEditorContent.ToolIconFree, "Free draw tool"),
                new GUIContent(Shadow2DOffsetMapEditorContent.ToolIconLine, "Line tool"),
                new GUIContent(Shadow2DOffsetMapEditorContent.ToolIconRect, "Rectangle tool"),
                new GUIContent(Shadow2DOffsetMapEditorContent.ToolIconRemap, "Remap tool"),
                new GUIContent(Shadow2DOffsetMapEditorContent.ToolIconNoise, "Noise tool"),
            }, 5);

            EditorGUILayout.Space(20);

            if(GUILayout.Button(new GUIContent(Shadow2DOffsetMapEditorContent.ToolIconUndo, "UNDO")))
            {
                HandleUndo();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (selectedTool == 0)
            {
                _cursorSize = EditorGUILayout.Vector2IntField("Brush Size", _cursorSize);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUILayout.EndArea();
        }

        /// <summary>
        /// Draw the generic scroll view with scaling
        /// </summary>
        private void DrawWorkspace()
        {
            Event e = Event.current;
            ProcessMiddleMouseDrag(e);

           var center = viewCenter;

            ProcessScrollWheel(e);

            ProcessCoordLocks(e);

            ScaleWindowGroup();

            EditorGUILayout.BeginScrollView(_scrollPosition, false, false);

            ScaleScrollGroup();
            Matrix4x4 old = GUI.matrix;
            Matrix4x4 translation = Matrix4x4.TRS(new Vector3(0, BaseY, 1), Quaternion.identity, Vector3.one);
            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(_scaling, _scaling, _scaling));
            GUI.matrix = translation * scale * translation.inverse;

            //Start Drawing Content
            GUILayout.BeginArea(new Rect(0, 0, MaxGraphSize * _scaling, MaxGraphSize * _scaling));

            DrawWorkspaceViewport();

            GUILayout.EndArea();
            //End Drawing Content

            //Reset the view matrix to it's previous state
            GUI.matrix = old;

            EditorGUILayout.EndScrollView();

            viewCenter = center;

            DrawToolBar();
        }

        private void ScaleWindowGroup()
        {
            GUI.EndGroup();
            CalculateScaledWindowRect();
            GUI.BeginGroup(_groupRect);
        }

        private void CalculateScaledWindowRect()
        {
            _groupRect.x = 0;
            _groupRect.y = BaseY;
            _groupRect.width = (MaxGraphSize + _scrollPosition.x) / _scaling;
            _groupRect.height = (MaxGraphSize + _scrollPosition.y - _groupRect.y) / _scaling;
        }

        private void ScaleScrollGroup()
        {
            GUI.EndGroup();
            CalculateScaledScrollRect();
            GUI.BeginGroup(_groupRect);
        }

        private void CalculateScaledScrollRect()
        {
            _groupRect.x = -_scrollPosition.x / _scaling;
            _groupRect.y = -_scrollPosition.y / _scaling;
            _groupRect.width = (position.width + _scrollPosition.x) / _scaling;
            _groupRect.height = (position.height + _scrollPosition.y) / _scaling;
        }
    }
}
