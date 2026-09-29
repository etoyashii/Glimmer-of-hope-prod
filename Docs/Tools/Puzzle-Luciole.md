# Puzzle Luciole

Le joueur s'approche d'un groupe de lucioles. Le groupe se réveille et se met à tourner autour
de lui. Quand le joueur l'amène près d'une lanterne cassée, les lucioles plongent dedans, la
lanterne se répare et le puzzle est résolu.

`Tools > GlimmerOfHope > Puzzle Luciole Setup`

Code : `Assets/_Project/Scripts/Gameplay/AI/Firefly/`, `Gameplay/PuzzleSystem/LanternPuzzleElement.cs`,
`Editor/Tools/PuzzleLuciole*.cs`

---

## 1. Les pièces

| Composant | Posé sur | Rôle |
|---|---|---|
| `FireflyAI` | `Luciole` | La machine à états : repos, orbite autour du joueur, entrée dans la lanterne, disparition |
| `FireflySwarm` | `Luciole` | Le visuel : remplace la sphère par un petit nuage de points lumineux |
| `LanternPuzzleElement` | `Lanterne_Casser` | Écoute sa luciole, passe l'élément en résolu, montre la lanterne saine |
| `PuzzleManager` | le parent | Le puzzle lui-même, avec ses events `OnPuzzleSolved` / `OnPuzzleReset` |

Une luciole va dans **une seule** lanterne. Le groupe de points est purement visuel : pour le
code, c'est une seule luciole.

La détection se fait **à la distance**, pas par trigger. `Luciole` et `Lanterne_Casser` n'ont
pas de Rigidbody, donc `OnTriggerEnter` ne peut pas se déclencher entre les deux.

---

## 2. Créer un puzzle luciole

1. Créer un GameObject vide qui sera le puzzle (par exemple `Puzzle5`).
2. Glisser dedans, **en enfants directs** :
   - `Prefabs/LDForest/Luciole.prefab`
   - `Prefabs/LDForest/Lanterne_Casser.prefab`
3. Les placer où il faut. La luciole flotte sur place tant que le joueur ne s'approche pas.
4. Ouvrir l'outil, **Scanner la scène**, cocher l'emplacement, **Appliquer**.
5. `Ctrl+S`.

C'est tout. L'outil reconnaît un emplacement à cette seule règle : un objet qui a à la fois un
enfant dont le nom commence par `Luciole` et un enfant dont le nom commence par `Lanterne_Casser`.

---

## 3. L'outil

**Hors Play**

- **Scanner la scène** : liste les emplacements trouvés et leur état (`a cabler`,
  `partiellement cable`, `deja cable`). Ne modifie rien.
- **Appliquer** : sur chaque emplacement coché, pose les 4 composants, relie les références
  entre elles, donne un `_puzzleId` unique, crée une `Lanterne_Reparee` cachée (instance de
  `Lanterne.prefab`) et assigne le matériau `Art/Materials/Puzzle/M_FireflyGlow.mat`.
  Tout part dans un seul `Ctrl+Z`.
- **Câbler GrayZone.Repaint()** : si une `GrayZone` se trouve sous l'emplacement, ajoute son
  `Repaint()` dans `OnPuzzleSolved`. S'il y en a plusieurs, seule la première est branchée.
- **Remettre le réglage taille monde** : réapplique les valeurs de référence (tableau
  ci-dessous) sur toutes les lucioles trouvées. Utile si quelqu'un a déréglé une luciole.

**En Play**

- **Scanner la scène** d'abord : Unity recharge la scène au lancement.
- **TP Luciole** : pose le joueur à 1 m de la luciole, donc dans son rayon de réveil.
- **TP Lanterne** : pose le joueur à 0,6 m de la lanterne.
- **TP le joueur près de l'objet sélectionné** : marche avec n'importe quoi, par exemple
  `Puzzle2`.

Tout ce qui est fait en Play est annulé à l'arrêt, la scène sur disque n'est pas touchée.

---

## 4. Les réglages

Valeurs de référence, calées sur la taille actuelle du monde :

| Réglage | Valeur | Effet |
|---|---|---|
| Échelle de `Luciole` | 0.15 | Taille du groupe (le nuage de points suit l'échelle) |
| Wake Radius | 1.5 | Distance joueur / luciole qui réveille le groupe |
| Orbit Radius | 0.25 | Rayon de l'orbite autour du joueur |
| Orbit Speed | 75 | Degrés par seconde, négatif pour tourner dans l'autre sens |
| Orbit Height Offset | 0 | Décalage vertical depuis le milieu du corps du joueur |
| Orbit Wave Height | 0.2 | Hauteur de la vague sur l'orbite, 0 pour un cercle plat |
| Orbit Wave Count | 3 | Nombre de bosses par tour |
| Lantern Radius | 1 | Distance luciole / lanterne qui fait quitter le joueur |
| Arrive Threshold | 0.05 | Précision de l'arrivée dans la lanterne |
| Entry Offset | 0 | Décalage du point d'entrée depuis le centre de la lanterne |
| Spread / Dot Size (`FireflySwarm`) | 0.5 / 0.07 | Étalement du nuage et taille d'un point |

L'orbite est centrée sur le **milieu du corps** du joueur (centre de son collider) et la
luciole vise le **centre** de la lanterne (centre de son collider), pas leurs pivots.

---

## 5. Brancher les conséquences

Tout passe par des UnityEvents, rien à coder :

| Event | Sur | Quand |
|---|---|---|
| `OnAwakened` | `FireflyAI` | Le groupe se réveille (son, VFX) |
| `OnAbsorbed` | `FireflyAI` | Le groupe entre dans la lanterne |
| `OnRepaired` | `LanternPuzzleElement` | La lanterne vient d'être réparée |
| `OnPuzzleSolved` | `PuzzleManager` | Le puzzle est résolu (GrayZone, porte, dialogue...) |
| `OnPuzzleReset` | `PuzzleManager` | Le puzzle est remis à zéro |

Dans `OnPuzzleSolved`, l'ordre compte : un `SetActive(true)` doit passer **avant** tout appel
sur l'objet qu'il active.

La bascule lanterne cassée / lanterne saine est déjà faite par `LanternPuzzleElement` : il
cache les renderers de `Lanterne_Casser` (LOD compris) et active `Lanterne_Reparee`. Le reset
remet tout comme au départ, luciole comprise.

---

## 6. Lire les gizmos

Sélectionner la luciole :

- **jaune** : le rayon de réveil ;
- **orange** : l'orbite, avec sa vague (autour du joueur en Play) ;
- **magenta** : le rayon d'approche de la lanterne ;
- **ligne cyan** : le trajet vers la lanterne. La luciole vole en ligne droite, **sans
  évitement d'obstacle** : si la ligne traverse un rocher, elle le traversera aussi.
- **cube rouge** : luciole sans lanterne assignée, elle se désactivera au lancement.

Le `PuzzleManager` affiche son pourcentage puis `SOLVED` au-dessus de lui.

---

## 7. Limites connues

- **Pas de persistance en build.** Aucun bootstrapper n'est actif dans la scène du build, donc
  le `ServiceLocator` est vide et rien n'est sauvegardé. Dans l'éditeur, le `DevBootstrapper`
  masque le problème. Ça dépasse ce puzzle.
- **La lanterne saine** est posée à la position et à la rotation de la cassée, mais garde
  l'échelle de `Lanterne.prefab`. À ajuster à la main si les deux modèles ne se superposent pas.
- **`PortalZone2`** contient trois GrayZone, l'outil a branché la première. À valider avec le LD.
- **`Zone4`, `PortalZone`, `PortalZone2`** : la désactivation du `BoxCollider` bloquant et
  l'effet `PostProcessEffects` ne sont pas branchés. À ajouter dans `OnPuzzleSolved` selon ce
  que veut le LD.
