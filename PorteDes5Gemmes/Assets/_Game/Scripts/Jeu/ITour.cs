using System.Collections;
using UnityEngine;

namespace PorteDes5Gemmes
{
    /// <summary>Une tâche d'un enfant dans le tableau (trouver des objets, choisir un nombre...).</summary>
    public interface ITour
    {
        /// <summary>Se joue jusqu'à la réussite. Résultat : le nombre compté (ou -1).</summary>
        IEnumerator Jouer();

        int Resultat { get; }

        /// <summary>Où Lumi doit aider (null s'il n'y a plus rien à trouver).</summary>
        Vector2? CibleAide();

        /// <summary>Fait scintiller (ou non) la bonne réponse : 3e niveau d'aide.</summary>
        void Marquer(bool oui);

        /// <summary>Dernier niveau d'aide : Lumi fait l'action à la place de l'enfant.</summary>
        void Resoudre();

        void Nettoyer();
    }
}
