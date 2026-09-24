using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildPhysicalLathe
{
    private const string ScenePath = "Assets/UnityWarehouseSceneHDRP/Scene_Warehouse/WarehouseSceneSample.unity";

    private static Material blue;
    private static Material dark;
    private static Material steel;
    private static Material chrome;
    private static Material rubber;
    private static Material yellow;
    private static Material red;

    [MenuItem("Tools/Factory Floor/Build Physical Lathe")]
    private static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("Open WarehouseSceneSample before building the lathe.");
            return;
        }

        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.name == "Lathe")
            {
                Debug.LogWarning("Lathe already exists. No duplicate was created.");
                return;
            }
        }

        blue = Load("Press_Blue");
        dark = Load("Press_DarkSteel");
        steel = Load("Press_Steel");
        chrome = Load("Press_Chrome");
        rubber = Load("HoseBlack");
        yellow = Load("Box_Yellow");
        red = Load("RedMat");
        if (blue == null || dark == null || steel == null || chrome == null ||
            rubber == null || yellow == null || red == null) return;

        Transform lathe = Group("Lathe", null);
        lathe.position = new Vector3(6.485f, 0.02f, 42.26f);
        lathe.rotation = Quaternion.Euler(0f, -90f, 0f);

        Transform baseGroup = Group("Base", lathe);
        Transform bedGroup = Group("Bed", lathe);
        Transform headstock = Group("Headstock", lathe);
        Transform spindle = Group("Spindle", lathe);
        Transform chuck = Group("Chuck", lathe);
        Transform tailstock = Group("Tailstock", lathe);
        Transform toolPost = Group("ToolPost", lathe);
        Transform cuttingTool = Group("CuttingTool", lathe);

        BuildBase(baseGroup);
        BuildBed(bedGroup);
        BuildHeadstock(headstock);
        BuildSpindle(spindle);
        BuildChuck(chuck);
        BuildTailstock(tailstock);
        BuildToolPost(toolPost);
        BuildCuttingTool(cuttingTool);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = lathe.gameObject;
        Debug.Log("Built the physical lathe at (6.485, 0.02, 42.26), facing the press. No animation or audio was added.");
    }

    private static void BuildBase(Transform parent)
    {
        Cube("FootprintPlate", parent, V(0, .11f, 0), V(3.8f, .22f, 1.5f), blue, true);
        Cube("HeadstockPedestal", parent, V(-1.32f, .52f, 0), V(.86f, .78f, 1.2f), blue, true);
        Cube("TailstockPedestal", parent, V(1.30f, .52f, 0), V(.88f, .78f, 1.2f), blue, true);
        Cube("UndersideBridge", parent, V(0, .71f, .13f), V(2.08f, .30f, .56f), dark, true);
        Cube("HeadstockCabinetDoor", parent, V(-1.32f, .54f, -.613f), V(.67f, .56f, .024f), dark);
        Cube("TailstockCabinetDoor", parent, V(1.30f, .54f, -.613f), V(.68f, .56f, .024f), dark);
        Cube("HeadstockDoorHandle", parent, V(-1.04f, .54f, -.637f), V(.08f, .21f, .025f), chrome);
        Cube("TailstockDoorHandle", parent, V(1.57f, .54f, -.637f), V(.08f, .21f, .025f), chrome);
        foreach (float x in new[] { -1.55f, 1.55f })
        {
            foreach (float z in new[] { -.56f, .56f })
                Cube("RubberLevelingFoot", parent, V(x, .025f, z), V(.42f, .05f, .27f), rubber);
        }
        Cube("FrontSafetyStripe", parent, V(0, .12f, -.754f), V(3.72f, .045f, .012f), yellow);
    }

    private static void BuildBed(Transform parent)
    {
        Cube("ChipPan", parent, V(0, .91f, 0), V(3.53f, .10f, 1.34f), dark, true);
        Cube("CastIronBed", parent, V(0, 1.04f, 0), V(3.46f, .22f, 1.08f), dark, true);
        foreach (float z in new[] { -.37f, .37f })
        {
            Cube("MachinedWay", parent, V(0, 1.18f, z), V(3.42f, .09f, .15f), steel);
            Cube("WayEdge", parent, V(0, 1.235f, z), V(3.36f, .025f, .065f), chrome);
        }
        Cube("FrontRack", parent, V(0, 1.025f, -.565f), V(3.2f, .075f, .045f), steel);
        Cylinder("LeadScrew", parent, V(0, .84f, -.61f), V(.045f, 1.68f, .045f), chrome,
            Quaternion.Euler(0, 0, 90));
        Cube("LeftEndCap", parent, V(-1.76f, 1.08f, 0), V(.08f, .26f, 1.20f), blue);
        Cube("RightEndCap", parent, V(1.76f, 1.08f, 0), V(.08f, .26f, 1.20f), blue);
    }

    private static void BuildHeadstock(Transform parent)
    {
        Cube("HeadstockCasting", parent, V(-1.31f, 1.515f, 0), V(.94f, .83f, 1.14f), blue, true);
        Cube("GearboxCover", parent, V(-1.31f, 1.965f, 0), V(.99f, .07f, 1.16f), dark);
        Cube("FrontControlPanel", parent, V(-1.30f, 1.51f, -.584f), V(.72f, .58f, .032f), dark);
        Cube("MotorHousing", parent, V(-1.30f, 1.17f, .50f), V(.68f, .42f, .38f), blue);
        Cube("DriveCover", parent, V(-1.76f, 1.46f, .05f), V(.16f, .84f, .82f), dark);
        for (int i = 0; i < 5; i++)
            Cube("CoolingFin", parent, V(-1.65f + i * .14f, 1.65f, .578f),
                V(.055f, .34f, .025f), dark);
        Cylinder("SpindleBearingHousing", parent, V(-.80f, 1.53f, 0), V(.32f, .07f, .32f),
            dark, Quaternion.Euler(0, 0, 90));
        Cylinder("SpeedDial", parent, V(-1.47f, 1.61f, -.62f), V(.12f, .035f, .12f),
            chrome, Quaternion.Euler(90, 0, 0));
        Cylinder("FeedDial", parent, V(-1.15f, 1.61f, -.62f), V(.12f, .035f, .12f),
            chrome, Quaternion.Euler(90, 0, 0));
        Cube("ControlLegend", parent, V(-1.31f, 1.38f, -.608f), V(.52f, .07f, .01f), steel);
        Cylinder("EmergencyStop", parent, V(-1.48f, 1.38f, -.635f), V(.082f, .045f, .082f),
            red, Quaternion.Euler(90, 0, 0));
        Cylinder("StartButton", parent, V(-1.31f, 1.28f, -.633f), V(.065f, .035f, .065f),
            yellow, Quaternion.Euler(90, 0, 0));
        Cylinder("StopButton", parent, V(-1.12f, 1.28f, -.633f), V(.065f, .035f, .065f),
            red, Quaternion.Euler(90, 0, 0));
        Cube("CautionTab", parent, V(-1.09f, 1.82f, -.608f), V(.14f, .07f, .015f), yellow);
    }

    private static void BuildSpindle(Transform parent)
    {
        Cylinder("SpindleShaft", parent, V(-.68f, 1.53f, 0), V(.165f, .22f, .165f),
            chrome, Quaternion.Euler(0, 0, 90));
        Cylinder("SpindleShoulder", parent, V(-.77f, 1.53f, 0), V(.245f, .055f, .245f),
            steel, Quaternion.Euler(0, 0, 90));
        Cylinder("SpindleNose", parent, V(-.45f, 1.53f, 0), V(.21f, .06f, .21f),
            dark, Quaternion.Euler(0, 0, 90));
    }

    private static void BuildChuck(Transform parent)
    {
        Cylinder("ThreeJawChuckBody", parent, V(-.31f, 1.53f, 0), V(.34f, .145f, .34f),
            dark, Quaternion.Euler(0, 0, 90));
        Cylinder("ChuckFace", parent, V(-.145f, 1.53f, 0), V(.315f, .035f, .315f),
            steel, Quaternion.Euler(0, 0, 90));
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f;
            float radians = angle * Mathf.Deg2Rad;
            Cube("ChuckJaw" + (i + 1), parent,
                V(-.08f, 1.53f + Mathf.Cos(radians) * .19f, Mathf.Sin(radians) * .19f),
                V(.16f, .19f, .12f), chrome, false, Quaternion.Euler(angle, 0, 0));
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            Cylinder("FaceBolt" + (i + 1), parent,
                V(-.102f, 1.53f + Mathf.Cos(a) * .27f, Mathf.Sin(a) * .27f),
                V(.035f, .012f, .035f), dark, Quaternion.Euler(0, 0, 90));
        }
        Cylinder("LatheWorkpiece", parent, V(.19f, 1.53f, 0), V(.12f, .35f, .12f),
            chrome, Quaternion.Euler(0, 0, 90));
    }

    private static void BuildTailstock(Transform parent)
    {
        Cube("TailstockSlide", parent, V(1.22f, 1.235f, 0), V(.91f, .21f, .86f), blue, true);
        Cube("TailstockCasting", parent, V(1.27f, 1.49f, 0), V(.70f, .52f, .69f), blue, true);
        Cube("TailstockCover", parent, V(1.27f, 1.78f, 0), V(.74f, .065f, .72f), dark);
        Cylinder("Quill", parent, V(.86f, 1.53f, 0), V(.15f, .22f, .15f),
            chrome, Quaternion.Euler(0, 0, 90));
        Cylinder("LiveCenterBase", parent, V(.635f, 1.53f, 0), V(.115f, .075f, .115f),
            steel, Quaternion.Euler(0, 0, 90));
        Sphere("LiveCenterPoint", parent, V(.54f, 1.53f, 0), V(.11f, .11f, .11f), chrome);
        Cylinder("QuillHandwheel", parent, V(1.71f, 1.53f, 0), V(.22f, .035f, .22f),
            dark, Quaternion.Euler(0, 0, 90));
        Cylinder("QuillHandwheelHub", parent, V(1.75f, 1.53f, 0), V(.075f, .035f, .075f),
            chrome, Quaternion.Euler(0, 0, 90));
        Cube("TailstockLock", parent, V(1.40f, 1.80f, -.23f), V(.055f, .13f, .055f), chrome);
    }

    private static void BuildToolPost(Transform parent)
    {
        Cube("CarriageSaddle", parent, V(.28f, 1.205f, -.04f), V(.84f, .17f, 1.08f), blue, true);
        Cube("CarriageApron", parent, V(.28f, .97f, -.615f), V(.82f, .39f, .105f), blue, true);
        Cube("CrossSlide", parent, V(.28f, 1.32f, -.32f), V(.64f, .13f, .62f), steel);
        Cube("CompoundSlide", parent, V(.28f, 1.425f, -.39f), V(.51f, .09f, .47f), dark);
        Cube("ToolHolder", parent, V(.28f, 1.505f, -.39f), V(.27f, .14f, .28f), dark);
        Cube("ToolHolderClamp", parent, V(.28f, 1.585f, -.38f), V(.28f, .035f, .20f), chrome);
        Cylinder("CarriageHandwheel", parent, V(.04f, .97f, -.70f), V(.14f, .035f, .14f),
            chrome, Quaternion.Euler(90, 0, 0));
        Cylinder("CrossFeedHandwheel", parent, V(.53f, 1.33f, -.655f), V(.11f, .03f, .11f),
            dark, Quaternion.Euler(90, 0, 0));
        foreach (float x in new[] { .08f, .48f })
            Cube("SlideLock", parent, V(x, 1.51f, -.55f), V(.055f, .075f, .055f), chrome);
    }

    private static void BuildCuttingTool(Transform parent)
    {
        Cube("ToolShank", parent, V(.28f, 1.53f, -.32f), V(.31f, .075f, .30f), dark);
        Cube("CarbideInsert", parent, V(.28f, 1.53f, -.205f), V(.11f, .025f, .11f),
            chrome, false, Quaternion.Euler(0, 45, 0));
        Cube("InsertClamp", parent, V(.28f, 1.565f, -.29f), V(.10f, .018f, .09f), steel);
    }

    private static Material Load(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/" + name + ".mat");
        if (material == null) Debug.LogError("Missing material: " + name);
        return material;
    }

    private static Transform Group(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build physical lathe");
        if (parent != null) go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    private static Transform Cube(string name, Transform parent, Vector3 position, Vector3 scale,
        Material material, bool keepCollider = false, Quaternion? rotation = null)
        => Part(name, PrimitiveType.Cube, parent, position, scale, material, keepCollider, rotation);

    private static Transform Cylinder(string name, Transform parent, Vector3 position, Vector3 scale,
        Material material, Quaternion rotation)
        => Part(name, PrimitiveType.Cylinder, parent, position, scale, material, false, rotation);

    private static Transform Sphere(string name, Transform parent, Vector3 position, Vector3 scale,
        Material material)
        => Part(name, PrimitiveType.Sphere, parent, position, scale, material, false, null);

    private static Transform Part(string name, PrimitiveType primitive, Transform parent,
        Vector3 position, Vector3 scale, Material material, bool keepCollider, Quaternion? rotation)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        Undo.RegisterCreatedObjectUndo(go, "Build physical lathe");
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation ?? Quaternion.identity;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
        }
        return go.transform;
    }
}
