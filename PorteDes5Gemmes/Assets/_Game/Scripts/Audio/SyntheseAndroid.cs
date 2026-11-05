using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Synthèse vocale d'Android (android.speech.tts.TextToSpeech), en français.
    /// Sur les autres plateformes, Disponible reste faux et le jeu affiche les sous-titres.
    /// </summary>
    public class SyntheseAndroid
    {
        public bool Disponible { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject tts;

        class EcouteurInit : AndroidJavaProxy
        {
            readonly SyntheseAndroid parent;
            public EcouteurInit(SyntheseAndroid p) : base("android.speech.tts.TextToSpeech$OnInitListener") { parent = p; }

            // Appelé par Android (sur un autre fil) quand le moteur est prêt.
            public void onInit(int status) { parent.Pret(status == 0); }
        }

        public SyntheseAndroid()
        {
            try
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                var activite = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activite, new EcouteurInit(this));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Synthèse vocale indisponible : " + e.Message);
            }
        }

        void Pret(bool ok)
        {
            if (!ok || tts == null) return;
            AndroidJNI.AttachCurrentThread();
            try
            {
                using var locale = new AndroidJavaObject("java.util.Locale", "fr", "FR");
                int resultat = tts.Call<int>("setLanguage", locale);
                // -1 : langue manquante, -2 : langue non prise en charge
                Disponible = resultat >= 0;
                if (!Disponible) Debug.LogWarning("Voix française absente sur cet appareil.");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Réglage de la langue impossible : " + e.Message);
            }
        }

        public void Dire(string texte, float volume, float vitesse, float hauteur)
        {
            if (!Disponible) return;
            tts.Call<int>("setSpeechRate", vitesse);
            tts.Call<int>("setPitch", hauteur);
            using var params_ = new AndroidJavaObject("java.util.HashMap");
            // Clé « volume » = TextToSpeech.Engine.KEY_PARAM_VOLUME
            params_.Call<AndroidJavaObject>("put", "volume", volume.ToString(System.Globalization.CultureInfo.InvariantCulture));
            // Ancienne signature speak(String, int, HashMap) : la plus simple à appeler depuis Unity.
            tts.Call<int>("speak", texte, 0, params_);
        }

        public bool Parle => Disponible && tts.Call<bool>("isSpeaking");

        public void Arreter()
        {
            if (Disponible) tts.Call<int>("stop");
        }

        public void Fermer()
        {
            if (tts == null) return;
            tts.Call("shutdown");
            tts.Dispose();
            tts = null;
            Disponible = false;
        }
#else
        public void Dire(string texte, float volume, float vitesse, float hauteur) { }
        public bool Parle => false;
        public void Arreter() { }
        public void Fermer() { }
#endif
    }
}
