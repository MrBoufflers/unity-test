using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>Réglages parent, enregistrés sur l'appareil (PlayerPrefs).</summary>
    public static class Reglages
    {
        public const float VitesseMin = 0.8f;
        public const float VitesseMax = 1.1f;

        public static string PremierJoueur
        {
            get => PlayerPrefs.GetString("premier_joueur", "elea");
            set { PlayerPrefs.SetString("premier_joueur", value); PlayerPrefs.Save(); }
        }

        public static float VolumeVoix
        {
            get => PlayerPrefs.GetFloat("volume_voix", 1f);
            set { PlayerPrefs.SetFloat("volume_voix", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static float VitesseVoix
        {
            get => PlayerPrefs.GetFloat("vitesse_voix", 0.95f);
            set { PlayerPrefs.SetFloat("vitesse_voix", Mathf.Clamp(value, VitesseMin, VitesseMax)); PlayerPrefs.Save(); }
        }

        /// <summary>Sous-titres pour le parent. Activés par défaut quand l'appareil ne sait pas parler.</summary>
        public static bool SousTitres
        {
            get => PlayerPrefs.GetInt("sous_titres", Application.platform == RuntimePlatform.Android ? 0 : 1) == 1;
            set { PlayerPrefs.SetInt("sous_titres", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static string Autre(string joueur) => joueur == "elea" ? "isao" : "elea";

        public static string Prenom(string joueur) => joueur == "isao" ? "Isao" : "Elea";
    }
}
