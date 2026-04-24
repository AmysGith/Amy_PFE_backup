using System;
using UnityEngine;

[Serializable]
public class InstancedObject 
{
    public string label;
    public Mesh[] meshes;
    public Material material;
    [Min(0)] public int count = 200;
    public Vector2 scaleMinMax = new Vector2(0.8f, 1.2f);
    public Vector2 rotationMinMax = new Vector2(0f, 360f);
    public bool alignToSurface = false;
    public bool usePerlinDensity = true;
    [Range(0.01f, 1f)] public float perlinFrequency = 0.12f;
    public Vector2 perlinDensityMinMax = new Vector2(0.2f, 1.3f);
    public bool useClusters = true;
    [Range(1, 16)] public int clustersPerChunk = 6;
    [Range(0.2f, 20f)] public float clusterRadius = 4f;
    [Range(0f, 1f)] public float clusterWeight = 0.85f;
    public bool snapYawTo90 = true;
    public Vector3 baseRotation;

    [Header("Instance Colors")]
    [Tooltip("Vide = couleur du material. Sinon, couleur aleatoire parmi cette liste.")]
    public Color[] instanceColors;
}