# Réorganisation de l'arborescence — le mode d'emploi du jour du gel

Ce dossier contient l'outillage qui range les 1 500 fichiers d'`Assets/` selon l'ADR-007.
Il ne s'exécute pas tout seul. Il se lance **une fois**, pendant une fenêtre de gel, et il
refuse de démarrer si la fenêtre n'en est pas une.

L'opération elle-même prend **quelques minutes**. Ce qui prend du temps, c'est ce qu'il y a
autour : faire atterrir les branches en vol, et faire installer `unityyamlmerge` à tout le
monde. C'est pour ça que le gel est court et que le `preflight` est intraitable.

---

## Ce que ça fait

**440 fichiers déplacés, zéro contenu modifié.**

Les références Unity sont par GUID, le GUID vit dans le `.meta`, et le `.meta` suit son
fichier. Donc **aucune scène, aucun prefab, aucun matériau ne peut changer**. Ce n'est pas
une promesse, c'est une propriété : `verify.py` exige un diff YAML strictement vide et
s'arrête si une seule ligne bouge.

Ce que ça range, dans l'ordre d'importance :

- **`ZPreprod/` est dissous.** C'était un **second arbre `_Project` complet**, en parallèle
  du vrai — avec ses propres `Scripts/Gameplay`, `UI/`, `Art/`, et `LV_Main.unity` dedans.
  Le dépôt n'avait pas une arborescence sale : il en avait deux.
- **Plus rien à la racine d'`Assets/`** — 34 fichiers y traînaient, dont 18 terrains, qui sont
  de la donnée vivante. La cause était mécanique : un outil ouvrait sa boîte de sauvegarde sur
  un dossier inexistant, et Unity repliait silencieusement sur la racine.
- **Les scènes ne sont plus classées par prénom.** 20 des 53 l'étaient. Le prénom devient un
  préfixe de nom de fichier : on garde la traçabilité, on perd la balkanisation.
- **Les packs Asset Store partent sous `ThirdParty/`** (dont un pack d'oiseaux enfoui sous
  `Proto LD/PlaceHoldersAssets/`).
- **Les dossiers à espace disparaissent** (`Proto LD`, `LD Forest`) — ils cassent la moitié des
  scripts shell qu'on écrit.
- **Les 52 scripts hors asmdef sont regroupés sous `Scripts/Legacy/`.** Voir plus bas : c'est
  le point le plus subtil du lot.

---

## Le piège qui commande tout le reste : `Scripts/Legacy/`

52 fichiers `.cs` vivent dans **`Assembly-CSharp`**, hors de tout `asmdef`.

Les ranger « proprement » sous `Scripts/Gameplay/` ou `Scripts/Editor/` les ferait entrer dans
un asmdef. Or **un asmdef ne peut pas référencer `Assembly-CSharp`** — c'est une règle d'Unity,
pas un réglage qu'on peut changer. Ces fichiers ne compileraient plus. Ce ne serait pas du
rangement : ce serait une migration de code déguisée en `git mv`.

Alors ils sont regroupés sous **`Scripts/Legacy/`**, qui n'est couvert par aucun asmdef — et
`Legacy/Editor/`, qui reste un dossier magique `Editor` hors asmdef. **Leur assembly ne change
pas.** Le déplacement est pur, la compilation est intacte.

`plan.py` **refuse** de générer un déplacement qui changerait l'assembly d'un `.cs`, et il le
dit. (Vérifié : en tordant volontairement les règles pour envoyer ces fichiers dans un asmdef,
il en refuse 24 et les liste.)

Et surtout : **`Legacy/` devient un compteur de dette.** 52 aujourd'hui. Il n'a le droit que de
descendre. Les faire entrer dans les asmdefs est une vague de code, avec sa propre PR et sa
propre vérification de compilation.

---

## La panne qu'aucun outil ne signale

Un dossier nommé dans une **chaîne littérale** du C# et déplacé par le manifeste :

- `git mv` réussit,
- Unity compile sans une erreur,
- et l'outil renvoie **zéro résultat, sans un mot.**

C'est la même famille que `AssetDatabase.FindAssets(filter, searchInFolders)` sur un dossier
absent : tableau vide, zéro exception, zéro log. Rien ne proteste, et on s'en aperçoit trois
semaines plus tard.

`plan.py` les trouve et les écrit dans **`fixups.tsv`**. `apply.py` les corrige dans un
**second commit** — jamais le même que les déplacements, parce que git ne *stocke* pas les
renommages, il les *détecte* par similarité : un fichier à la fois déplacé et modifié casse la
détection, et avec elle le `git revert` qui est notre seul plan de repli.

`verify.py` re-vérifie après coup, avec le bon critère : pas « ce chemin existe-t-il » (le
dépôt contient déjà des chemins morts, antérieurs à nous), mais **« son ancre a-t-elle reculé »**.

---

## Le jour J, dans l'ordre

### 1. Le préflight — il a le droit de dire non

```bash
python3 .glimmer/reorg/preflight.py
```

Il bloque, entre autres, s'il reste **une seule branche avec du travail non fusionné**. C'est
le contrôle central : chaque branche en vol devra être rebasée par-dessus le déplacement, et
nos scènes font jusqu'à 85 Mo de YAML mergé en texte brut.

Il bloque aussi si **`unityyamlmerge` n'est pas configuré**. Ce n'est pas du zèle : un `.meta`
a déjà été commité **avec ses marqueurs de conflit et deux GUID dedans**. La preuve est dans
l'historique.

**Aucun de ces contrôles n'est informatif. Tous bloquent.** Un préflight qui se contente
d'avertir se fait ignorer le jour où tout le monde est pressé — c'est-à-dire le seul jour où
il sert.

### 2. Le plan — et on le relit

```bash
python3 .glimmer/reorg/plan.py
```

Il **dérive** le manifeste des règles (`rules.json`) appliquées à l'arbre **réel**, maintenant.
Il ne rejoue pas une liste figée d'avance : entre le jour où on écrit les règles et le jour du
gel, l'arbre bouge, et un manifeste figé échouerait en silence sur les chemins disparus.

Il produit :

| fichier | quoi |
|---|---|
| `report.md` | **à lire.** Ce qui bouge, ce qu'il refuse, et pourquoi. |
| `manifest.tsv` | la liste `source → destination`. **Éditable :** supprimer une ligne = ne pas déplacer ce fichier. |
| `fixups.tsv` | les constantes de chemin à repointer ensuite. |

Ce qu'il refuse, il le refuse **pour de bon** : un `.cs` qui changerait d'assembly, une
collision de destination (y compris celles qui n'existent que sur NTFS, où `Foo.png` et
`foo.png` sont le **même** fichier), un sanctuaire Unity.

### 3. Unity fermé. À blanc, puis pour de vrai.

```bash
python3 .glimmer/reorg/apply.py                    # simulation, n'écrit rien
python3 .glimmer/reorg/apply.py --apply --commit   # deux commits
```

Unity **doit être fermé** : il réécrit les `.meta` pendant qu'il tourne.

Deux commits, et la séparation n'est pas cosmétique (cf. plus haut) :

1. **les déplacements**, et rien d'autre ;
2. **les chemins en dur** — les constantes du C#, et les chemins de scène dans
   `EditorBuildSettings.asset`.

Sur ce second point : Unity résout les scènes du build par GUID et **réécrit le chemin tout
seul** à la première ouverture. Le build n'est donc jamais cassé. Mais si on le laisse faire,
les 25 postes régénèrent chacun le fichier de leur côté et se disputent celui qui décide de ce
qui part en build. On le fait une fois, ici.

### 4. La preuve

```bash
python3 .glimmer/reorg/verify.py
```

| contrôle | ce qu'il prouve |
|---|---|
| **diff YAML vide** | aucun GUID réécrit. L'invariant central, gratuit et binaire. |
| `.meta` cohérents | aucun orphelin, aucun asset sans `.meta` (sinon : GUID régénéré, différent sur chaque poste) |
| GUID uniques | pas deux `.meta` avec le même — Unity choisirait au hasard |
| références cassées | **ne montent pas.** Le nombre absolu n'a aucun sens (beaucoup sont des GUID de packages) ; ce qui en a un, c'est qu'il ne bouge pas. |
| chemins en dur | aucune ancre perdue |
| scènes du build | les 3 résolvent |
| racine d'`Assets/` | vide (ADR-007) |

Puis, la vraie preuve de fin : **ouvrir Unity.** S'il est content, il ne touche à rien —
`git status` reste propre. C'est ce qui a été observé sur l'essai à blanc : **aucun `.meta`
régénéré, zéro erreur de compilation, les 8 assemblies produites** (dont `Assembly-CSharp` et
`Assembly-CSharp-Editor`, ce qui prouve que les 52 scripts de `Legacy/` sont bien restés chez
eux).

---

## Si ça tourne mal

Une vague = une PR = un merge commit à périmètre homogène.

```bash
git revert -m 1 <sha-du-merge>
```

Le revert **avance** l'historique, il ne le réécrit pas : personne n'a besoin de re-cloner.
C'est précisément pour ça que le commit de déplacement ne contient **que** des déplacements —
si git n'y détecte plus des renommages, le revert n'est plus fiable.

---

## Ce que cet outillage ne fait pas

- Il ne **dédoublonne** rien. Trois `NewLayer.terrainlayer` portent le même nom : il les
  distingue par leur provenance, il ne décide pas lequel est le bon. C'est un arbitrage
  d'artiste, et un renommage garde les GUID, donc reste annulable.
- Il ne **renomme pas en masse**. `SM_ZF_` (118 fichiers) est **gelé** : le préfixe a débordé
  et ne veut plus rien dire, mais 500 renommages sur un dépôt à 25 contributeurs avec LFS
  détruiraient la semaine de tout le monde. On cesse de le propager, on ne le rétrocède pas.
  Seules les fautes d'orthographe sont corrigées (`Folliage` → `Foliage`).
- Il ne **touche jamais** aux sanctuaires : `Plugins/` (FMOD, référencé en dur dans le
  `.gitignore`), `Resources/` (DOTween y charge **par nom**), `StreamingAssets/`,
  `ScriptTemplates/`, `TextMesh Pro/`, `AddressableAssetsData/`. Un dossier nommé `Resources`
  est magique **à n'importe quelle profondeur** — le déplacer casse au runtime, en silence,
  sans que rien ne compile de travers.
- Il ne **patche pas le code tiers**. FMOD cite `Assets/Editor/FMODMigrationUtil.cs` : corriger
  cette ligne serait modifier du code vendor, que sa prochaine mise à jour écraserait.

## Ce qu'il a trouvé en chemin, et qui ne le regarde pas

`SO_PhotoTask.cs:45` pointe sur `Assets/_Project/Prefabs/UI/Camera/Text.prefab`. **Ce dossier
n'existe pas, et n'existait pas avant la réorg.** C'est une casse antérieure, indépendante.
`verify.py` la voit et ne bloque pas dessus — mais elle est réelle, et elle mérite son ticket.
