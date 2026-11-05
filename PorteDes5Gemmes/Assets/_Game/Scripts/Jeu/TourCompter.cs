using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// « Combien de champignons ? » Trois panneaux en bois (2, 3, 4).
    /// Une mauvaise réponse ne pénalise pas : Lumi propose de compter ensemble les champignons du panier.
    /// </summary>
    public class TourCompter : ITour
    {
        class Choix
        {
            public int valeur;
            public SpriteRenderer sr;
            public Touchable zone;
        }

        readonly Plateau plateau;
        readonly Tour tour;
        readonly string joueur;
        readonly int reponse;
        readonly List<Choix> choix = new List<Choix>();
        bool trouve;
        bool fini;
        int comptage;
        bool compteEnCours;

        public int Resultat => reponse;

        public TourCompter(Plateau plateau, Tour tour, string joueur, int reponse)
        {
            this.plateau = plateau;
            this.tour = tour;
            this.joueur = joueur;
            this.reponse = reponse;
        }

        List<int> Valeurs()
        {
            var v = new List<int>();
            if (tour.choices != null && System.Array.IndexOf(tour.choices, reponse) >= 0) v.AddRange(tour.choices);
            else { if (reponse > 0) v.Add(reponse - 1); v.Add(reponse); v.Add(reponse + 1); }
            return v;
        }

        public IEnumerator Jouer()
        {
            var valeurs = Valeurs();
            Vector2 centre = Coords.DepuisScene(800, tour.choices_y);
            float pas = tour.choices_spacing / 5f;
            for (int i = 0; i < valeurs.Count; i++)
            {
                float x = centre.x + (i - (valeurs.Count - 1) / 2f) * pas;
                // L'image a son point d'ancrage en bas : on la pose pour que son centre soit à la hauteur demandée
                var sr = Fabrique.Sprite("Panneau " + valeurs[i], "panneau_" + valeurs[i], plateau.transform,
                    new Vector2(x, centre.y - 22), Couches.Panneaux);
                var c = new Choix { valeur = valeurs[i], sr = sr };
                c.zone = Fabrique.ZoneTactile(sr.gameObject, Fabrique.TailleAvecMarge(sr, 0.1f), Fabrique.CentreImage(sr), 20);
                c.zone.Pose += _ => plateau.StartCoroutine(Toucher(c));
                choix.Add(c);
                plateau.StartCoroutine(Anim.Pop(sr.transform, 0.5f, i * 0.15f));
            }
            while (!fini) yield return null;
        }

        IEnumerator Toucher(Choix c)
        {
            if (trouve) yield break;
            if (c.valeur == reponse)
            {
                trouve = true;
                comptage++;
                plateau.Progres();
                plateau.ArreterAides();
                Voix.Instance.Taire();
                Bruitages.Instance?.Succes();
                foreach (var autre in choix)
                {
                    autre.zone.Actif = false;
                    if (autre != c) plateau.StartCoroutine(Anim.Fondu(autre.sr, 0.25f, 0.4f));
                }
                plateau.StartCoroutine(Anim.Echelle(c.sr.transform, new Vector3(1.2f, 1.2f, 1f), 0.4f, Anim.Rebond));
                plateau.Panier.SurlignerTout();
                yield return Voix.Instance.Dire(tour.success_line?.Pour(joueur));
                foreach (var autre in choix) plateau.StartCoroutine(Anim.Fondu(autre.sr, 0f, 0.5f));
                yield return new WaitForSeconds(0.55f);
                fini = true;
                yield break;
            }

            // Mauvaise réponse : petit tremblement, puis on compte ensemble
            Bruitages.Instance?.Tap();
            plateau.StartCoroutine(Anim.Tremble(c.sr.transform, 3f, 0.55f));
            if (!compteEnCours) plateau.StartCoroutine(CompterEnsemble());
        }

        IEnumerator CompterEnsemble()
        {
            compteEnCours = true;
            int moi = ++comptage;
            yield return Voix.Instance.Dire(tour.retry_line?.Pour(joueur));
            for (int i = 0; i < plateau.Panier.Nombre && moi == comptage && !trouve; i++)
            {
                plateau.StartCoroutine(plateau.Panier.Surligner(i));
                yield return Voix.Instance.Dire(plateau.Donnees.lines.Nombre(i + 1));
            }
            compteEnCours = false;
        }

        Choix Bon()
        {
            foreach (var c in choix) if (c.valeur == reponse) return c;
            return null;
        }

        public Vector2? CibleAide()
        {
            var b = Bon();
            if (b == null || trouve) return null;
            return (Vector2)b.sr.transform.localPosition + new Vector2(0, 30);
        }

        public void Marquer(bool oui)
        {
            foreach (var c in choix)
            {
                var s = c.sr.GetComponent<Scintille>();
                if (s) s.enabled = false;
            }
            var b = Bon();
            if (oui && b != null && !trouve) Scintille.Sur(b.sr.gameObject, new Vector2(0, 20), 18f).enabled = true;
        }

        public void Resoudre()
        {
            var b = Bon();
            if (b != null) plateau.StartCoroutine(Toucher(b));
        }

        public void Nettoyer()
        {
            Marquer(false);
            foreach (var c in choix) if (c.sr) Object.Destroy(c.sr.gameObject);
            choix.Clear();
        }
    }
}
