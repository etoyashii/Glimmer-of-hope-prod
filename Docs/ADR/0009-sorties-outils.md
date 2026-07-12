# ADR-009 — Assets générés par un outil

## Statut

Accepté.

## Contexte

Une partie du désordre du projet n'a été tapée par **personne**. Elle a été écrite par nos propres outils. C'est ce qui la rend particulièrement pernicieuse : aucune revue de code ne l'attrape, parce qu'aucun humain n'est passé par là.

**Preuve 1 — le repli silencieux sur la racine.**
Un outil interne ouvrait son panneau de sauvegarde sur un dossier qui **n'existait pas**. Unity, dans ce cas, ne lève pas d'erreur : il se replie sur `Assets/`. L'utilisateur clique « Save », voit un asset apparaître, et ne remarque rien. C'est la cause mécanique directe d'une bonne partie des **34 fichiers de la racine d'`Assets/`** : 18 terrains, 5 `TerrainData_<uuid>.asset`, `New Terrain 1.asset`, `GameObject.prefab`, `GameObject 1.prefab`. Le désordre n'était pas un choix ; c'était un chemin par défaut jamais vérifié.

**Preuve 2 — le générateur de LOD qui se mange lui-même.**
Un générateur de LOD écrivait sa sortie **à côté de sa source**, dans le dossier du mesh d'origine. Et il **relisait sa propre sortie** au passage suivant. Conséquence directe et observée : des **LOD de LOD** (le LOD1 devient la source d'un LOD1 de LOD1) et des **dossiers auto-imbriqués** qui se creusent à chaque exécution.

Ce bug tient en deux décisions, et chacune est réparable :

- La destination dépendait de la source. Donc la sortie **atterrissait dans l'espace source**, où le scan du passage suivant allait forcément la retrouver.
- Le générateur relisait l'espace où il écrivait. Donc sa sortie était indistinguable de son entrée.

Ce n'est pas un bug d'implémentation, c'est un bug de **topologie** : tant que sortie et entrée partagent le même espace, la récursion est une question de temps, pas de chance.

**Preuve 3 — les clés instables.**
Un générateur qui nomme sa sortie d'après le `name` de la source produit des collisions dès que deux sous-meshes d'un même `.fbx` portent le même nom — ce qui est courant (`Cube`, `default`, `Mesh`). Deux sorties se marchent dessus, la seconde écrase la première, et personne ne le sait. Le `name` n'est ni unique ni stable ; le GUID de l'asset et le localId de l'objet dans cet asset, eux, le sont.

**Preuve 4 — les outils cassent la nomenclature.**
Sur nos `[CreateAssetMenu]` : **11 ont un `fileName` contenant un espace** (`"Brush Asset"`, `"New Bool Event"`…), et **6 n'en ont aucun** — auquel cas Unity génère `New <Classe>`, donc un espace **garanti**. Une part des **63 fichiers à espace écrits par l'équipe** (`New Material.mat`, `New Scene.unity`, `Sans titre 3.mat`) provient de là. On ne peut pas demander à l'équipe de respecter l'ADR-008 pendant que les outils la violent à chaque clic.

## Décision

### 1. Un dossier canonique, unique, plat : `_Generated/`

Tout asset **produit par un outil** vit sous `Assets/_Generated/`, et **nulle part ailleurs**.

```
Assets/
└── _Generated/
    ├── LOD/
    ├── Terrain/
    ├── Atlases/
    └── Baked/
```

**Plat** : un seul niveau de sous-dossier, par **type de génération** — jamais une arborescence qui reflète celle de la source. Le jour où une source est déplacée (et l'ADR-007 en déplace beaucoup), aucune structure générée ne doit avoir à suivre.

**Hors de l'arbre source** : `_Generated/` n'est **pas** sous `_Project/`. C'est délibéré. `_Project/` est ce que l'équipe écrit et relit ; `_Generated/` est ce que la machine écrit. Les deux ne se mélangent pas, et un outil qui scanne `_Project/` ne rencontrera jamais sa propre sortie.

**Aucune édition manuelle.** Un asset de `_Generated/` est jetable par construction : on doit pouvoir supprimer le dossier entier et le régénérer. Toute retouche à la main y sera écrasée sans avertissement.

### 2. La destination d'un asset généré ne dépend **jamais** de sa source

Un générateur écrit dans `_Generated/<Type>/`, point. Le chemin de la source n'entre **pas** dans le calcul du chemin de sortie.

C'est la règle qui tue le bug du LOD à la racine : tant que la sortie atterrit « à côté » de la source, elle est dans l'espace que le scan suivant va parcourir. Sortir la sortie de l'espace source rend la boucle **structurellement impossible**, pas seulement improbable.

Corollaire : **un générateur ne crée jamais un dossier implicite**. Il crée sa destination explicitement (`Directory.CreateDirectory` + `AssetDatabase.Refresh`) avant d'écrire. Il ne fait **jamais** confiance au chemin par défaut d'un panneau de sauvegarde — c'est ce chemin par défaut, pointant sur un dossier inexistant, qui a rempli la racine d'`Assets/`.

Corollaire : **un générateur ne s'ouvre pas sur un dossier inexistant**. S'il propose une destination à l'utilisateur, il la crée d'abord, ou il refuse de s'exécuter. Le repli silencieux d'Unity sur `Assets/` doit être rendu inatteignable.

### 3. Un générateur ne relit **jamais** sa propre sortie

L'entrée d'un générateur est **explicitement** l'arbre source (`_Project/`). `_Generated/` est **exclu** de tout scan d'entrée, sans exception et sans option.

Test d'acceptation, à faire passer avant tout merge d'un outil : **exécuter le générateur trois fois de suite, sans rien changer d'autre.** Le second et le troisième passage doivent produire un `git status` **vide** (idempotence) et un nombre d'assets **identique**. Si le compte augmente, l'outil se relit ; il ne passe pas.

### 4. La clé d'un asset généré est stable et unique

Un asset généré est identifié par sa source. Cette identité est :

**`<GUID de l'asset source>_<localId de l'objet dans cet asset>`**

Jamais le `name`.

- Le **`name`** collisionne : deux sous-meshes homonymes d'un même `.fbx` (`Cube`, `default`) produisent la même clé, donc l'un écrase l'autre — silencieusement.
- Le **chemin** n'est pas stable : l'ADR-007 va déplacer beaucoup de fichiers, et l'ADR-008 va en renommer.
- Le **GUID** est stable au déplacement **et** au renommage (c'est exactement ce que le `.meta` garantit — ADR-012). Le **localId** discrimine les objets à l'intérieur d'un même asset.

Un générateur qui trouve une sortie existante pour une clé la **remplace** (même GUID de sortie conservé, donc les références des scènes survivent). Il n'en crée pas une deuxième à côté. C'est ce qui empêche les `(N)` — on en compte **8** au dépôt aujourd'hui, dont `pack_fleurs_lowpoly (11).prefab`.

Le nom **lisible** de la sortie reste conforme à l'ADR-008 (`SM_Foliage_FernLarge_LOD1`). La clé, elle, vit dans un manifeste (`_Generated/<Type>/manifest.json`) qui associe `GUID_localId` → chemin de sortie. Le nom sert à l'humain, la clé sert à la machine — on ne demande pas au nom d'assurer les deux.

### 5. Tout `[CreateAssetMenu]` a un `fileName` conforme à l'ADR-008

Aucun `fileName` avec espace. Aucun `[CreateAssetMenu]` **sans** `fileName` — l'omission fait générer `New <Classe>` par Unity, avec un espace garanti, et c'est une des sources documentées de nos 63 fichiers à espaces.

Chantier immédiat : les **11** `fileName` à espace sont corrigés, les **6** manquants sont ajoutés.

```csharp
// Interdit — espace dans le fileName
[CreateAssetMenu(fileName = "New Bool Event", menuName = "Glimmer/Events/Bool")]

// Interdit — pas de fileName : Unity génère "New BoolEventChannel"
[CreateAssetMenu(menuName = "Glimmer/Events/Bool")]

// Conforme
[CreateAssetMenu(fileName = "OnBoolChanged", menuName = "Glimmer/Events/Bool")]
```

Vérifiable en une ligne de CI : un `[CreateAssetMenu]` sans `fileName`, ou avec un espace dedans, bloque la PR.

### 6. Statut Git de `_Generated/`

`_Generated/` est **versionné** (avec ses `.meta`), et non ignoré, tant que les scènes et prefabs y référencent des assets par GUID : ignorer le dossier ferait apparaître des références cassées chez quiconque n'a pas relancé les outils. C'est un choix conscient, à réévaluer le jour où la génération tourne en CI. En attendant, la règle qui le rend supportable est l'**idempotence** (§3) : un dossier généré qui ne bouge pas quand on relance l'outil ne pollue pas l'historique.

## Conséquences

**Positives**

- La classe de bugs « LOD de LOD » et « dossiers auto-imbriqués » devient **impossible par construction**, pas seulement corrigée dans un outil.
- Le repli silencieux sur `Assets/` est rendu inatteignable : plus de terrain à la racine.
- La séparation source / généré rend le diff lisible : on sait, en regardant le chemin, si un changement a été **décidé** ou **produit**.
- Un asset généré peut être supprimé et régénéré sans peur — c'est ce que « jetable » veut dire, et c'est le seul état dans lequel un dossier de sortie est sain.
- La clé `GUID_localId` supprime les collisions de sous-meshes homonymes, qui écrasaient des sorties sans le dire.
- Corriger les 17 `[CreateAssetMenu]` supprime une source **automatique** de violations de l'ADR-008. Une règle que les outils respectent n'a plus besoin d'être rappelée en revue.

**Négatives / coûts**

- Migrer les générateurs existants (LOD, terrain, brush) demande de réécrire leur logique de chemin. Ce n'est pas une retouche : le calcul du chemin de sortie disparaît.
- Les assets générés déjà en place et référencés par des scènes doivent être **déplacés depuis Unity** (le `.meta` suit, donc le GUID survit, donc les scènes ne cassent pas — ADR-012). Un déplacement à la main dans l'explorateur casserait tout.
- Le manifeste (`GUID_localId` → chemin) est un artefact de plus à maintenir. Il est le prix d'une identité stable ; le `name` était gratuit, et il collisionnait.
- Corriger un `fileName` ne renomme **pas** les assets déjà créés avec l'ancien. Ceux-là relèvent du chantier de renommage de l'ADR-008.

## Alternatives écartées

**Laisser chaque générateur écrire à côté de sa source, et lui apprendre à ignorer ses propres sorties (par suffixe `_LOD1`, par exemple).**
Écarté. C'est la structure qui a produit le bug. Un filtre par suffixe est une rustine : il tient jusqu'à ce qu'un outil produise une sortie qui ne porte pas le suffixe attendu, ou qu'un humain renomme une sortie. La seule garantie robuste est **topologique** : la sortie n'est pas dans l'espace scanné.

**Ignorer `_Generated/` dans Git et regénérer localement.**
Écarté **pour l'instant**. Les scènes référencent les assets générés par GUID ; un dossier ignoré signifie des références cassées chez tout coéquipier qui n'a pas lancé l'outil — sur une équipe de ~25 personnes de niveaux hétérogènes, c'est un canal permanent de « ça marche chez moi ». Réévaluable le jour où la génération est garantie par la CI.

**Reproduire l'arborescence source dans `_Generated/` (`_Generated/LOD/Art/Models/Environment/…`).**
Écarté. Cela recrée la dépendance destination → source que le §2 supprime. Chaque déplacement de source (et l'ADR-007 en déplace beaucoup) obligerait à re-déplacer la sortie, ou laisserait des orphelins. Le manifeste porte le lien source → sortie ; le chemin n'a pas à le porter aussi.

**Nommer les sorties d'après le chemin de la source (chemin aplati en nom de fichier).**
Écarté. Le chemin n'est pas stable (ADR-007 déplace, ADR-008 renomme), et l'aplatissement produit des noms interminables. Le GUID est précisément l'identifiant que Unity maintient stable à travers déplacements et renommages : c'est celui-là qu'il faut utiliser.

**Corriger les fichiers à la racine et faire confiance à la vigilance.**
Écarté. Les 34 fichiers de la racine ne sont pas un défaut de vigilance : ils sont la sortie normale d'un outil dont le chemin par défaut était faux. Tant que l'outil n'est pas corrigé, nettoyer la racine ne fait que remettre le compteur à zéro avant le prochain clic.
