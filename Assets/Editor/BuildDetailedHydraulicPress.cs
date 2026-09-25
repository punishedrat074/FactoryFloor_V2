using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Builds a separate machine. All geometry edits are restricted to the clone.
public static class BuildDetailedHydraulicPress
{
    private const string ScenePath = "Assets/UnityWarehouseSceneHDRP/Scene_Warehouse/WarehouseSceneSample.unity";
    private const string RootName = "HydraulicPress_Detailed";
    private static Material blue, dark, steel, chrome, rubber, yellow, red, oil;
    private static readonly Quaternion Face = Quaternion.Euler(90, 0, 0);
    private static readonly Quaternion Across = Quaternion.Euler(0, 0, 90);

    [MenuItem("Tools/Factory Floor/Build Detailed Hydraulic Press")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty)
        { Debug.LogError("Detailed press: open the saved warehouse scene outside Play mode first."); return; }
        var roots = scene.GetRootGameObjects();
        if (roots.Any(g => g.name == RootName))
        { Debug.LogWarning("Detailed press already exists; nothing changed."); return; }
        GameObject source = roots.FirstOrDefault(g => g.name == "HydraulicPress");
        if (!source) { Debug.LogError("HydraulicPress template is missing."); return; }
        blue = Load("Press_Blue"); dark = Load("Press_DarkSteel"); steel = Load("Press_Steel");
        chrome = Load("Press_Chrome"); rubber = Load("HoseBlack"); yellow = Load("Box_Yellow");
        red = Load("RedMat"); oil = Load("OilAmber");
        var protectedComponents = roots.SelectMany(g => g.GetComponentsInChildren<Component>(true))
            .Where(c => c != null).ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
        Directory.CreateDirectory("Temp");
        File.Copy(ScenePath, "Temp/DetailedPress.before.unity", true);
        GameObject copy = null;
        try
        {
            copy = Object.Instantiate(source);
            copy.name = RootName;
            var root = copy.transform;
            BuildFrame(root);
            BuildGuides(root);
            BuildPowerPack(root);
            BuildControls(root);
            Physics.SyncTransforms();
            Bounds original = BoundsOf(source), model = BoundsOf(copy);
            root.position = FindPlacement(copy, original, model);
            // ExecuteAlways hoses use world endpoints; update only this copy now.
            foreach (var hose in copy.GetComponentsInChildren<PipeBetween>())
                hose.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
            Physics.SyncTransforms();
            foreach (var pair in protectedComponents)
                if (!pair.Key || EditorJsonUtility.ToJson(pair.Key) != pair.Value)
                    throw new InvalidOperationException("A protected component changed; build was not saved: " + pair.Key);
            Undo.RegisterCreatedObjectUndo(copy, "Create detailed hydraulic press");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Bounds final = BoundsOf(copy);
            string report = "Detailed press built at " + root.position.ToString("F3") +
                "; size " + final.size.ToString("F3") + "; original bounds " + original +
                "; new bounds " + final + ". All existing components unchanged. Placement has 1.8 m machine separation and 0.65 m structure clearance.";
            File.WriteAllText("Temp/DetailedPress-build.txt", report);
            Debug.Log(report, copy);
            Selection.activeGameObject = copy;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.LookAt(final.center, Quaternion.Euler(17, 25, 0), 6.8f);
        }
        catch (Exception e)
        {
            if (copy) Object.DestroyImmediate(copy);
            Debug.LogException(e);
        }
    }

    private static void BuildFrame(Transform root)
    {
        Transform frame = Group("FrameReinforcement", root);
        foreach (float x in new[] { -1.4f, 1.4f })
        {
            string side = x < 0 ? "Left" : "Right";
            Find(root, side + "Column").localScale = V(.4f, 3.95f, .9f);
            Transform plate = Find(root, side + "ColumnFrontPlate");
            plate.localPosition = V(x, 2.15f, -.49f);
            Box(side + "RearFlange", frame, V(x, 2.15f, .49f), V(.65f, 3.5f, .08f), blue);
            Box(side + "FootShoe", frame, V(x, .53f, 0), V(.78f, .12f, 1.3f), dark);
            foreach (float z in new[] { -.56f, .56f })
            {
                Box("WeldedBaseStiffener", frame, V(x, .76f, z), V(.34f, .40f, .07f), blue);
                Box("CrownJointPlate", frame, V(x, 3.92f, z), V(.60f, .55f, .09f), dark);
                for (int i = 0; i < 7; i++)
                    Bolt(frame, V(x, .8f + i * .45f, z), Face, .058f);
                foreach (float dx in new[] { -.21f, .21f })
                    foreach (float y in new[] { 3.75f, 4.06f })
                        Bolt(frame, V(x + dx, y, z * 1.10f), Face, .065f);
            }
            foreach (float z in new[] { -.9f, .9f })
            {
                Box("AnchorPad", frame, V(x, .035f, z), V(.61f, .07f, .60f), rubber);
                foreach (float dx in new[] { -.20f, .20f })
                    Bolt(frame, V(x + dx, .38f, z), Quaternion.identity, .075f);
            }
        }
        Box("CrownTopFlange", frame, V(0, 4.405f, 0), V(3.64f, .06f, 1.12f), dark);
        Box("CrownLowerFlange", frame, V(0, 3.895f, 0), V(3.55f, .06f, 1.04f), dark);
        for (int i = 0; i < 8; i++) Bolt(frame, V(-1.35f + i * .385f, 4.15f, -.546f), Face, .055f);
        Box("CrownNameplate", frame, V(0, 4.15f, -.55f), V(.72f, .19f, .018f), steel);
        Box("NameplateInset", frame, V(0, 4.15f, -.563f), V(.61f, .105f, .009f), dark);
        Box("BedFrontMachinedLip", frame, V(0, 1.01f, -.809f), V(2.39f, .10f, .025f), steel);
        for (int i = 0; i < 5; i++)
        {
            Box("BolsterTSlot", frame, V(-.92f + i * .46f, 1.076f, 0), V(.035f, .003f, 1.5f), dark);
            Box("BedSupportRib", frame, V(-.78f + i * .39f, .63f, -.68f), V(.06f, .42f, .1f), blue);
        }
        Box("BaseFrontWearPlate", frame, V(0, .22f, -1.211f), V(3.42f, .18f, .024f), dark);
        Box("BaseSafetyBand", frame, V(0, .30f, -1.227f), V(3.32f, .035f, .008f), yellow);
    }

    private static void BuildGuides(Transform root)
    {
        Transform guides = Group("PlatenGuides", root);
        Transform moving = Group("PlatenGuideCarriage", Find(root, "Ram"));
        foreach (float x in new[] { -.99f, .99f })
        {
            Cyl("ChromeGuideRod", guides, V(x, 2.48f, 0), V(.105f, 1.38f, .105f), chrome);
            foreach (float y in new[] { 1.17f, 3.80f })
            {
                Cyl("GuideRodEndBlock", guides, V(x, y, 0), V(.22f, .065f, .22f), dark);
                Bolt(guides, V(x, y + .07f, 0), Quaternion.identity, .09f);
            }
            Box("PlatenGuideEar", moving, V(x, 1.95f, 0), V(.32f, .24f, .48f), steel);
            Cyl("BronzeGuideBushing", moving, V(x, 1.95f, 0), V(.19f, .18f, .19f), oil);
            Cyl("BushingDustSeal", moving, V(x, 2.14f, 0), V(.20f, .018f, .20f), rubber);
            foreach (float z in new[] { -.16f, .16f })
                Bolt(moving, V(x, 2.08f, z), Quaternion.identity, .044f);
        }
        Box("PlatenFrontFinish", moving, V(0, 1.95f, -.637f), V(1.68f, .17f, .025f), steel);
        for (int i = 0; i < 6; i++) Bolt(moving, V(-.69f + i * .276f, 1.95f, -.66f), Face, .045f);
        Transform cylinder = Group("CylinderFittings", root);
        foreach (float y in new[] { 2.94f, 3.84f })
        {
            Cyl("CylinderFlange", cylinder, V(0, y, 0), V(.67f, .045f, .67f), dark);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4;
                Bolt(cylinder, V(Mathf.Cos(a) * .275f, y + .053f, Mathf.Sin(a) * .275f), Quaternion.identity, .047f);
            }
        }
        foreach (float x in new[] { -.22f, .22f })
            foreach (float z in new[] { -.22f, .22f })
                Cyl("CylinderTieRod", cylinder, V(x, 3.4f, z), V(.035f, .42f, .035f), steel);
        Cyl("RodWiperSeal", cylinder, V(0, 2.672f, 0), V(.27f, .025f, .27f), rubber);
        Box("CylinderSpecificationPlate", cylinder, V(0, 3.39f, -.245f), V(.18f, .22f, .018f), dark);
    }

    private static void BuildPowerPack(Transform root)
    {
        Transform pack = Group("PowerPackDetails", root);
        // Dimensions follow the template tank and motor, without moving their hose anchors.
        Vector3 tank = Find(root, "HydraulicReservoir").localPosition;
        Box("ReservoirMountingSkid", pack, V(tank.x, .07f, tank.z), V(.98f, .14f, .91f), dark);
        Box("TankInspectionCover", pack, V(tank.x, .47f, tank.z - .341f), V(.54f, .48f, .028f), blue);
        foreach (float dx in new[] { -.22f, .22f })
            foreach (float y in new[] { .29f, .65f })
                Bolt(pack, V(tank.x + dx, y, tank.z - .365f), Face, .042f);
        for (int i = 0; i < 9; i++)
            Cyl("MotorCoolingFin", pack, V(1.94f + i * .055f, 1.018f, .574f), V(.335f, .012f, .335f), steel, Across);
        Box("MotorTerminalBox", pack, V(2.16f, 1.218f, .574f), V(.24f, .12f, .21f), dark);
        Cyl("MotorFanGrille", pack, V(2.60f, 1.018f, .574f), V(.265f, .014f, .265f), rubber, Across);
        for (int i = 0; i < 5; i++)
            Box("FanGuardSlat", pack, V(2.617f, .918f + i * .05f, .574f), V(.012f, .012f, .20f), steel);
        Box("ValveManifold", pack, V(2.1f, .95f, .93f), V(.45f, .20f, .15f), steel);
        foreach (float x in new[] { 1.98f, 2.22f })
        {
            Cyl("SolenoidValve", pack, V(x, 1.13f, .93f), V(.105f, .10f, .105f), dark);
            Box("ValveConnector", pack, V(x, 1.245f, .93f), V(.075f, .06f, .08f), rubber);
        }
        Cyl("ReturnLineFilter", pack, V(2.50f, .59f, 1.0f), V(.18f, .23f, .18f), steel);
        Cyl("FilterHead", pack, V(2.50f, .845f, 1.0f), V(.23f, .035f, .23f), dark);
        Pipe(pack, "FilterReturn", V(2.50f, .88f, 1.0f), V(2.28f, .95f, 1.0f), .038f, rubber);
        Cyl("TankDrainPlug", pack, V(2.50f, .23f, .574f), V(.085f, .035f, .085f), chrome, Across);
        foreach (float x in new[] { 1.716f, 1.836f })
        {
            Cyl("PumpCompressionFitting", pack, V(x, 1.17f, .574f), V(.093f, .058f, .093f), steel);
            foreach (float y in new[] { 1.65f, 2.65f, 3.6f })
            {
                if (x < 1.8f) Box("HoseClampBracket", pack, V(1.70f, y, .574f), V(.35f, .07f, .05f), dark);
                Cyl("HoseClampCollar", pack, V(x, y, .574f), V(.084f, .044f, .084f), steel);
            }
        }
        Pipe(pack, "GaugeStem", V(1.98f, 1.05f, .93f), V(1.98f, 1.48f, .93f), .035f, chrome);
        Dial(pack, V(1.98f, 1.56f, .88f), .22f);
    }

    private static void BuildControls(Transform root)
    {
        // Replace only the clone's low control box with a raised operator station.
        Object.DestroyImmediate(Find(root, "ControlBox").gameObject);
        Transform panel = Group("OperatorStation", root);
        Box("ConsolePost", panel, V(2.10f, .67f, -.55f), V(.12f, 1.20f, .12f), dark);
        Box("ConsoleFoot", panel, V(2.10f, .13f, -.55f), V(.42f, .10f, .45f), blue);
        Box("ControlCabinet", panel, V(2.10f, 1.47f, -.55f), V(.66f, .62f, .30f), blue, true);
        Box("PanelDoor", panel, V(2.10f, 1.47f, -.715f), V(.60f, .55f, .025f), steel);
        Box("DisplayBezel", panel, V(2.10f, 1.61f, -.735f), V(.36f, .16f, .02f), dark);
        Box("DisplayFace", panel, V(2.10f, 1.61f, -.749f), V(.29f, .10f, .01f), rubber);
        for (int i = 0; i < 4; i++)
            Box("DisplayReadout", panel, V(2.01f + i * .06f, 1.61f, -.756f), V(.031f, .048f, .004f), steel);
        foreach (float x in new[] { 1.93f, 2.10f, 2.27f })
            Cyl("ButtonBezel", panel, V(x, 1.35f, -.74f), V(.09f, .018f, .09f), dark, Face);
        Cyl("Start", panel, V(1.93f, 1.35f, -.77f), V(.063f, .014f, .063f), Load("StatusLight"), Face);
        Cyl("Stop", panel, V(2.10f, 1.35f, -.77f), V(.063f, .014f, .063f), red, Face);
        Cyl("EStopSafetyRing", panel, V(2.27f, 1.35f, -.77f), V(.12f, .012f, .12f), yellow, Face);
        Cyl("EmergencyStop", panel, V(2.27f, 1.35f, -.803f), V(.09f, .024f, .09f), red, Face);
        foreach (float x in new[] { 1.86f, 2.34f })
            foreach (float y in new[] { 1.23f, 1.70f }) Bolt(panel, V(x, y, -.741f), Face, .028f);
        Pipe(panel, "ConsoleConduit", V(2.10f, .48f, -.5f), V(2.10f, .48f, .21f), .035f, rubber);
        Cyl("BeaconStem", panel, V(2.10f, 1.92f, -.55f), V(.045f, .14f, .045f), dark);
        Renderer status = null;
        for (int i = 0; i < 3; i++)
        {
            var lamp = Cyl("StackLight" + i, panel, V(2.10f, 2.105f + i * .10f, -.55f),
                V(.115f, .04f, .115f), i == 0 ? Load("StatusLight") : i == 1 ? yellow : red);
            if (i == 0) status = lamp.GetComponent<Renderer>();
            Cyl("BeaconSeparator", panel, V(2.10f, 2.153f + i * .10f, -.55f), V(.123f, .009f, .123f), dark);
        }
        root.GetComponent<HydraulicPressController>().statusLight = status;
    }

    private static Vector3 FindPlacement(GameObject copy, Bounds original, Bounds model)
    {
        Vector3 initial = copy.transform.position;
        Vector3[] offsets = {
            V(original.min.x - model.max.x - 1.8f, 0, 0),
            V(0, 0, original.max.z - model.min.z + 2.2f),
            V(original.max.x - model.min.x + 1.8f, 0, 0),
            V(0, 0, original.min.z - model.max.z - 2.2f)
        };
        var probe = new GameObject("PlacementProbe");
        var box = probe.AddComponent<BoxCollider>();
        box.size = V(model.size.x + 1.30f, model.size.y - .20f, model.size.z + 1.30f);
        try
        {
            foreach (Vector3 offset in offsets)
            {
                Vector3 centre = model.center + offset + V(0, .11f, 0);
                probe.transform.position = centre;
                Physics.SyncTransforms();
                bool blocked = false;
                foreach (Collider c in Physics.OverlapBox(centre, box.size * .5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (c == box || c.transform.IsChildOf(copy.transform)) continue;
                    if (Physics.ComputePenetration(box, centre, Quaternion.identity, c, c.transform.position,
                        c.transform.rotation, out _, out float depth) && depth > .005f)
                    { blocked = true; break; }
                }
                if (blocked) continue;
                // Require supporting floor at every corner, excluding the temporary probe and machine.
                foreach (float x in new[] { -.5f, .5f })
                    foreach (float z in new[] { -.5f, .5f })
                    {
                        Vector3 ray = V(centre.x + model.size.x * x, initial.y + .12f, centre.z + model.size.z * z);
                        if (!Physics.RaycastAll(ray, Vector3.down, .40f, ~0, QueryTriggerInteraction.Ignore)
                            .Any(h => h.collider != box && !h.transform.IsChildOf(copy.transform) && h.normal.y > .8f))
                            blocked = true;
                    }
                if (!blocked) return initial + offset;
            }
        }
        finally { Object.DestroyImmediate(probe); }
        throw new InvalidOperationException("No adjacent position passed floor and clearance checks. No machine saved.");
    }

    private static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
    private static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/" + name + ".mat")
        ?? throw new InvalidOperationException("Missing material " + name);
    private static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    private static Transform Group(string name, Transform parent)
    { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    private static Transform Part(string name, PrimitiveType type, Transform p, Vector3 pos, Vector3 scale, Material mat, Quaternion rot, bool solid)
    {
        var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(p, false);
        g.transform.localPosition = pos; g.transform.localRotation = rot; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }
    private static Transform Box(string n, Transform p, Vector3 v, Vector3 s, Material m, bool solid = false) => Part(n, PrimitiveType.Cube, p, v, s, m, Quaternion.identity, solid);
    private static Transform Cyl(string n, Transform p, Vector3 v, Vector3 s, Material m, Quaternion? q = null) => Part(n, PrimitiveType.Cylinder, p, v, s, m, q ?? Quaternion.identity, false);
    private static void Bolt(Transform p, Vector3 v, Quaternion q, float diameter)
    {
        Cyl("BoltWasher", p, v, V(diameter * 1.45f, .009f, diameter * 1.45f), steel, q);
        Cyl("BoltHead", p, v + q * Vector3.up * .017f, V(diameter, .013f, diameter), dark, q);
    }
    private static void Pipe(Transform p, string n, Vector3 a, Vector3 b, float width, Material mat)
    { Cyl(n, p, (a + b) * .5f, V(width, (b - a).magnitude * .5f, width), mat, Quaternion.FromToRotation(Vector3.up, b - a)); }
    private static void Dial(Transform p, Vector3 v, float d)
    {
        Cyl("PressureGaugeCase", p, v, V(d, .038f, d), dark, Face);
        Cyl("PressureGaugeFace", p, v + V(0, 0, -.04f), V(d * .86f, .005f, d * .86f), steel, Face);
        for (int i = 0; i < 9; i++)
        {
            float a = (-130 + i * 32.5f) * Mathf.Deg2Rad;
            Vector3 pt = v + V(Mathf.Sin(a) * d * .34f, Mathf.Cos(a) * d * .34f, -.048f);
            var tick = Box("GaugeGraduation", p, pt, V(.008f, .024f, .005f), dark);
            tick.localRotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg);
        }
        var needle = Box("PressureNeedle", p, v + V(-.021f, .021f, -.053f), V(.008f, .09f, .005f), red);
        needle.localRotation = Quaternion.Euler(0, 0, 45);
    }
}
