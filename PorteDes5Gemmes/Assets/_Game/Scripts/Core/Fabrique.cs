using System.Collections.Generic;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>Charge les images (Resources/Art) et crée les objets du tableau.</summary>
    public static class Fabrique
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Image(string nom)
        {
            if (string.IsNullOrEmpty(nom)) return null;
            if (cache.TryGetValue(nom, out var s) && s != null) return s;
            s = Resources.Load<Sprite>("Art/" + nom);
            if (s == null) Debug.LogWarning($"Image introuvable : Resources/Art/{nom}.png");
            cache[nom] = s;
            return s;
        }

        public static SpriteRenderer Sprite(string nomObjet, string image, Transform parent, Vector2 position, int ordre)
        {
            var go = new GameObject(nomObjet);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Coords.Arrondi(position);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Image(image);
            sr.sortingOrder = ordre;
            return sr;
        }

        /// <summary>Ajoute une zone tactile au moins aussi grande que la cible minimale (≈ 2 cm sur téléphone).</summary>
        public static Touchable ZoneTactile(GameObject go, Vector2 taille, Vector2 decalage, int priorite)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(Mathf.Max(taille.x, Touchable.TailleMin), Mathf.Max(taille.y, Touchable.TailleMin));
            col.offset = decalage;
            var t = go.AddComponent<Touchable>();
            t.Priorite = priorite;
            return t;
        }

        /// <summary>Taille de l'image en pixels, marge invisible de 25 % comprise.</summary>
        public static Vector2 TailleAvecMarge(SpriteRenderer sr, float marge = 0.25f)
        {
            if (sr == null || sr.sprite == null) return Vector2.one * Touchable.TailleMin;
            return sr.sprite.bounds.size * (1f + marge);
        }

        public static Vector2 CentreImage(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return Vector2.zero;
            return sr.sprite.bounds.center;
        }
    }
}
