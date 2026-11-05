using System.Collections.Generic;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Petits sons synthétisés (comme dans le jeu web) : pas de fichier audio à fournir.
    /// Aucun son d'erreur : un mauvais choix fait juste « pouic ».
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Bruitages : MonoBehaviour
    {
        public static Bruitages Instance { get; private set; }

        const int Frequence = 44100;
        AudioSource source;
        readonly Dictionary<string, AudioClip> sons = new Dictionary<string, AudioClip>();

        enum Onde { Sinus, Triangle }

        struct Note
        {
            public float freq, debut, duree, gain, freqFin;
            public Onde onde;
        }

        void Awake()
        {
            Instance = this;
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;

            sons["tap"] = Fabriquer(new[] { N(880, 0, 0.09f, Onde.Triangle, 0.14f) });
            sons["pouic"] = Fabriquer(new[] { N(600, 0, 0.2f, Onde.Sinus, 0.12f, 1050) });
            sons["succes"] = Fabriquer(new[]
            {
                N(523.25f, 0.00f, 0.35f, Onde.Triangle, 0.2f), N(659.25f, 0.08f, 0.35f, Onde.Triangle, 0.2f),
                N(783.99f, 0.16f, 0.35f, Onde.Triangle, 0.2f), N(1046.5f, 0.24f, 0.5f, Onde.Sinus, 0.14f),
            });
            var fete = new List<Note>();
            float[] arpege = { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f, 1318.5f };
            for (int i = 0; i < arpege.Length; i++) fete.Add(N(arpege[i], i * 0.12f, 0.42f, Onde.Triangle, 0.17f));
            sons["fete"] = Fabriquer(fete.ToArray());
            sons["envol"] = Fabriquer(new[] { N(400, 0, 0.3f, Onde.Sinus, 0.12f, 1700), N(1650, 0.07f, 0.12f, Onde.Sinus, 0.1f) });
            sons["dodo"] = Fabriquer(new[] { N(659.25f, 0, 0.5f, Onde.Sinus, 0.12f), N(523.25f, 0.3f, 0.5f, Onde.Sinus, 0.12f), N(392f, 0.6f, 0.8f, Onde.Sinus, 0.12f) });
        }

        static Note N(float f, float debut, float duree, Onde onde, float gain, float freqFin = 0f) =>
            new Note { freq = f, debut = debut, duree = duree, onde = onde, gain = gain, freqFin = freqFin > 0 ? freqFin : f };

        static AudioClip Fabriquer(Note[] notes)
        {
            float fin = 0f;
            foreach (var n in notes) fin = Mathf.Max(fin, n.debut + n.duree);
            int total = Mathf.CeilToInt((fin + 0.05f) * Frequence);
            var data = new float[total];
            foreach (var n in notes)
            {
                int i0 = Mathf.RoundToInt(n.debut * Frequence);
                int len = Mathf.RoundToInt(n.duree * Frequence);
                double phase = 0;
                for (int i = 0; i < len && i0 + i < total; i++)
                {
                    float t = i / (float)Frequence;
                    float k = i / (float)len;
                    float f = Mathf.Lerp(n.freq, n.freqFin, k);
                    phase += 2.0 * Mathf.PI * f / Frequence;
                    float v = n.onde == Onde.Sinus
                        ? Mathf.Sin((float)phase)
                        : 2f / Mathf.PI * Mathf.Asin(Mathf.Sin((float)phase));
                    float attaque = Mathf.Clamp01(t / 0.008f);
                    float declin = Mathf.Exp(-4f * k) * (1f - k);
                    data[i0 + i] += v * n.gain * attaque * declin;
                }
            }
            var clip = AudioClip.Create("son", total, 1, Frequence, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Jouer(string nom)
        {
            if (sons.TryGetValue(nom, out var c)) source.PlayOneShot(c);
        }

        public void Tap() => Jouer("tap");
        public void Pouic() => Jouer("pouic");
        public void Succes() => Jouer("succes");
        public void Fete() => Jouer("fete");
        public void Envol() => Jouer("envol");
        public void Dodo() => Jouer("dodo");
    }
}
