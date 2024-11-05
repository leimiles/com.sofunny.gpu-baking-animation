using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MileSkinningPlayer
{
    private delegate void OnAnimEvent(MileSkinningPlayer player, int eventId);
    GameObject gameObject;
    Transform transform;
    MileSkinningData mileSkinningData;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MaterialPropertyBlock materialPropertyBlock;
    float time = 0;
    float timeDiff = 0;
    private event OnAnimEvent onAnimEvent;
    GPUSkinningClip currentClip;
    GPUSkinningClip lastPlayedClip;
    GPUSkinningClip lastPlayingClip;
    int lastPlayingFrameIndex = -1;
    float lastPlayedTime = 0;
    //int rootMotionFrameIndex = -1;
    List<MileSkinningJoint> joints = null;
    private List<MileSkinningJoint> Joints
    {
        get
        {
            return joints;
        }
    }
    private GPUSkinningWrapMode WrapMode
    {
        get
        {
            return currentClip == null ? GPUSkinningWrapMode.Once : currentClip.wrapMode;
        }
    }
    bool visible = false;
    public bool Visible
    {
        get
        {
            return Application.isPlaying ? visible : true;
        }
        set
        {
            visible = value;
        }
    }
    MileSkinningCullingMode cullingMode = MileSkinningCullingMode.CullUpdateTransforms;
    public MileSkinningCullingMode CullingMode
    {
        get
        {
            return Application.isPlaying ? cullingMode : MileSkinningCullingMode.AlwaysAnimate;
        }
        set
        {
            cullingMode = value;
        }
    }
    public Vector3 Position
    {
        get
        {
            return transform == null ? Vector3.zero : transform.position;
        }
    }
    private bool isPlaying = false;
    public bool IsPlaying
    {
        get
        {
            return isPlaying;
        }
    }
    public bool IsTimeAtTheEndOfLoop
    {
        get
        {
            if (currentClip == null)
            {
                return false;
            }
            else
            {
                return GetFrameIndex() == ((int)(currentClip.length * currentClip.fps) - 1);
            }
        }
    }
    public float NormalizedTime
    {
        get
        {
            if (currentClip == null)
            {
                return 0;
            }
            else
            {
                return (float)GetFrameIndex() / (float)((int)(currentClip.length * currentClip.fps) - 1);
            }
        }
        set
        {
            if (currentClip != null)
            {
                float v = Mathf.Clamp01(value);
                if (WrapMode == GPUSkinningWrapMode.Once)
                {
                    this.time = v * currentClip.length;
                }
                else if (WrapMode == GPUSkinningWrapMode.Loop)
                {
                    if (currentClip.individualDifferenceEnabled)
                    {
                        mileSkinningData.Time = currentClip.length + v * currentClip.length - this.timeDiff;
                    }
                    else
                    {
                        mileSkinningData.Time = v * currentClip.length;
                    }
                }
                else
                {
                    throw new System.NotImplementedException();
                }
            }
        }
    }

    public void Resume()
    {
        if (currentClip != null)
        {
            isPlaying = true;
        }
    }

    public void Stop()
    {
        isPlaying = false;
    }

    private int GetFrameIndex()
    {
        float time = GetCurrentTime();
        if (currentClip.length == time)
        {
            return GetTheLastFrameIndex_WrapMode_Once(currentClip);
        }
        else
        {
            return GetFrameIndex_WrapMode_Loop(currentClip, time);
        }
    }

    private int GetTheLastFrameIndex_WrapMode_Once(GPUSkinningClip clip)
    {
        return (int)(clip.length * clip.fps) - 1;
    }

    private int GetFrameIndex_WrapMode_Loop(GPUSkinningClip clip, float time)
    {
        return (int)(time * clip.fps) % (int)(clip.length * clip.fps);
    }

    private float GetCurrentTime()
    {
        float time = 0;
        if (WrapMode == GPUSkinningWrapMode.Once)
        {
            time = this.time;
        }
        else if (WrapMode == GPUSkinningWrapMode.Loop)
        {
            time = mileSkinningData.Time + (currentClip.individualDifferenceEnabled ? this.timeDiff : 0);
        }
        else
        {
            throw new System.NotImplementedException();
        }
        return time;
    }

    MileSkinningCullingMode mileSkinningCullingMode = MileSkinningCullingMode.CullUpdateTransforms;
    public MileSkinningCullingMode MileSkinningCullingMode
    {
        get
        {
            return Application.isPlaying ? mileSkinningCullingMode : MileSkinningCullingMode.AlwaysAnimate;
        }
        set
        {
            mileSkinningCullingMode = value;
        }
    }
    public MileSkinningPlayer(GameObject gameObject, MileSkinningData data)
    {
        this.gameObject = gameObject;
        this.transform = this.gameObject.transform;
        mileSkinningData = data;
        meshRenderer = this.gameObject.GetComponent<MeshRenderer>();
        meshFilter = this.gameObject.GetComponent<MeshFilter>();

        MileSkinningMaterial skinningMaterial = GetCurrentMaterial();
        meshRenderer.sharedMaterial = skinningMaterial == null ? null : skinningMaterial.Material;
        meshFilter.sharedMesh = data.mesh;

        materialPropertyBlock = new MaterialPropertyBlock();
        //ConstructJoints();

    }

    private void ConstructJoints()
    {
        if (joints == null)
        {
            MileSkinningJoint[] existingJoints = gameObject.GetComponentsInChildren<MileSkinningJoint>();

            GPUSkinningBone[] bones = mileSkinningData.gpuSkinningAnimation.bones;
            int numBones = bones == null ? 0 : bones.Length;
            for (int i = 0; i < numBones; ++i)
            {
                GPUSkinningBone bone = bones[i];
                if (bone.isExposed)
                {
                    if (joints == null)
                    {
                        joints = new List<MileSkinningJoint>();
                    }

                    bool inTheExistingJoints = false;
                    if (existingJoints != null)
                    {
                        for (int j = 0; j < existingJoints.Length; ++j)
                        {
                            if (existingJoints[j] != null && existingJoints[j].BoneGUID == bone.guid)
                            {
                                if (existingJoints[j].BoneIndex != i)
                                {
                                    existingJoints[j].Init(i, bone.guid);
                                    MileSkinningUtils.MarkAllScenesDirty();
                                }
                                joints.Add(existingJoints[j]);
                                existingJoints[j] = null;
                                inTheExistingJoints = true;
                                break;
                            }
                        }
                    }

                    if (!inTheExistingJoints)
                    {
                        GameObject jointGo = new GameObject(bone.name);
                        jointGo.transform.parent = gameObject.transform;
                        jointGo.transform.localPosition = Vector3.zero;
                        jointGo.transform.localScale = Vector3.one;

                        MileSkinningJoint joint = jointGo.AddComponent<MileSkinningJoint>();
                        joints.Add(joint);
                        joint.Init(i, bone.guid);
                        MileSkinningUtils.MarkAllScenesDirty();
                    }
                }
            }

            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.CallbackFunction DelayCall = null;
                DelayCall = () =>
                {
                    UnityEditor.EditorApplication.delayCall -= DelayCall;
                    DeleteInvalidJoints(existingJoints);
                };
                UnityEditor.EditorApplication.delayCall += DelayCall;
#endif
            }
            else
            {
                DeleteInvalidJoints(existingJoints);
            }
        }
    }

    void DeleteInvalidJoints(MileSkinningJoint[] joints)
    {
        if (joints != null)
        {
            for (int i = 0; i < joints.Length; ++i)
            {
                if (joints[i] != null)
                {
                    for (int j = 0; j < joints[i].transform.childCount; ++j)
                    {
                        Transform child = joints[i].transform.GetChild(j);
                        child.parent = gameObject.transform;
                        child.localPosition = Vector3.zero;
                    }
                    Object.DestroyImmediate(joints[i].transform.gameObject);
                    MileSkinningUtils.MarkAllScenesDirty();
                }
            }
        }
    }

    void SetNewPlayingClip(GPUSkinningClip clip)
    {
        lastPlayedClip = currentClip;
        lastPlayedTime = GetCurrentTime();

        isPlaying = true;
        currentClip = clip;
        //rootMotionFrameIndex = -1;
        time = 0;
        timeDiff = Random.Range(0, currentClip.length);
    }

    public void Play(string clipName)
    {
        GPUSkinningClip[] clips = mileSkinningData.gpuSkinningAnimation.clips;
        int numClips = clips == null ? 0 : clips.Length;
        for (int i = 0; i < numClips; ++i)
        {
            if (clips[i].name == clipName)
            {
                if (currentClip != clips[i] || (currentClip != null && currentClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) || (currentClip != null && !isPlaying))
                {
                    SetNewPlayingClip(clips[i]);
                }
                return;
            }
        }
    }
    public void Play(int clipIndex)
    {
        GPUSkinningClip[] clips = mileSkinningData.gpuSkinningAnimation.clips;
        if (clipIndex >= 0 && clipIndex < clips.Length)
        {
            if (currentClip != clips[clipIndex] || (currentClip != clips[clipIndex] && currentClip != null && currentClip.wrapMode == GPUSkinningWrapMode.Once && IsTimeAtTheEndOfLoop) || (currentClip != null && !isPlaying))
            {
                SetNewPlayingClip(clips[clipIndex]);
            }
        }
    }

    public void Update(float timeDelta)
    {
        Update_Internal(timeDelta);
    }

    void Update_Internal(float timeDelta)
    {
        if (!isPlaying || currentClip == null)
        {
            return;
        }

        MileSkinningMaterial currentMaterial = GetCurrentMaterial();
        if (currentMaterial == null)
        {
            return;
        }

        if (meshRenderer.sharedMaterial != currentMaterial.Material)
        {
            meshRenderer.sharedMaterial = currentMaterial.Material;
        }
        if (currentClip.wrapMode == GPUSkinningWrapMode.Loop)
        {
            //Debug.Log("debug m");
            UpdateMaterial(timeDelta, currentMaterial);
        }
        else if (currentClip.wrapMode == GPUSkinningWrapMode.Once)
        {
            if (time >= currentClip.length)
            {
                time = currentClip.length;
                UpdateMaterial(timeDelta, currentMaterial);
            }
            else
            {
                UpdateMaterial(timeDelta, currentMaterial);
                time += timeDelta;
                if (time > currentClip.length)
                {
                    time = currentClip.length;
                }
            }
        }
        else
        {
            throw new System.NotImplementedException();
        }

        //crossFadeProgress += timeDelta;
        lastPlayedTime += timeDelta;
    }

    void UpdateMaterial(float deltaTime, MileSkinningMaterial currentMaterial)
    {
        int frameIndex = GetFrameIndex();
        if (lastPlayingClip == currentClip && lastPlayingFrameIndex == frameIndex)
        {
            mileSkinningData.Update(deltaTime, currentMaterial);
            return;
        }

        lastPlayingClip = currentClip;
        lastPlayingFrameIndex = frameIndex;
        GPUSkinningFrame frame = currentClip.frames[frameIndex];
        if (Visible || CullingMode == MileSkinningCullingMode.AlwaysAnimate)
        {
            mileSkinningData.Update(deltaTime, currentMaterial);
            mileSkinningData.UpdatePlayingData(materialPropertyBlock, currentClip, frameIndex);
            mileSkinningData.UpdateExtraProperty(materialPropertyBlock, extraProperty);
            meshRenderer.SetPropertyBlock(materialPropertyBlock);

        }
        UpdateEvents(currentClip, frameIndex);
    }
    Vector4 extraProperty;
    public Vector4 ExtraProperty
    {
        get
        {
            return extraProperty;
        }
        set
        {
            extraProperty = value;
        }
    }

    void UpdateEvents(GPUSkinningClip clip, int frameIndex)
    {
        UpdateClipEvent(clip, frameIndex);
    }

    void UpdateClipEvent(GPUSkinningClip clip, int frameIndex)
    {
        if (clip == null || clip.events == null || clip.events.Length == 0)
        {
            return;
        }

        GPUSkinningAnimEvent[] events = clip.events;
        int numEvents = events.Length;
        for (int i = 0; i < numEvents; ++i)
        {
            if (events[i].frameIndex == frameIndex && onAnimEvent != null)
            {
                onAnimEvent(this, events[i].eventId);
                break;
            }
        }
    }

    void UpdateJoints(GPUSkinningFrame frame)
    {
        if (joints == null)
        {
            return;
        }

        Matrix4x4[] matrices = frame.matrices;
        GPUSkinningBone[] bones = mileSkinningData.gpuSkinningAnimation.bones;
        int numJoints = joints.Count;
        for (int i = 0; i < numJoints; ++i)
        {
            MileSkinningJoint joint = joints[i];
            Transform jointTransform = Application.isPlaying ? joint.Transform : joint.transform;
            if (jointTransform != null)
            {
                // TODO: animation blend
                Matrix4x4 jointMatrix = frame.matrices[joint.BoneIndex] * bones[joint.BoneIndex].BindposeInv;
                jointTransform.localPosition = jointMatrix.MultiplyPoint(Vector3.zero);
                Vector3 jointDir = jointMatrix.MultiplyVector(Vector3.right);
                Quaternion jointRotation = Quaternion.FromToRotation(Vector3.right, jointDir);
                jointTransform.localRotation = jointRotation;
            }
            else
            {
                joints.RemoveAt(i);
                --i;
                --numJoints;
            }
        }
    }

    MileSkinningMaterial GetCurrentMaterial()
    {
        if (mileSkinningData == null)
        {
            return null;
        }

        return mileSkinningData.GetMaterial();

    }

#if UNITY_EDITOR
    public void Update_Editor(float timeDelta)
    {
        Update_Internal(timeDelta);
    }
#endif

}
