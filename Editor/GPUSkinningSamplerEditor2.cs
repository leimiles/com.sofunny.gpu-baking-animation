using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(GPUSkinningSampler2))]
public class GPUSkinningSamplerEditor2 : Editor
{
    MileSkinning preview = null;
    GPUSkinningSampler2 sampler2;
    bool guiEnabled = false;
    float time = .0f;
    int animClips_count = 0;

    SerializedProperty animClips_array_size_sp;
    List<SerializedProperty> animClips_item_sp;
    SerializedProperty fpsList_array_size_sp;
    List<SerializedProperty> fpsList_item_sp;

    void Awake()
    {
        if (sampler2 == null)
        {
            sampler2 = target as GPUSkinningSampler2;
        }
        EditorApplication.update += UpdateHandler;
        time = Time.realtimeSinceStartup;
    }

    void UpdateHandler()
    {
        if (preview != null && EditorApplication.isPlaying)
        {
            // todo: destroy preview
            return;
        }

        if (EditorApplication.isCompiling)
        {
            if (Selection.activeGameObject == sampler2.gameObject)
            {
                Selection.activeGameObject = null;
                return;
            }
        }

        float deltaTime = Time.realtimeSinceStartup - time;

        if (preview != null)
        {
            // todo: render preview window
        }

        time = Time.realtimeSinceStartup;

        if (!sampler2.isSampling && sampler2.IsSamplingProgress())
        {
            if (++sampler2.samplingIndex < sampler2.animClips.Length)
            {
                sampler2.StartSample();
            }
            else
            {
                sampler2.EndSample();
                EditorApplication.isPlaying = false;
                // todo: clear progress bar
                LockInspector(false);
            }
        }

        if (sampler2.isSampling)
        {
            string msg = sampler2.animClip.name + "(" + (sampler2.samplingIndex + 1) + "/" + sampler2.animClips.Length + ")";

            Debug.Log(sampler2.samplingIndex + 1 / sampler2.samplingFramesTotal);
            //EditorUtility.DisplayProgressBar("Sampling, DONOT stop playing", msg, (float)(sampler2.samplingIndex + 1) / sampler2.samplingFramesTotal);
        }

        Debug.Log("i'm a handler");
    }

    void OnDestroy()
    {
        EditorApplication.update -= UpdateHandler;
        // todo: clear progress bar
        // todo: destroy preview
    }


    public override void OnInspectorGUI()
    {
        if (sampler2 == null)
        {
            sampler2 = target as GPUSkinningSampler2;
        }
        OnGUI_SamplerInfo();
        OnGUI_Sampler();

        //OnGUI_Preview();

        serializedObject.ApplyModifiedProperties();
    }

    void OnGUI_Preview()
    {

    }

    void OnGUI_SamplerInfo()
    {
        guiEnabled = !Application.isPlaying;
        GUI.enabled = guiEnabled;
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animName"), new GUIContent("Animation Data Name"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rootBone"), new GUIContent("Root Bone"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animClips"), new GUIContent("Animation Clips"));
            OnGUI_AnimClips();
        }
        GUI.enabled = true;
    }

    void OnGUI_AnimClips()
    {
        System.Action ResetItemSp = () =>
        {
            animClips_item_sp.Clear();
            fpsList_item_sp.Clear();
            fpsList_array_size_sp.intValue = animClips_array_size_sp.intValue;

            for (int i = 0; i < animClips_array_size_sp.intValue; i++)
            {
                animClips_item_sp.Add(serializedObject.FindProperty(string.Format("animClips.Array.data[{0}]", i)));
                fpsList_item_sp.Add(serializedObject.FindProperty(string.Format("fpsList.Array.data[{0}]", i)));
            }

            animClips_count = animClips_item_sp.Count;
        };

        if (animClips_array_size_sp == null) animClips_array_size_sp = serializedObject.FindProperty("animClips.Array.size");
        if (fpsList_array_size_sp == null) fpsList_array_size_sp = serializedObject.FindProperty("fpsList.Array.size");

        if (animClips_item_sp == null)
        {
            animClips_item_sp = new List<SerializedProperty>();
            fpsList_item_sp = new List<SerializedProperty>();
            ResetItemSp();
        }

        //OnGUI_AnimClipsArea();
    }

    void OnGUI_AnimClipsArea()
    {
        if (sampler2.IsAnimator())
        {
            EditorGUILayout.HelpBox("Set AnimClips with Animation Component", MessageType.Info);
        }

        EditorGUILayout.PrefixLabel("Clips: ");
        GUI.enabled = sampler2.IsAnimator() && guiEnabled;
        EditorGUILayout.BeginHorizontal();
        animClips_count = EditorGUILayout.IntField("Count: ", animClips_count);
        if (GUILayout.Button("Apply", GUILayout.Width(60)))
        {

        }

        if (GUILayout.Button("Reset", GUILayout.Width(60)))
        {
            GUI.FocusControl(string.Empty);
            return;
        }
        EditorGUILayout.EndHorizontal();
    }

    void OnGUI_Sampler()
    {
        BeginEditorGUIBox();
        if (GUILayout.Button("Step1: Play"))
        {
            // todo: destroy preview
            EditorApplication.isPlaying = true;
        }

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Step2: Sample"))
            {
                // todo: check lod
                // todo: destory preview
                LockInspector(true);
                sampler2.ResetSampleIndex();
                sampler2.StartSample();
            }
        }
        EndEditorGUIBox();
    }

    void LockInspector(bool isLocked)
    {
        System.Type type = Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.InspectorWindow");
        FieldInfo field = type.GetField("m_AllInspectors", BindingFlags.Static | BindingFlags.NonPublic);
        System.Collections.ArrayList windows = new System.Collections.ArrayList(field.GetValue(null) as System.Collections.ICollection);
        foreach (var window in windows)
        {
            PropertyInfo property = type.GetProperty("isLocked");
            property.SetValue(window, isLocked, null);
        }
    }


    void BeginEditorGUIBox()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.GetStyle("Box"));
        EditorGUILayout.Space();
    }

    void EndEditorGUIBox()
    {
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();
    }
}