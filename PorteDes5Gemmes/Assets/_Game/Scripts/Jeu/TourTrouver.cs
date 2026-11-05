using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// « Trouve les 3 champignons rouges. » Les bons objets sautent et vont dans le panier ;
    /// les autres font « pouic » et se dandinent. Jamais de son d'erreur.
    /// </summary>
    public class TourTrouver : ITour
    {
        class Objet
        {
            public ObjetCache def;
            public bool bon;
            public bool ramasse;
            public SpriteRenderer sr;
            public Touchable zone;
        }

        readonly Plateau plateau;
        readonly Tour tour;
        readonly string joueur;
        readonly List<Objet> objets = new List<Objet>();
        int trouves;
        bool fini;

        public int Resultat => trouves;

        public TourTrouver(Plateau plateau, Tour tour, string joueur, List<(ObjetCache def, bool bon, SpriteRenderer sr)> places)
        {
            this.plateau = plateau;
            this.tour = tour;
            this.joueur = joueur;
            foreach (var (def, bon, sr) in places)
            {
                var o = new Objet { def = def, bon = bon, sr = sr };
                o.zone = sr.GetComponent<Touchable>();
                o.zone.ViderEcouteurs();
                o.zone.Pose += _ => Toucher(o);
                objets.Add(o);
            }
            plateau.Panier.Capacite = CompterBons();
        }

        int CompterBons()
        {
            int n = 0;
            foreach (var o in objets) if (o.bon) n++;
            return n;
        }

        public IEnumerator Jouer()
        {
            int total = CompterBons();
            while (!fini) yield return null;
            yield return Voix.Instance.Dire(tour.success_line?.Pour(joueur));
            trouves = total;
        }

        void Toucher(Objet o)
        {
            if (o.ramasse || fini) return;
            if (!o.bon)
            {
                Bruitages.Instance?.Pouic();
                plateau.StartCoroutine(Anim.Dandine(o.sr.transform, 14f, 0.65f));
                return;
            }
            Ramasser(o);
        }

        void Ramasser(Objet o)
        {
            o.ramasse = true;
            o.zone.Actif = false;
            trouves++;
            int n = trouves;
            plateau.Progres();
            Bruitages.Instance?.Succes();
            if (tour.count_aloud) Voix.Instance.DireSansAttendre(plateau.Donnees.lines.Nombre(n));
            plateau.StartCoroutine(VersLePanier(o, n == CompterBons()));
        }

        IEnumerator VersLePanier(Objet o, bool dernier)
        {
            var tr = o.sr.transform;
            yield return Anim.Saut(tr, 14, 0.55f);
            var place = plateau.Panier.ProchainePlace();
            tr.GetComponent<Scintille>()?.Let(s => s.enabled = false);
            o.sr.sortingOrder = Couches.Effets;
            yield return Anim.Arc(tr, place, 24f, 0.5f);
            plateau.Panier.Ajouter(tr);
            if (dernier) fini = true;
        }

        public Vector2? CibleAide()
        {
            foreach (var o in objets)
                if (o.bon && !o.ramasse) return (Vector2)o.sr.transform.localPosition + new Vector2(0, 12);
            return null;
        }

        public void Marquer(bool oui)
        {
            foreach (var o in objets)
            {
                var s = o.sr.GetComponent<Scintille>();
                if (s) s.enabled = false;
            }
            if (!oui) return;
            foreach (var o in objets)
            {
                if (o.bon && !o.ramasse)
                {
                    Scintille.Sur(o.sr.gameObject, new Vector2(0, 12), 14f).enabled = true;
                    return;
                }
            }
        }

        public void Resoudre()
        {
            foreach (var o in objets)
            {
                if (o.bon && !o.ramasse)
                {
                    Ramasser(o);
                    return;
                }
            }
        }

        public void Nettoyer()
        {
            Marquer(false);
            foreach (var o in objets)
            {
                o.zone.ViderEcouteurs();
                var tr = o.sr.transform;
                o.zone.Pose += _ => { Bruitages.Instance?.Pouic(); plateau.StartCoroutine(Anim.Dandine(tr, 10f, 0.5f)); };
            }
        }
    }

    static class ExtensionsObjet
    {
        public static void Let<T>(this T obj, System.Action<T> action) where T : Object
        {
            if (obj != null) action(obj);
        }
    }
}
