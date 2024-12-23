using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GPUSkinningSampler2 : MonoBehaviour
{
#if UNITY_EDITOR
    [HideInInspector][SerializeField] string animName;
    [HideInInspector][SerializeField] Transform rootBone;
    [HideInInspector][SerializeField] AnimationClip[] animClips;
    [HideInInspector][SerializeField] int[] fpsList;
    [HideInInspector][SerializeField] GPUSkinningAnimation gpuSkinningAnimation_old;
    [HideInInspector][System.NonSerialized] bool isSampling = false;
    [HideInInspector][System.NonSerialized] AnimationClip animClip;
    [HideInInspector][System.NonSerialized] int samplingIndex = -1;
    Animator animator;
    RuntimeAnimatorController runtimeAnimatorController;
    SkinnedMeshRenderer skinnedMeshRenderer;
    GPUSkinningClip gpuSkinningClip;
    GPUSkinningAnimation gpuSkinningAnimation_new;



    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            DestroyImmediate(this);
            ShowDialog("cannot find animator component");
            return;
        }
        if (animator.runtimeAnimatorController == null || animator.runtimeAnimatorController is AnimatorOverrideController)
        {
            DestroyImmediate(this);
            ShowDialog("not compatable with AnimatorOverrideController");
            return;
        }
        runtimeAnimatorController = animator.runtimeAnimatorController;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        InitTransform();
        return;
    }

    void Update()
    {
        if (!isSampling)
        {
            return;
        }

        ShowDialog("wrong");
    }

    void ResetSampleIndex()
    {
        samplingIndex = 0;
    }

    void StartSample()
    {
        if (!SampleValidation())
        {
            return;
        }
        ResetSampleIndex();
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        // do i need to create new one?
        gpuSkinningAnimation_new = gpuSkinningAnimation_old == null ? ScriptableObject.CreateInstance<GPUSkinningAnimation>() : gpuSkinningAnimation_old;
        gpuSkinningAnimation_new.name = animName;

        if (gpuSkinningAnimation_old)
        {
            gpuSkinningAnimation_new.guid = System.Guid.NewGuid().ToString();
        }

        List<GPUSkinningBone> bones_result = new List<GPUSkinningBone>();
        //CollectionBones(bones_result, skinnedMeshRenderer.bones, mesh.bindposes, null, rootBone, 0);


        isSampling = true;
    }

    bool SampleValidation()
    {
        if (isSampling)
        {
            return false;
        }

        if (string.IsNullOrEmpty(animName.Trim()))
        {
            ShowDialog("animation name is empty");
            return false;
        }

        if (rootBone == null)
        {
            ShowDialog("root bone is not set");
            return false;
        }

        if (animClips == null || animClips.Length == 0)
        {
            ShowDialog("animation clips not set");
            return false;
        }

        animClip = animClips[samplingIndex];
        if (animClip == null)
        {
            isSampling = false;
            ShowDialog("animation clip not found");
            return false;
        }

        int numFrames = (int)(GetClipFPS(animClip, samplingIndex) * animClip.length);
        if (numFrames == 0)
        {
            isSampling = false;
            ShowDialog("frames not set");
            return false;
        }

        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            ShowDialog("cannot find SkinnedMeshRenderer component");
            return false;
        }

        if (skinnedMeshRenderer.sharedMesh == null)
        {
            ShowDialog("cannot find SkinnedMeshRenderer's mesh");
            return false;
        }

        return true;
    }

    void InitTransform()
    {
        transform.parent = null;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    /// <summary>
    /// return the framerate for this clip, the decimal part will be truncated
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="clipIndex"></param>
    /// <returns></returns>
    int GetClipFPS(AnimationClip clip, int clipIndex)
    {
        return fpsList[clipIndex] == 0 ? (int)clip.frameRate : fpsList[clipIndex];
    }

    static void ShowDialog(string msg)
    {
        EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
    }
#endif
}
