using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace GunplayHUD
{
    internal static class HeadshotAudio
    {
        private static readonly HeadshotSoundGate Gate = new HeadshotSoundGate();
        private static readonly AudioSource[] Voices = new AudioSource[2];
        private static AudioClip _clip;
        private static bool _attempted;
        internal static void Tick(Runner runner)
        {
            if (!Plugin.HudEnabled.Value || !Plugin.HeadshotSound.Value) { Stop(); return; }
            for (int i = 0; i < Voices.Length; i++)
                if (Voices[i] == null)
                {
                    var source = runner.gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;
                    source.dopplerLevel = 0f; source.clip = _clip; Voices[i] = source;
                }
            if (!_attempted) { _attempted = true; runner.StartCoroutine(Load()); }
        }
        private static IEnumerator Load()
        {
            string path = Path.Combine(Plugin.Dir, "Sounds/headshot.mp3");
            if (!File.Exists(path)) { Plugin.Log.LogWarning("Headshot sound missing: " + path); yield break; }
            using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.MPEG))
            {
                var handler = (DownloadHandlerAudioClip)request.downloadHandler;
                handler.compressed = false; handler.streamAudio = false;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                { Plugin.Log.LogWarning("Headshot sound: " + request.error); yield break; }
                _clip = DownloadHandlerAudioClip.GetContent(request);
                if (_clip == null) { Plugin.Log.LogWarning("Headshot sound could not be decoded."); yield break; }
                // AudioSource.volume clamps to 1, so boost decoded samples once instead.
                var samples = new float[_clip.samples * _clip.channels];
                if (_clip.GetData(samples, 0))
                {
                    for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Clamp(samples[i] * 1.3f, -1f, 1f);
                    if (!_clip.SetData(samples, 0)) Plugin.Log.LogWarning("Headshot sound gain could not be applied.");
                }
                else Plugin.Log.LogWarning("Headshot sound samples unavailable; using original volume.");
                _clip.hideFlags = HideFlags.HideAndDontSave;
                foreach (var voice in Voices) if (voice != null) voice.clip = _clip;
            }
        }
        internal static void Play()
        {
            if (_clip == null || AudioListener.pause || Time.timeScale == 0f) return;
            int index = Gate.Acquire(Time.unscaledTime, _clip.length);
            if (index < 0 || Voices[index] == null || Voices[index].isPlaying) return;
            Voices[index].Play();
        }
        internal static void Stop()
        { foreach (var voice in Voices) if (voice != null) voice.Stop(); Gate.Reset(); }
    }
}
