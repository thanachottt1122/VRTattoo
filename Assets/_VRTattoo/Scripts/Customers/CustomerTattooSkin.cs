using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// On the Customer prefab. While the customer lies on the bed, the forearm on the tattoo artist's
// side becomes the tattoo surface:
//   - a MeshCollider baked from the posed forearm (BakeMesh), so the needle touches the real skin
//   - a TattooCanvas (ink RenderTexture) on that collider
//   - an overlay SkinnedMeshRenderer: the same forearm triangles with the same bones, drawing the ink
//     and the stencil on top of the skin
// Both meshes get their own UVs in skin coordinates instead of the model's UVs (which game models
// often mirror or share between the arms): u = metres around the arm, v = metres along it, scaled
// into a canvasSize square. A stencil therefore wraps around the arm without stretching, and the
// ink texture is the same space the scorer compares with the stencil.
public class CustomerTattooSkin : MonoBehaviour
{
    [Header("Materials")]
    [Tooltip("VRTattoo/TattooBrush")]
    public Material brushMaterial;

    [Tooltip("VRTattoo/TattooSkinOverlay")]
    public Material overlayMaterial;

    [Header("Ink")]
    [Tooltip("Side of the square of skin covered by the ink texture (m)")]
    public float canvasSize = 0.20f;
    public int textureSize = 1024;

    [Tooltip("Line width on the skin (m)")]
    public float brushDiameter = 0.005f;
    public Color inkColor = new Color(0.02f, 0.02f, 0.02f);

    [Header("Stencil")]
    [Tooltip("Width and height of the stencil on the skin (m)")]
    public float stencilSize = 0.14f;

    [Tooltip("Stencil centre along the forearm (0 = elbow, 1 = wrist)")]
    [Range(0f, 1f)] public float stencilAlong = 0.5f;

    [Tooltip("Turn the stencil centre from the top of the arm towards the artist (degrees)")]
    public float stencilTurnTowardsArtist = 15f;

    [Header("Forearm")]
    [Tooltip("Vertices skinned at least this much to the forearm bones become tattoo skin")]
    [Range(0.1f, 1f)] public float minForearmWeight = 0.5f;

    [Tooltip("Wait for the lying pose and IK to settle before baking the collider (s)")]
    public float settleDelay = 0.5f;

    // The skin of the customer on the bed (null while nobody is ready to be tattooed).
    public static CustomerTattooSkin Active { get; private set; }

    public bool IsReady { get; private set; }
    public TattooCanvas Canvas { get; private set; }
    public MeshCollider SkinCollider { get; private set; }
    public StencilPlacement Placement { get; private set; }
    public Texture2D CurrentStencil { get; private set; }
    public bool UsesLeftArm { get; private set; }

    static readonly int InkTexId = Shader.PropertyToID("_InkTex");
    static readonly int InkColorId = Shader.PropertyToID("_InkColor");
    static readonly int StencilTexId = Shader.PropertyToID("_StencilTex");
    static readonly int StencilColorId = Shader.PropertyToID("_StencilColor");
    static readonly int CenterId = Shader.PropertyToID("_Center");
    static readonly int AxisXId = Shader.PropertyToID("_AxisX");
    static readonly int AxisYId = Shader.PropertyToID("_AxisY");

    GameObject colliderObject;
    GameObject overlayObject;
    Mesh colliderMesh;
    Mesh overlayMesh;
    Material overlayInstance;
    Coroutine building;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Active = null;

    void OnDestroy() => Release();

    // Called when the customer lies down.
    public void Build(Animator animator)
    {
        Release();
        building = StartCoroutine(BuildWhenSettled(animator));
    }

    // Called when the customer gets up or leaves: ink, stencil and collider are gone.
    public void Release()
    {
        if (building != null)
        {
            StopCoroutine(building);
            building = null;
        }
        if (Active == this)
            Active = null;
        IsReady = false;
        Canvas = null;
        SkinCollider = null;

        DestroyObject(ref colliderObject);
        DestroyObject(ref overlayObject);
        DestroyObject(ref colliderMesh);
        DestroyObject(ref overlayMesh);
        DestroyObject(ref overlayInstance);
    }

    // Can be called before the skin is ready; it is shown as soon as it is.
    public void ShowStencil(Texture2D stencil)
    {
        CurrentStencil = stencil;
        ApplyStencil();
    }

    public void HideStencil() => ShowStencil(null);

    IEnumerator BuildWhenSettled(Animator animator)
    {
        yield return new WaitForSeconds(settleDelay);
        building = null;

        if (animator == null || !animator.isHuman || !BuildSkin(animator))
        {
            Debug.LogWarning($"[CustomerTattooSkin] {name}: could not build a tattoo surface on the forearm");
            yield break;
        }
        IsReady = true;
        Active = this;
    }

    bool BuildSkin(Animator animator)
    {
        // The forearm on the tattoo artist's side: the one closer to the tattoo chair.
        var seat = FindFirstObjectByType<TattooSeat>();
        Vector3 artist = seat != null ? seat.transform.position : transform.position + transform.right;
        UsesLeftArm = DistanceToForearm(animator, true, artist) < DistanceToForearm(animator, false, artist);

        Transform elbow = animator.GetBoneTransform(UsesLeftArm ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        Transform wrist = animator.GetBoneTransform(UsesLeftArm ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        if (elbow == null || wrist == null)
            return false;
        var forearmBones = ForearmBones(elbow, wrist);

        // The skin mesh with the most forearm vertices (not a watch or a sleeve).
        SkinnedMeshRenderer source = null;
        float[] forearmWeight = null;
        int bestCount = 0;
        foreach (var smr in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (!smr.enabled || smr.sharedMesh == null || !smr.sharedMesh.isReadable)
                continue;
            var weights = VertexWeights(smr, forearmBones);
            int count = 0;
            foreach (float w in weights)
                if (w >= minForearmWeight)
                    count++;
            if (count > bestCount)
            {
                bestCount = count;
                source = smr;
                forearmWeight = weights;
            }
        }
        if (source == null)
            return false;

        Vector3[] world = BakeWorldPositions(source, forearmWeight, elbow.position, wrist.position);

        // Skin coordinates: t along the arm from the elbow, s = arc length around it, measured from
        // the stencil centre line (the top of the arm turned a little towards the artist) and
        // positive away from the artist so the stencil reads upright from the chair.
        Vector3 axis = wrist.position - elbow.position;
        float length = axis.magnitude;
        axis /= length;
        Vector3 top = Vector3.ProjectOnPlane(Vector3.up, axis).normalized;
        Vector3 towardsArtist = Vector3.ProjectOnPlane(artist - elbow.position, axis).normalized;
        float turnSign = Mathf.Sign(Vector3.Dot(Vector3.Cross(top, towardsArtist), axis));
        Vector3 centre = Quaternion.AngleAxis(turnSign * stencilTurnTowardsArtist, axis) * top;
        Vector3 away = Vector3.Cross(axis, centre);
        if (Vector3.Dot(away, towardsArtist) > 0f)
            away = -away;
        float tCentre = length * stencilAlong;

        var uv = new Vector2[world.Length];
        for (int i = 0; i < world.Length; i++)
        {
            if (forearmWeight[i] < minForearmWeight)
                continue;
            Vector3 d = world[i] - elbow.position;
            float t = Vector3.Dot(d, axis);
            Vector3 radial = d - axis * t;
            float angle = Mathf.Atan2(Vector3.Dot(radial, away), Vector3.Dot(radial, centre));
            float s = angle * radial.magnitude;
            uv[i] = new Vector2(0.5f + s / canvasSize, 0.5f + (t - tCentre) / canvasSize);
        }

        // Forearm triangles, minus the ones across the seam on the underside of the arm.
        var mesh = source.sharedMesh;
        var used = new Dictionary<int, int>();
        var triangles = new List<int>();
        for (int sub = 0; sub < mesh.subMeshCount; sub++)
        {
            var tris = mesh.GetTriangles(sub);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                if (forearmWeight[a] < minForearmWeight || forearmWeight[b] < minForearmWeight || forearmWeight[c] < minForearmWeight)
                    continue;
                float minU = Mathf.Min(uv[a].x, Mathf.Min(uv[b].x, uv[c].x));
                float maxU = Mathf.Max(uv[a].x, Mathf.Max(uv[b].x, uv[c].x));
                if (maxU - minU > 0.25f)
                    continue;
                foreach (int v in new[] { a, b, c })
                {
                    if (!used.TryGetValue(v, out int index))
                    {
                        index = used.Count;
                        used.Add(v, index);
                    }
                    triangles.Add(index);
                }
            }
        }
        if (triangles.Count == 0)
            return false;

        var order = new int[used.Count];
        foreach (var pair in used)
            order[pair.Value] = pair.Key;

        BuildCollider(world, uv, order, triangles);
        BuildOverlay(source, uv, order, triangles);
        ApplyStencil();
        return true;
    }

    void BuildCollider(Vector3[] world, Vector2[] uv, int[] order, List<int> triangles)
    {
        // Inactive while the canvas is configured, so its Awake uses these settings.
        colliderObject = new GameObject("TattooSkin");
        colliderObject.SetActive(false);
        colliderObject.transform.SetParent(transform, false);
        colliderObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var local = colliderObject.transform;

        colliderMesh = new Mesh { name = "TattooSkinCollider" };
        var vertices = new Vector3[order.Length];
        var uvs = new Vector2[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            vertices[i] = local.InverseTransformPoint(world[order[i]]);
            uvs[i] = uv[order[i]];
        }
        colliderMesh.vertices = vertices;
        colliderMesh.uv = uvs;
        colliderMesh.SetTriangles(triangles, 0);
        colliderMesh.RecalculateNormals();
        colliderMesh.RecalculateBounds();

        SkinCollider = colliderObject.AddComponent<MeshCollider>();
        SkinCollider.sharedMesh = colliderMesh;

        Canvas = colliderObject.AddComponent<TattooCanvas>();
        Canvas.textureSize = textureSize;
        Canvas.brushMaterial = brushMaterial;
        Canvas.brushDiameter = brushDiameter;
        Canvas.inkColor = inkColor;
        Canvas.MetersPerUV = new Vector2(canvasSize, canvasSize);
        colliderObject.SetActive(true);

        // Like a real forearm tattoo the design stands along the arm (top towards the elbow), so tall
        // designs use the arm's length instead of wrapping far around it. Seen from above that makes
        // its right side point away from the artist (u grows that way), so it is not mirrored.
        float k = canvasSize / stencilSize;
        Placement = new StencilPlacement
        {
            centerUV = new Vector2(0.5f, 0.5f),
            axisX = new Vector2(k, 0f),   // stencil x runs around the arm, away from the artist
            axisY = new Vector2(0f, -k),  // stencil y runs along the arm, towards the elbow
            uvBounds = new Rect(0.5f - 0.5f / k, 0.5f - 0.5f / k, 1f / k, 1f / k),
        };
    }

    // A copy of the forearm triangles skinned to the same bones, so it follows the arm exactly.
    void BuildOverlay(SkinnedMeshRenderer source, Vector2[] uv, int[] order, List<int> triangles)
    {
        var mesh = source.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        BoneWeight[] weights = mesh.boneWeights;

        overlayMesh = new Mesh { name = "TattooSkinOverlay" };
        var v = new Vector3[order.Length];
        var n = new Vector3[order.Length];
        var w = new BoneWeight[order.Length];
        var uvs = new Vector2[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            v[i] = vertices[order[i]];
            n[i] = normals.Length == vertices.Length ? normals[order[i]] : Vector3.up;
            w[i] = weights[order[i]];
            uvs[i] = uv[order[i]];
        }
        overlayMesh.vertices = v;
        overlayMesh.normals = n;
        overlayMesh.uv = uvs;
        overlayMesh.boneWeights = w;
        overlayMesh.bindposes = mesh.bindposes;
        overlayMesh.SetTriangles(triangles, 0);
        overlayMesh.RecalculateBounds();

        overlayObject = new GameObject("TattooOverlay");
        overlayObject.transform.SetParent(source.transform.parent, false);
        overlayObject.transform.SetLocalPositionAndRotation(source.transform.localPosition, source.transform.localRotation);
        overlayObject.transform.localScale = source.transform.localScale;

        var overlay = overlayObject.AddComponent<SkinnedMeshRenderer>();
        overlay.sharedMesh = overlayMesh;
        overlay.bones = source.bones;
        overlay.rootBone = source.rootBone;
        overlay.updateWhenOffscreen = true;
        overlay.shadowCastingMode = ShadowCastingMode.Off;
        overlay.receiveShadows = false;

        overlayInstance = new Material(overlayMaterial);
        overlayInstance.SetTexture(InkTexId, Canvas.TattooTexture);
        overlayInstance.SetColor(InkColorId, inkColor);
        overlay.sharedMaterial = overlayInstance;
    }

    void ApplyStencil()
    {
        if (overlayInstance == null)
            return;

        var color = overlayMaterial.GetColor(StencilColorId);
        if (CurrentStencil == null)
            color.a = 0f;
        overlayInstance.SetColor(StencilColorId, color);
        overlayInstance.SetTexture(StencilTexId, CurrentStencil != null ? CurrentStencil : Texture2D.blackTexture);
        overlayInstance.SetVector(CenterId, Placement.centerUV);
        overlayInstance.SetVector(AxisXId, Placement.axisX);
        overlayInstance.SetVector(AxisYId, Placement.axisY);
    }

    static float DistanceToForearm(Animator animator, bool left, Vector3 point)
    {
        var a = animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        var b = animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        if (a == null || b == null)
            return float.MaxValue;
        Vector3 middle = (a.position + b.position) * 0.5f;
        middle.y = point.y = 0f;
        return Vector3.Distance(middle, point);
    }

    // The forearm bone and its helpers (twist/share bones), but not the hand.
    static HashSet<Transform> ForearmBones(Transform elbow, Transform wrist)
    {
        var set = new HashSet<Transform>();
        var stack = new Stack<Transform>();
        stack.Push(elbow);
        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t == wrist)
                continue;
            set.Add(t);
            foreach (Transform child in t)
                stack.Push(child);
        }
        return set;
    }

    static float[] VertexWeights(SkinnedMeshRenderer smr, HashSet<Transform> boneSet)
    {
        var bones = smr.bones;
        var inSet = new bool[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            inSet[i] = bones[i] != null && boneSet.Contains(bones[i]);

        var weights = smr.sharedMesh.boneWeights;
        var result = new float[weights.Length];
        for (int i = 0; i < weights.Length; i++)
        {
            var w = weights[i];
            result[i] = (inSet[w.boneIndex0] ? w.weight0 : 0f) + (inSet[w.boneIndex1] ? w.weight1 : 0f)
                      + (inSet[w.boneIndex2] ? w.weight2 : 0f) + (inSet[w.boneIndex3] ? w.weight3 : 0f);
        }
        return result;
    }

    // World positions of the posed mesh. Whether BakeMesh's output still needs the renderer's
    // scale differs between imports, so both are tried and the one that lands on the forearm wins.
    static Vector3[] BakeWorldPositions(SkinnedMeshRenderer smr, float[] forearmWeight, Vector3 elbow, Vector3 wrist)
    {
        var baked = new Mesh();
        smr.BakeMesh(baked, true);
        Vector3[] local = baked.vertices;
        Destroy(baked);

        var t = smr.transform;
        Matrix4x4 withScale = t.localToWorldMatrix;
        Matrix4x4 noScale = Matrix4x4.TRS(t.position, t.rotation, Vector3.one);
        Vector3 middle = (elbow + wrist) * 0.5f;

        Vector3 sumA = Vector3.zero, sumB = Vector3.zero;
        int count = 0;
        for (int i = 0; i < local.Length; i++)
        {
            if (forearmWeight[i] < 0.5f)
                continue;
            sumA += withScale.MultiplyPoint3x4(local[i]);
            sumB += noScale.MultiplyPoint3x4(local[i]);
            count++;
        }
        Matrix4x4 m = count > 0 && (sumA / count - middle).sqrMagnitude < (sumB / count - middle).sqrMagnitude ? withScale : noScale;

        var world = new Vector3[local.Length];
        for (int i = 0; i < local.Length; i++)
            world[i] = m.MultiplyPoint3x4(local[i]);
        return world;
    }

    static void DestroyObject<T>(ref T obj) where T : Object
    {
        if (obj != null)
            Destroy(obj);
        obj = null;
    }
}
