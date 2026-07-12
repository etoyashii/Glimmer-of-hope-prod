# ADR-007 — Arborescence d'`Assets/`

## Statut

Accepté.

## Contexte

`Assets/` n'a jamais eu de racine normative. Le résultat est mesurable :

- **34 fichiers traînent directement à la racine d'`Assets/`** : 18 terrains (`Border1..10`, `Volcan`, `Forest`…), 5 `TerrainData_<uuid>.asset`, `GameObject.prefab`, `GameObject 1.prefab`, `New Terrain 1.asset`, `pack_fleurs_lowpoly (11).prefab`, et une texture nommée `d34a659c5dc947bd1cd36411d0618d926a1e94ac.png` — un nom de hash, illisible pour un humain.
- **20 des 53 scènes (38 %) sont rangées par prénom** : `Scenes/Proto/Erwan/` (12), `Romain/` (6), `Thibault/` (1), `Bastien/` (1).
- Un pack Asset Store (`pack_fleurs_lowpoly`) a déversé un prefab à la racine.

La cause n'est pas seulement la négligence : elle est **mécanique**. Un outil interne ouvrait son panneau de sauvegarde sur un dossier qui n'existait pas ; Unity, dans ce cas, se replie **silencieusement** sur `Assets/`. Personne ne voit l'erreur, personne n'est prévenu, et le fichier atterrit à la racine. Une règle sociale ne suffira donc pas : il faut à la fois une arborescence explicite et des outils qui écrivent au bon endroit (voir ADR-009).

Le rangement **par personne** est le second problème structurel. Il paraît inoffensif au moment où il est créé (« je mets mes tests dans mon dossier ») mais il produit trois effets : la propriété d'un asset se confond avec son emplacement, un départ d'équipe orphelinise un dossier entier, et l'arborescence encode l'organigramme au lieu du jeu. Sur une équipe de ~25 personnes majoritairement étudiante, cet organigramme change à chaque semestre — pas l'arborescence.

Enfin, Unity possède des dossiers dont le nom **est** un contrat avec le moteur. Les renommer ou les déplacer ne casse pas la compilation : ça casse le runtime, silencieusement.

## Décision

### 1. Nombre fixe de racines autorisées

`Assets/` contient **exactement six entrées**, et rien d'autre :

| Racine | Rôle |
|---|---|
| `_Project/` | Tout le contenu **produit par l'équipe**. Aucune exception. |
| `ThirdParty/` | Tout ce qui vient d'ailleurs : Asset Store, packs achetés, code externe copié. |
| `Plugins/` | Dossier magique Unity (natif, FMOD). Contrat moteur. |
| `Resources/` | Dossier magique Unity (chargement par nom). Contrat moteur. |
| `StreamingAssets/` | Dossier magique Unity (chemin réservé). Contrat moteur. |
| `Settings/` | Assets de configuration URP / pipeline que Unity et les templates attendent ici. |

**Aucun fichier n'est autorisé à la racine d'`Assets/`.** Zéro. Un `.asset`, un `.prefab`, un `.png` à la racine est un bug, pas un choix.

### 2. `_Project/` : par type d'abord, par domaine ensuite

Niveau 1 = **type d'asset** (six dossiers, liste fermée) : `Art`, `Scenes`, `Prefabs`, `Data`, `Scripts`, `Settings`.
Niveau 2 = **domaine** (Environment, Characters, UI, VFX, Player, Enemies…).

Le type d'abord, parce que c'est l'axe stable : un mesh reste un mesh quand le niveau change de nom. Le domaine ensuite, parce que c'est l'axe qui bouge.

### 3. Interdits explicites

- **Ranger par personne.** Aucun dossier `Erwan/`, `Romain/`, `Thibault/`, `Bastien/`, ni aucun autre prénom, nulle part. Le nom d'un dossier décrit un **contenu**, jamais un auteur. Git connaît déjà l'auteur.
- **Un pack Asset Store hors de `ThirdParty/`.** Un pack importé s'installe intégralement sous `ThirdParty/<Vendor>_<Pack>/`, et on ne modifie pas son contenu (une mise à jour du pack écraserait la modification). Si un asset tiers doit être adapté, on en copie une **variante** sous `_Project/`.
- **Un dossier `Editor/` ailleurs que `_Project/Scripts/Editor/`.** `Editor/` est un nom magique : Unity exclut son contenu du build. Éparpillé, il devient invisible et un script d'édition finit par se retrouver dans une assembly runtime — ou pire, dans le build.
- **Tout fichier à la racine d'`Assets/`** (répété ici parce que c'est la violation la plus fréquente : 34 occurrences).

### 4. Dossiers sanctuarisés — ne pas déplacer, ne pas renommer

Ces trois dossiers ne sont **pas** soumis à la règle « tout sous `_Project/` ». Ils restent à la racine, tels quels :

- **`StreamingAssets/`** — le chemin est réservé par Unity ; les fichiers y sont copiés verbatim dans le build et adressés par chemin relatif. Le déplacer casse chaque lecture.
- **`Resources/`** — DOTween y charge son asset de configuration **par nom**, à l'exécution. Il n'existe aucune référence de projet à suivre : si le dossier bouge, la résolution échoue au runtime, pas à la compilation. (Règle associée : on n'ajoute **rien de nouveau** dans `Resources/` ; tout ce qui y entre est chargé en mémoire au démarrage et gonfle le build — critique sur Android et WebGL.)
- **`Plugins/FMOD`** — référencé en dur par **6 règles du `.gitignore`**, et `FMODStudioSettings.asset` référence chaque script de plateforme **par GUID**. Un déplacement casse simultanément l'ignore et la résolution des scripts par le settings asset.

Toucher à ces trois dossiers demande un ADR dédié, pas une PR.

### 5. Arborescence cible

```
Assets/
├── _Project/                     # TOUT le contenu maison. Rien d'autre.
│   ├── Art/
│   │   ├── Models/               # .fbx sources
│   │   │   ├── Environment/
│   │   │   ├── Characters/
│   │   │   └── Props/
│   │   ├── Textures/
│   │   │   ├── Environment/
│   │   │   ├── Characters/
│   │   │   └── UI/
│   │   ├── Materials/
│   │   │   ├── Environment/
│   │   │   ├── Characters/
│   │   │   └── UI/
│   │   ├── Shaders/              # Shader Graph + .shader
│   │   ├── VFX/
│   │   ├── Animations/
│   │   │   ├── Player/
│   │   │   └── Enemies/
│   │   ├── Audio/                # events/banks FMOD exclus (voir Plugins/FMOD)
│   │   └── Fonts/
│   ├── Scenes/                   # voir ADR-010
│   │   ├── Core/                 # _Bootstrap, MainMenu, Gameplay
│   │   ├── Levels/               # niveaux jouables
│   │   └── Sandbox/              # bacs à sable, prototypes — hors build
│   ├── Prefabs/
│   │   ├── Environment/
│   │   ├── Characters/
│   │   ├── UI/
│   │   ├── VFX/
│   │   └── Systems/              # managers, services, prefabs de bootstrap
│   ├── Data/                     # ScriptableObjects
│   │   ├── EventChannels/
│   │   ├── Spells/
│   │   ├── Levels/
│   │   └── Config/
│   ├── Scripts/
│   │   ├── Core/                 # asmdef GlimmerOfHope.Core
│   │   ├── Gameplay/             # asmdef GlimmerOfHope.Gameplay
│   │   ├── UI/                   # asmdef GlimmerOfHope.UI
│   │   ├── Editor/               # asmdef GlimmerOfHope.Editor — SEUL Editor/ du projet
│   │   └── Examples/             # asmdef GlimmerOfHope.Examples
│   └── Settings/                 # presets d'import, configs de projet maison
│
├── _Generated/                   # sorties d'outils — voir ADR-009. Plat, jamais édité à la main.
│
├── ThirdParty/                   # tout ce qui n'est pas écrit par l'équipe
│   ├── DOTween/
│   ├── CustomInspector/          # asmdef CustomInspector.Attributes
│   └── <Vendor>_<Pack>/          # ex. Synty_PolygonNature, LowPoly_Flowers
│
├── Plugins/                      # SANCTUARISÉ
│   └── FMOD/                     # 6 règles .gitignore + GUIDs dans FMODStudioSettings.asset
│
├── Resources/                    # SANCTUARISÉ — DOTween charge son asset PAR NOM. N'y ajouter RIEN.
│
├── StreamingAssets/              # SANCTUARISÉ — chemin réservé Unity
│
└── Settings/                     # URP : Renderer Data, Quality, Volume Profiles
```

## Conséquences

**Positives**

- Un asset a **une** place possible. La question « où je le mets ? » a une réponse déterministe, ce qui compte quand l'équipe compte ~25 personnes de niveaux hétérogènes.
- Les 34 fichiers de la racine deviennent une liste de tâches finie, pas un bruit de fond.
- `ThirdParty/` isolé rend une mise à jour de pack sans risque : on sait exactement ce qui nous appartient.
- La couche `Scripts/` (Core ← Gameplay ← UI ← Editor) se lit dans l'arborescence, ce qui rend une violation de dépendance visible à l'œil avant même de l'être au compilateur.

**Négatives / coûts**

- La migration déplace beaucoup de fichiers. Un déplacement dans Unity est sûr (les références sont par GUID, pas par chemin), **à condition** de déplacer le `.meta` avec le fichier — voir ADR-012. Un déplacement fait depuis l'explorateur Windows sans le `.meta` casse toutes les références.
- Les 18 terrains de la racine sont référencés par des scènes lourdes ; leur déplacement doit se faire **depuis l'éditeur Unity**, projet fermé côté équipe, et être poussé seul, dans un commit dédié, sans autre changement.
- Le déplacement massif produit un diff illisible. C'est acceptable une fois ; il ne doit pas se reproduire.

**Application**

- Toute PR ajoutant un fichier à la racine d'`Assets/` est refusée sans discussion.
- Toute PR créant un dossier portant un prénom est refusée sans discussion.
- Les outils internes qui écrivaient à la racine sont corrigés en amont (ADR-009) — un outil qui sème n'est pas rattrapable par une revue humaine.

## Alternatives écartées

**Ranger par domaine d'abord, puis par type** (`Environment/Models/`, `Environment/Textures/`…)
Écarté. Séduisant sur le papier, mais un asset appartient souvent à plusieurs domaines (une texture de rocher sert à l'environnement **et** à un prop) et l'arbitrage devient subjectif — donc divergent, exactement le mécanisme qui a produit `Foliage`/`Folliage` (ADR-008). Le type d'un asset, lui, n'est jamais ambigu : un `.fbx` est un modèle.

**Ranger par feature (dossiers verticaux « Player/ », « Combat/ » contenant scripts + art + prefabs)**
Écarté. C'est un bon modèle pour du code pur, mais Unity impose des contraintes transverses par type : réglages d'import par dossier, dossiers magiques, frontières d'asmdef, atlasing de textures. Une organisation verticale les combat en permanence.

**Garder les dossiers par prénom pour les prototypes uniquement**
Écarté. C'est exactement l'état actuel — `Scenes/Proto/<Prénom>/` — et il a produit 38 % des scènes hors de tout classement. « Temporaire » n'a pas de mécanisme d'expiration. Les prototypes vont dans `Scenes/Sandbox/`, nommés par **sujet** (ADR-010) ; l'auteur est dans `git log`.

**Ne rien formaliser et compter sur la revue de code**
Écarté. Une partie du désordre est produite par les **outils** eux-mêmes (repli silencieux sur la racine, `fileName` sans nom), pas par des humains distraits. On ne relit pas ce qu'aucun humain n'a tapé.
