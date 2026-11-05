using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>Quelques lucioles qui flottent dans la forêt (ambiance, non interactives).</summary>
    public class Lucioles : MonoBehaviour
    {
        const int Nombre = 9;
        SpriteRenderer[] points;
        Vector2[] bases;
        float[] phases;

        void Start()
        {
            points = new SpriteRenderer[Nombre];
            bases = new Vector2[Nombre];
            phases = new float[Nombre];
            var rnd = new System.Random(3);
            for (int i = 0; i < Nombre; i++)
            {
                bases[i] = new Vector2(rnd.Next(-170, 170), rnd.Next(-30, 60));
                phases[i] = (float)rnd.NextDouble() * 10f;
                points[i] = Fabrique.Sprite("Luciole", "fx_pixel", transform, bases[i], 295);
                points[i].transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            }
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < Nombre; i++)
            {
                float p = phases[i];
                var pos = bases[i] + new Vector2(Mathf.Sin(t * 0.4f + p) * 14f, Mathf.Sin(t * 0.7f + p * 2f) * 8f);
                points[i].transform.localPosition = Coords.Arrondi(pos);
                float a = Mathf.Clamp01(Mathf.Sin(t * 1.3f + p * 3f) * 0.8f + 0.3f);
                points[i].color = new Color(1f, 0.97f, 0.55f, a);
            }
        }
    }
}
