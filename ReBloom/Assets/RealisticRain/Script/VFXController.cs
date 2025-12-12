using UnityEngine;

namespace RealisticRain
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class VFXController : MonoBehaviour
    {
        [Header("Paramètres Modifiables")]
        [SerializeField, Tooltip("Couleur des particules générées.")]
        private Color particleColor = Color.white;

        [SerializeField, Min(0f), Tooltip("Taux d'émission des particules (Rate over Time). Par défaut : 200.")]
        private float intensity = 200f;

        [SerializeField, Tooltip("Direction et force du vent appliquée aux particules.")]
        private Vector3 windDirection = Vector3.zero;

        [SerializeField, Range(0f, 10f), Tooltip("Puissance globale du vent appliquée à la direction.")]
        private float windStrength = 1f;

        private ParticleSystem[] particleSystems;

        // Cache pour éviter les mises à jour inutiles
        private Color lastColor;
        private float lastIntensity;
        private Vector3 lastWindDirection;
        private float lastWindStrength;

        // =====================
        // == Cycle de vie ==
        // =====================

        private void Awake()
        {
            ApplySettings();
        }

        private void OnValidate()
        {
            // Évite les réapplications en boucle pendant le Play mode
            if (!Application.isPlaying)
                ApplySettings();
        }

        // =====================
        // == Méthodes internes ==
        // =====================

        private void EnsureParticlesCached()
        {
            if (particleSystems == null || particleSystems.Length == 0)
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            }
        }

        private void ApplySettings()
        {
            EnsureParticlesCached();

            // Empêche le recalcul si rien n’a changé
            if (particleColor == lastColor &&
                Mathf.Approximately(intensity, lastIntensity) &&
                windDirection == lastWindDirection &&
                Mathf.Approximately(windStrength, lastWindStrength))
                return;

            // Met à jour les caches
            lastColor = particleColor;
            lastIntensity = intensity;
            lastWindDirection = windDirection;
            lastWindStrength = windStrength;

            // Applique les paramètres à tous les systèmes
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;

                var main = ps.main;
                var emission = ps.emission;
                var velocityOverLifetime = ps.velocityOverLifetime;

                // Couleur des particules
                main.startColor = particleColor;

                // Taux d'émission
                var rate = emission.rateOverTime;
                rate.constant = intensity;
                emission.rateOverTime = rate;

                // Direction du vent (si activé)
                if (velocityOverLifetime.enabled)
                {
                    velocityOverLifetime.x = windDirection.x * windStrength;
                    velocityOverLifetime.y = windDirection.y * windStrength;
                    velocityOverLifetime.z = windDirection.z * windStrength;
                }
            }
        }

        // =====================
        // == Méthodes publiques ==
        // =====================

        public void SetParticleColor(Color newColor)
        {
            particleColor = newColor;
            ApplySettings();
        }

        public void SetIntensity(float newIntensity)
        {
            intensity = Mathf.Max(0f, newIntensity);
            ApplySettings();
        }

        public void SetWindDirection(Vector3 newWindDirection)
        {
            windDirection = newWindDirection;
            ApplySettings();
        }

        public void SetWindStrength(float newStrength)
        {
            windStrength = Mathf.Max(0f, newStrength);
            ApplySettings();
        }

        // === Getters ===
        public Color GetParticleColor() => particleColor;
        public float GetIntensity() => intensity;
        public Vector3 GetWindDirection() => windDirection;
        public float GetWindStrength() => windStrength;
    }
}
