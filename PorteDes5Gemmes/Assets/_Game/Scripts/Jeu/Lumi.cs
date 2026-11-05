using System.Collections;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Lumi, la fée-luciole : elle porte la voix du jeu et sert d'aide progressive
    /// (elle volette près de la bonne zone, puis se pose dessus).
    /// </summary>
    public class Lumi : MonoBehaviour
    {
        enum Mode { Vole, Pose, Dort }

        SpriteRenderer corps;
        SpriteRenderer halo;
        SpriteRenderer zz;
        Sprite aile0, aile1, dort;
        Vector3 cible;
        Vector3 maison;
        Mode mode = Mode.Vole;
        float vitesse = 140f;
        bool parle;

        public static Lumi Creer(Transform parent, Vector2 depart, Vector2 maison)
        {
            var go = new GameObject("Lumi");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<Lumi>();
            l.Init(depart, maison);
            return l;
        }

        void Init(Vector2 depart, Vector2 maisonPos)
        {
            aile0 = Fabrique.Image("lumi_0");
            aile1 = Fabrique.Image("lumi_1");
            dort = Fabrique.Image("lumi_dort");
            halo = Fabrique.Sprite("Halo", "fx_halo", transform, Vector2.zero, Couches.Lumi - 1);
            corps = Fabrique.Sprite("Corps", "lumi_0", transform, Vector2.zero, Couches.Lumi);
            zz = Fabrique.Sprite("Zz", "fx_zz", transform, new Vector2(7, 9), Couches.Lumi + 1);
            zz.enabled = false;
            transform.localPosition = depart;
            maison = maisonPos;
            cible = maisonPos;

            var voix = Voix.Instance;
            if (voix != null)
            {
                voix.DebutPhrase += _ => parle = true;
                voix.FinPhrase += () => parle = false;
            }
        }

        void Update()
        {
            float t = Time.time;
            // Battement d'ailes et flottement
            if (mode != Mode.Dort) corps.sprite = (Mathf.FloorToInt(t * 10f) % 2 == 0) ? aile0 : aile1;

            Vector3 flottement = mode == Mode.Vole ? new Vector3(Mathf.Sin(t * 1.3f) * 3f, Mathf.Sin(t * 2.6f) * 2f, 0f) : Vector3.zero;
            Vector3 visee = cible + flottement;
            Vector3 p = Vector3.MoveTowards(transform.localPosition, visee, vitesse * Time.deltaTime);
            transform.localPosition = p;
            // Le dessin reste aligné sur la grille de pixels
            corps.transform.localPosition = Coords.Arrondi(p) - p;

            // Halo : pulse quand Lumi parle
            float pulse = parle ? 0.75f + Mathf.Sin(t * 12f) * 0.25f : 0.55f + Mathf.Sin(t * 2f) * 0.08f;
            if (mode == Mode.Dort) pulse = 0.25f;
            var c = halo.color;
            c.a = pulse;
            halo.color = c;
            float s = parle ? 1.15f : 1f;
            halo.transform.localScale = new Vector3(s, s, 1f);

            if (zz.enabled) zz.transform.localPosition = new Vector3(7, 9 + Mathf.Round(Mathf.PingPong(t * 3f, 3f)), 0);
        }

        public void Maison()
        {
            Reveiller();
            mode = Mode.Vole;
            vitesse = 140f;
            cible = maison;
        }

        /// <summary>Aide n°2 : Lumi volette près de la cible.</summary>
        public void VoletterPres(Vector2 point)
        {
            Reveiller();
            mode = Mode.Vole;
            vitesse = 120f;
            float cote = point.x > 0 ? -1f : 1f;
            cible = new Vector3(point.x + cote * 22f, point.y + 18f, 0f);
        }

        /// <summary>Aide n°3 : Lumi se pose sur la cible.</summary>
        public void SePoser(Vector2 point)
        {
            Reveiller();
            mode = Mode.Pose;
            vitesse = 120f;
            cible = new Vector3(point.x, point.y + 6f, 0f);
        }

        public IEnumerator Dormir(Vector2 point)
        {
            mode = Mode.Vole;
            vitesse = 60f;
            cible = new Vector3(point.x, point.y, 0f);
            while (Vector3.Distance(transform.localPosition, cible) > 1f) yield return null;
            mode = Mode.Dort;
            corps.sprite = dort;
            zz.enabled = true;
        }

        void Reveiller()
        {
            zz.enabled = false;
        }
    }
}
