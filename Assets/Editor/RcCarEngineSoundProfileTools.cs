#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// -----------------------------------------------------------------------------
// 编辑器工具：把 RcCarEngineSoundProfile 当前参数渲染成 .wav 资产。
// · 在 Project 视图选中一份 RcCarEngineSoundProfile 资产 → 右键 → Mini RC/Generate Engine Loop WAV
// · 输出 16-bit PCM 单声道 wav，写到 SO 同目录；命名按 profile.clipName，不重名
// · 写完调 AssetDatabase.Refresh，Unity 自动作为 AudioClip 导入；
//   再把它拖进车 prefab 的 RcCarEngineAudio.engineLoopClip 字段就生效
// · Unity 不能写 mp3（许可问题）；wav 在打包时会按 Audio Importer 自动压缩成 Vorbis，
//   最终包体大小与 mp3 同档次，对玩家无差别
// -----------------------------------------------------------------------------

public static class RcCarEngineSoundProfileTools
{
    const string MenuPath = "Assets/Mini RC/Generate Engine Loop WAV";

    [MenuItem(MenuPath, true)]
    static bool ValidateGenerate()
    {
        return Selection.activeObject is RcCarEngineSoundProfile;
    }

    [MenuItem(MenuPath)]
    static void Generate()
    {
        var profile = Selection.activeObject as RcCarEngineSoundProfile;
        if (profile == null)
        {
            return;
        }

        string profilePath = AssetDatabase.GetAssetPath(profile);
        string dir = string.IsNullOrEmpty(profilePath) ? "Assets" : Path.GetDirectoryName(profilePath);
        string baseName = string.IsNullOrEmpty(profile.clipName) ? profile.name : profile.clipName;
        string wavPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(dir, baseName + ".wav"));

        int rate = Mathf.Max(8000, profile.sampleRate);
        int sampleCount = Mathf.Max(8, Mathf.RoundToInt(rate * Mathf.Max(0.1f, profile.lengthSeconds)));
        float[] samples = new float[sampleCount];
        profile.FillSamples(samples, rate);

        WriteWav16BitMono(wavPath, samples, rate);

        AssetDatabase.Refresh();
        Debug.Log($"[RcCarEngineSoundProfileTools] Generated WAV: {wavPath}");

        // 让 Unity 在 Project 里高亮这份新资产，方便用户立即拖到 prefab 上
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(wavPath);
        if (clip != null)
        {
            EditorGUIUtility.PingObject(clip);
            Selection.activeObject = clip;
        }
    }

    /// <summary>16-bit PCM 单声道 wav 写盘；标准 RIFF/WAVE 头。</summary>
    static void WriteWav16BitMono(string path, float[] samples, int sampleRate)
    {
        const short channels = 1;
        const short bitsPerSample = 16;
        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        short blockAlign = (short)(channels * (bitsPerSample / 8));
        int dataLen = samples.Length * (bitsPerSample / 8);

        using (var fs = File.Create(path))
        using (var w = new BinaryWriter(fs))
        {
            // RIFF header
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + dataLen);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk（PCM）
            w.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            w.Write(16);
            w.Write((short)1); // PCM
            w.Write(channels);
            w.Write(sampleRate);
            w.Write(byteRate);
            w.Write(blockAlign);
            w.Write(bitsPerSample);

            // data chunk
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            w.Write(dataLen);

            for (int i = 0; i < samples.Length; i++)
            {
                // [-1,1] → 16-bit signed；越界做硬钳位
                int v = Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                if (v > short.MaxValue)
                {
                    v = short.MaxValue;
                }
                else if (v < short.MinValue)
                {
                    v = short.MinValue;
                }
                w.Write((short)v);
            }
        }
    }
}
#endif
