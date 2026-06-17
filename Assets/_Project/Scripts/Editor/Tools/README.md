# LOD Manager

Outil Editor qui génère automatiquement des **LODGroups** sur les meshes statiques de la scène
ou des prefabs. Le but : afficher un mesh plus léger quand l'objet est loin de la caméra (prend
peu de place à l'écran), et le faire disparaître quand il devient minuscule. Moins de triangles
rendus = plus de FPS, sans toucher au mesh d'origine.

Menu : **Tools > GlimmerOfHope > LOD Manager**

---

## 🚀 Démarrage rapide

1. Ouvrir **Tools > GlimmerOfHope > LOD Manager**
2. Cliquer **Scan Active Scene** (toute la scène) ou sélectionner des objets puis **Scan Selection**
3. Lire le tableau : chaque ligne propose une stratégie (voir plus bas). On peut la changer
   manuellement dans la colonne **Stratégie**, ou décocher une ligne pour l'ignorer.
4. Cliquer **Appliquer** → confirmer la boîte de dialogue.
5. L'outil génère les meshes décimés, construit les LODGroups et re-scanne.

Pour vérifier le gain : fenêtre **Game** → bouton **Stats** (triangles affichés), ou
**Window > Analysis > Frame Debugger**. Déplacer la caméra et regarder le nombre de tris chuter
quand les objets s'éloignent.

---

## 🧠 Comment ça marche

Le traitement passe par 5 étages, un fichier par responsabilité :

| Fichier | Rôle |
|---------|------|
| `LODSettings.cs` | Tous les seuils et constantes. C'est le seul fichier à toucher pour régler l'outil. Contient aussi l'enum `LODStrategy` et la fiche `LODCandidate`. |
| `LODClassifier.cs` | Scanne les renderers, compte les triangles / la taille / le nombre d'instances, et recommande une stratégie. |
| `LODMeshGenerator.cs` | Décime un mesh avec UnityMeshSimplifier et sauvegarde le résultat en asset (mis en cache). |
| `LODGroupBuilder.cs` | Construit les 3 niveaux (enfants `_LOD0/_LOD1/_LOD2`) et configure le composant `LODGroup`. |
| `LODApplier.cs` | Applique le build au bon endroit : objet de scène, instance de prefab, ou asset model. |
| `LODManagerWindow.cs` | La fenêtre Editor (scan, tableau, bouton Appliquer). |

Le flux : **Window** déclenche `Classifier.Scan()` → l'utilisateur valide → `Applier.Apply()`
appelle `GroupBuilder.Build()` qui demande ses meshes décimés à `MeshGenerator`.

### Les 3 niveaux de détail

- **LOD0** = le mesh d'origine, 100 % des triangles (objet proche)
- **LOD1** = mesh simplifié à **50 %** des triangles (`LOD1_QUALITY = 0.5`)
- **LOD2** = mesh simplifié à **20 %** des triangles (`LOD2_QUALITY = 0.2`)

Le mesh racine est retiré et remplacé par 3 GameObjects enfants, un par niveau. Le `LODGroup`
choisit lequel afficher selon la **hauteur de l'objet à l'écran** (screen-relative height) et
gère un fondu (`CrossFade`) entre les niveaux.

---

## 🎯 Les 3 stratégies

Chaque objet reçoit une stratégie automatique, modifiable à la main avant d'appliquer.

### `Skip`
On ne fait rien. Un objet est ignoré si :
- c'est un mesh **skinné** (personnage animé) — risque de casser le skinning
- il est sur le layer **UI** ou porte un `RectTransform`
- il a moins de **200 triangles** (`MIN_TRIANGLES`) — rien à gagner
- sa taille (diagonale des bounds) est sous **0.5** (`MIN_BOUNDS`) — trop petit
- aucun autre critère ne déclenche un gain

### `LODGroup` (avec cull)
Trois niveaux + l'objet **disparaît** quand il devient minuscule à l'écran (sous 8 %).
Réservé à la déco répétée, où chaque objet économisé compte. Déclenché si :
- l'objet est instancié **8 fois ou plus** (`MASS_INSTANCE_COUNT`), ou
- son nom contient un mot-clé déco : `grass`, `fleur`, `herbe`, `buisson`, `flower`, `arbre`,
  `bush`, `tree`, `plant`

Seuils de transition : LOD0 jusqu'à 50 %, LOD1 jusqu'à 25 %, LOD2 jusqu'à 8 %, puis culling.

### `AlwaysVisible` (sans cull)
Trois niveaux mais l'objet ne disparaît **jamais** (le LOD2 va jusqu'à 0 %). Pour les gros
éléments toujours visibles (sol, murs, décor de fond). Déclenché si :
- sa taille dépasse **12** (`LARGE_BOUNDS`), ou
- son nom contient un mot-clé structure : `ground`, `road`, `wall`, `platform`, `mountain`,
  `word`, `floor`, `terrain`, ou
- il dépasse **1500 triangles** (`ALWAYS_VISIBLE_TRIANGLES`)

Seuils de transition : LOD0 jusqu'à 50 %, LOD1 jusqu'à 20 %, LOD2 ensuite (jamais culling).

---

## ⚙️ Réglages (`LODSettings.cs`)

Tout se règle ici, sans toucher au reste du code.

| Constante | Valeur | Effet |
|-----------|--------|-------|
| `MIN_TRIANGLES` | 200 | En dessous → Skip |
| `ALWAYS_VISIBLE_TRIANGLES` | 1500 | Au dessus → AlwaysVisible |
| `MIN_BOUNDS` | 0.5 | Plus petit → Skip |
| `LARGE_BOUNDS` | 12 | Plus grand → AlwaysVisible |
| `MASS_INSTANCE_COUNT` | 8 | Nb d'instances pour passer en déco mass |
| `LOD1_QUALITY` | 0.5 | % de triangles gardés au LOD1 |
| `LOD2_QUALITY` | 0.2 | % de triangles gardés au LOD2 |
| `CULL_LOD0/1/2` | 0.5 / 0.25 / 0.08 | Seuils de transition (stratégie LODGroup) |
| `VISIBLE_LOD0/1/2` | 0.5 / 0.2 / 0 | Seuils de transition (stratégie AlwaysVisible) |
| `MASS_KEYWORDS` | grass, fleur… | Noms qui forcent la déco mass |
| `STRUCTURE_KEYWORDS` | ground, wall… | Noms qui forcent AlwaysVisible |

Pour tuner : baisser un `LODx_QUALITY` agresse plus la simplification (plus de FPS, moins de
détail). Monter un seuil `CULL_LODx` fait basculer/disparaître les objets plus tôt.

---

## 📦 Ce qui est généré

- **Meshes décimés** : sauvegardés en `.asset` dans un dossier `LOD_Generated/` créé à côté du
  mesh source, nommés `<mesh>_LOD<niveau>_q<qualité>.asset`. Ils sont mis en cache : un mesh déjà
  généré est réutilisé, pas recalculé.
- **Enfants** : 3 GameObjects `<objet>_LOD0/_LOD1/_LOD2` sous l'objet traité, chacun avec son
  `MeshFilter` + `MeshRenderer`. Les matériaux, ombres et layer de l'objet d'origine sont recopiés.
- **Composant** : un `LODGroup` sur l'objet racine (le `MeshRenderer`/`MeshFilter` d'origine est
  retiré, l'affichage passe par les enfants).

---

## ✅ Prérequis

- **Read/Write Enabled** sur le mesh. La simplification a besoin de lire la géométrie. Si la case
  est décochée, la ligne affiche `Read/Write requis` et est ignorée.
  Pour l'activer : sélectionner le model dans le Project → Inspector, onglet **Model** →
  cocher **Read/Write** → **Apply**.
- **UnityMeshSimplifier** (déjà installé, via `Packages/manifest.json`).

---

## ⚠️ Limites et pièges

- **Une seule passe par objet.** Après application, l'objet racine n'a plus de renderer (remplacé
  par les enfants). Re-scanner puis ré-appliquer traiterait les enfants `_LOD0/...` et imbriquerait
  les LOD. Pour refaire un objet : annuler (Ctrl+Z) ou supprimer à la main les enfants + le
  `LODGroup` et restaurer le mesh racine avant de relancer.
- **Édition de prefab non annulable.** Avec l'option **Éditer le prefab source** cochée, l'outil
  écrit directement dans l'asset prefab (`SaveAsPrefabAsset`) : Ctrl+Z ne revient pas dessus.
  Commit ou backup avant. La boîte de confirmation le rappelle.
- **Objets de scène uniquement annulables.** En scène (option décochée), le build supporte l'Undo.
- **Personnages exclus.** Les meshes skinnés sont volontairement en Skip.

---

## 🎤 Pour l'expliquer

Le principe en une phrase : *on affiche moins de triangles pour les objets loin de la caméra, et
on les cache complètement quand ils sont trop petits pour qu'on les voie.*

Trois choix à justifier :
1. **Pourquoi 3 niveaux** : compromis classique détail / mémoire. 0 = proche, 1 = moyen, 2 = loin.
2. **Pourquoi deux stratégies** : la déco répétée peut disparaître quand elle est minuscule (gros
   gain), mais le décor structurel doit rester visible (sinon trous dans le niveau).
3. **Pourquoi automatique** : le classifier décide à partir de critères mesurables (triangles,
   taille, nombre d'instances, nom) plutôt qu'à la main objet par objet — mais tout reste
   modifiable dans le tableau avant d'appliquer.
