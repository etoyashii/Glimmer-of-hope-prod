# ADR-008 — Nomenclature des assets et des fichiers

## Statut

Accepté.

## Contexte

La section « Nomenclature » du guide d'équipe existant contient littéralement **« In coming… »**. La règle est exigée dans les revues, réclamée par les leads, invoquée en réunion — et elle n'a **jamais été écrite**. C'est le trou central du projet : tout le reste en découle.

Ce que produit une règle non écrite, mesuré sur le dépôt :

**La convention est suivie à 42 %.** 58 % des assets ne portent aucun préfixe. Distribution des préfixes existants :

| Préfixe | Nombre |
|---|---|
| `SM_` | 118 |
| `M_` | 43 |
| `TMP_` | 22 |
| `LV_` | 17 |
| `Mat_` | 14 |
| `SO_` | 10 |
| `BA_` | 10 |
| `SG_` | 8 |
| `T_` | **5** |
| `VFX_` | 1 |
| `SK_` | **0** |

Deux enseignements. D'abord, `SM_` et `M_` **sont** installés : 161 fichiers les portent, la bataille est gagnée, on ne la rejoue pas. Ensuite, `T_` (textures) est **déclaré mais non suivi** — 5 fichiers, sur un projet qui en compte des centaines — et `SK_` est à zéro. Une convention déclarée et non appliquée est pire que pas de convention : elle donne l'illusion d'un ordre, et elle rend les recherches par préfixe silencieusement fausses.

**Le préfixe `ZF` a débordé.** Les 118 `SM_` sont **tous** `SM_ZF_`. `ZF` n'est documenté nulle part — personne dans l'équipe actuelle ne sait ce qu'il signifie. Catégories observées :

| Catégorie | Nombre |
|---|---|
| `Foliage` | 46 |
| `TreeTrunk` | 25 |
| `Rock` | 12 |
| `TreeLeaves` | 9 |
| **`Folliage`** (faute) | **8** |
| `Archi` | 7 |
| `Tree` | 5 |
| **`Trees`** (doublon de `Tree`) | **3** |
| `Vehicle` | 1 |
| `Fauna` | 1 |
| `Buildings` | 1 |

Le débordement est flagrant : `SM_ZF_Vehicle_Robot.fbx` et `SM_ZF_Buildings_Incinerator.fbx` ne sont **pas** du contenu de forêt. `ZF` n'est plus un préfixe sémantique, c'est un préfixe **par défaut, vide de sens**, que l'on recopie parce que le fichier d'à côté l'avait.

**Le vocabulaire de catégories a divergé tout seul.** `Foliage` (46) et `Folliage` (8) coexistent. `Tree` (5) et `Trees` (3) coexistent. Personne n'a décidé ça. C'est le résultat mécanique d'un **vocabulaire ouvert** : quand chacun invente sa catégorie au moment de nommer, les variantes orthographiques et les singuliers/pluriels apparaissent, et ils ne disparaissent jamais. Une recherche sur `Foliage` rate 8 fichiers. Un filtre d'import basé sur la catégorie rate 8 fichiers. Silencieusement.

**Les espaces, accents et `(N)` cassent des choses réelles.**

- **98 fichiers contiennent un espace**, dont **63 écrits par l'équipe** : `New Material.mat`, `Sans titre 3.mat`, `GameObject 1.prefab`, `Spell Manager.cs`, `New Scene.unity`…
- **8 fichiers contiennent un `(N)`** — signature nette d'une duplication accidentelle (`pack_fleurs_lowpoly (11).prefab`).
- Des **accents dans des chemins** (`T_tree_chêne/`, `T_TrimSheet_Nénuphar.png`) **cassent déjà des scripts**, et il faut positionner `core.quotepath=false` pour que git accepte simplement de les **afficher** au lieu de les échapper en octets.

Un espace dans un nom de fichier est un piège permanent : dès qu'un chemin non quoté traverse un script shell, un `.bat`, un argument de build, un outil d'export ou une commande CI, il se coupe en deux. Un accent ajoute une dépendance à l'encodage du système de fichiers — et le projet vise Android, PC et WebGL, avec une chaîne d'outils qui traverse Windows, WSL et une CI Linux.

**Les outils sèment eux-mêmes le désordre.** Sur nos `[CreateAssetMenu]` : **11 déclarent un `fileName` contenant un espace** (`"Brush Asset"`, `"New Bool Event"`…) et **6 n'en déclarent aucun** — dans ce cas Unity génère `New <Classe>`, donc un espace **garanti**. Autrement dit, une partie des 63 espaces n'a pas été tapée par un humain : elle a été produite par notre propre code. Une règle qui ne corrige pas les outils ne tiendra pas.

## Décision

### Règles dures (non négociables)

1. **Anglais pour tout ce que la machine lit** : noms de fichiers, noms de dossiers, code, identifiants, catégories.
   **Français pour ce que l'humain lit** : documentation, ADR, messages de commit, commentaires, descriptions de PR.
   Rationnel : le code est déjà en anglais (Unity, C#, les packages), et un nom de fichier français attire mécaniquement les accents.

2. **Aucun espace** dans un nom de fichier ou de dossier. Jamais. On sépare par `_` (entre segments) et on capitalise en PascalCase (à l'intérieur d'un segment).

3. **Aucun accent, aucun caractère non-ASCII** dans un nom de fichier ou de dossier. `chêne` → `Oak`. `Nénuphar` → `WaterLily`.

4. **Aucun `(N)`**, aucun `Copy`, aucun ` 1`, ` 2`. Un `(N)` n'est pas un nom, c'est un accident de duplication. Le fichier est soit renommé correctement, soit supprimé.

5. **Aucun nom généré par défaut** : `New Material`, `New Scene`, `Sans titre 3`, `GameObject`, `New Terrain 1` sont interdits en l'état. Un asset sans nom est un asset sans intention.

6. **Aucun nom de hash** : `d34a659c5dc947bd1cd36411d0618d926a1e94ac.png` ne dit rien. Un nom sert à un humain ; la machine a déjà le GUID.

### Table normative par type d'asset

| Type | Préfixe | Schéma | Exemple |
|---|---|---|---|
| Static Mesh (`.fbx`, `.obj`) | `SM_` | `SM_<Catégorie>_<Nom>[_<Variante>]` | `SM_Foliage_FernLarge` |
| Skinned Mesh (rigged) | `SK_` | `SK_<Catégorie>_<Nom>` | `SK_Character_Hero` |
| Texture | `T_` | `T_<Nom>_<Canal>` | `T_RockCliff_BC`, `T_RockCliff_N` |
| Matériau | `M_` | `M_<Catégorie>_<Nom>` | `M_Foliage_FernLarge` |
| Shader Graph | `SG_` | `SG_<Nom>` | `SG_Water` |
| Prefab | `P_` | `P_<Catégorie>_<Nom>` | `P_Enemy_Slime` |
| Scène | `LV_` / `SC_` | voir ADR-010 | `LV_Forest_01` |
| ScriptableObject (donnée) | `SO_` | `SO_<Type>_<Nom>` | `SO_Spell_Fireball` |
| ScriptableObject (Event Channel) | — | `On<Événement>` (exception, ci-dessous) | `OnPlayerDeath` |
| VFX (VisualEffect / système de particules) | `VFX_` | `VFX_<Nom>` | `VFX_Explosion` |
| Animation Clip | `A_` | `A_<Acteur>_<Action>` | `A_Hero_Run` |
| Animator Controller | `AC_` | `AC_<Acteur>` | `AC_Hero` |
| Audio Bank (FMOD) | `BA_` | `BA_<Nom>` | `BA_Ambience` |
| Presets / Profils | `PR_` | `PR_<Nom>` | `PR_TextureImport_UI` |

**Canaux de texture** (suffixes normatifs, liste fermée) : `_BC` (base color), `_N` (normal), `_MRA` / `_ORM` (masks packés), `_E` (emissive), `_H` (height), `_AO` (ambient occlusion).

Ce sont ces suffixes qui rendent `T_` réellement utile : un pipeline d'import peut alors régler l'espace colorimétrique et la compression **par suffixe**, automatiquement. Aujourd'hui, avec 5 textures conformes sur des centaines, c'est impossible — chaque texture est réglée à la main, ou pas réglée du tout. C'est un coût direct sur Android et WebGL, où la taille des textures est la première contrainte de build.

### Le cas `ZF` : **gelé**, pas corrigé

Les 118 fichiers `SM_ZF_*` **restent tels quels**. Aucun renommage de masse.

Pourquoi ne pas les renommer : ils sont référencés par les scènes lourdes du projet (`LD-Forest.unity`, 85 Mo — ADR-011). Un renommage de masse produit un diff colossal sur des fichiers YAML déjà énormes, sur un dépôt où le driver de merge Unity n'est configuré sur **aucun poste** (ADR-012). Le risque de perdre du travail dépasse largement le bénéfice cosmétique. Les références Unity sont par GUID : un fichier mal nommé **fonctionne**. C'est de la dette lisible, pas de la casse.

Pourquoi on ne le propage plus : `ZF` ne veut rien dire, et il a débordé (`SM_ZF_Vehicle_Robot`, `SM_ZF_Buildings_Incinerator`). Chaque nouveau `SM_ZF_` aggrave une convention morte.

**Règle opérationnelle :**

- Tout **nouveau** static mesh se nomme `SM_<Catégorie>_<Nom>`, **sans `ZF`**.
- Un `SM_ZF_*` existant qui doit être touché pour une autre raison (retopo, ré-export, changement de pivot) **peut** être renommé à cette occasion, un fichier à la fois, dans un commit dédié.
- On ne « corrige » pas `Folliage` → `Foliage` sur les 8 fichiers existants pour la même raison. Le vocabulaire fermé ci-dessous empêche le 9e.

### Vocabulaire de catégories : **liste fermée**

Les catégories autorisées pour `SM_`, `SK_`, `M_`, `P_` sont **exactement** celles-ci :

```
Foliage      Tree        Rock        Ground      Water
Architecture Prop        Vehicle     Fauna       Character
Weapon       UI          VFX         Debug
```

**Singulier. Anglais. Pas d'ajout sans PR sur ce fichier.**

Pourquoi une liste **fermée** et pas une convention ouverte : parce qu'on a déjà l'expérience de la liste ouverte, et elle est chiffrée. `Foliage` 46 / `Folliage` 8. `Tree` 5 / `Trees` 3. Personne n'a voulu créer `Folliage` — il est né parce que rien ne l'a empêché. Une liste ouverte **redivergera**, avec certitude : c'est le mécanisme exact qui a produit les deux doublons observés. Une liste fermée transforme la question « comment j'appelle ça ? » (subjective, donc divergente) en « laquelle de ces 14 ? » (objective, donc convergente). Et le jour où une catégorie manque vraiment, l'ajouter coûte une PR d'une ligne — ce qui est le bon prix pour une décision qui engage tout le monde.

Correspondances avec l'existant, pour lever l'ambiguïté : `Archi` → `Architecture`, `Buildings` → `Architecture`, `Trees` → `Tree`, `Folliage` → `Foliage`, `TreeTrunk` et `TreeLeaves` → `Tree` (le détail passe dans `<Nom>` : `SM_Tree_OakTrunk`, `SM_Tree_OakLeaves`).

### Exception documentée : les EventChannels en `On<Événement>`

Les ScriptableObject Event Channels ne portent **pas** de préfixe `SO_`. Ils se nomment `On<Événement>` : `OnPlayerDeath`, `OnLevelLoaded`, `OnBoolChanged`.

C'est une exception, et elle est **assumée** : la population est **100 % cohérente**. Aucun EventChannel ne dévie. On documente donc l'usage réel plutôt que de le corriger — renommer une convention que tout le monde respecte déjà pour la faire rentrer dans une case est un coût pur, sans bénéfice.

Elle se justifie en plus : `On<X>` se lit comme la souscription à laquelle il correspond (`OnPlayerDeath.Raise()` / `OnPlayerDeath.Register(...)`), ce qui est précisément ce que les Event Channels servent à rendre lisible pour une équipe junior — le motif a été choisi parce que les events statiques faisaient oublier les désabonnements. Un nom qui se lit comme son usage renforce ce choix.

Corollaire : les EventChannels vivent dans `_Project/Data/EventChannels/`, et **seulement** là. Le préfixe est remplacé par le dossier.

### Les outils doivent obéir à cet ADR

**Tout `[CreateAssetMenu]` déclare un `fileName` conforme à cette table.** Pas d'espace, pas de `New `.

- `"Brush Asset"` → `"SO_Brush_New"` (ou mieux : `"SO_Brush"`).
- `"New Bool Event"` → `"OnBoolChanged"`.
- Les 6 attributs **sans** `fileName` en reçoivent un : sinon Unity génère `New <Classe>`, avec un espace, **garanti**.

Cette clause est reprise et détaillée en ADR-009. Elle est ici parce qu'elle est la seule qui empêche la nomenclature de se dégrader sans qu'aucun humain n'ait mal tapé.

## Conséquences

**Positives**

- Le trou « In coming… » est comblé. La règle devient **opposable** : une PR peut être refusée en pointant une ligne, pas une opinion.
- Le suffixe de canal (`_BC`, `_N`, `_MRA`) rend possible un **preset d'import automatique par suffixe** — le levier n°1 sur la taille de build Android et WebGL, aujourd'hui inaccessible avec 5 textures conformes.
- La liste fermée de catégories rend une recherche par catégorie **exacte**. Aujourd'hui elle rate 8 fichiers sur `Foliage` et 3 sur `Tree`, sans le dire.
- Zéro espace, zéro accent : les scripts d'outillage, de build et de CI cessent de casser sur des chemins.
- 161 fichiers (`SM_`, `M_`) sont **déjà conformes**. On capitalise, on ne repart pas de zéro.

**Négatives / coûts**

- Deux nomenclatures coexistent pendant longtemps : `SM_ZF_*` (118, gelés) et `SM_<Catégorie>_*` (nouveaux). C'est le prix du gel, et il est assumé — c'est moins cher qu'un renommage de masse sur des scènes de 85 Mo sans driver de merge.
- Les 8 `Folliage` et les 3 `Trees` restent en base. Ils sont documentés ici comme dette connue.
- Les 63 fichiers à espaces écrits par l'équipe doivent être renommés **depuis Unity** (jamais depuis l'explorateur — le `.meta` doit suivre, ADR-012). C'est un chantier à part, par lots, hors des grosses PR.
- La liste fermée frustrera quelqu'un. C'est le but : la friction est là où le vocabulaire diverge.

**Application**

- Un fichier avec espace, accent ou `(N)` bloque la PR. Automatisable en pré-commit (rejet sur `[[:space:]]`, sur non-ASCII, sur `([0-9])` dans un chemin d'`Assets/`).
- Une catégorie hors liste bloque la PR.
- Un nouveau `SM_ZF_` bloque la PR.

## Alternatives écartées

**Renommer les 118 `SM_ZF_*` d'un coup.**
Écarté. Le gain est cosmétique (les GUID font le travail, les fichiers fonctionnent) ; le coût est un diff massif sur des scènes YAML de 85 Mo, sur un dépôt où `unityyamlmerge` n'est configuré sur **aucun poste** et où un `.meta` a déjà été commité avec des marqueurs de conflit et deux GUID (ADR-012). C'est une opération à risque de perte de travail pour un bénéfice esthétique. Gel, propagation stoppée.

**Garder `ZF` et le documenter comme préfixe officiel.**
Écarté. Il a déjà débordé de son sens (`Vehicle`, `Buildings`) : documenter un préfixe qui ne veut plus rien dire, c'est officialiser le bruit. Un préfixe qui s'applique à tout ne discrimine rien.

**Vocabulaire de catégories ouvert, avec « bon sens » attendu.**
Écarté — c'est l'état actuel, et il a produit `Foliage`/`Folliage` et `Tree`/`Trees`. On a la mesure du bon sens : 11 fichiers divergents sur 118, soit ~9 % de dérive sur une seule dimension, en un seul projet. Sur ~25 personnes de niveaux hétérogènes, l'ouverture est une garantie de divergence.

**Autoriser les espaces mais quoter les chemins partout dans l'outillage.**
Écarté. Cela déplace la charge sur chaque script, chaque `.bat`, chaque étape de CI, chaque outil tiers — dont ceux qu'on n'écrit pas. Le premier chemin non quoté casse, et il casse silencieusement. Interdire l'espace à la source coûte une règle ; le tolérer coûte une vigilance permanente sur toute la chaîne d'outils.

**Renommer les EventChannels en `SO_Event_<X>` pour l'uniformité.**
Écarté. La population est cohérente à 100 %, la convention `On<X>` se lit comme son usage, et l'uniformité pour l'uniformité coûte un renommage sans bénéfice. On documente le réel.
