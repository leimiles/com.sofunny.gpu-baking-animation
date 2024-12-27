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
    [HideInInspector][SerializeField] string animName = null;
    [HideInInspector][SerializeField] Transform rootBone;
    [HideInInspector][SerializeField] public AnimationClip[] animClips;
    [HideInInspector][SerializeField] int[] fpsList;
    [HideInInspector][SerializeField] GPUSkinningAnimation gpuSkinningAnimation_old;
    [HideInInspector][System.NonSerialized] public bool isSampling = false;
    [HideInInspector][System.NonSerialized] public AnimationClip animClip;
    [HideInInspector][System.NonSerialized] public int samplingIndex = -1;
    [HideInInspector][System.NonSerialized] public int samplingFramesTotal = 0;
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
        int framesTotal = (int)(gpuSkinningClip.length * gpuSkinningClip.fps);
        samplingFramesTotal = framesTotal;

        Debug.Log("sampling frames total: " + framesTotal);

        //ShowDialog("wrong");
    }

    public void ResetSampleIndex()
    {
        samplingIndex = 0;
    }

    public bool IsSamplingProgress()
    {
        return samplingIndex != -1;
    }

    public void StartSample()
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

        if (gpuSkinningAnimation_old == null)
        {
            gpuSkinningAnimation_new.guid = System.Guid.NewGuid().ToString();
        }

        List<GPUSkinningBone> bones_result = new List<GPUSkinningBone>();
        CollectionBones(bones_result, skinnedMeshRenderer.bones, mesh.bindposes, null, rootBone, 0);        // start with rootbone via index of 0 one
        GPUSkinningBone[] bones_result_array = bones_result.ToArray();
        GenerateUniqueID(bones_result_array);
        Debug.Log(bones_result_array[1].childrenBonesIndices.Length);

        //isSampling = true;
    }

    public void EndSample()
    {
        samplingIndex = -1;
    }

    void GenerateUniqueID(GPUSkinningBone[] bones)
    {
        if (bones != null && bones.Length > 0)
        {
            for (int i = 0; i < bones.Length; ++i)
            {
                string hierarchyPathOfBone = MileSkinningUtils.BoneHierarchyPath(bones, i);
                string guid = MileSkinningUtils.MD5(hierarchyPathOfBone);
                bones[i].guid = guid;
            }
        }
    }

    /// <summary>
    /// transform a unity bindpose to GPUSkinningBone
    /// </summary>
    /// <param name="bones"></param>
    /// <param name="bones_Transforms"></param>
    void CollectionBones(List<GPUSkinningBone> gpuSkinningBones, Transform[] skinnedMeshRendererBones, Matrix4x4[] skinnedMeshBindPoses, GPUSkinningBone gpuSkinningBone_Parent, Transform currentBoneTransform, int currentBoneIndex)
    {
        GPUSkinningBone gpuSkinningBone_Current = new GPUSkinningBone();
        gpuSkinningBones.Add(gpuSkinningBone_Current);
        int indexOfBone = System.Array.IndexOf(skinnedMeshRendererBones, currentBoneTransform);

        gpuSkinningBone_Current.transform = currentBoneTransform;
        gpuSkinningBone_Current.name = currentBoneTransform.name;
        gpuSkinningBone_Current.bindpose = indexOfBone == -1 ? Matrix4x4.identity : skinnedMeshBindPoses[indexOfBone];
        gpuSkinningBone_Current.parentBoneIndex = gpuSkinningBone_Parent == null ? -1 : gpuSkinningBones.IndexOf(gpuSkinningBone_Parent);

        if (gpuSkinningBone_Parent != null)
        {
            gpuSkinningBone_Parent.childrenBonesIndices[currentBoneIndex] = gpuSkinningBones.IndexOf(gpuSkinningBone_Current);
        }

        int numOfChildrenBone = currentBoneTransform.childCount;
        if (numOfChildrenBone > 0)
        {
            gpuSkinningBone_Current.childrenBonesIndices = new int[numOfChildrenBone];

            for (int i = 0; i < numOfChildrenBone; ++i)
            {
                CollectionBones(gpuSkinningBones, skinnedMeshRendererBones, skinnedMeshBindPoses, gpuSkinningBone_Current, gpuSkinningBone_Current.transform.GetChild(i), i);
            }
        }

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

    public bool IsAnimator()
    {
        return animator != null;
    }

    static void ShowDialog(string msg)
    {
        EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
    }
#endif
}
