# ADR-012 — Outillage Git obligatoire pour Unity

## Statut

Accepté.

## Contexte

Un dépôt Unity n'est pas un dépôt de code ordinaire. Il contient des fichiers texte volumineux, générés par un éditeur graphique, dont la sémantique n'est **pas** ligne-à-ligne — et un système d'identité (les GUID) que Git ne connaît pas et ne protège pas.

Trois faits mesurés au dépôt :

**1. `unityyamlmerge` est déclaré dans `.gitattributes` — et configuré sur aucun poste.**

Le fichier `.gitattributes` désigne un driver de merge nommé. Mais un driver **nommé** dans `.gitattributes` n'est pas un driver **installé** : Git cherche sa définition dans la configuration locale de chaque machine (`merge.unityyamlmerge.driver`). Cette configuration n'existe nulle part dans l'équipe. Résultat : Git **ignore silencieusement** la déclaration et retombe sur son merge textuel par défaut.

L'équipe croit donc être protégée. Elle ne l'est pas. C'est la pire des situations : une garantie affichée, absente à l'exécution.

**2. Conséquence directe et constatée : un `.meta` a été commité avec des marqueurs de conflit Git et deux GUID.**

Le merge textuel s'est appliqué à un `.meta`. Git a produit un fichier contenant ses marqueurs (`<<<<<<<`, `=======`, `>>>>>>>`) et **deux `guid:` concurrents**. Ce fichier a été commité.

Ce qui se passe ensuite est le pire scénario possible dans Unity : le `.meta` n'est plus du YAML valide, Unity ne peut pas le lire, il considère l'asset comme **dépourvu de meta** et lui **régénère un GUID neuf** — un GUID **différent sur chaque poste**. À partir de là, chaque développeur a un identifiant distinct pour le même asset. Chaque scène, chaque prefab qui référence cet asset le référence par un GUID qui n'existe que chez une personne. Les références se cassent en cascade, chez tout le monde, et le symptôme (« ça marche chez moi », des `Missing (Mesh)` qui apparaissent et disparaissent selon qui a pull) est presque impossible à diagnostiquer sans connaître ce mécanisme.

**3. Les scènes pèsent jusqu'à 85 Mo de YAML texte** (`LD-Forest.unity` ; `BrushTest.unity` 40 Mo — ADR-011).

Sur des fichiers de cette taille, un merge textuel n'est pas seulement risqué : il est ingérable. Personne ne relit 85 Mo de YAML pour valider une résolution de conflit. En pratique, la résolution se fait en écrasant — et quelqu'un perd son travail.

### La mécanique qu'il faut comprendre : le `.meta` **est** l'identité de l'asset

C'est le point que l'ADR entier sert à protéger, et qu'une équipe majoritairement junior n'a aucune raison de connaître spontanément.

Unity ne référence **jamais** un asset par son chemin. Il le référence par un **GUID**, un identifiant stocké dans le fichier `.meta` qui accompagne chaque asset. Une scène qui utilise `SM_ZF_Foliage_Fern.fbx` ne contient pas la chaîne `"SM_ZF_Foliage_Fern.fbx"` : elle contient le GUID lu dans `SM_ZF_Foliage_Fern.fbx.meta`.

Cela a deux conséquences symétriques, et il faut les tenir toutes les deux :

- **Déplacer ou renommer un asset ne casse rien** — tant que le `.meta` le suit. Le GUID est inchangé, donc toutes les références sont inchangées. C'est ce qui rend le grand rangement de l'ADR-007 et les renommages de l'ADR-008 **possibles sans risque**.
- **Perdre, corrompre ou dissocier un `.meta` casse tout** — irréversiblement, et silencieusement. Unity régénère un GUID neuf, les références deviennent `Missing`, et il n'existe aucun moyen automatique de retrouver l'ancien.

Autrement dit : le `.meta` porte plus d'information que l'asset lui-même. Un `.fbx` perdu peut être ré-exporté. Un `.meta` perdu casse toutes les scènes qui pointaient dessus.

## Décision

### 1. `unityyamlmerge` est **obligatoire sur chaque poste**

Aucun membre de l'équipe ne pousse sur ce dépôt sans avoir configuré le driver de merge. C'est un prérequis d'onboarding, au même titre que cloner le dépôt.

**Configuration (une fois par machine) :**

```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver \
  '"<UnityInstallPath>/Editor/Data/Tools/UnityYAMLMerge" merge -p %O %B %A %A'
git config --global merge.unityyamlmerge.recursive binary
```

Le chemin de `UnityYAMLMerge` dépend de l'installation locale (il est fourni **avec Unity**, il n'y a rien à télécharger).

**Vérification :**

```bash
git config --get merge.unityyamlmerge.driver
```

Une sortie vide signifie que le driver n'est **pas** configuré, et donc que `.gitattributes` ment. C'est l'état actuel de tous les postes.

Le `.gitattributes` (déjà en place) reste :

```
*.unity   merge=unityyamlmerge eol=lf
*.prefab  merge=unityyamlmerge eol=lf
*.asset   merge=unityyamlmerge eol=lf
*.mat     merge=unityyamlmerge eol=lf
*.anim    merge=unityyamlmerge eol=lf
*.controller merge=unityyamlmerge eol=lf
*.meta    merge=unityyamlmerge eol=lf
```

Une déclaration dans `.gitattributes` **sans** configuration locale n'a **aucun effet**. Les deux sont nécessaires, et c'est exactement ce que le projet avait oublié.

### 2. Force Text obligatoire : `m_SerializationMode: 2`

Dans `ProjectSettings/EditorSettings.asset` :

```yaml
m_SerializationMode: 2   # Force Text
```

Ce réglage est **versionné** et ne se change pas. Il est ce qui rend les scènes et prefabs lisibles, diffables et **mergeables**. Sans lui, `unityyamlmerge` n'a rien à merger et Git n'a rien à differ.

C'est aussi la contrepartie assumée du poids des scènes (ADR-011) : les 85 Mo de `LD-Forest.unity` sont le prix de la mergeabilité. On ne le paie pas pour le jeter en passant en binaire, ni en LFS.

### 3. Un `.meta` accompagne **toujours** son fichier

**Règles :**

- On déplace un asset **et** son `.meta`. Jamais l'un sans l'autre.
- On supprime un asset **et** son `.meta`. Jamais l'un sans l'autre.
- On renomme un asset **et** son `.meta`. Jamais l'un sans l'autre.
- On commite un asset **et** son `.meta`. **Un `.meta` orphelin, ou un asset sans `.meta`, ne passe pas en PR.**
- Chaque **dossier** a aussi son `.meta`. Il compte.

**La bonne pratique, et elle est simple : faire tous les déplacements et renommages depuis l'éditeur Unity** (fenêtre Project). Unity déplace le `.meta` avec le fichier, automatiquement, sans erreur possible.

Un déplacement depuis l'explorateur Windows, depuis un terminal, ou depuis un IDE qui ne connaît pas Unity, laisse le `.meta` derrière. Le GUID est perdu. Toutes les références cassent. C'est le mode d'échec n°1 sur un projet Unity en équipe, et il est **entièrement évitable**.

Cette règle est ce qui rend les chantiers de l'ADR-007 (grand rangement) et de l'ADR-008 (renommages) sûrs. Faits depuis Unity, ils ne cassent **rien**. Faits depuis l'explorateur, ils cassent **tout**.

Corollaire : `.gitignore` ne doit **jamais** ignorer un `.meta` d'un asset versionné. Rappel : `Plugins/FMOD` est référencé en dur par **6 règles** du `.gitignore`, et `FMODStudioSettings.asset` référence chaque script de plateforme **par GUID** — une règle d'ignore trop large dans cette zone décapiterait la résolution de FMOD. On ne touche pas à ces règles sans mesurer.

### 4. **Interdiction absolue de committer un fichier contenant des marqueurs de conflit**

`<<<<<<<`, `=======`, `>>>>>>>` : aucun de ces marqueurs n'a le droit d'entrer dans un commit. Aucun. Dans aucun fichier.

C'est une règle qui semble aller de soi. Elle n'allait pas de soi : **c'est arrivé, sur un `.meta`, et le résultat a été un asset avec deux GUID, illisible par Unity, et un GUID régénéré différemment sur chaque poste.**

**Application :**

- Un hook `pre-commit` rejette tout fichier indexé contenant un marqueur de conflit en début de ligne. C'est une vérification de quelques lignes, et elle aurait évité l'incident.
- La CI rejette la même chose sur la branche cible, en filet de sécurité.

**En cas de conflit sur un `.meta` :** on ne « résout » pas à la main en choisissant un GUID au hasard. On garde le GUID de la branche **cible** (celui qui est déjà partagé par l'équipe et référencé par les scènes déjà mergées), et on vérifie ensuite dans Unity qu'aucune référence n'est passée en `Missing`. Le mauvais choix de GUID casse les scènes des autres, pas les siennes — il ne se voit donc pas chez celui qui résout.

### 5. Hygiène de commit sur les gros fichiers

- Un déplacement massif d'assets (ADR-007) se fait dans un **commit dédié**, sans autre changement. Un diff de rangement mêlé à un diff de fonctionnalité est irrelisable, et une branche qui contient les deux est irrémédiable en cas de conflit.
- Une branche qui touche une scène lourde est **courte**. Chaque jour de divergence sur `LD-Forest.unity` (85 Mo) augmente le risque d'un conflit que personne ne saura résoudre.
- On **pull avant** d'ouvrir une scène lourde, pas après l'avoir modifiée.

## Conséquences

**Positives**

- Les merges de scènes et de prefabs deviennent **sémantiques** : `unityyamlmerge` fusionne GameObject par GameObject, au lieu de recoller des lignes de YAML au hasard. C'est ce que `.gitattributes` promettait déjà, et qui n'a jamais eu lieu.
- La classe de bugs « `.meta` à deux GUID / GUID régénéré différemment par poste » disparaît. Elle était la plus coûteuse à diagnostiquer du projet, parce que son symptôme est un `Missing` intermittent qui dépend de qui a pull.
- Les chantiers de l'ADR-007 et de l'ADR-008 deviennent **exécutables sans risque** : le GUID survit au déplacement et au renommage, à condition que le `.meta` suive. Cet ADR est le prérequis technique des deux autres.
- Le hook pre-commit rend une catégorie entière d'erreurs **impossible**, plutôt que de compter sur la relecture — ce qui est le bon niveau d'exigence pour une équipe de ~25 personnes de niveaux hétérogènes.

**Négatives / coûts**

- Une étape d'onboarding de plus, par poste. Elle est incompressible : un driver de merge Git **est** une configuration locale, il n'existe aucun moyen de l'imposer depuis le dépôt. C'est précisément pour ça qu'elle a été oubliée jusqu'ici, et pourquoi elle doit être vérifiée explicitement, pas supposée.
- `unityyamlmerge` n'est pas magique : il échoue sur certains conflits structurels et rend la main. Il transforme une catastrophe silencieuse en un conflit visible — ce qui est déjà un immense progrès, mais ce n'est pas zéro travail.
- La règle « tout déplacement depuis Unity » ralentit les gros rangements (l'éditeur est plus lent qu'un `mv`). C'est le prix de ne pas casser 53 scènes.

**Application**

- **Onboarding** : `git config --get merge.unityyamlmerge.driver` doit renvoyer une valeur. Sortie vide = poste non configuré = interdiction de pousser.
- **Hook pre-commit** : marqueur de conflit dans un fichier indexé → commit refusé.
- **PR** : `.meta` orphelin, asset sans `.meta`, ou `m_SerializationMode` modifié → PR refusée.

## Alternatives écartées

**Continuer à déclarer `unityyamlmerge` dans `.gitattributes` sans l'installer.**
Écarté — c'est l'état actuel, et il est **pire que rien**. Une garantie affichée mais absente produit de la fausse confiance : l'équipe merge des scènes en croyant être protégée, Git fait un merge textuel, et le résultat est un `.meta` à deux GUID. Mieux vaudrait pas de `.gitattributes` du tout, au moins la prudence serait proportionnée au risque réel.

**Mettre scènes et prefabs en LFS pour éviter d'avoir à merger.**
Écarté, traité en détail dans l'ADR-011. LFS rend le fichier **non mergeable** : chaque conflit devient un « l'un ou l'autre » qui détruit le travail d'une des deux personnes. On échangerait un merge difficile contre une perte de données garantie.

**Passer en sérialisation binaire pour alléger les fichiers.**
Écarté. Plus compact, plus rapide à charger — et **non mergeable, non diffable**. On perdrait à la fois `unityyamlmerge` et la capacité de comprendre ce qu'un commit a changé. Force Text est non négociable.

**Un verrou de fichiers (LFS locking, ou une convention d'équipe « je préviens sur Discord avant de toucher la forêt »).**
Écarté comme **substitut** au merge — accepté comme complément ponctuel sur un chantier lourd (ADR-011, §4). Un verrou sérialise le travail de 25 personnes sur le contenu principal du jeu, il repose sur la mémoire de chacun, et il échoue silencieusement : on découvre le double travail au merge, quand il est trop tard. Le verrou n'est pas une alternative à un merge qui marche ; c'est ce qu'on fait quand le merge ne marche pas.

**Faire les déplacements de masse en ligne de commande (`git mv`), plus rapide que l'éditeur.**
Écarté. `git mv` déplace le fichier ; il ne sait rien du `.meta`, et rien ne rappellera de le déplacer aussi. Une seule omission sur un chantier de plusieurs centaines de fichiers suffit à régénérer un GUID et à casser des références dans des scènes de 85 Mo — c'est-à-dire dans les fichiers les plus coûteux à réparer du projet. L'éditeur est plus lent ; il ne se trompe pas.
