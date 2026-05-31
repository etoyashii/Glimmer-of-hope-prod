using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Générateur de waveform à partir d'AudioClips
    /// </summary>
    public class WaveformRenderer
    {
        //Cache : clé = InstanceID * 100000 + width * 100 + height
        private static readonly Dictionary<long, Texture2D> _cache = new Dictionary<long, Texture2D>();

        public static Texture2D Get(AudioClip clip, int width, int height)
        {
            if (clip == null) return null;

            long key = (long)clip.GetInstanceID() * 100000L + width * 100 + height;

            if (_cache.TryGetValue(key, out Texture2D cached) && cached != null)
                return cached;

            Texture2D tex = Generate(clip, width, height);
            if (tex != null) _cache[key] = tex;
            return tex;
        }

        public static bool IsStreaming(AudioClip clip)
        {
            if (clip == null) return false;

            string path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path)) return false;

            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return false;

            return importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming;
        }

        public static bool IsPreloadDisabled(AudioClip clip)
        {
            if (clip == null) return false;
            return !clip.preloadAudioData;
        }

        public static void Invalidate(int clipInstanceID)
        {
            var toRemove = new List<long>();
            foreach (long k  in _cache.Keys)
            {
                if (k / 100000L == clipInstanceID)
                {
                    toRemove.Add(k);
                }
            }
            foreach (long k in toRemove)
            {
                _cache.Remove(k);
            }
        }

        private static Texture2D Generate(AudioClip clip, int width, int height)
        {
            if (clip.loadState != AudioDataLoadState.Loaded)
                clip.LoadAudioData();

            float[] samples = new float[clip.samples * clip.channels];
            if (!clip.GetData(samples, 0))
            {
                string assetPath = AssetDatabase.GetAssetPath(clip);
                if (!string.IsNullOrEmpty(assetPath) && assetPath.ToLower().EndsWith(".wav"))
                    samples = ReadWavSamples(assetPath);
                if (samples == null) return null;
            }

            var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            Color[] pixels = new Color[width * height];

            Color bgColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            Color waveColor = new Color(0.28f, 0.65f, 1.0f, 1f);
            Color centerColor = new Color(0.30f, 0.30f, 0.30f, 1f);

            for (int i = 0; i < pixels.Length; ++i)
            {
                pixels[i] = bgColor;
            }

            int cy = height / 2;
            for (int x = 0; x < width; ++x)
            {
                pixels[cy * width + x] = centerColor;
            }

            int channels = clip.channels;
            int totalSamples = clip.samples;

            for (int x = 0; x < width; ++x)
            {
                int sStart = (int)((float)x / width * totalSamples);
                int sEnd = Mathf.Min((int)((float)(x + 1) / width * totalSamples), totalSamples - 1);

                float peak = 0f;
                for (int s = sStart; s <= sEnd; ++s)
                {
                    for (int c = 0; c < channels; ++c)
                    {
                        int idx = s * channels + c;
                        if (idx < samples.Length)
                        {
                            peak = Mathf.Max(peak, Mathf.Abs(samples[idx]));
                        }
                    }
                }

                int lineH = Mathf.RoundToInt(peak * (height / 2f - 1f));
                for (int y = cy - lineH; y <= cy + lineH; ++y)
                {
                    if (y >= 0 && y < height)
                    {
                        pixels[y * width + x] = waveColor;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static float[] ReadWavSamples(string filePath)
        {
            try
            {
                byte[] data = System.IO.File.ReadAllBytes(filePath);
                if (data.Length < 44) return null;

                if (System.Text.Encoding.ASCII.GetString(data, 0, 4) != "RIFF") return null;
                if (System.Text.Encoding.ASCII.GetString(data, 8, 4) != "WAVE") return null;

                int channels = 1;
                int bitsPerSample = 16;
                int dataOffset = -1;
                int dataSize = 0;
                int pos = 12;

                while(pos < data.Length - 8)
                {
                    string chunkID = System.Text.Encoding.ASCII.GetString(data, pos, 4);
                    int chunkSize = System.BitConverter.ToInt32(data, pos + 4);

                    if (chunkID == "fmt")
                    {
                        channels = System.BitConverter.ToInt16(data, pos + 10);
                        bitsPerSample = System.BitConverter.ToInt16(data, pos + 22);
                    }
                    else if (chunkID == "data")
                    {
                        dataOffset = pos + 8;
                        dataSize = chunkSize;
                        break;
                    }

                    pos += 8 + chunkSize;
                    if (chunkSize <= 0) break;
                }

                if (dataOffset < 0) return null;

                int bytesPerSample = bitsPerSample / 8;
                int sampleCount = dataSize / bytesPerSample;
                float[] samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; ++i)
                {
                    int offset = dataOffset + i * bytesPerSample;
                    if (offset + bytesPerSample > data.Length) break;

                    if (bitsPerSample == 16)
                    {
                        short s = System.BitConverter.ToInt16(data, offset);
                        samples[i] = s / 32768f;
                    }
                    else if (bitsPerSample == 24)
                    {
                        int s = data[offset] | (data[offset + 1] << 8) | ((sbyte)data[offset + 2] << 16);
                        samples[i] = s / 8388608f;
                    }
                    else if (bitsPerSample == 32)
                    {
                        samples[i] = System.BitConverter.ToSingle(data, offset);
                    }
                }

                return samples;
            }
            catch { return null; }
        }
    }
}
