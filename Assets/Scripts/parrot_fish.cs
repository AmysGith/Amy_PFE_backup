using UnityEngine;

/// <summary>
/// FishTerrainSwimmer - Fait nager un poisson autour du Terrain Unity
/// sans jamais le dépasser et en restant toujours au-dessus de sa surface.
///
/// SETUP :
///  1. Ajoute ce script sur ton objet "Poisson".
///  2. Assigne le Terrain dans l'Inspector (champ "terrain").
///  3. Règle les paramètres selon tes goûts.
///
/// FONCTIONNEMENT :
///  - Le poisson suit une trajectoire elliptique autour du terrain.
///  - Sa hauteur est constamment clampée au-dessus de la surface du terrain.
///  - Il fait la tour en permanence, en regardant toujours dans la direction du mouvement.
/// </summary>
public class FishTerrainSwimmer : MonoBehaviour
{
    [Header("Terrain")]
    [Tooltip("Le Terrain Unity autour duquel le poisson nage.")]
    public Terrain terrain;

    [Header("Trajectoire")]
    [Tooltip("Marge (en unités) par rapport aux bords du terrain.")]
    public float borderMargin = 5f;

    [Tooltip("Vitesse angulaire du poisson en degrés par seconde.")]
    public float orbitSpeed = 30f;

    [Tooltip("Hauteur minimale au-dessus de la surface du terrain.")]
    public float minHeightAboveTerrain = 2f;

    [Tooltip("Hauteur maximale au-dessus de la surface du terrain (pour ajouter de l'ondulation).")]
    public float maxHeightAboveTerrain = 8f;

    [Tooltip("Vitesse d'ondulation verticale (monte/descend doucement).")]
    public float verticalWaveSpeed = 0.5f;

    [Header("Rotation du poisson")]
    [Tooltip("Temps de lissage de la rotation (plus c'est grand, plus c'est doux). 0.1 = réactif, 0.4 = très fluide.")]
    public float rotationSmoothTime = 0.25f;

    [Tooltip("Inclinaison maximale (pitch) selon la montée/descente, en degrés.")]
    public float pitchTiltAmount = 20f;

    // -- Privé --
    private float currentAngle = 0f;
    private float verticalWaveOffset = 0f;
    private Vector3 terrainCenter;
    private float orbitRadiusX;
    private float orbitRadiusZ;

    // SmoothDamp sur les angles Euler pour une rotation vraiment fluide
    private Vector3 currentEuler;
    private Vector3 eulerVelocity = Vector3.zero;

    void Start()
    {
        if (terrain == null)
        {
            // Tente de trouver le terrain automatiquement
            terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Debug.LogError("[FishTerrainSwimmer] Aucun Terrain assigné et aucun Terrain actif trouvé !");
                enabled = false;
                return;
            }
        }

        // Centre du terrain
        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;
        terrainCenter = new Vector3(tPos.x + tSize.x / 2f, tPos.y, tPos.z + tSize.z / 2f);

        // Rayons de l'ellipse = demi-taille du terrain moins la marge
        orbitRadiusX = (tSize.x / 2f) - borderMargin;
        orbitRadiusZ = (tSize.z / 2f) - borderMargin;

        // S'assure que les rayons sont positifs
        orbitRadiusX = Mathf.Max(orbitRadiusX, 1f);
        orbitRadiusZ = Mathf.Max(orbitRadiusZ, 1f);

        // Offset d'ondulation aléatoire pour que plusieurs poissons soient désynchronisés
        verticalWaveOffset = Random.Range(0f, Mathf.PI * 2f);

        // Place le poisson à sa position initiale
        transform.position = GetOrbitPosition(currentAngle, GetWaveHeight(0f));

        // Initialise la rotation sur la tangente de départ
        Vector3 startTangent = GetOrbitTangent(currentAngle, 0f);
        currentEuler = Quaternion.LookRotation(startTangent).eulerAngles;
        transform.rotation = Quaternion.Euler(currentEuler);

        Debug.Log($"[FishTerrainSwimmer] Initialisé — Centre: {terrainCenter}, Rayons: ({orbitRadiusX}, {orbitRadiusZ})");
    }

    void Update()
    {
        // 1. Avance l'angle de l'orbite
        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        // 2. Calcul de la hauteur d'ondulation
        float waveTime = Time.time * verticalWaveSpeed + verticalWaveOffset;
        float currentWaveHeight = GetWaveHeight(waveTime);

        // 3. Position sur l'ellipse
        transform.position = GetOrbitPosition(currentAngle, currentWaveHeight);

        // 4. Direction analytique (tangente exacte de l'ellipse + pente verticale)
        //    → aucune saccade liée au delta de position entre frames
        Vector3 tangent = GetOrbitTangent(currentAngle, waveTime);

        // 5. Rotation cible depuis la tangente
        Quaternion targetRot = Quaternion.LookRotation(tangent);
        Vector3 targetEuler = targetRot.eulerAngles;

        // Corrige le wrap-around 0°/360° pour éviter les rotations parasites
        currentEuler.x = WrapAngle(currentEuler.x);
        currentEuler.y = WrapAngle(currentEuler.y);
        currentEuler.z = WrapAngle(currentEuler.z);
        targetEuler.x = WrapAngle(targetEuler.x);
        targetEuler.y = WrapAngle(targetEuler.y);
        targetEuler.z = WrapAngle(targetEuler.z);

        // 6. SmoothDamp sur les angles Euler → transition 100 % fluide
        currentEuler = new Vector3(
            Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref eulerVelocity.x, rotationSmoothTime),
            Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref eulerVelocity.y, rotationSmoothTime),
            Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref eulerVelocity.z, rotationSmoothTime)
        );

        transform.rotation = Quaternion.Euler(currentEuler);
    }

    /// <summary>
    /// Calcule la tangente analytique de l'ellipse à l'angle donné,
    /// combinée avec la dérivée verticale de l'onde → direction exacte du mouvement.
    /// </summary>
    private Vector3 GetOrbitTangent(float angleDeg, float waveTime)
    {
        float rad = angleDeg * Mathf.Deg2Rad;

        // Dérivée de (cos θ · rX, sin θ · rZ) par rapport à θ
        float dx = -Mathf.Sin(rad) * orbitRadiusX;
        float dz = Mathf.Cos(rad) * orbitRadiusZ;

        // Dérivée verticale de l'onde (cosinus car dérivée de sinus)
        float dyWave = Mathf.Cos(waveTime) * verticalWaveSpeed
                       * (maxHeightAboveTerrain - minHeightAboveTerrain) * 0.5f;

        // Inclinaison clampée
        float horizontalMag = Mathf.Sqrt(dx * dx + dz * dz);
        float pitchRad = Mathf.Atan2(dyWave, horizontalMag);
        float clampedPitch = Mathf.Clamp(pitchRad * Mathf.Rad2Deg, -pitchTiltAmount, pitchTiltAmount);
        float dy = Mathf.Tan(clampedPitch * Mathf.Deg2Rad) * horizontalMag;

        return new Vector3(dx, dy, dz).normalized;
    }

    /// <summary>Ramène un angle dans [-180, 180] pour un SmoothDamp stable.</summary>
    private static float WrapAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// Calcule la position 3D sur l'ellipse pour un angle donné,
    /// en clampant la hauteur au-dessus du terrain.
    /// </summary>
    private Vector3 GetOrbitPosition(float angleDeg, float waveHeight)
    {
        float rad = angleDeg * Mathf.Deg2Rad;

        // Position XZ sur l'ellipse
        float x = terrainCenter.x + Mathf.Cos(rad) * orbitRadiusX;
        float z = terrainCenter.z + Mathf.Sin(rad) * orbitRadiusZ;

        // Hauteur du terrain en ce point
        float terrainY = terrain.SampleHeight(new Vector3(x, 0f, z))
                         + terrain.transform.position.y;

        // Hauteur finale = terrain + hauteur d'onde (déjà entre min et max)
        float y = terrainY + waveHeight;

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Retourne une hauteur d'ondulation entre minHeightAboveTerrain et maxHeightAboveTerrain.
    /// </summary>
    private float GetWaveHeight(float t)
    {
        float normalized = (Mathf.Sin(t) + 1f) / 2f; // 0..1
        return Mathf.Lerp(minHeightAboveTerrain, maxHeightAboveTerrain, normalized);
    }

    /// <summary>
    /// Dessine les gizmos dans l'éditeur pour visualiser la trajectoire.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (terrain == null) return;

        Vector3 tPos = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;
        Vector3 center = new Vector3(tPos.x + tSize.x / 2f, tPos.y, tPos.z + tSize.z / 2f);
        float rX = Mathf.Max((tSize.x / 2f) - borderMargin, 1f);
        float rZ = Mathf.Max((tSize.z / 2f) - borderMargin, 1f);

        Gizmos.color = Color.cyan;
        int steps = 64;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= steps; i++)
        {
            float angle = (i / (float)steps) * 360f * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(angle) * rX;
            float z = center.z + Mathf.Sin(angle) * rZ;
            float y = terrain.SampleHeight(new Vector3(x, 0f, z))
                      + terrain.transform.position.y + minHeightAboveTerrain;
            Vector3 p = new Vector3(x, y, z);

            if (i > 0) Gizmos.DrawLine(prev, p);
            prev = p;
        }

        // Dessine les bords du terrain
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            new Vector3(tPos.x + tSize.x / 2f, tPos.y + tSize.y / 2f, tPos.z + tSize.z / 2f),
            tSize
        );
    }
}