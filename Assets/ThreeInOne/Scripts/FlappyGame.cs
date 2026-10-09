using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Fly Like a Bird: tap to flap the plane through the gaps. Pass every gate to win.
public class FlappyGame : MonoBehaviour
{
    public Transform plane;
    public Transform cameraTransform;
    public GameUI ui;

    [Header("Flight")]
    public float forwardSpeed = 14;
    public float gravity = 32;
    public float flapVelocity = 11.5f;

    [Header("Level")]
    public int gateCount = 10;
    public float firstGateZ = 45;
    public float gateSpacing = 24;
    public float gapSize = 10;
    public float pipeRadius = 2.5f;
    public float floorY = -14;
    public float ceilingY = 14;

    [Header("Materials")]
    public Material pipeMaterial;
    public Material capMaterial;
    public Material groundMaterial;
    public Material cloudMaterial;
    public Material finishLightMaterial;
    public Material finishDarkMaterial;

    // The plane's pivot is under its nose, so the body is described relative to it.
    private const float BodyCenterY = 1.2f;
    private const float BodyHalfHeight = 0.9f;
    private const float NoseZ = 1.7f;
    private const float TailZ = -4.7f;

    private enum State { Ready, Playing, Ended }
    private State state = State.Ready;
    private float[] gapCenters;
    private float verticalVelocity;
    private int score;
    private Vector3 cameraOffset = new Vector3(30, 0, 10);

    public float FinishZ { get; private set; }
    public bool IsPlaying { get { return state == State.Playing; } }
    public int Score { get { return score; } }

    void Awake()
    {
        BuildLevel();
        plane.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    void Start()
    {
        ui.SetHud("Score 0 / " + gateCount);
        ui.SetCenterMessage("Press SPACE to fly");
    }

    void Update()
    {
        if (Time.timeScale == 0 || state == State.Ended)
        {
            return;
        }

        if (state == State.Ready)
        {
            // Hover in place until the first flap
            plane.position = new Vector3(0, Mathf.Sin(Time.time * 3) * 0.5f, 0);
            if (FlapPressedThisFrame())
            {
                Flap();
            }
            return;
        }

        if (FlapPressedThisFrame())
        {
            Flap();
        }

        verticalVelocity -= gravity * Time.deltaTime;
        Vector3 position = plane.position;
        position.y += verticalVelocity * Time.deltaTime;
        position.z += forwardSpeed * Time.deltaTime;

        // Bump gently against the top of the sky instead of crashing
        float maxY = ceilingY - BodyCenterY - BodyHalfHeight;
        if (position.y > maxY)
        {
            position.y = maxY;
            verticalVelocity = Mathf.Min(verticalVelocity, 0);
        }

        plane.position = position;
        plane.rotation = Quaternion.Euler(Mathf.Clamp(-verticalVelocity * 2.5f, -30, 40), 0, 0);

        CheckGates(position);
    }

    void LateUpdate()
    {
        cameraTransform.position = new Vector3(cameraOffset.x, cameraOffset.y, plane.position.z + cameraOffset.z);
    }

    public void Flap()
    {
        if (state == State.Ended)
        {
            return;
        }
        if (state == State.Ready)
        {
            state = State.Playing;
            ui.SetCenterMessage("");
        }
        verticalVelocity = flapVelocity;
    }

    // Height the plane's pivot should aim for to pass the next gate.
    public float NextTargetY()
    {
        for (int i = 0; i < gateCount; i++)
        {
            if (plane.position.z + TailZ < GateZ(i) + pipeRadius)
            {
                return gapCenters[i] - BodyCenterY;
            }
        }
        return -BodyCenterY;
    }

    private float GateZ(int index)
    {
        return firstGateZ + index * gateSpacing;
    }

    private void CheckGates(Vector3 position)
    {
        float bottom = position.y + BodyCenterY - BodyHalfHeight;
        float top = position.y + BodyCenterY + BodyHalfHeight;
        float nose = position.z + NoseZ;
        float tail = position.z + TailZ;

        int passed = 0;
        for (int i = 0; i < gateCount; i++)
        {
            float gateZ = GateZ(i);
            if (tail > gateZ + pipeRadius)
            {
                passed++;
            }
            else if (nose > gateZ - pipeRadius)
            {
                if (bottom < gapCenters[i] - gapSize / 2 || top > gapCenters[i] + gapSize / 2)
                {
                    End(false, "You hit a pipe!");
                    return;
                }
            }
        }

        if (passed != score)
        {
            score = passed;
            ui.SetHud("Score " + score + " / " + gateCount);
        }

        if (bottom < floorY)
        {
            End(false, "You hit the ground!");
        }
        else if (position.z > FinishZ)
        {
            End(true, "You flew through all " + gateCount + " gates!");
        }
    }

    private void End(bool won, string message)
    {
        state = State.Ended;
        if (won)
        {
            ui.ShowWin(message);
        }
        else
        {
            ui.ShowLose(message + "\nScore " + score + " / " + gateCount);
        }
    }

    private bool FlapPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        bool key = Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame
            || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame);
        bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return key || mouse;
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)
            || Input.GetKeyDown(KeyCode.W) || Input.GetMouseButtonDown(0);
#endif
    }

    // ---------- Level building ----------

    private void BuildLevel()
    {
        Transform level = new GameObject("Level").transform;
        gapCenters = new float[gateCount];
        float margin = gapSize / 2 + 3;
        float previous = 0;

        for (int i = 0; i < gateCount; i++)
        {
            // Keep each gap within reach of the one before it
            float center = Mathf.Clamp(previous + Random.Range(-8f, 8f), floorY + margin, ceilingY - margin);
            gapCenters[i] = center;
            previous = center;

            float gateZ = GateZ(i);
            BuildPipe(level, gateZ, floorY - 1, center - gapSize / 2, true);
            BuildPipe(level, gateZ, center + gapSize / 2, ceilingY + 14, false);
        }

        FinishZ = GateZ(gateCount - 1) + gateSpacing;
        BuildFinishLine(level);

        float length = FinishZ + 300;
        CreatePrimitive(PrimitiveType.Cube, "Ground", level, new Vector3(-150, floorY - 1, length / 2 - 100),
            new Vector3(420, 2, length), groundMaterial);

        for (int i = 0; i < 36; i++)
        {
            BuildCloud(level, new Vector3(Random.Range(-110f, -25f), Random.Range(-4f, 26f), Random.Range(-60f, FinishZ + 120)));
        }
    }

    private void BuildPipe(Transform parent, float z, float bottomY, float topY, bool capOnTop)
    {
        float height = topY - bottomY;
        float diameter = pipeRadius * 2;
        CreatePrimitive(PrimitiveType.Cylinder, "Pipe", parent, new Vector3(0, bottomY + height / 2, z),
            new Vector3(diameter, height / 2, diameter), pipeMaterial);

        float capY = capOnTop ? topY - 0.6f : bottomY + 0.6f;
        CreatePrimitive(PrimitiveType.Cylinder, "Pipe Cap", parent, new Vector3(0, capY, z),
            new Vector3(diameter + 1.2f, 0.6f, diameter + 1.2f), capMaterial);
    }

    private void BuildFinishLine(Transform parent)
    {
        // Checkered banner standing just behind the flight path
        int rows = Mathf.CeilToInt((ceilingY - floorY) / 2);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < 2; column++)
            {
                Material material = (row + column) % 2 == 0 ? finishLightMaterial : finishDarkMaterial;
                CreatePrimitive(PrimitiveType.Cube, "Finish", parent,
                    new Vector3(-7, floorY + 1 + row * 2, FinishZ + column * 2), new Vector3(0.5f, 2, 2), material);
            }
        }
    }

    private void BuildCloud(Transform parent, Vector3 position)
    {
        float size = Random.Range(4f, 9f);
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
        Destroy(primitive.GetComponent<Collider>()); // collisions are checked by position
    }
}
