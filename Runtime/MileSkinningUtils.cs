using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

public class MileSkinningUtils
{
    public static Texture2D CreateTexture2D(TextAsset textAsset, GPUSkinningAnimation animation)
    {
        if (textAsset == null || animation == null)
        {
            return null;
        }

        Texture2D texture2D = new Texture2D(animation.textureWidth, animation.textureHeight, TextureFormat.RGBAHalf, false, true);
        texture2D.name = "GPUSkinningTextureMatrix";
        texture2D.filterMode = FilterMode.Point;
        texture2D.LoadRawTextureData(textAsset.bytes);
        texture2D.Apply(false, true);
        return texture2D;
    }

    public static void MarkAllScenesDirty()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.CallbackFunction DelayCall = null;
            DelayCall = () =>
            {
                UnityEditor.EditorApplication.delayCall -= DelayCall;
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            };
            UnityEditor.EditorApplication.delayCall += DelayCall;
        }
#endif
    }

    public static string BoneHierarchyPath(GPUSkinningBone[] bones, int boneIndex)
    {
        if (bones == null || boneIndex < 0 || boneIndex >= bones.Length)
        {
            return null;
        }

        GPUSkinningBone bone = bones[boneIndex];
        string path = bone.name;
        while (bone.parentBoneIndex != -1)
        {
            bone = bones[bone.parentBoneIndex];
            path = bone.name + "/" + path;
        }
        return path;
    }

    public static string MD5(string input)
    {
        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        byte[] bytValue, bytHash;
        bytValue = System.Text.Encoding.UTF8.GetBytes(input);
        bytHash = md5.ComputeHash(bytValue);
        md5.Clear();
        string sTemp = string.Empty;
        for (int i = 0; i < bytHash.Length; i++)
        {
            sTemp += bytHash[i].ToString("X").PadLeft(2, '0');
        }
        return sTemp.ToLower();
    }

    public static int NormalizeTimeToFrameIndex(GPUSkinningClip clip, float normalizedTime)
    {
        if (clip == null)
        {
            return 0;
        }

        normalizedTime = Mathf.Clamp01(normalizedTime);
        return (int)(normalizedTime * (clip.length * clip.fps - 1));
    }

    public static float FrameIndexToNormalizedTime(GPUSkinningClip clip, int frameIndex)
    {
        if (clip == null)
        {
            return 0;
        }

        int totalFrams = (int)(clip.fps * clip.length);
        frameIndex = Mathf.Clamp(frameIndex, 0, totalFrams - 1);
        return (float)frameIndex / (float)(totalFrams - 1);
    }

}
