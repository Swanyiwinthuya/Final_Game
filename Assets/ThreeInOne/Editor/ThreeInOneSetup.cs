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
            GameUI ui = BuildInGameMenu(scene);
            if (path == DrivingScenePath)
            {
                SetupDrivingScene(scene, ui);
            }
            else if (path == FlyingScenePath)
            {
                SetupFlyingScene(scene, ui);
            }
            else
            {
                SetupSumoScene(scene, ui);
            }
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
        CreateMenuButton(items, "Mad Driver", TextAnchor.MiddleLeft, menu.PlayDriving);
        CreateMenuButton(items, "Fly Like a Bird", TextAnchor.MiddleLeft, menu.PlayFlying);
        CreateMenuButton(items, "I'm a Sumo and a Ball", TextAnchor.MiddleLeft, menu.PlaySumo);
        CreateMenuButton(items, "Exit", TextAnchor.MiddleLeft, menu.ExitGame);

        Text credit = CreateText(canvas.transform, "Credit", "By " + AuthorName, 34, TextAnchor.MiddleRight, FontStyle.Normal);
        Place(credit.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60, 40), new Vector2(800, 60));

        Directory.CreateDirectory(Path.GetDirectoryName(MenuScenePath));
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }

    // ---------- In-Game Menu ----------

    static GameUI BuildInGameMenu(Scene scene)
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

        GameUI ui = BuildGameUI(canvas, pauseMenu);
        EditorSceneManager.MarkSceneDirty(scene);
        return ui;
    }

    // HUD plus the win / game over screen
    static GameUI BuildGameUI(Canvas canvas, PauseMenu pauseMenu)
    {
        GameUI ui = canvas.gameObject.AddComponent<GameUI>();
        ui.pauseMenu = pauseMenu;

        ui.hudText = CreateText(canvas.transform, "HUD", "", 46, TextAnchor.UpperCenter, FontStyle.Bold);
        Place(ui.hudText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(1200, 70));
        ui.hudText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);
        ui.hudText.transform.SetSiblingIndex(0);

        ui.centerText = CreateText(canvas.transform, "Center Message", "", 64, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(ui.centerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1400, 120));
        ui.centerText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.85f);
        ui.centerText.transform.SetSiblingIndex(1);

        Image panel = CreateStretchedImage(canvas.transform, "Result Panel", new Color(0.03f, 0.04f, 0.09f, 0.85f));

        ui.resultTitle = CreateText(panel.transform, "Title", "YOU WIN!", 120, TextAnchor.MiddleCenter, FontStyle.Bold);
        Place(ui.resultTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(1400, 180));

        ui.resultMessage = CreateText(panel.transform, "Message", "", 44, TextAnchor.MiddleCenter, FontStyle.Normal);
        Place(ui.resultMessage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1500, 140));

        RectTransform items = CreateMenuColumn(panel.transform, "Menu Items", new Vector2(0, -120), 700, TextAnchor.UpperCenter);
        Button playAgain = CreateMenuButton(items, "Play Again", TextAnchor.MiddleCenter, ui.PlayAgain);
        CreateMenuButton(items, "Main Menu", TextAnchor.MiddleCenter, ui.MainMenu);

        ui.resultPanel = panel.gameObject;
        ui.firstButton = playAgain.gameObject;
        panel.gameObject.SetActive(false);
        return ui;
    }

    // ---------- Driving game (Mad Driver) ----------

    const string CourseLibrary = "Assets/Course Library/";
    const string LevelName = "Level";
    const string ManagerName = "Game Manager";
    static readonly float[] Lanes = { -7.5f, -2.5f, 2.5f, 7.5f };

    // The class scene only had the road and sky, so this adds the vehicle and follow
    // camera from the Prototype 1 lessons, then builds a full track around them.
    static void SetupDrivingScene(Scene scene, GameUI ui)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Obstacles" || root.name == LevelName || root.name == ManagerName)
            {
                Object.DestroyImmediate(root);
            }
        }

        PlayerController controller = FindInScene<PlayerController>(scene);
        GameObject vehicle;
        if (controller == null)
        {
            vehicle = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab("Vehicles/Veh_Car_Blue_Z"), scene);
            vehicle.name = "Vehicle";
            MakeCollidersConvex(vehicle);
            vehicle.AddComponent<Rigidbody>();
            vehicle.AddComponent<PlayerController>();
        }
        else
        {
            vehicle = controller.gameObject;
        }
        vehicle.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Rigidbody body = vehicle.GetComponent<Rigidbody>();
        body.mass = 100;
        body.constraints = RigidbodyConstraints.FreezeRotation; // crashes should not flip the car

        Camera camera = FindInScene<Camera>(scene);
        camera.transform.SetPositionAndRotation(new Vector3(0, 6, -11), Quaternion.Euler(16, 0, 0));
        FollowPlayer follow = camera.GetComponent<FollowPlayer>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<FollowPlayer>();
        }
        follow.player = vehicle;

        Transform level = new GameObject(LevelName).transform;
        System.Random random = new System.Random(7);

        // Road: the original 200 m piece plus two more
        Transform road = FindByName(scene, "Road");
        float roadStart = road.position.z - 100;
        float roadEnd = road.position.z + 500;
        float finishZ = roadEnd - 30;
        Transform roads = NewGroup("Roads", level);
        for (int i = 1; i <= 2; i++)
        {
            GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab("Ground/Ground_Road"), roads);
            piece.name = "Road " + (i + 1);
            piece.transform.position = road.position + new Vector3(0, 0, 200 * i);
        }

        float length = roadEnd - roadStart;
        float middle = (roadStart + roadEnd) / 2;
        Primitive(PrimitiveType.Cube, "Grass", level, new Vector3(0, -0.15f, middle), new Vector3(700, 0.2f, length + 700),
            GetMaterial("Grass", new Color(0.36f, 0.66f, 0.28f)), false);

        // Invisible walls keep the car on the road
        Transform walls = NewGroup("Walls", level);
        Wall(walls, new Vector3(-10.6f, 3, middle), new Vector3(1, 6, length));
        Wall(walls, new Vector3(10.6f, 3, middle), new Vector3(1, 6, length));
        Wall(walls, new Vector3(0, 3, roadStart + 2), new Vector3(22, 6, 1));

        // Rows of obstacles that always leave at least one lane open
        Transform obstacles = NewGroup("Obstacles", level);
        string[] props = { "Crate_01", "Barrel_02", "Prop_Cone_01", "Prop_Spool_02", "Prop_Barrier02" };
        for (float z = 45; z < finishZ - 30; z += 24)
        {
            int freeLane = random.Next(Lanes.Length);
            int blocked = 1 + random.Next(2);
            for (int i = 0; i < blocked; i++)
            {
                int lane = (freeLane + 1 + random.Next(Lanes.Length - 1)) % Lanes.Length;
                string prop = props[random.Next(props.Length)];
                Vector3 position = new Vector3(Lanes[lane], 0, z + i * 5);
                PlaceObstacle(obstacles, prop, position);
                if (prop == "Crate_01")
                {
                    PlaceObstacle(obstacles, prop, position + new Vector3(0, 1.56f, 0));
                }
            }
        }

        // Oncoming traffic
        Transform traffic = NewGroup("Traffic", level);
        string[] vehicles = { "Veh_Bus_Blue_Z", "Veh_Van_Green_Z", "Veh_Ute_Red_Z", "Veh_Armor_Car_01" };
        for (float z = 130; z < finishZ - 20; z += 72)
        {
            GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab("Vehicles/" + vehicles[random.Next(vehicles.Length)]), traffic);
            car.name = "Traffic";
            car.transform.SetPositionAndRotation(new Vector3(Lanes[random.Next(Lanes.Length)], 0, z + 12), Quaternion.Euler(0, 180, 0));
            MakeCollidersConvex(car);
            car.AddComponent<Rigidbody>().isKinematic = true;
            car.AddComponent<Hazard>();
            TrafficVehicle driver = car.AddComponent<TrafficVehicle>();
            driver.player = vehicle.transform;
            driver.speed = 8 + random.Next(6);
        }

        // Trees and rocks along the roadside
        GameObject treePrefab = GetTreePrefab();
        Transform roadside = NewGroup("Roadside", level);
        for (float z = roadStart; z < roadEnd + 140; z += 9)
        {
            foreach (int side in new[] { -1, 1 })
            {
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, roadside);
                tree.transform.position = new Vector3(side * (14 + (float)random.NextDouble() * 34), 0, z + (float)random.NextDouble() * 7);
                tree.transform.localScale = Vector3.one * (0.8f + (float)random.NextDouble() * 0.9f);
                tree.transform.rotation = Quaternion.Euler(0, random.Next(360), 0);
            }
            if (random.Next(3) == 0)
            {
                GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab("Obstacles/SM_Rock_Boulder_01"), roadside);
                rock.transform.position = new Vector3((random.Next(2) == 0 ? -1 : 1) * (12.5f + (float)random.NextDouble() * 4), 0.4f, z + 4);
                rock.transform.localScale = Vector3.one * (0.6f + (float)random.NextDouble() * 0.8f);
            }
        }

        BuildFinishGate(NewGroup("Finish Line", level), finishZ);

        DrivingGame game = new GameObject(ManagerName).AddComponent<DrivingGame>();
        game.player = vehicle.transform;
        game.ui = ui;
        game.finishZ = finishZ;
        game.scenery = new[] { FindByName(scene, "SkyDome"), FindByName(scene, "MountainSkybox") }.Where(t => t != null).ToArray();

        PlayerCrashDetector detector = vehicle.GetComponent<PlayerCrashDetector>();
        if (detector == null)
        {
            detector = vehicle.AddComponent<PlayerCrashDetector>();
        }
        detector.game = game;
    }

    static void PlaceObstacle(Transform parent, string prop, Vector3 position)
    {
        GameObject obstacle = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab("Obstacles/" + prop), parent);
        obstacle.name = "Obstacle";
        obstacle.transform.position = position;
        MakeCollidersConvex(obstacle);
        obstacle.AddComponent<Rigidbody>().mass = 25;
        obstacle.AddComponent<Hazard>();
    }

    static void BuildFinishGate(Transform parent, float z)
    {
        Material light = GetMaterial("FinishLight", Color.white);
        Material dark = GetMaterial("FinishDark", new Color(0.08f, 0.08f, 0.08f));
        Primitive(PrimitiveType.Cube, "Pole", parent, new Vector3(-10.5f, 4.5f, z), new Vector3(0.8f, 9, 0.8f), light, false);
        Primitive(PrimitiveType.Cube, "Pole", parent, new Vector3(10.5f, 4.5f, z), new Vector3(0.8f, 9, 0.8f), light, false);
        for (int i = 0; i < 20; i++)
        {
            float x = -9.5f + i;
            for (int row = 0; row < 2; row++)
            {
                Material material = (i + row) % 2 == 0 ? light : dark;
                Primitive(PrimitiveType.Cube, "Banner", parent, new Vector3(x, 7.5f + row, z), new Vector3(1, 1, 0.4f), material, false);
                Primitive(PrimitiveType.Cube, "Line", parent, new Vector3(x, 0.02f, z + row), new Vector3(1, 0.02f, 1), material, false);
            }
        }
    }

    static GameObject GetTreePrefab()
    {
        const string path = "Assets/ThreeInOne/Prefabs/Tree.prefab";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        GameObject tree = new GameObject("Tree");
        Material leaves = GetMaterial("Leaves", new Color(0.2f, 0.55f, 0.22f));
        Primitive(PrimitiveType.Cylinder, "Trunk", tree.transform, new Vector3(0, 1.2f, 0), new Vector3(0.6f, 1.2f, 0.6f),
            GetMaterial("Trunk", new Color(0.42f, 0.28f, 0.16f)), false);
        Primitive(PrimitiveType.Sphere, "Leaves", tree.transform, new Vector3(0, 3.6f, 0), new Vector3(3.4f, 3.6f, 3.4f), leaves, false);
        Primitive(PrimitiveType.Sphere, "Leaves", tree.transform, new Vector3(0, 5.4f, 0), new Vector3(2.3f, 2.6f, 2.3f), leaves, false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tree, path);
        Object.DestroyImmediate(tree);
        return prefab;
    }

    // ---------- Flying game (Fly Like a Bird) ----------

    // Turns the Challenge 1 plane scene into a flappy game. The pipes, clouds and
    // finish line are created by FlappyGame when the scene starts.
    static void SetupFlyingScene(Scene scene, GameUI ui)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == ManagerName)
            {
                Object.DestroyImmediate(root);
            }
            else if (root.name == "Obstacles")
            {
                root.SetActive(false); // the old walls are replaced by the pipes
            }
        }

        Transform plane = FindByName(scene, "Player");
        PlanePlayerControllerX oldController = plane.GetComponent<PlanePlayerControllerX>();
        if (oldController != null)
        {
            Object.DestroyImmediate(oldController);
        }
        plane.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Rigidbody body = plane.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        Camera camera = FindInScene<Camera>(scene);
        FollowPlayerX oldFollow = camera.GetComponent<FollowPlayerX>();
        if (oldFollow != null)
        {
            Object.DestroyImmediate(oldFollow);
        }
        camera.transform.SetPositionAndRotation(new Vector3(30, 0, 10), Quaternion.Euler(0, 270, 0));
        camera.fieldOfView = 60;

        FlappyGame game = new GameObject(ManagerName).AddComponent<FlappyGame>();
        game.plane = plane;
        game.cameraTransform = camera.transform;
        game.ui = ui;
        game.pipeMaterial = GetMaterial("Pipe", new Color(0.3f, 0.78f, 0.3f), 0.45f);
        game.capMaterial = GetMaterial("PipeCap", new Color(0.2f, 0.62f, 0.24f), 0.45f);
        game.groundMaterial = GetMaterial("Grass", new Color(0.36f, 0.66f, 0.28f));
        game.cloudMaterial = GetMaterial("Cloud", Color.white, 0, true);
        game.finishLightMaterial = GetMaterial("FinishLight", Color.white);
        game.finishDarkMaterial = GetMaterial("FinishDark", new Color(0.08f, 0.08f, 0.08f));
    }

    // ---------- Sumo game (I'm a Sumo and a Ball) ----------

    // Opens up the Challenge 4 box into a small stadium: the walls keep their
    // colliders but are hidden, and boards, goal frames, lines and trees are added.
    static void SetupSumoScene(Scene scene, GameUI ui)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == LevelName || root.name == ManagerName)
            {
                Object.DestroyImmediate(root);
            }
        }

        foreach (MeshRenderer wall in FindByName(scene, "Walls").GetComponentsInChildren<MeshRenderer>(true))
        {
            wall.enabled = false;
        }

        Transform level = new GameObject(LevelName).transform;
        System.Random random = new System.Random(11);
        const float groundY = -0.75f;
        Material white = GetMaterial("FinishLight", Color.white);
        Material blue = GetMaterial("BoardBlue", new Color(0.16f, 0.36f, 0.85f));
        Material red = GetMaterial("BoardRed", new Color(0.85f, 0.2f, 0.2f));

        Primitive(PrimitiveType.Cube, "Surroundings", level, new Vector3(0, groundY - 0.15f, 10), new Vector3(400, 0.2f, 400),
            GetMaterial("Grass", new Color(0.36f, 0.66f, 0.28f)), false);

        // Boards around the pitch: blue at the far (enemy) end, red at the player's end.
        // They only face inwards, so they never block the camera when it swings outside.
        Transform boards = NewGroup("Boards", level);
        float boardY = groundY + 0.75f;
        Panel(boards, "Side Board", new Vector3(-20.2f, boardY, 10), new Vector2(40.8f, 1.5f), 270, white);
        Panel(boards, "Side Board", new Vector3(20.2f, boardY, 10), new Vector2(40.8f, 1.5f), 90, white);
        foreach (int side in new[] { -1, 1 })
        {
            Panel(boards, "Far Board", new Vector3(side * 12.7f, boardY, 30.2f), new Vector2(15, 1.5f), 0, blue);
            Panel(boards, "Near Board", new Vector3(side * 12.7f, boardY, -10.2f), new Vector2(15, 1.5f), 180, red);
        }

        // Goal frames
        Transform frames = NewGroup("Goal Frames", level);
        foreach (float z in new[] { 29.7f, -9.8f })
        {
            Material color = z > 0 ? blue : red;
            float turn = z > 0 ? 0 : 180;
            Panel(frames, "Post", new Vector3(-5.2f, groundY + 2.4f, z), new Vector2(0.5f, 4.8f), turn, color);
            Panel(frames, "Post", new Vector3(5.2f, groundY + 2.4f, z), new Vector2(0.5f, 4.8f), turn, color);
            Panel(frames, "Crossbar", new Vector3(0, groundY + 4.95f, z), new Vector2(10.9f, 0.5f), turn, color);
        }

        // Pitch markings
        Transform lines = NewGroup("Lines", level);
        float lineY = groundY + 0.02f;
        Primitive(PrimitiveType.Cube, "Halfway Line", lines, new Vector3(0, lineY, 10), new Vector3(40, 0.02f, 0.25f), white, false);
        Primitive(PrimitiveType.Cylinder, "Centre Spot", lines, new Vector3(0, lineY, 10), new Vector3(1.2f, 0.01f, 1.2f), white, false);
        foreach (float z in new[] { 24f, -4f })
        {
            Primitive(PrimitiveType.Cube, "Box Line", lines, new Vector3(0, lineY, z), new Vector3(18, 0.02f, 0.25f), white, false);
            float edge = z > 10 ? 27 : -7;
            Primitive(PrimitiveType.Cube, "Box Line", lines, new Vector3(-9, lineY, edge), new Vector3(0.25f, 0.02f, 6), white, false);
            Primitive(PrimitiveType.Cube, "Box Line", lines, new Vector3(9, lineY, edge), new Vector3(0.25f, 0.02f, 6), white, false);
        }

        // Trees and rocks outside the boards
        GameObject treePrefab = GetTreePrefab();
        Transform outside = NewGroup("Trees", level);
        for (int i = 0; i < 70; i++)
        {
            float angle = (float)(random.NextDouble() * Mathf.PI * 2);
            float distance = 34 + (float)random.NextDouble() * 40;
            Vector3 position = new Vector3(Mathf.Cos(angle) * distance, groundY, 10 + Mathf.Sin(angle) * distance);
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, outside);
            tree.transform.position = position;
            tree.transform.localScale = Vector3.one * (0.9f + (float)random.NextDouble() * 1.1f);
        }

        SumoGame game = new GameObject(ManagerName).AddComponent<SumoGame>();
        game.spawnManager = FindInScene<SpawnManagerX>(scene);
        game.ui = ui;
    }

    // ---------- Level helpers ----------

    static GameObject LoadPrefab(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(CourseLibrary + name + ".prefab");
    }

    static Material GetMaterial(string name, Color color, float smoothness = 0.1f, bool glow = false)
    {
        string path = "Assets/ThreeInOne/Materials/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.color = color;
        material.SetFloat("_Glossiness", smoothness);
        if (glow)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.55f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    static Transform NewGroup(string name, Transform parent)
    {
        Transform group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    static void Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = position;
        primitive.transform.localScale = scale;
        primitive.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
        }
    }

    // A flat panel that is only visible from the side it faces
    static void Panel(Transform parent, string name, Vector3 position, Vector2 size, float turn, Material material)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = name;
        panel.transform.SetParent(parent, false);
        panel.transform.SetPositionAndRotation(position, Quaternion.Euler(0, turn, 0));
        panel.transform.localScale = new Vector3(size.x, size.y, 1);
        MeshRenderer renderer = panel.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Object.DestroyImmediate(panel.GetComponent<Collider>());
    }

    static void Wall(Transform parent, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject("Wall");
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;
        wall.AddComponent<BoxCollider>().size = size;
    }

    static Transform FindByName(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }
        }
        return null;
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
        colors.selectedColor = Color.white; // only hovering highlights a button
        colors.pressedColor = new Color(1f, 0.55f, 0.15f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }
}
