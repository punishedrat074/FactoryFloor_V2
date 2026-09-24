using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildReservoirDetails
{
    [MenuItem("Tools/Hydraulic Press/Build Reservoir Details")]
    private static void Build()
    {
        GameObject press = GameObject.Find("HydraulicPress");
        Transform system = press != null ? press.transform.Find("HydraulicSystem") : null;
        Transform tank = system != null ? system.Find("HydraulicReservoir") : null;
        Transform cap = system != null ? system.Find("CylinderTopCap") : null;
        if (tank == null || cap == null)
        {
            Debug.LogError("HydraulicSystem needs HydraulicReservoir and CylinderTopCap before building the details.");
            return;
        }

        if (system.Find("ReservoirDetails") != null)
        {
            Debug.LogWarning("ReservoirDetails already exists. No duplicate parts were added.");
            return;
        }

        Material darkSteel = LoadMaterial("Press_DarkSteel");
        Material steel = LoadMaterial("Press_Steel");
        Material oil = LoadMaterial("OilAmber");
        Material hose = LoadMaterial("HoseBlack");
        if (darkSteel == null || steel == null || oil == null || hose == null) return;

        Transform details = CreateEmpty("ReservoirDetails", system, Vector3.zero);
        Vector3 tankCentre = tank.localPosition;
        Vector3 tankSize = tank.localScale;
        float tankTop = tankCentre.y + tankSize.y * 0.5f;

        CreatePart("TankLid", PrimitiveType.Cube, details,
            new Vector3(tankCentre.x, tankTop + 0.02f, tankCentre.z),
            new Vector3(tankSize.x + 0.05f, 0.04f, tankSize.z + 0.05f),
            Quaternion.identity, darkSteel);

        float motorY = tankTop + 0.20f;
        CreatePart("Motor", PrimitiveType.Cylinder, details,
            new Vector3(tankCentre.x + 0.05f, motorY, tankCentre.z),
            new Vector3(0.3f, 0.3f, 0.3f), Quaternion.Euler(0f, 0f, 90f), steel);
        CreatePart("MotorCap", PrimitiveType.Cylinder, details,
            new Vector3(tankCentre.x + 0.41f, motorY, tankCentre.z),
            new Vector3(0.32f, 0.06f, 0.32f), Quaternion.Euler(0f, 0f, 90f), darkSteel);
        CreatePart("Pump", PrimitiveType.Cylinder, details,
            new Vector3(tankCentre.x - 0.35f, motorY, tankCentre.z),
            new Vector3(0.2f, 0.12f, 0.2f), Quaternion.Euler(0f, 0f, 90f), darkSteel);
        CreatePart("BreatherCap", PrimitiveType.Cylinder, details,
            new Vector3(tankCentre.x + 0.23f, tankTop + 0.08f, tankCentre.z - 0.20f),
            new Vector3(0.1f, 0.04f, 0.1f), Quaternion.identity, darkSteel);
        CreatePart("SightGauge", PrimitiveType.Cube, details,
            new Vector3(tankCentre.x + 0.18f, tankCentre.y, tankCentre.z - tankSize.z * 0.5f - 0.01f),
            new Vector3(0.05f, 0.3f, 0.02f), Quaternion.identity, oil);

        // Both starts sit on top of the pump. The second line is a copy shifted
        // 0.12 m along X, so all three anchors and the elbow stay parallel.
        float pumpX = tankCentre.x - 0.35f;
        float pumpTop = motorY + 0.10f;
        float halfSpacing = 0.06f;
        float capRadius = cap.localScale.z * 0.5f;
        float capSideZ = cap.localPosition.z + Mathf.Sqrt(capRadius * capRadius - halfSpacing * halfSpacing);
        CreateHose(details, "", pumpX + halfSpacing, tankCentre.z,
            cap.localPosition.x + halfSpacing, capSideZ,
            pumpTop, cap.localPosition.y, hose);
        CreateHose(details, "2", pumpX - halfSpacing, tankCentre.z,
            cap.localPosition.x - halfSpacing, capSideZ,
            pumpTop, cap.localPosition.y, hose);

        EditorSceneManager.MarkSceneDirty(details.gameObject.scene);
        EditorSceneManager.SaveScene(details.gameObject.scene);
        Selection.activeGameObject = details.gameObject;
        Debug.Log("Built reservoir details and two hose lines. The reservoir collider was retained.");
    }

    private static Material LoadMaterial(string name)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/" + name + ".mat");
        if (material == null) Debug.LogError("Missing material: " + name);
        return material;
    }

    private static Transform CreateEmpty(string name, Transform parent, Vector3 localPosition)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build reservoir details");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        return go.transform;
    }

    private static Transform CreatePart(string name, PrimitiveType primitive, Transform parent,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(primitive);
        Undo.RegisterCreatedObjectUndo(go, "Build reservoir details");
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Undo.DestroyObjectImmediate(collider);
        return go.transform;
    }

    private static void CreateHose(Transform details, string suffix, float startX, float startZ,
        float endX, float endZ, float startY, float endY, Material material)
    {
        Transform start = CreateEmpty("H_Start" + suffix, details, new Vector3(startX, startY, startZ));
        Transform up = CreateEmpty("H_Up" + suffix, details, new Vector3(startX, endY, startZ));
        Transform end = CreateEmpty("H_End" + suffix, details, new Vector3(endX, endY, endZ));

        CreatePipe("HoseVertical" + suffix, details, start, up, material);
        CreatePipe("HoseHorizontal" + suffix, details, up, end, material);
        CreatePart("HoseElbow" + suffix, PrimitiveType.Sphere, details, up.localPosition,
            Vector3.one * 0.075f, Quaternion.identity, material);
    }

    private static void CreatePipe(string name, Transform parent, Transform pointA,
        Transform pointB, Material material)
    {
        Transform pipe = CreatePart(name, PrimitiveType.Cylinder, parent,
            Vector3.zero, Vector3.one, Quaternion.identity, material);
        PipeBetween component = Undo.AddComponent<PipeBetween>(pipe.gameObject);
        component.pointA = pointA;
        component.pointB = pointB;

        Vector3 direction = pointB.position - pointA.position;
        pipe.position = (pointA.position + pointB.position) * 0.5f;
        pipe.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        pipe.localScale = new Vector3(component.thickness, direction.magnitude * 0.5f, component.thickness);
    }
}
