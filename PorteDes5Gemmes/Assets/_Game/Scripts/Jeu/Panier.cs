using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>Le panier où atterrissent les champignons trouvés. Sert aussi à compter « avec moi ».</summary>
    public class Panier
    {
        readonly MonoBehaviour hote;
        readonly Transform racine;
        readonly List<Transform> contenu = new List<Transform>();
        public int Capacite = 3;

        public int Nombre => contenu.Count;

        public Panier(MonoBehaviour hote, Transform parent, Vector2 position)
        {
            this.hote = hote;
            var go = new GameObject("Panier");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Coords.Arrondi(position);
            racine = go.transform;
            Fabrique.Sprite("Dos", "panier_dos", racine, Vector2.zero, Couches.PanierDos);
            Fabrique.Sprite("Face", "panier_face", racine, Vector2.zero, Couches.PanierFace);
        }

        /// <summary>Position (dans le parent du panier) où le prochain champignon va se poser.</summary>
        public Vector3 ProchainePlace()
        {
            int i = contenu.Count;
            int n = Mathf.Max(Capacite, i + 1);
            float pas = n <= 1 ? 0f : Mathf.Min(13f, 34f / (n - 1));
            float x = (i - (n - 1) / 2f) * pas;
            return racine.localPosition + new Vector3(Mathf.Round(x), -8f, 0f);
        }

        public void Ajouter(Transform objet)
        {
            objet.SetParent(racine, true);
            foreach (var sr in objet.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder = Couches.PanierContenu;
            contenu.Add(objet);
        }

        public IEnumerator Surligner(int i)
        {
            if (i < 0 || i >= contenu.Count) yield break;
            Bruitages.Instance?.Tap();
            yield return Anim.Saut(contenu[i], 8, 0.4f);
        }

        public void SurlignerTout()
        {
            for (int i = 0; i < contenu.Count; i++) hote.StartCoroutine(SurlignerApres(i, i * 0.12f));
        }

        IEnumerator SurlignerApres(int i, float delai)
        {
            yield return new WaitForSeconds(delai);
            yield return Anim.Saut(contenu[i], 8, 0.4f);
        }
    }
}
