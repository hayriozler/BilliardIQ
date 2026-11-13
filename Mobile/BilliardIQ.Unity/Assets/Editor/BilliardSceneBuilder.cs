using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BilliardIQ.Game;
using Assets.Scripts.Game;
using UnityEngine.Rendering;

public static class BilliardSceneBuilder
{
    private const string TableFbxPath      = "Assets/Models/BilliardTable.fbx";
    private const string BallWhiteFbxPath  = "Assets/Models/BilliardBall_White.fbx";
    private const string BallYellowFbxPath = "Assets/Models/BilliardBall_Yellow.fbx";
    private const string BallRedFbxPath    = "Assets/Models/BilliardBall_Red.fbx";

    // FBX boyutları (vertex verisinden ölçüldü, Blender cm birimi):
    //   Top:  çap = 6.15 cm (FBX'te 0.0615), yarıçap = 3.075 cm (FBX'te 0.0307)
    //   Masa: 284 cm × 142 cm (FBX'te 2.84 × 1.42), oyun 2× büyük (568 cm × 284 cm)
    //
    // Top ölçeği: oyunda çap 0.18 birim olmalı → scale = 0.18 / 0.0615 ≈ 2.927
    private const float BallFbxScale   = 0.18f / 0.0615f;  // ≈ 2.927
    private const float BallFbxRadius  = 0.0307f;           // FBX local uzayında yarıçap (3.075 cm)
    // Primitive sphere fallback: Unity sphere yarıçapı = 0.5 (local), scale = 0.18
    private const float BallPrimScale  = 0.18f;
    private const float BallPrimRadius = 0.5f;
    // Masa ölçeği: 2× (284 cm → 568 cm oyun birimi)
    private const float TableScale     = 2f;
    // Masa Y ofseti: FBX'te cloth child'ı local Y=0.755, scale=2 → dünya Y=1.51.
    // Cloth üstünü Y=0.05'e getirmek için: offset = 0.05 - (0.755 + 0.025) × 2 = -1.51
    // Doğrulama: cloth_top = -1.51 + 0.78×2 = 0.05, ball_bottom = 0.14-0.09 = 0.05 ✓
    private const float TableSurfaceY  = -1.51f;

    [MenuItem("BilliardIQ/Build Scene")]
    public static void BuildScene()
    {
        // Clear existing scene objects (except camera/light)
        foreach (var obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (obj == null) continue;
            if (obj.name == "Main Camera" || obj.name == "Directional Light") continue;
            Object.DestroyImmediate(obj);
        }

        // --- Physics Materials ---
        var cushionPhysic = CreatePhysicMaterial("CushionPhysic", 0.85f, 0.1f);
        var ballPhysic    = CreatePhysicMaterial("BallPhysic",    0.60f, 0.3f);

        // --- Visual Materials ---
        var tableMat   = CreateMaterial("TableMat",   new Color(0.13f, 0.45f, 0.13f));
        var cushionMat = CreateMaterial("CushionMat", new Color(0.08f, 0.30f, 0.08f));
        var cueMat     = CreateMaterial("CueBallMat", Color.white);
        var yellowMat  = CreateMaterial("ObjBall1Mat", new Color(1.00f, 0.85f, 0.00f));
        var redMat     = CreateMaterial("ObjBall2Mat", new Color(0.90f, 0.10f, 0.10f));

        // --- Table ---
        AssetDatabase.ImportAsset(TableFbxPath, ImportAssetOptions.ForceUpdate);
        var tableMainAsset = AssetDatabase.LoadMainAssetAtPath(TableFbxPath);
        var tablePrefab    = tableMainAsset as GameObject;
        if (tablePrefab == null && tableMainAsset != null)
        {
            // Ana asset GameObject değil; alt asset'leri tara
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(TableFbxPath))
            {
                if (a is GameObject go && go.transform.parent == null)
                    { tablePrefab = go; break; }
            }
        }
        bool usingFbxTable = tablePrefab != null;
        Debug.Log($"[BilliardSceneBuilder] Masa FBX: mainAsset={tableMainAsset?.GetType().Name ?? "NULL"}  prefab={tablePrefab?.name ?? "NULL"}");

        if (usingFbxTable)
        {
            var tableObj = Object.Instantiate(tablePrefab);
            tableObj.name = "Table";
            // Oynama yüzeyi FBX'te Y=0; TableSurfaceY kadar yukarı taşı
            tableObj.transform.position   = new Vector3(0f, TableSurfaceY, 0f);
            // FBX 284 cm × 142 cm, oyun 568 cm × 284 cm → 2× ölçek
            tableObj.transform.localScale = Vector3.one * TableScale;
            // FBX'ten gelen Rigidbody varsa kaldır (masa statik olmalı)
            foreach (var rb in tableObj.GetComponentsInChildren<Rigidbody>())
                Object.DestroyImmediate(rb);
            Debug.Log("[BilliardSceneBuilder] BilliardTable.fbx kullanıldı.");
        }
        else
        {
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "Table";
            table.transform.position   = Vector3.zero;
            table.transform.localScale = new Vector3(5.68f, 0.1f, 2.84f);
            table.GetComponent<Renderer>().material = tableMat;
            Object.DestroyImmediate(table.GetComponent<Rigidbody>());

            // Bacaklar — FBX masa yoksa primitive silindir ile oluştur
            var legMat = CreateMaterial("LegMat", new Color(0.12f, 0.07f, 0.03f));
            float legY = -0.55f;
            CreateLeg("Leg_L1", new Vector3(-2.5f, legY, -0.8f), legMat);
            CreateLeg("Leg_L2", new Vector3(-2.5f, legY,  0.8f), legMat);
            CreateLeg("Leg_R1", new Vector3( 2.5f, legY, -0.8f), legMat);
            CreateLeg("Leg_R2", new Vector3( 2.5f, legY,  0.8f), legMat);
            CreateLeg("Leg_M1", new Vector3(0f, legY, -1.2f), legMat);
            CreateLeg("Leg_M2", new Vector3(0f, legY,  1.2f), legMat);
            Debug.LogWarning("[BilliardSceneBuilder] BilliardTable.fbx bulunamadı, primitive kullanıldı.");
        }

        // --- Cushions ---
        // FBX masa kendi yastık görsellerini içerdiğinden renderer gizlenir; BoxCollider+tag korunur.
        CreateCushion("CushionTop",    new Vector3(0f,     0.16f,  1.42f), new Vector3(5.80f, 0.22f, 0.08f), new Vector3(-20f, 0f,  0f),  cushionMat, cushionPhysic, showRenderer: !usingFbxTable);
        CreateCushion("CushionBottom", new Vector3(0f,     0.16f, -1.42f), new Vector3(5.80f, 0.22f, 0.08f), new Vector3( 20f, 0f,  0f),  cushionMat, cushionPhysic, showRenderer: !usingFbxTable);
        CreateCushion("CushionLeft",   new Vector3(-2.84f, 0.15f,  0f),    new Vector3(0.06f, 0.2f, 3.06f),  Vector3.zero,                 cushionMat, cushionPhysic, showRenderer: !usingFbxTable);
        CreateCushion("CushionRight",  new Vector3( 2.84f, 0.15f,  0f),    new Vector3(0.06f, 0.2f, 3.06f),  Vector3.zero,                 cushionMat, cushionPhysic, showRenderer: !usingFbxTable);

        // --- Balls ---
        var cueBallObj    = CreateBall("CueBall",    BallWhiteFbxPath,  new Vector3(-1.5f, 0.14f,  0f),   cueMat,    ballPhysic, BallRole.CueBall);
        var yellowBallObj = CreateBall("YellowBall", BallYellowFbxPath, new Vector3( 1.5f, 0.14f,  0f),   yellowMat, ballPhysic, BallRole.YellowBall);
        var redBallObj    = CreateBall("RedBall",    BallRedFbxPath,    new Vector3( 0f,   0.14f,  0.5f), redMat,    ballPhysic, BallRole.RedBall);
        cueBallObj.tag = "CueBall";

        // --- Start Position Markers ---
        var startP1     = CreateMarker("StartCueBall",    new Vector3(-1.5f, 0.14f,  0f));
        var startYellow = CreateMarker("StartYellowBall", new Vector3( 1.5f, 0.14f,  0f));
        var startRed    = CreateMarker("StartRedBall",    new Vector3( 0f,   0.14f,  0.5f));

        // --- Aim Line ---
        var aimLineObj = new GameObject("AimLine");
        var lr = aimLineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = 0.02f;
        lr.endWidth   = 0.02f;
        lr.material   = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 1f, 1f, 0.6f);
        lr.endColor   = new Color(1f, 1f, 1f, 0f);
        lr.enabled    = false;

        // --- GameController ---
        var gcObj = new GameObject("GameController");
        gcObj.AddComponent<GameController>();

        // --- TableController ---
        var tcObj = new GameObject("TableController");
        var tc = tcObj.AddComponent<TableController>();
        tc.cueBall         = cueBallObj.GetComponent<BallController>();
        tc.yellowBall      = yellowBallObj.GetComponent<BallController>();
        tc.redBall         = redBallObj.GetComponent<BallController>();
        tc.cueBallStart    = startP1.transform;
        tc.yellowBallStart = startYellow.transform;
        tc.redBallStart    = startRed.transform;

        // --- CueController ---
        var ccObj = new GameObject("CueController");
        var cc = ccObj.AddComponent<CueController>();
        cc.AimLine = lr;

        // --- MauiBridge ---
        var mbObj = new GameObject("MauiBridge");
        mbObj.AddComponent<MauiBridge>();

        // --- UIManager (Back button + Camera toggle) ---
        var uiObj = new GameObject("UIManager");
        uiObj.AddComponent<UIManager>();

        // --- GameMaterials ---
        var gmObj = new GameObject("GameMaterials");
        var gm    = gmObj.AddComponent<GameMaterials>();
        gm.Rail    = CreateUnlitMaterial("RailMat",    new Color(0.20f, 0.13f, 0.05f));
        gm.Diamond = CreateUnlitMaterial("DiamondMat", new Color(1.00f, 0.97f, 0.88f));
        gm.AimLine = CreateLineMaterial("AimLineMat");
        gm.CueLine = CreateLineMaterial("CueLineMat");

        // --- Tags ---
        EnsureTag("Cushion");
        EnsureTag("CueBall");

        // --- Camera ---
        var cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic  = false;
            cam.fieldOfView   = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane  = 20f;

            var camTarget = new GameObject("CameraTarget");
            camTarget.transform.position = Vector3.zero;

            var camCtrl = cam.gameObject.AddComponent<CameraController>();
            camCtrl.Target = camTarget.transform;
        }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[BilliardSceneBuilder] Scene built and saved successfully!");
    }

    // --- Helpers ---

    private static PhysicsMaterial CreatePhysicMaterial(string name, float bounciness, float friction)
    {
        var mat = new PhysicsMaterial(name)
        {
            bounciness      = bounciness,
            dynamicFriction = friction,
            staticFriction  = friction,
            bounceCombine   = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        AssetDatabase.CreateAsset(mat, $"Assets/{name}.asset");
        return mat;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        var sh  = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(sh) { color = color };
        AssetDatabase.CreateAsset(mat, $"Assets/{name}.mat");
        return mat;
    }

    private static Material CreateUnlitMaterial(string name, Color color)
    {
        var sh  = Shader.Find("Universal Render Pipeline/Unlit")
               ?? Shader.Find("Universal Render Pipeline/Lit")
               ?? Shader.Find("Standard");
        var mat = new Material(sh) { color = color };
        AssetDatabase.CreateAsset(mat, $"Assets/{name}.mat");
        return mat;
    }

    private static Material CreateLineMaterial(string name)
    {
        var sh  = Shader.Find("Universal Render Pipeline/Particles/Unlit")
               ?? Shader.Find("Sprites/Default")
               ?? Shader.Find("Standard");
        var mat = new Material(sh) { color = Color.white };
        AssetDatabase.CreateAsset(mat, $"Assets/{name}.mat");
        return mat;
    }

    private static void CreateCushion(string name, Vector3 pos, Vector3 scale, Vector3 rotation,
                                      Material mat, PhysicsMaterial physMat, bool showRenderer = true)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.position    = pos;
        obj.transform.localScale  = scale;
        obj.transform.eulerAngles = rotation;
        obj.GetComponent<BoxCollider>().material = physMat;
        obj.tag = "Cushion";
        Object.DestroyImmediate(obj.GetComponent<Rigidbody>());

        if (showRenderer)
        {
            obj.GetComponent<Renderer>().material = mat;
        }
        else
        {
            // Fizik collider korunur; FBX masa kendi görsel yastıklarını sağlar
            Object.DestroyImmediate(obj.GetComponent<MeshRenderer>());
            Object.DestroyImmediate(obj.GetComponent<MeshFilter>());
        }
    }

    // FBX top modelini yükler ve fizik bileşenlerini ekler.
    // FBX bulunamazsa Unity primitive sphere ile devam edilir.
    private static GameObject CreateBall(string name, string fbxPath, Vector3 pos,
                                         Material mat, PhysicsMaterial physMat, BallRole role)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        GameObject obj;
        float colliderRadius;
        if (prefab != null)
        {
            obj = Object.Instantiate(prefab);
            obj.name = name;
            obj.transform.position   = pos;
            // FBX top çapı 6.15 cm → oyun çapı 0.18 birim için ≈ 2.927× ölçek
            obj.transform.localScale = Vector3.one * BallFbxScale;

            // FBX'ten gelen tüm collider ve Rigidbody'leri temizle; bizimkiler eklenecek
            foreach (var c in obj.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
            foreach (var rb in obj.GetComponentsInChildren<Rigidbody>())
                Object.DestroyImmediate(rb);

            // Top rengini tüm alt renderer'lara uygula
            foreach (var r in obj.GetComponentsInChildren<Renderer>())
                r.material = mat;

            // FBX yarıçapı 0.0307 (local) × 2.927 = 0.09 dünya birimi ✓
            colliderRadius = BallFbxRadius;
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = name;
            obj.transform.position   = pos;
            obj.transform.localScale = Vector3.one * BallPrimScale;
            obj.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            Debug.LogWarning($"[BilliardSceneBuilder] {fbxPath} bulunamadı, primitive kullanıldı.");

            // Primitive sphere yarıçapı 0.5 (local) × 0.18 = 0.09 dünya birimi ✓
            colliderRadius = BallPrimRadius;
        }

        var col = obj.AddComponent<SphereCollider>();
        col.radius   = colliderRadius;
        col.material = physMat;

        var rb2 = obj.AddComponent<Rigidbody>();
        rb2.mass           = 0.2f;
        rb2.linearDamping  = 0.8f;
        rb2.angularDamping = 0.8f;
        rb2.constraints    = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationZ;

        var bc = obj.AddComponent<BallController>();
        bc.Role = role;

        return obj;
    }

    private static void CreateLeg(string name, Vector3 pos, Material mat)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.position   = pos;
        obj.transform.localScale = new Vector3(0.14f, 0.55f, 0.14f);
        obj.GetComponent<Renderer>().material = mat;
        Object.DestroyImmediate(obj.GetComponent<Rigidbody>());
    }

    private static GameObject CreateMarker(string name, Vector3 pos)
    {
        var obj = new GameObject(name);
        obj.transform.position = pos;
        return obj;
    }

    private static void EnsureTag(string tag)
    {
        var asset = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (asset == null || asset.Length == 0) return;
        var so   = new UnityEditor.SerializedObject(asset[0]);
        var tags = so.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.arraySize++;
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        so.ApplyModifiedProperties();
    }
}
