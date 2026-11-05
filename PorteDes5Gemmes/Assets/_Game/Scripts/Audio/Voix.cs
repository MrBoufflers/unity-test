using System;
using System.Collections;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// La voix de Lumi. Une nouvelle phrase coupe la précédente.
    /// Sans synthèse vocale (éditeur, ordinateur), on attend une durée estimée et les sous-titres s'affichent.
    /// </summary>
    public class Voix : MonoBehaviour
    {
        public const float Hauteur = 1.15f;

        public static Voix Instance { get; private set; }

        /// <summary>La dernière consigne, répétée par le bouton « oreille ».</summary>
        public string DerniereConsigne { get; private set; }

        public bool EnTrainDeParler { get; private set; }

        /// <summary>Prévient Lumi (elle brille quand elle parle) et les sous-titres.</summary>
        public event Action<string> DebutPhrase;
        public event Action FinPhrase;

        SyntheseAndroid synthese;
        int jeton;

        public bool SyntheseDisponible => synthese != null && synthese.Disponible;

        void Awake()
        {
            Instance = this;
            synthese = new SyntheseAndroid();
        }

        void OnDestroy()
        {
            synthese?.Fermer();
            if (Instance == this) Instance = null;
        }

        /// <summary>Dit une phrase et attend qu'elle soit finie (à utiliser avec yield return).</summary>
        public IEnumerator Dire(string texte, bool consigne = false)
        {
            if (string.IsNullOrEmpty(texte)) yield break;
            if (consigne) DerniereConsigne = texte;
            int moi = ++jeton;
            EnTrainDeParler = true;
            DebutPhrase?.Invoke(texte);

            if (SyntheseDisponible)
            {
                synthese.Dire(texte, Reglages.VolumeVoix, Reglages.VitesseVoix, Hauteur);
                // Le moteur met un court instant à démarrer
                float attente = 0f;
                while (!synthese.Parle && attente < 1.2f && moi == jeton) { attente += Time.unscaledDeltaTime; yield return null; }
                while (synthese.Parle && moi == jeton) yield return null;
            }
            else
            {
                float duree = DureeEstimee(texte);
                float t = 0f;
                while (t < duree && moi == jeton) { t += Time.unscaledDeltaTime; yield return null; }
            }

            if (moi == jeton)
            {
                EnTrainDeParler = false;
                FinPhrase?.Invoke();
            }
        }

        /// <summary>Dit une phrase sans attendre (compter « Un ! » pendant qu'un champignon saute).</summary>
        public void DireSansAttendre(string texte, bool consigne = false) => StartCoroutine(Dire(texte, consigne));

        public void Repeter()
        {
            if (!string.IsNullOrEmpty(DerniereConsigne)) DireSansAttendre(DerniereConsigne);
        }

        public void Taire()
        {
            jeton++;
            synthese?.Arreter();
            if (EnTrainDeParler)
            {
                EnTrainDeParler = false;
                FinPhrase?.Invoke();
            }
        }

        public static float DureeEstimee(string texte)
        {
            float vitesse = Mathf.Max(0.5f, Reglages.VitesseVoix);
            return 0.35f + texte.Length * 0.062f / vitesse;
        }
    }
}
