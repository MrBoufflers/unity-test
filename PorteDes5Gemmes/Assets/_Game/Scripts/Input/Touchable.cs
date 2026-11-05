using System;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>
    /// Un objet qu'on peut toucher. Il lui faut un Collider2D (la zone tactile, plus large que le dessin).
    /// </summary>
    public class Touchable : MonoBehaviour
    {
        /// <summary>Zone tactile minimale en pixels de référence (environ 1,3 cm sur un téléphone, 2 cm sur une tablette).</summary>
        public const float TailleMin = 36f;

        /// <summary>Quand plusieurs zones se chevauchent, la priorité la plus haute gagne (les objets du jeu passent avant le décor).</summary>
        public int Priorite;

        public bool Actif = true;

        /// <summary>Doigt posé (identifiant du doigt, -1 pour la souris).</summary>
        public event Action<int> Pose;

        /// <summary>Doigt levé.</summary>
        public event Action<int> Leve;

        public void Poser(int doigt) => Pose?.Invoke(doigt);
        public void Lever(int doigt) => Leve?.Invoke(doigt);

        public void ViderEcouteurs()
        {
            Pose = null;
            Leve = null;
        }
    }
}
