using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameSetup
{
    [MenuItem("Tools/Build Coin Collector Scene")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        const string workshopScenePath = "Assets/Unity_Worshop_2.unity";
        if (scene.path != workshopScenePath)
            scene = EditorSceneManager.OpenScene(workshopScenePath);

        Ensure3DRenderer();

        if (!AssetDatabase.IsValidFolder("Assets/Game/Materials"))
            AssetDatabase.CreateFolder("Assets/Game", "Materials");

        Material platformMat = MakeMaterial("Platform", new Color(0.10f, 0.26f, 0.30f), 0.1f, 0.35f);
        Material coinMat = MakeMaterial("Coin", new Color(1.00f, 0.74f, 0.12f), 0.75f, 0.80f);
        coinMat.EnableKeyword("_EMISSION");
        coinMat.SetColor("_EmissionColor", new Color(1.00f, 0.65f, 0.10f) * 0.45f);
        EditorUtility.SetDirty(coinMat);

        DestroyIfExists("Platform");
        DestroyIfExists("Player");
        DestroyIfExists("CoinSpawner");
        DestroyIfExists("GameManager");
        DestroyIfExists("Canvas");
        DestroyIfExists("Directional Light");
        DestroyIfExists("Walls");

        GameObject platform = GameObject.Find("Cube");
        if (platform == null) platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.position = new Vector3(0f, -0.25f, 0f);
        platform.transform.rotation = Quaternion.identity;
        platform.transform.localScale = new Vector3(20f, 0.5f, 20f);
        platform.GetComponent<MeshRenderer>().sharedMaterial = platformMat;

        Material wallMat = MakeMaterial("Wall", new Color(0.16f, 0.38f, 0.40f), 0.1f, 0.4f);
        GameObject wallsGo = new GameObject("Walls");
        CreateWall(wallsGo.transform, "Wall North", new Vector3(0f, 0.25f, 10.25f), new Vector3(20.5f, 0.5f, 0.5f), wallMat);
        CreateWall(wallsGo.transform, "Wall South", new Vector3(0f, 0.25f, -10.25f), new Vector3(20.5f, 0.5f, 0.5f), wallMat);
        CreateWall(wallsGo.transform, "Wall East", new Vector3(10.25f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 20.5f), wallMat);
        CreateWall(wallsGo.transform, "Wall West", new Vector3(-10.25f, 0.25f, 0f), new Vector3(0.5f, 0.5f, 20.5f), wallMat);

        GameObject carAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Models/DeLoreanTimeMachine.obj");
        if (carAsset == null)
        {
            throw new System.InvalidOperationException(
                "The DeLorean model could not be imported. Confirm the model exists at Assets/Game/Models/DeLoreanTimeMachine.obj.");
        }

        GameObject player = new GameObject("Player");
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.transform.rotation = Quaternion.identity;
        player.tag = "Player";
        GameObject carVisual = (GameObject)PrefabUtility.InstantiatePrefab(carAsset, player.transform);
        carVisual.name = "DeLorean";
        carVisual.transform.localPosition = Vector3.zero;
        carVisual.transform.localRotation = Quaternion.identity;
        carVisual.transform.localScale = Vector3.one * 100f;
        BoxCollider carCollider = player.AddComponent<BoxCollider>();
        carCollider.center = new Vector3(0f, 0.55f, 0f);
        carCollider.size = new Vector3(2.2f, 1.1f, 4.5f);
        Rigidbody playerBody = player.AddComponent<Rigidbody>();
        playerBody.mass = 1200f;
        player.AddComponent<PlayerController>();

        GameObject coinTemp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coinTemp.name = "Coin";
        coinTemp.transform.position = Vector3.zero;
        coinTemp.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        coinTemp.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);
        coinTemp.GetComponent<MeshRenderer>().sharedMaterial = coinMat;
        coinTemp.GetComponent<Collider>().isTrigger = true;
        coinTemp.AddComponent<Coin>();
        GameObject coinPrefab = PrefabUtility.SaveAsPrefabAsset(coinTemp, "Assets/Game/Prefabs/Coin.prefab");
        Object.DestroyImmediate(coinTemp);

        GameObject spawnerGo = new GameObject("CoinSpawner");
        spawnerGo.transform.position = Vector3.zero;
        CoinSpawner spawner = spawnerGo.AddComponent<CoinSpawner>();
        spawner.coinPrefab = coinPrefab;
        spawner.player = player.transform;
        spawner.initialCoins = 8;
        spawner.areaSize = new Vector2(17f, 17f);
        spawner.spawnHeight = 1f;
        spawner.minDistanceFromPlayer = 3f;

        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();

        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 56);

        Text scoreText = CreateLabel(canvasGo.transform, "ScoreText", font, 56, FontStyle.Bold,
            TextAnchor.UpperLeft, new Color(1f, 1f, 1f, 1f), "Coins: 0");
        RectTransform scoreRt = scoreText.rectTransform;
        scoreRt.anchorMin = new Vector2(0f, 1f);
        scoreRt.anchorMax = new Vector2(0f, 1f);
        scoreRt.pivot = new Vector2(0f, 1f);
        scoreRt.anchoredPosition = new Vector2(32f, -24f);
        scoreRt.sizeDelta = new Vector2(600f, 100f);
        Shadow shadow = scoreText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(3f, -3f);

        Text hintText = CreateLabel(canvasGo.transform, "HintText", font, 30, FontStyle.Normal,
            TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f), "WASD / Arrow Keys to move");
        RectTransform hintRt = hintText.rectTransform;
        hintRt.anchorMin = new Vector2(0.5f, 0f);
        hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 28f);
        hintRt.sizeDelta = new Vector2(900f, 60f);

        gm.scoreText = scoreText;

        GameObject camGo = GameObject.Find("Main Camera");
        if (camGo != null)
        {
            camGo.transform.position = new Vector3(0f, 8f, -10f);
            camGo.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            Camera cam = camGo.GetComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 55f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.14f);
            CarCameraFollow cameraFollow = camGo.GetComponent<CarCameraFollow>();
            if (cameraFollow == null) cameraFollow = camGo.AddComponent<CarCameraFollow>();
            cameraFollow.target = player.transform;
            cameraFollow.offset = new Vector3(0f, 8f, -10f);
        }

        GameObject lightGo = new GameObject("Directional Light", typeof(Light));
        Light directionalLight = lightGo.GetComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1.3f;
        directionalLight.color = new Color(1f, 0.96f, 0.88f);
        directionalLight.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.36f, 0.45f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.transform.rotation = Quaternion.identity;
        wall.transform.localScale = scale;
        wall.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void Ensure3DRenderer()
    {
        const string rendererPath = "Assets/Settings/GameRenderer.asset";
        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);

        if (rendererData == null)
        {
            MethodInfo createMethod = typeof(UniversalRenderPipelineAsset).GetMethod(
                "CreateRendererAsset", BindingFlags.Static | BindingFlags.NonPublic);
            rendererData = (ScriptableRendererData)createMethod.Invoke(
                null, new object[] { rendererPath, RendererType.UniversalRenderer, false, "Renderer" });
        }

        UniversalRenderPipelineAsset urpAsset =
            AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
        if (urpAsset == null || rendererData == null) return;

        SerializedObject serializedAsset = new SerializedObject(urpAsset);
        SerializedProperty rendererList = serializedAsset.FindProperty("m_RendererDataList");
        rendererList.arraySize = 1;
        rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
        serializedAsset.FindProperty("m_DefaultRendererIndex").intValue = 0;
        serializedAsset.ApplyModifiedProperties();

        EditorUtility.SetDirty(urpAsset);
        AssetDatabase.SaveAssets();
    }

    private static Material MakeMaterial(string name, Color color, float metallic, float smoothness)
    {
        string path = "Assets/Game/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Text CreateLabel(Transform parent, string name, Font font, int size, FontStyle style,
        TextAnchor alignment, Color color, string content)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void DestroyIfExists(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Object.DestroyImmediate(existing);
    }
}
