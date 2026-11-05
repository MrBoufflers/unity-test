# Isao & Elea — La Porte des 5 gemmes (version Unity, pixel art)

Portage Unity du point-and-click pour enfants de 4-5 ans « La Porte des 5 gemmes ».
Cette première version contient **le tableau 1 : la forêt enchantée (gemme rouge)**, en pixel art.

## Ce qui se passe dans le tableau

1. Écran d'accueil avec un gros bouton « Jouer ».
2. Lumi, la fée-luciole, arrive et présente la forêt (voix française du téléphone).
3. **Elea** trouve les 3 champignons rouges ; chaque champignon saute dans le panier pendant que Lumi compte.
   Un mauvais champignon fait juste « pouic » : il n'y a jamais d'erreur ni de son d'échec.
4. **Isao** dit combien il y en a en touchant le panneau 2, 3 ou 4.
   En cas d'erreur, Lumi propose : « Compte avec moi… » et compte les champignons du panier.
5. La gemme rouge apparaît : « Touchez-la tous les deux ! » (deux doigts en même temps = grande fête ;
   un seul doigt marche aussi, en appuyant 2 secondes ou en tapotant 2 fois).
6. La gemme s'envole dans la couronne des 5 creux en haut de l'écran.
7. La nuit tombe, Lumi s'endort sur sa branche : « À demain pour le Pays de l'Imaginaire ! »

**Aide progressive** si un enfant hésite : 8 s, Lumi redit la consigne · 15 s, elle volette près de la bonne
réponse · 25 s, elle se pose dessus et la réponse scintille · 35 s, « Je vais t'aider ! ».

**Réglages parent** : appui long de 3 secondes sur la petite roue en haut à droite
(qui commence, volume et vitesse de la voix, sous-titres, recommencer).

À chaque partie, les couleurs des champignons changent de place.

## Ouvrir le projet

1. Installe **Unity Hub**, puis l'éditeur **Unity 6000.6.8f1** avec le module **Android Build Support**.
2. Clone ce dépôt, puis dans Unity Hub : **Add → Add project from disk** → choisis le dossier `PorteDes5Gemmes`.
3. Ouvre la scène `Assets/_Game/Scenes/Foret.unity` et appuie sur ▶ (Play).
   Dans l'éditeur il n'y a pas de voix : les phrases de Lumi s'affichent en sous-titres.

Le menu **Porte des 5 gemmes** (barre du haut de l'éditeur) permet de recréer la scène et de construire le jeu
pour Android (APK), Windows ou Linux.

## Installer sur un téléphone Android

Menu **Porte des 5 gemmes → 3. Construire pour Android (APK)**, puis copie
`PorteDes5Gemmes/Builds/Android/PorteDes5Gemmes.apk` sur le téléphone et ouvre-le
(il faut autoriser l'installation d'applications de sources inconnues).
Si Lumi reste muette : Paramètres Android → Synthèse vocale → installer la voix **française**.

## Où modifier quoi

| Je veux… | Fichier |
|---|---|
| Changer les phrases, les positions des objets, les délais d'aide | `PorteDes5Gemmes/Assets/_Game/Resources/Tableaux/foret.json` |
| Retoucher un dessin | les PNG de `PorteDes5Gemmes/Assets/_Game/Resources/Art/` (Aseprite, LibreSprite, Piskel…) |
| Redessiner tout le pixel art par le code | `python3 tools/generer_pixel_art.py` |
| Changer le déroulement d'un tableau | `PorteDes5Gemmes/Assets/_Game/Scripts/Jeu/Plateau.cs` |
| Changer la voix (Android) | `PorteDes5Gemmes/Assets/_Game/Scripts/Audio/` |

Les coordonnées du JSON sont celles du jeu web (scène de 1600 × 900). Le jeu est dessiné en 320 × 180 pixels
puis agrandi sans flou (caméra « Pixel Perfect »).

## Tester sans téléphone

Le jeu peut se jouer tout seul et prendre des captures d'écran :

```bash
PorteDes5Gemmes/Builds/Linux/PorteDes5Gemmes.x86_64 -jeu-auto -captures ./captures
```

## Prochaines étapes possibles

- Tableaux 2 à 5 (pont du troll, porte du château, forge du nain, lac de la sirène), prologue et épilogue.
- Voix enregistrées (la tienne pour Lumi, un « Youpi ! » de chaque enfant) à la place de la synthèse vocale.
- Sauvegarde des gemmes gagnées.
