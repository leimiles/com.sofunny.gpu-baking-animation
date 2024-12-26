using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(GPUSkinningSampler2))]
public class GPUSkinningSamplerEditor2 : Editor
{
    MileSkinning preview = null;
    GPUSkinningSampler2 sampler2;
    bool guiEnabled = false;
    float time = .0f;

    void Awake()
    {
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
            EditorUtility.DisplayProgressBar("Sampling, DONOT stop playing", msg, (float)(sampler2.samplingIndex + 1) / sampler2.samplingFramesTotal);
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
        base.OnInspectorGUI();
        OnGUI_SamplerInfo();
        OnGUI_Sampler(sampler2);
    }

    void OnGUI_SamplerInfo()
    {
        guiEnabled = !Application.isPlaying;
        GUI.enabled = guiEnabled;
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("animName"), new GUIContent("Animation Name"));
        }

        GUI.enabled = true;

    }

    void OnGUI_Sampler(GPUSkinningSampler2 gpuSkinningSampler2)
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