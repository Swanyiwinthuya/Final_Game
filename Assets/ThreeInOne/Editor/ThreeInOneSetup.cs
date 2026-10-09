using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Builds the Main Menu scene and adds the In-Game Menu to the three game scenes.
// Run it again from Tools > Three-in-One > Rebuild Menus after changing the title or name below.
[InitializeOnLoad]
public static class ThreeInOneSetup
{
    const string GameTitle = "TRIPLE PLAY ARCADE";
    const string AuthorName = "Swanyi";

    const string MenuScenePath = "Assets/ThreeInOne/Scenes/MainMenu.unity";
    const string DrivingScenePath = "Assets/Scenes/Prototype 1.unity";
    const string FlyingScenePath = "Assets/Challenge 1/Challenge 1.unity";
    const string SumoScenePath = "Assets/Challenge 4/Challenge 4.unity";

    const string InGameMenuName = "In-Game Menu";
    const string EventSystemName = "EventSystem";

    static readonly Color Background = new Color(0.07f, 0.09f, 0.16f);
    static readonly Color Highlight = new Color(1f, 0.82f, 0.25f);

    static ThreeInOneSetup()
    {
        EditorApplication.delayCall += EnsureProjectSettings;
    }

    // Tags and the build scene list live in ProjectSettings, which a .unitypackage
    // does not carry, so they are restored here whenever the project loads.
    static void EnsureProjectSettings()
    {
        bool addedTag = EnsureTag("Enemy");
        addedTag |= EnsureTag("Powerup");
        if (addedTag && AssetDatabase.IsValidFolder("Assets/Challenge 4/Prefabs"))
        {
            AssetDatabase.ImportAsset("Assets/Challenge 4/Prefabs",
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        string[] wanted = new[] { MenuScenePath, DrivingScenePath, FlyingScenePath, SumoScenePath }
            .Where(File.Exists).ToArray();
        string[] current = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (!wanted.SequenceEqual(current))
        {
            EditorBuildSettings.scenes = wanted.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        }
    }

    static bool EnsureTag(string tag)
    {
        Object[] tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagManager == null || tagManager.Length == 0)
        {
            return false;
        }

        SerializedObject serialized = new SerializedObject(tagManager[0]);
        SerializedProperty tags = serialized.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
            {
                return false;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return true;
    }

    [MenuItem("Tools/Three-in-One/Rebuild Menus")]
    public static void BuildAll()
    {
        EnsureProjectSettings();

        foreach (string path in new[] { DrivingScenePath, FlyingScenePath, SumoScenePath })
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            if (path == DrivingScenePath)
            {
                SetupDrivingScene(scene);
            }
            BuildInGameMenu(scene);
            EditorSceneManager.SaveScene(scene);
        }

        BuildMainMenuScene();
        EnsureProjectSettings();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Three-in-One/Export unitypackage")]
    public static void ExportPackage()
    {
        AssetDatabase.ExportPackage("Assets", "ThreeInOneGame.unitypackage", ExportPackageOptions.Recurse);
        Debug.Log("Exported ThreeInOneGame.unitypackage to the project folder");
    }

    // ---------- Main Menu ----------

    static void BuildMainMenuScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
        cameraObject.AddComponent<AudioListener>();

        CreateEventSystem();
        Canvas canvas = CreateCanvas("Main Menu");
        MainMenu menu = canvas.gameObject.AddComponent<MainMenu>();
        menu.drivingScene = Path.GetFileNameWithoutExtension(DrivingScenePath);
        menu.flyingScene = Path.GetFileNameWithoutExtension(FlyingScenePath);
        menu.sumoScene = Path.GetFileNameWithoutExtension(SumoScenePath);

        Image background = CreateStretchedImage(canvas.transform, "Background", Background);
        background.raycastTarget = false;

        Text title = CreateText(canvas.transform, "Title", GameTitle, 96, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1700, 160));

        RectTransform items = CreateMenuColumn(canvas.transform, "Menu Items", new Vector2(0, -90), 620, TextAnchor.UpperLeft);
        Button first = CreateMenuButton(items, "Mad Driver", TextAnchor.MiddleLeft, menu.PlayDriving);
        CreateMenuButton(items, "Fly Like a Bird", TextAnchor.MiddleLeft, menu.PlayFlying);
        CreateMenuButton(items, "I'm a Sumo and a Ball", TextAnchor.MiddleLeft, menu.PlaySumo);
        CreateMenuButton(items, "Exit", TextAnchor.MiddleLeft, menu.ExitGame);

        Text credit = CreateText(canvas.transform, "Credit", "By " + AuthorName, 34, TextAnchor.MiddleRight, FontStyle.Normal);
        Place(credit.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60, 40), new Vector2(800, 60));

        EventSystem.current = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            EventSystem eventSystem = root.GetComponent<EventSystem>();
            if (eventSystem != null)
            {
                eventSystem.firstSelectedGameObject = first.gameObject;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(MenuScenePath));
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // ---------- In-Game Menu ----------

    static void BuildInGameMenu(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == InGameMenuName || root.name == EventSystemName)
            {
                Object.DestroyImmediate(root);
            }
        }

        CreateEventSystem();
        Canvas canvas = CreateCanvas(InGameMenuName);
        canvas.sortingOrder = 100;
        PauseMenu pauseMenu = canvas.gameObject.AddComponent<PauseMenu>();
        pauseMenu.mainMenuScene = Path.GetFileNameWithoutExtension(MenuScenePath);

        Text hint = CreateText(canvas.transform, "Pause Hint", "Esc - Pause", 30, TextAnchor.UpperLeft, FontStyle.Normal);
        Place(hint.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -20), new Vector2(400, 50));
        hint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

        Image panel = CreateStretchedImage(canvas.transform, "Pause Panel", new Color(0, 0, 0, 0.78f));

        Text title = CreateText(panel.transform, "Title", "PAUSED", 110, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(1200, 170));

        RectTransform items = CreateMenuColumn(panel.transform, "Menu Items", new Vector2(0, -60), 700, TextAnchor.UpperCenter);
        Button resume = CreateMenuButton(items, "Resume", TextAnchor.MiddleCenter, pauseMenu.Resume);
        CreateMenuButton(items, "Restart", TextAnchor.MiddleCenter, pauseMenu.Restart);
        CreateMenuButton(items, "Back to Main Menu", TextAnchor.MiddleCenter, pauseMenu.BackToMainMenu);

        pauseMenu.pausePanel = panel.gameObject;
        pauseMenu.firstButton = resume.gameObject;
        panel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
    }

    // ---------- Driving game ----------

    // The class scene only had the road and sky, so add the vehicle, the follow
    // camera and some obstacles the same way the Prototype 1 lessons do.
    static void SetupDrivingScene(Scene scene)
    {
        if (FindInScene<PlayerController>(scene) != null)
        {
            return;
        }

        GameObject vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Course Library/Vehicles/Veh_Car_Blue_Z.prefab");
        GameObject vehicle = (GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab, scene);
        vehicle.name = "Vehicle";
        vehicle.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        MakeCollidersConvex(vehicle);
        vehicle.AddComponent<Rigidbody>().mass = 100;
        vehicle.AddComponent<PlayerController>();

        Camera camera = FindInScene<Camera>(scene);
        camera.transform.SetPositionAndRotation(new Vector3(0, 5, -7), Quaternion.Euler(15, 0, 0));
        FollowPlayer follow = camera.gameObject.AddComponent<FollowPlayer>();
        follow.player = vehicle;

        GameObject obstacles = new GameObject("Obstacles");
        GameObject cratePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Course Library/Obstacles/Crate_01.prefab");
        float[] lanes = { 0, -3, 3, 0, 3, -3 };
        for (int i = 0; i < lanes.Length; i++)
        {
            GameObject crate = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, obstacles.transform);
            crate.name = "Obstacle";
            crate.transform.position = new Vector3(lanes[i], 0, 25 + i * 25);
            MakeCollidersConvex(crate);
            crate.AddComponent<Rigidbody>().mass = 20;
        }
    }

    static void MakeCollidersConvex(GameObject root)
    {
        foreach (MeshCollider collider in root.GetComponentsInChildren<MeshCollider>(true))
        {
            collider.convex = true;
        }
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    // ---------- UI helpers ----------

    static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject(EventSystemName, typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    static Canvas CreateCanvas(string name)
    {
        GameObject canvasObject = new GameObject(name, typeof(RectTransform));
        canvasObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static GameObject CreateUIObject(Transform parent, string name)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.layer = LayerMask.NameToLayer("UI");
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    static Image CreateStretchedImage(Transform parent, string name, Color color)
    {
        Image image = CreateUIObject(parent, name).AddComponent<Image>();
        image.color = color;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;
        return image;
    }

    static Text CreateText(Transform parent, string name, string content, int size, TextAnchor anchor, FontStyle style)
    {
        Text text = CreateUIObject(parent, name).AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static RectTransform CreateMenuColumn(Transform parent, string name, Vector2 position, float width, TextAnchor alignment)
    {
        GameObject column = CreateUIObject(parent, name);
        RectTransform rect = (RectTransform)column.transform;
        Place(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(width, 0));

        VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = alignment;
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = column.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    static Button CreateMenuButton(Transform parent, string label, TextAnchor anchor, UnityAction action)
    {
        Text text = CreateText(parent, label, label, 54, anchor, FontStyle.Normal);
        text.raycastTarget = true;
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 78;

        Button button = text.gameObject.AddComponent<Button>();
        button.targetGraphic = text;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Highlight;
        colors.selectedColor = Highlight;
        colors.pressedColor = new Color(1f, 0.55f, 0.15f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }
}
