using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GunplayHUD
{
    // Change only the HUD component; weapon/ADS FSMs retain control of active state.
    internal static class CrosshairController
    {
        private static Transform _dot, _cross;
        private static Vector3 _dotScale, _crossScale;
        private static Image _image;
        private static RawImage _raw;
        private static Sprite _originalSprite, _originalOverride, _linesSprite, _dotSprite;
        private static Texture _originalTexture;
        private static Texture2D _linesTexture, _dotTexture;
        private static Rect _originalUv;
        private static bool _originalAspect, _assetsAttempted, _unsupportedWarned;
        private static float _nextLook;
        private static int _appliedMode = -1;

        internal static void Tick()
        {
            if ((_dot == null || _cross == null) && Time.unscaledTime >= _nextLook)
            {
                _nextLook = Time.unscaledTime + 2f;
                bool found = false;
                foreach (var sceneIndex in SceneIndices())
                {
                    foreach (var root in SceneManager.GetSceneAt(sceneIndex).GetRootGameObjects())
                    {
                        if (root.name != "Canvas") continue;
                        var dot = root.transform.Find("MousePoint");
                        var cross = root.transform.Find("MouseCrosshair");
                        if (dot == null && cross == null) continue;
                        Attach(dot, cross);
                        found = true;
                        break;
                    }
                    if (found) break;
                }
                if (_dot == null && _cross == null)
                {
                    var dot = GameObject.Find("MousePoint");
                    if (dot != null && dot.transform.parent != null) Attach(dot.transform, dot.transform.parent.Find("MouseCrosshair"));
                }
            }
            if (_dot != null) _dot.localScale = Plugin.SelectionDot.Value ? _dotScale : Vector3.zero;
            if (_cross == null) return;
            int mode = Plugin.Crosshair.Value;
            _cross.localScale = mode == 0 ? Vector3.zero : mode == 3 ? _crossScale * 0.5f : _crossScale;
            if (_appliedMode == mode) return;
            if (mode >= 2) LoadAssets();
            if (_image != null)
            {
                _image.sprite = mode == 2 && _linesSprite != null ? _linesSprite : mode == 3 && _dotSprite != null ? _dotSprite : _originalSprite;
                _image.overrideSprite = mode >= 2 ? null : _originalOverride;
                _image.preserveAspect = mode >= 2 || _originalAspect;
            }
            else if (_raw != null)
            {
                _raw.texture = mode == 2 && _linesTexture != null ? _linesTexture : mode == 3 && _dotTexture != null ? _dotTexture : _originalTexture;
                _raw.uvRect = mode >= 2 ? new Rect(0, 0, 1, 1) : _originalUv;
            }
            else if (mode >= 2 && !_unsupportedWarned)
            { Plugin.Log.LogWarning("Weapon crosshair has no Image/RawImage component; keeping its vanilla graphic."); _unsupportedWarned = true; }
            _appliedMode = mode;
        }

        private static System.Collections.Generic.IEnumerable<int> SceneIndices()
        { for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isLoaded) yield return i; }

        private static void Attach(Transform dot, Transform cross)
        {
            if (_dot != dot)
            {
                if (_dot != null) _dot.localScale = _dotScale;
                _dot = dot;
                if (dot != null) _dotScale = dot.localScale == Vector3.zero ? Vector3.one : dot.localScale;
            }
            if (_cross == cross) return;
            RestoreCross(); _cross = cross; _appliedMode = -1; _unsupportedWarned = false;
            _image = cross != null ? cross.GetComponent<Image>() : null;
            _raw = cross != null ? cross.GetComponent<RawImage>() : null;
            if (cross != null) _crossScale = cross.localScale == Vector3.zero ? Vector3.one : cross.localScale;
            if (_image != null) { _originalSprite = _image.sprite; _originalOverride = _image.overrideSprite; _originalAspect = _image.preserveAspect; }
            if (_raw != null) { _originalTexture = _raw.texture; _originalUv = _raw.uvRect; }
        }
        private static void RestoreCross()
        {
            if (_cross != null) _cross.localScale = _crossScale;
            if (_image != null) { _image.sprite = _originalSprite; _image.overrideSprite = _originalOverride; _image.preserveAspect = _originalAspect; }
            if (_raw != null) { _raw.texture = _originalTexture; _raw.uvRect = _originalUv; }
        }
        internal static void OnSceneLoaded()
        {
            if (_dot != null) _dot.localScale = _dotScale;
            RestoreCross(); _dot = null; _cross = null; _image = null; _raw = null; _nextLook = 0f; _appliedMode = -1;
        }
        private static void LoadAssets()
        {
            if (_assetsAttempted) return;
            _assetsAttempted = true;
            Load("crosshair_lines.png", out _linesTexture, out _linesSprite);
            Load("crosshair_dot.png", out _dotTexture, out _dotSprite);
        }
        private static void Load(string name, out Texture2D texture, out Sprite sprite)
        {
            texture = null; sprite = null;
            try
            {
                var bytes = File.ReadAllBytes(Path.Combine(Plugin.Dir, "Textures/" + name));
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = name, hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
                if (!ImageConversion.LoadImage(texture, bytes)) throw new InvalidDataException("Unreadable PNG");
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                sprite.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (Exception e)
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                texture = null; Plugin.Log.LogWarning("Crosshair asset " + name + ": " + e.Message);
            }
        }
    }
}
