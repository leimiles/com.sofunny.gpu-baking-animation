using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class MileSkinning : MonoBehaviour
{
    [SerializeField] GPUSkinningAnimation gpuSkinningAnimation;
    [SerializeField] Material material;
    [SerializeField] Mesh mesh;
    [SerializeField] TextAsset textAsset;
    [SerializeField] int defaultPlayingClipIndex = 0;
    [SerializeField] MileSkinningCullingMode mileSkinningCullingMode = MileSkinningCullingMode.CullUpdateTransforms;
    static MileSkinningManager mileSkinningManager = new MileSkinningManager();
    MileSkinningPlayer player;
    public MileSkinningPlayer Player
    {
        get
        {
            return player;
        }
    }

#if UNITY_EDITOR
    public void DeletePlayer()
    {
        player = null;
    }

    public void Update_Editor(float deltaTime)
    {
        if (player != null && !Application.isPlaying)
        {
            player.Update_Editor(deltaTime);
        }
    }
    /*
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("wrong calling");
            Init();
            Update_Editor(0);
        }
    }
    */

    public void Init(GPUSkinningAnimation anim, Mesh mesh, Material mtrl, TextAsset textureRawData)
    {
        if (player != null)
        {
            return;
        }

        this.gpuSkinningAnimation = anim;
        this.mesh = mesh;
        this.material = mtrl;
        this.textAsset = textureRawData;
        Init();
    }


#endif

    void Start()
    {
        Init();
#if UNITY_EDITOR
        Update_Editor(0);
#endif
    }

    private void Update()
    {
        if (player != null)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                player.Update(Time.deltaTime);
            }
            else
            {
                player.Update_Editor(0);
            }
#else
            player.Update(Time.deltaTime);
#endif
        }
    }

    public void Init()
    {
        if (player != null)
        {
            return;
        }

        if (gpuSkinningAnimation != null && material != null && mesh != null && textAsset != null)
        {
            MileSkinningData data = null;
            if (Application.isPlaying)
            {
                mileSkinningManager.Register(gpuSkinningAnimation, mesh, material, textAsset, this, out data);
            }
            else
            {
                data = new MileSkinningData();
                data.gpuSkinningAnimation = gpuSkinningAnimation;
                data.mesh = mesh;
                data.InitMaterial(material, HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor);
                data.texture2D = MileSkinningUtils.CreateTexture2D(textAsset, gpuSkinningAnimation);
                data.texture2D.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            }
            player = new MileSkinningPlayer(gameObject, data);
            player.MileSkinningCullingMode = mileSkinningCullingMode;

            if (gpuSkinningAnimation != null && gpuSkinningAnimation.clips != null && gpuSkinningAnimation.clips.Length > 0)
            {
                player.Play(gpuSkinningAnimation.clips[Mathf.Clamp(defaultPlayingClipIndex, 0, gpuSkinningAnimation.clips.Length)].name);
            }
        }
    }

    private void OnDestroy()
    {
        player = null;
        gpuSkinningAnimation = null;
        mesh = null;
        material = null;
        textAsset = null;

        if (Application.isPlaying)
        {
            mileSkinningManager.Unregister(this);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Resources.UnloadUnusedAssets();
            UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
        }
#endif
    }

}

public class MileSkinningManager
{
    private List<MileSkinningData> items = new List<MileSkinningData>();
    public void Register(GPUSkinningAnimation animation, Mesh mesh, Material material, TextAsset textAsset, MileSkinning mileSkinning, out MileSkinningData mileSkinningData)
    {
        mileSkinningData = null;
        if (animation == null || mesh == null || material == null || textAsset == null)
        {
            return;
        }
        MileSkinningData data = null;
        int num = items.Count;
        for (int i = 0; i < num; ++i)
        {
            if (items[i].gpuSkinningAnimation.guid == animation.guid)
            {
                data = items[i];
                break;
            }
        }

        if (data == null)
        {
            data = new MileSkinningData();
            items.Add(data);
        }

        if (data.gpuSkinningAnimation == null)
        {
            data.gpuSkinningAnimation = animation;
        }

        if (data.mesh == null)
        {
            data.mesh = mesh;
        }

        data.InitMaterial(material, HideFlags.None);

        if (data.texture2D == null)
        {
            data.texture2D = MileSkinningUtils.CreateTexture2D(textAsset, animation);
        }

        if (!data.mileSkinnings.Contains(mileSkinning))
        {
            data.mileSkinnings.Add(mileSkinning);
            data.AddCullingBounds();
        }

        mileSkinningData = data;

    }


    public void Unregister(MileSkinning mileSkinning)
    {
        if (mileSkinning == null)
        {
            return;
        }

        int numItems = items.Count;
        for (int i = 0; i < numItems; ++i)
        {
            int playerIndex = items[i].mileSkinnings.IndexOf(mileSkinning);
            if (playerIndex != -1)
            {
                items[i].mileSkinnings.RemoveAt(playerIndex);
                items[i].RemoveCullingBounds(playerIndex);
                if (items[i].mileSkinnings.Count == 0)
                {
                    items[i].Destroy();
                    items.RemoveAt(i);
                }
                break;
            }
        }
    }
}
