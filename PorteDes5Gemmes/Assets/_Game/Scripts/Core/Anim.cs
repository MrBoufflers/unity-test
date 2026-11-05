using System;
using System.Collections;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>Petites animations par coroutines (pas de dépendance externe).</summary>
    public static class Anim
    {
        public static float Douce(float t) => t * t * (3f - 2f * t);
        public static float Sortie(float t) => 1f - (1f - t) * (1f - t);

        public static float Rebond(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        public static IEnumerator Duree(float duree, Action<float> etape)
        {
            float t = 0f;
            while (t < duree)
            {
                etape(Mathf.Clamp01(t / duree));
                t += Time.deltaTime;
                yield return null;
            }
            etape(1f);
        }

        public static IEnumerator Deplacer(Transform tr, Vector3 vers, float duree, Func<float, float> courbe = null)
        {
            if (tr == null) yield break;
            Vector3 depart = tr.localPosition;
            courbe ??= Douce;
            yield return Duree(duree, t => { if (tr) tr.localPosition = Vector3.LerpUnclamped(depart, vers, courbe(t)); });
        }

        public static IEnumerator Echelle(Transform tr, Vector3 vers, float duree, Func<float, float> courbe = null)
        {
            if (tr == null) yield break;
            Vector3 depart = tr.localScale;
            courbe ??= Douce;
            yield return Duree(duree, t => { if (tr) tr.localScale = Vector3.LerpUnclamped(depart, vers, courbe(t)); });
        }

        public static IEnumerator Fondu(SpriteRenderer sr, float alpha, float duree)
        {
            if (sr == null) yield break;
            float depart = sr.color.a;
            yield return Duree(duree, t =>
            {
                if (!sr) return;
                var c = sr.color;
                c.a = Mathf.Lerp(depart, alpha, t);
                sr.color = c;
            });
        }

        /// <summary>Petit saut sur place avec écrasement (les bons objets).</summary>
        public static IEnumerator Saut(Transform tr, float hauteur, float duree)
        {
            if (tr == null) yield break;
            Vector3 base0 = tr.localPosition;
            Vector3 ech0 = tr.localScale;
            yield return Duree(duree, t =>
            {
                if (!tr) return;
                float h = Mathf.Sin(t * Mathf.PI) * hauteur;
                tr.localPosition = base0 + new Vector3(0f, Mathf.Round(h), 0f);
                float e = t < 0.15f ? 1f - t * 0.6f : (t > 0.85f ? 1f - (1f - t) * 0.6f : 1f);
                tr.localScale = new Vector3(ech0.x * (2f - e), ech0.y * e, 1f);
            });
            if (tr) { tr.localPosition = base0; tr.localScale = ech0; }
        }

        /// <summary>Petit dandinement neutre (les mauvais objets, le décor).</summary>
        public static IEnumerator Dandine(Transform tr, float angle = 12f, float duree = 0.6f)
        {
            if (tr == null) yield break;
            Quaternion r0 = tr.localRotation;
            yield return Duree(duree, t =>
            {
                if (tr) tr.localRotation = r0 * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 3f) * angle * (1f - t));
            });
            if (tr) tr.localRotation = r0;
        }

        /// <summary>Tremblement horizontal (mauvaise réponse, buisson qui frissonne).</summary>
        public static IEnumerator Tremble(Transform tr, float amplitude = 3f, float duree = 0.5f)
        {
            if (tr == null) yield break;
            Vector3 p0 = tr.localPosition;
            yield return Duree(duree, t =>
            {
                if (tr) tr.localPosition = p0 + new Vector3(Mathf.Round(Mathf.Sin(t * Mathf.PI * 6f) * amplitude * (1f - t)), 0f, 0f);
            });
            if (tr) tr.localPosition = p0;
        }

        /// <summary>Apparition « pop » : de rien à un peu trop grand, puis taille normale.</summary>
        public static IEnumerator Pop(Transform tr, float duree = 0.45f, float delai = 0f)
        {
            if (tr == null) yield break;
            Vector3 ech = Vector3.one;
            tr.localScale = Vector3.zero;
            if (delai > 0f) yield return new WaitForSeconds(delai);
            yield return Duree(duree, t => { if (tr) tr.localScale = ech * Rebond(t); });
        }

        /// <summary>Trajectoire en cloche (un objet qui vole vers le panier ou la couronne).</summary>
        public static IEnumerator Arc(Transform tr, Vector3 vers, float hauteur, float duree)
        {
            if (tr == null) yield break;
            Vector3 depart = tr.localPosition;
            yield return Duree(duree, t =>
            {
                if (!tr) return;
                float k = Douce(t);
                Vector3 p = Vector3.Lerp(depart, vers, k);
                p.y += Mathf.Sin(k * Mathf.PI) * hauteur;
                tr.localPosition = p;
            });
        }
    }
}
