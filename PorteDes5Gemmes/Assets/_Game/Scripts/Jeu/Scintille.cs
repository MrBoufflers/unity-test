using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>Petites étincelles autour d'un objet : l'aide « c'est ici ! ».</summary>
    public class Scintille : MonoBehaviour
    {
        SpriteRenderer[] etincelles;
        Vector2 centre;
        float rayon;

        public static Scintille Sur(GameObject go, Vector2 centreLocal, float rayon)
        {
            var s = go.GetComponent<Scintille>();
            if (s == null) s = go.AddComponent<Scintille>();
            s.centre = centreLocal;
            s.rayon = rayon;
            return s;
        }

        void OnEnable()
        {
            if (etincelles == null)
            {
                etincelles = new SpriteRenderer[3];
                for (int i = 0; i < 3; i++)
                    etincelles[i] = Fabrique.Sprite("Étincelle", "fx_etincelle", transform, centre, Couches.Effets);
            }
            foreach (var e in etincelles) e.enabled = true;
        }

        void OnDisable()
        {
            if (etincelles == null) return;
            foreach (var e in etincelles) if (e) e.enabled = false;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < etincelles.Length; i++)
            {
                float a = t * 2.2f + i * Mathf.PI * 2f / 3f;
                var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rayon;
                etincelles[i].transform.localPosition = Coords.Arrondi(p);
                float c = 0.5f + 0.5f * Mathf.Sin(t * 9f + i * 2f);
                etincelles[i].color = new Color(1f, 1f, 0.8f, 0.4f + c * 0.6f);
            }
        }
    }
}
