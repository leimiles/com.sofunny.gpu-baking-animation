using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GPUSkinningBone
{
    [System.NonSerialized]
    public Transform transform = null;

    public Matrix4x4 bindpose;

    public int parentBoneIndex = -1;

    public int[] childrenBonesIndices = null;

    [System.NonSerialized]
    public Matrix4x4 animationMatrix;

    public string name = null;

    public string guid = null;

    public bool isExposed = false;

    [System.NonSerialized]
    private bool bindposeInvInit = false;
    [System.NonSerialized]
    private Matrix4x4 bindposeInv;
    public Matrix4x4 BindposeInv
    {
        get
        {
            if (!bindposeInvInit)
            {
                bindposeInv = bindpose.inverse;
                bindposeInvInit = true;
            }
            return bindposeInv;
        }
    }
}

[System.Serializable]
public class GPUSkinningClip
{
    public string name = null;

    public float length = 0.0f;

    public int fps = 0;

    public GPUSkinningWrapMode wrapMode = GPUSkinningWrapMode.Once;

    public GPUSkinningFrame[] frames = null;

    public int pixelSegmentation = 0;

    public bool rootMotionEnabled = false;

    public bool individualDifferenceEnabled = false;

    public GPUSkinningAnimEvent[] events = null;
}

public enum GPUSkinningWrapMode
{
    Once,
    Loop
}

[System.Serializable]
public class GPUSkinningFrame
{
    public Matrix4x4[] matrices = null;

    public Quaternion rootMotionDeltaPositionQ;

    public float rootMotionDeltaPositionL;

    public Quaternion rootMotionDeltaRotation;

    [System.NonSerialized]
    private bool rootMotionInvInit = false;
    [System.NonSerialized]
    private Matrix4x4 rootMotionInv;
    public Matrix4x4 RootMotionInv(int rootBoneIndex)
    {
        if (!rootMotionInvInit)
        {
            rootMotionInv = matrices[rootBoneIndex].inverse;
            rootMotionInvInit = true;
        }
        return rootMotionInv;
    }
}

[System.Serializable]
public class GPUSkinningAnimEvent : System.IComparable<GPUSkinningAnimEvent>
{
    public int frameIndex = 0;

    public int eventId = 0;

    public int CompareTo(GPUSkinningAnimEvent other)
    {
        return frameIndex > other.frameIndex ? -1 : 1;
    }
}


public enum MilesSkinningShaderType
{
    GPUSkinning
}

public enum GPUSkinningQuality
{
    Bone2
}
