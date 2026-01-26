using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class InteractableBase : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string displayName = "Interact";
    [SerializeField] private bool useCustomRange = false;
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private Transform interactionPoint;

    [Header("Focus Highlight")]
    [SerializeField] private bool enableFocusHighlight = true;
    [SerializeField] private Material focusHighlightMaterial;
    [SerializeField, Range(0f, 0.2f)] private float focusOutlineWidth = 0.03f;
    [SerializeField] private Color focusOutlineColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float focusOutlineSoftness = 0.35f;
    [SerializeField, Range(0f, 0.3f)] private float focusGlowWidth = 0.06f;
    [SerializeField, Range(0f, 1f)] private float focusGlowSoftness = 0.5f;
    [SerializeField, Min(0f)] private float focusGlowIntensity = 1f;
    [SerializeField] private Color focusGlowColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Renderer[] focusRenderers;

    private bool isFocused;
    private bool focusSetupComplete;
    private Renderer[] resolvedFocusRenderers = Array.Empty<Renderer>();
    private Material[][] originalSharedMaterials = Array.Empty<Material[]>();
    private Material focusMaterialInstance;
    private float focusBoundsScale = 1f;
    private List<Mesh> focusMeshInstances = new List<Mesh>();

    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineSoftnessId = Shader.PropertyToID("_OutlineSoftness");
    private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowSoftnessId = Shader.PropertyToID("_GlowSoftness");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");

    public string DisplayName => displayName;
    public float InteractionRange => interactionRange;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    public bool TryInteract(Interactor interactor)
    {
        if (!CanInteract(interactor)) return false;
        Interact(interactor);
        return true;
    }

    public virtual bool CanInteract(Interactor interactor)
    {
        if (!isActiveAndEnabled) return false;
        if (interactor == null) return false;

        if (useCustomRange && interactionRange > 0f)
        {
            float distance = Vector3.Distance(interactor.Origin, InteractionPoint.position);
            if (distance > interactionRange) return false;
        }

        return true;
    }

    public void FocusEnter(Interactor interactor)
    {
        if (isFocused) return;
        isFocused = true;

        if (enableFocusHighlight)
        {
            EnableFocusHighlight();
        }

        OnFocusEnter(interactor);
    }

    public void FocusExit(Interactor interactor)
    {
        if (!isFocused) return;
        isFocused = false;
        DisableFocusHighlight();

        OnFocusExit(interactor);
    }

    protected abstract void Interact(Interactor interactor);

    public virtual void OnFocusEnter(Interactor interactor) { }

    public virtual void OnFocusExit(Interactor interactor) { }

    protected virtual void OnDisable()
    {
        isFocused = false;
        DisableFocusHighlight();
    }

    protected virtual void OnDestroy()
    {
        if (focusMeshInstances == null || focusMeshInstances.Count == 0) return;

        for (int i = 0; i < focusMeshInstances.Count; i++)
        {
            Mesh meshInstance = focusMeshInstances[i];
            if (meshInstance == null) continue;
            Destroy(meshInstance);
        }

        focusMeshInstances.Clear();
    }

    private void EnableFocusHighlight()
    {
        EnsureFocusSetup();
        if (resolvedFocusRenderers.Length == 0) return;

        if (focusMaterialInstance == null)
        {
            Material baseMaterial = ResolveFocusHighlightMaterial();
            if (baseMaterial == null) return;

            focusMaterialInstance = new Material(baseMaterial)
            {
                name = $"{baseMaterial.name} (Instance)",
                hideFlags = HideFlags.DontSave
            };
        }

        focusBoundsScale = CalculateFocusBoundsScale();
        UpdateFocusMaterialProperties();

        for (int i = 0; i < resolvedFocusRenderers.Length; i++)
        {
            Renderer renderer = resolvedFocusRenderers[i];
            if (renderer == null) continue;

            Material[] currentMaterials = renderer.sharedMaterials;
            if (currentMaterials == null) continue;

            if (originalSharedMaterials[i] == null || originalSharedMaterials[i].Length == 0)
            {
                originalSharedMaterials[i] = currentMaterials;
            }

            if (Array.IndexOf(currentMaterials, focusMaterialInstance) >= 0) continue;

            Material[] updatedMaterials = new Material[currentMaterials.Length + 1];
            Array.Copy(currentMaterials, updatedMaterials, currentMaterials.Length);
            updatedMaterials[currentMaterials.Length] = focusMaterialInstance;
            renderer.sharedMaterials = updatedMaterials;
        }
    }

    private void DisableFocusHighlight()
    {
        if (resolvedFocusRenderers == null || resolvedFocusRenderers.Length == 0) return;

        for (int i = 0; i < resolvedFocusRenderers.Length; i++)
        {
            Renderer renderer = resolvedFocusRenderers[i];
            if (renderer == null) continue;

            Material[] originalMaterials = originalSharedMaterials[i];
            if (originalMaterials == null) continue;

            renderer.sharedMaterials = originalMaterials;
        }
    }

    private void EnsureFocusSetup()
    {
        if (focusSetupComplete) return;
        focusSetupComplete = true;

        resolvedFocusRenderers = ResolveFocusRenderers();
        originalSharedMaterials = new Material[resolvedFocusRenderers.Length][];

        for (int i = 0; i < resolvedFocusRenderers.Length; i++)
        {
            Renderer renderer = resolvedFocusRenderers[i];
            if (renderer == null) continue;
            PrepareRendererForFocus(renderer);
            originalSharedMaterials[i] = renderer.sharedMaterials;
        }
    }

    private Renderer[] ResolveFocusRenderers()
    {
        if (focusRenderers != null && focusRenderers.Length > 0)
        {
            return focusRenderers;
        }

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        if (allRenderers == null || allRenderers.Length == 0) return Array.Empty<Renderer>();

        List<Renderer> filtered = new List<Renderer>(allRenderers.Length);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (!ShouldAutoHighlightRenderer(renderer)) continue;
            filtered.Add(renderer);
        }

        return filtered.ToArray();
    }

    private static bool ShouldAutoHighlightRenderer(Renderer renderer)
    {
        if (renderer == null) return false;
        if (renderer is ParticleSystemRenderer) return false;
        if (renderer is TrailRenderer) return false;
        if (renderer is LineRenderer) return false;
        return true;
    }

    private Material ResolveFocusHighlightMaterial()
    {
        if (focusHighlightMaterial != null) return focusHighlightMaterial;

        focusHighlightMaterial = Resources.Load<Material>("Focus/FocusOutline");
        return focusHighlightMaterial;
    }

    private void PrepareRendererForFocus(Renderer renderer)
    {
        if (renderer is MeshRenderer meshRenderer)
        {
            MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
            if (filter == null) return;
            Mesh sourceMesh = filter.sharedMesh;
            if (!TryPrepareMeshInstance(sourceMesh, out Mesh meshInstance)) return;
            filter.sharedMesh = meshInstance;
            return;
        }

        if (renderer is SkinnedMeshRenderer skinnedMesh)
        {
            Mesh sourceMesh = skinnedMesh.sharedMesh;
            if (!TryPrepareMeshInstance(sourceMesh, out Mesh meshInstance)) return;
            skinnedMesh.sharedMesh = meshInstance;
        }
    }

    private bool TryPrepareMeshInstance(Mesh sourceMesh, out Mesh meshInstance)
    {
        meshInstance = null;
        if (sourceMesh == null) return false;
        if (!sourceMesh.isReadable) return false;

        meshInstance = Instantiate(sourceMesh);
        meshInstance.name = $"{sourceMesh.name} (Focus)";

        if (!TryBakeSmoothNormals(meshInstance))
        {
            Destroy(meshInstance);
            meshInstance = null;
            return false;
        }

        focusMeshInstances.Add(meshInstance);
        return true;
    }

    private static bool TryBakeSmoothNormals(Mesh mesh)
    {
        if (mesh == null) return false;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        if (vertices == null || normals == null) return false;
        if (vertices.Length != normals.Length || vertices.Length == 0) return false;

        var groups = new Dictionary<Vector3, List<int>>(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            if (!groups.TryGetValue(vertex, out List<int> indices))
            {
                indices = new List<int>(4);
                groups.Add(vertex, indices);
            }
            indices.Add(i);
        }

        Vector3[] smoothNormals = new Vector3[vertices.Length];
        foreach (var entry in groups)
        {
            List<int> indices = entry.Value;
            Vector3 average = Vector3.zero;
            for (int i = 0; i < indices.Count; i++)
            {
                average += normals[indices[i]];
            }
            average.Normalize();
            for (int i = 0; i < indices.Count; i++)
            {
                smoothNormals[indices[i]] = average;
            }
        }

        var smoothList = new List<Vector3>(smoothNormals.Length);
        smoothList.AddRange(smoothNormals);
        mesh.SetUVs(3, smoothList);
        return true;
    }

    private float CalculateFocusBoundsScale()
    {
        if (resolvedFocusRenderers == null || resolvedFocusRenderers.Length == 0) return 1f;

        bool hasBounds = false;
        Bounds combinedBounds = default;

        for (int i = 0; i < resolvedFocusRenderers.Length; i++)
        {
            Renderer renderer = resolvedFocusRenderers[i];
            if (renderer == null) continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds) return 1f;

        float magnitude = combinedBounds.extents.magnitude;
        return Mathf.Max(magnitude, 0.01f);
    }

    private void UpdateFocusMaterialProperties()
    {
        if (focusMaterialInstance == null) return;

        float outlineWidth = Mathf.Max(0f, focusOutlineWidth) * focusBoundsScale;
        float glowWidth = Mathf.Max(0f, focusGlowWidth) * focusBoundsScale;

        focusMaterialInstance.SetFloat(OutlineWidthId, outlineWidth);
        focusMaterialInstance.SetColor(OutlineColorId, focusOutlineColor);
        focusMaterialInstance.SetFloat(OutlineSoftnessId, Mathf.Clamp01(focusOutlineSoftness));
        focusMaterialInstance.SetFloat(GlowWidthId, glowWidth);
        focusMaterialInstance.SetColor(GlowColorId, focusGlowColor);
        focusMaterialInstance.SetFloat(GlowSoftnessId, Mathf.Clamp01(focusGlowSoftness));
        focusMaterialInstance.SetFloat(GlowIntensityId, Mathf.Max(0f, focusGlowIntensity));
    }
}
