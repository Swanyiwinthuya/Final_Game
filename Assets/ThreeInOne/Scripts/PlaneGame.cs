using UnityEngine;

// Fly Like a Bird: steer the plane through the gap in every wall and reach the finish line.
public class PlaneGame : MonoBehaviour
{
    public Transform plane;
    public GameUI ui;
    public Transform[] scenery; // sky and mountains travel with the plane

    [Header("Level")]
    public int gateCount = 8;
    public float firstGateZ = 50;
    public float gateSpacing = 45;
    public float gapSize = 10;
    public float wallThickness = 3;
    public float wallWidth = 14;
    public float floorY = -20;
    public float ceilingY = 42;

    [Header("Materials")]
    public Material wallMaterialA;
    public Material wallMaterialB;
    public Material trimMaterial;
    public Material groundMaterial;
    public Material cloudMaterial;
    public Material finishLightMaterial;
    public Material finishDarkMaterial;

    // Points along the plane's body (in its local space) used to detect crashes
    private static readonly Vector3[] BodyPoints =
    {
        new Vector3(0, 2.4f, 3.4f), new Vector3(0, 2.4f, 0.2f), new Vector3(0, 2.4f, -3f),
        new Vector3(0, 2.4f, -6.2f), new Vector3(0, 2.4f, -9.4f)
    };
    private const float BodyHalfHeight = 0.8f;

    private float[] gapCenters;
    private int score;
    private float messageUntil;
    private float lastPlaneZ;

    public float FinishZ { get; private set; }
    public int Score { get { return score; } }

    void Awake()
    {
        BuildLevel();
    }

    void Start()
    {
        lastPlaneZ = plane.position.z;
        ui.SetHud("Gate 0 / " + gateCount);
        ui.SetProgress(0);
        ui.SetCenterMessage("UP / DOWN arrows to steer through the gaps!");
        messageUntil = Time.time + 3.5f;
    }

    void Update()
    {
        if (ui.GameEnded)
        {
            return;
        }

        if (Time.time > messageUntil)
        {
            ui.SetCenterMessage("");
        }

        float tailZ = float.MaxValue;
        foreach (Vector3 bodyPoint in BodyPoints)
        {
            Vector3 point = plane.TransformPoint(bodyPoint);
            tailZ = Mathf.Min(tailZ, point.z);

            if (point.y - BodyHalfHeight < floorY)
            {
                ui.ShowLose("You hit the ground!\nGate " + score + " / " + gateCount);
                return;
            }

            for (int i = 0; i < gateCount; i++)
            {
                bool insideWall = Mathf.Abs(point.z - GateZ(i)) < wallThickness / 2;
                bool insideGap = point.y - BodyHalfHeight > GapCenter(i) - gapSize / 2
                    && point.y + BodyHalfHeight < GapCenter(i) + gapSize / 2;
                if (insideWall && !insideGap)
                {
                    ui.ShowLose("You hit a wall!\nGate " + score + " / " + gateCount);
                    return;
                }
            }
        }

        int passed = 0;
        for (int i = 0; i < gateCount; i++)
        {
            if (tailZ > GateZ(i) + wallThickness / 2)
            {
                passed++;
            }
        }
        if (passed != score)
        {
            score = passed;
            ui.SetHud("Gate " + score + " / " + gateCount);
        }

        ui.SetProgress(plane.position.z / FinishZ);
        if (plane.position.z > FinishZ)
        {
            ui.ShowWin("You flew through all " + gateCount + " gates!");
        }
    }

    void LateUpdate()
    {
        // Keep the plane below the top of the walls
        if (plane.position.y > ceilingY)
        {
            plane.position = new Vector3(plane.position.x, ceilingY, plane.position.z);
        }

        float delta = plane.position.z - lastPlaneZ;
        lastPlaneZ = plane.position.z;
        foreach (Transform item in scenery)
        {
            item.position += new Vector3(0, 0, delta);
        }
    }

    public float GateZ(int index)
    {
        return firstGateZ + index * gateSpacing;
    }

    public float GapCenter(int index)
    {
        return gapCenters[index];
    }

    // ---------- Level building ----------

    private void BuildLevel()
    {
        Transform level = new GameObject("Level").transform;
        gapCenters = new float[gateCount];
        float lowest = floorY + gapSize / 2 + 6;
        float highest = ceilingY - gapSize / 2 - 8;
        float previous = 1;

        for (int i = 0; i < gateCount; i++)
        {
            // Keep each gap within easy reach of the one before it
            float center = Mathf.Clamp(previous + Random.Range(-12f, 12f), lowest, highest);
            gapCenters[i] = center;
            previous = center;

            Material material = i % 2 == 0 ? wallMaterialA : wallMaterialB;
            BuildWall(level, GateZ(i), floorY - 1, center - gapSize / 2, true, material);
            BuildWall(level, GateZ(i), center + gapSize / 2, ceilingY + 25, false, material);
        }

        FinishZ = GateZ(gateCount - 1) + gateSpacing;
        BuildFinishLine(level);

        float length = FinishZ + 400;
        CreatePrimitive(PrimitiveType.Cube, "Ground", level, new Vector3(-150, floorY - 1, length / 2 - 150),
            new Vector3(460, 2, length), groundMaterial);

        for (int i = 0; i < 45; i++)
        {
            BuildCloud(level, new Vector3(Random.Range(-120f, -30f), Random.Range(-8f, 55f), Random.Range(-80f, FinishZ + 150)));
        }
    }

    private void BuildWall(Transform parent, float z, float bottomY, float topY, bool trimOnTop, Material material)
    {
        float height = topY - bottomY;
        CreatePrimitive(PrimitiveType.Cube, "Wall", parent, new Vector3(0, bottomY + height / 2, z),
            new Vector3(wallWidth, height, wallThickness), material);

        // Bright edge so the gap is easy to read
        float trimY = trimOnTop ? topY - 0.4f : bottomY + 0.4f;
        CreatePrimitive(PrimitiveType.Cube, "Wall Trim", parent, new Vector3(0, trimY, z),
            new Vector3(wallWidth + 1, 0.8f, wallThickness + 1), trimMaterial);
    }

    private void BuildFinishLine(Transform parent)
    {
        // Checkered banner standing just behind the flight path
        int rows = Mathf.CeilToInt((ceilingY + 10 - floorY) / 2);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                Material material = (row + column) % 2 == 0 ? finishLightMaterial : finishDarkMaterial;
                CreatePrimitive(PrimitiveType.Cube, "Finish", parent,
                    new Vector3(-8, floorY + 1 + row * 2, FinishZ + column * 2), new Vector3(0.5f, 2, 2), material);
            }
        }
    }

    private void BuildCloud(Transform parent, Vector3 position)
    {
        float size = Random.Range(5f, 11f);
        for (int i = 0; i < 3; i++)
        {
            Vector3 offset = new Vector3(0, Random.Range(-0.2f, 0.3f) * size, (i - 1) * size * 0.6f);
            float puff = size * Random.Range(0.7f, 1.1f);
            CreatePrimitive(PrimitiveType.Sphere, "Cloud", parent, position + offset,
                new Vector3(puff, puff * 0.6f, puff), cloudMaterial);
        }
    }

    private static void CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        primitive.transform.SetParent(parent);
        primitive.transform.position = position;
        primitive.transform.localScale = scale;
        primitive.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(primitive.GetComponent<Collider>()); // crashes are checked by position
    }
}
