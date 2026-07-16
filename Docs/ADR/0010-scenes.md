# ADR-010 — Organisation et chargement des scènes

## Statut

Accepté.

## Contexte

Le projet compte **53 scènes**. Leur état actuel :

- **20 scènes sur 53 (38 %) sont rangées par prénom** : `Scenes/Proto/Erwan/` (12), `Scenes/Proto/Romain/` (6), `Scenes/Proto/Thibault/` (1), `Scenes/Proto/Bastien/` (1).
- **3 scènes seulement sont dans le build** : `_Bootstrap`, `MainMenu`, `Gameplay`. Les 50 autres ne sont, de fait, jamais chargées par le jeu livré — mais elles sont dans le dépôt, elles sont clonées par les 25 personnes de l'équipe, et certaines pèsent des dizaines de mégaoctets (ADR-011).
- On trouve des scènes nommées `New Scene.unity` — un nom généré par défaut, qui ne dit rien de leur contenu.

**Le rangement par prénom est une impasse à trois têtes.**

D'abord, il confond *propriété* et *emplacement*. Une scène n'appartient à personne : elle appartient au jeu. `git log` sait déjà qui l'a écrite, et il le sait mieux qu'un dossier — il connaît aussi les cinq autres personnes qui l'ont modifiée depuis.

Ensuite, il ne survit pas au temps. Sur une équipe majoritairement étudiante, l'organigramme change à chaque semestre. `Erwan/` contient 12 scènes ; le jour où Erwan part, ces 12 scènes deviennent un dossier orphelin que plus personne n'ose ni ouvrir ni supprimer, parce que personne ne sait ce qu'il y a dedans. Le nom du dossier ne le dit pas.

Enfin, il empêche la seule question qui compte : **cette scène sert-elle au jeu, oui ou non ?** Avec 3 scènes dans le build sur 53, cette question est vitale, et l'arborescence actuelle est incapable d'y répondre.

**Le chargement par chaîne casse silencieusement.**

Preuve mesurée : un appel `SceneManager.LoadScene("LV_Main")` pointait vers une scène **absente des Build Settings**. Il n'y a eu aucune erreur de compilation, aucun avertissement, aucun test rouge. La casse s'est manifestée à l'exécution, chez le joueur : une **exception à chaque clic sur « Restart »**.

C'est la propriété la plus dangereuse de la surcharge par chaîne de `LoadScene` : le compilateur ne voit **rien**. Une chaîne est une chaîne. Renommer une scène (et l'ADR-008 va en renommer), la sortir du build, ou faire une faute de frappe dans le littéral produisent tous le même résultat — du code qui compile, qui passe la revue, et qui explose au runtime, souvent dans un chemin peu emprunté (un bouton « Restart », justement) que personne ne teste avant la démo.

Sur une équipe de ~25 personnes de niveaux hétérogènes, une classe de bugs invisible au compilateur est exactement celle qu'il faut supprimer par construction plutôt que par vigilance.

## Décision

### 1. Les scènes sont classées **par fonction**, jamais par auteur

```
_Project/Scenes/
├── Core/          # infrastructure du jeu — TOUJOURS dans le build
│   ├── SC_Bootstrap.unity
│   ├── SC_MainMenu.unity
│   └── SC_Gameplay.unity
├── Levels/        # niveaux jouables — dans le build s'ils sont livrés
│   ├── LV_Forest_01.unity
│   ├── LV_Volcano_01.unity
│   └── ...
└── Sandbox/       # prototypes, tests, bacs à sable — JAMAIS dans le build
    ├── SB_TerrainBrush.unity
    ├── SB_LightingTest.unity
    └── ...
```

Trois dossiers. Liste fermée. Le dossier répond à une seule question : **à quoi sert cette scène ?**

- `Core/` — la scène est de l'infrastructure. Elle est dans le build, toujours.
- `Levels/` — la scène est du contenu jouable. Elle est dans le build si elle est livrée.
- `Sandbox/` — la scène est un essai. Elle n'est **jamais** dans le build, et quiconque la supprime ne casse rien. C'est la définition d'un bac à sable, et c'est ce qui le rend sain.

**Interdiction : aucun dossier de scènes portant un nom de personne.** `Erwan/`, `Romain/`, `Thibault/`, `Bastien/` sont migrés dans `Sandbox/`, et leurs scènes sont **renommées par sujet** — ce sur quoi elles portent, pas qui les a faites (`SB_TerrainBrush`, `SB_WaterShader`, `SB_EnemyPathfinding`). L'auteur reste dans `git log`, où il est exact et à jour.

Une scène de `Sandbox/` qui devient utile au jeu **déménage** vers `Levels/` ou `Core/`. Ce déménagement est le moment où elle est nettoyée, nommée correctement, et ajoutée aux Build Settings — c'est un rite de passage explicite, pas un glissement.

### 2. Nomenclature (application de l'ADR-008)

| Dossier | Préfixe | Schéma |
|---|---|---|
| `Core/` | `SC_` | `SC_<Fonction>` |
| `Levels/` | `LV_` | `LV_<Zone>_<Index>` |
| `Sandbox/` | `SB_` | `SB_<Sujet>` |

`LV_` est déjà installé (17 fichiers). On le garde. `New Scene.unity` et tout nom généré par défaut sont interdits — un espace dans un nom de scène est doublement pénible, parce que ce nom finit dans des chemins de build et dans des scripts.

### 3. **Interdiction de `SceneManager.LoadScene("<chaîne>")`**

La surcharge par chaîne est **bannie du projet**. Sans exception.

Une scène se charge :

- **par référence sérialisée** — un `SceneAsset` (édition) ou un `AssetReference` / `SceneReference` exposé dans l'inspecteur, résolu vers un index ou un chemin **au build** ;
- **par index de build** — `SceneManager.LoadScene(buildIndex)` ;
- **par constante générée** — une classe `BuildScenes` générée depuis les Build Settings, exposant des membres typés (`BuildScenes.Gameplay`).

Pourquoi la chaîne est interdite, et pas seulement découragée : elle est la seule des trois formes où **rien** ne vérifie que la cible existe. Renommer une scène ne casse pas la compilation. La sortir du build ne casse pas la compilation. Une faute de frappe ne casse pas la compilation. `LoadScene("LV_Main")` a compilé, a été mergé, et a jeté une exception à chaque « Restart » **en jeu**. Une référence, elle, casse **au build**, là où c'est gratuit.

Un `SceneReference` (champ sérialisé, validé à l'édition, résolu au build) donne en plus ce que la chaîne n'a jamais donné : une **erreur si la scène ciblée n'est pas dans les Build Settings**. C'est précisément le bug observé, et il devient impossible.

Corollaire : le chargement d'une scène passe par un service dédié (Service Locator — cohérent avec le choix d'architecture du projet) qui prend une référence, pas une chaîne. Aucun code de gameplay n'appelle `SceneManager` directement.

### 4. Build Settings : la liste est **décidée**, pas subie

Les Build Settings ne contiennent que `Core/` et les `Levels/` livrés. **Jamais une scène de `Sandbox/`.**

Cette liste est un artefact de projet à part entière : elle est relue en PR quand elle change, et un ajout se justifie. Aujourd'hui, 3 scènes sur 53 y figurent — ce n'est pas une anomalie, c'est le bon ordre de grandeur, et il faut qu'il le reste. Chaque scène du build est du poids dans le `.apk` et dans le bundle WebGL, où le budget est le plus serré.

## Conséquences

**Positives**

- La question « cette scène sert-elle au jeu ? » se lit dans le chemin. C'est ce qui manque le plus quand 50 scènes sur 53 sont hors du build.
- Les 20 scènes par prénom deviennent identifiables **par leur sujet**. Un dossier `Sandbox/` dont on comprend le contenu est un dossier qu'on peut nettoyer ; un dossier `Erwan/` ne l'est pas.
- Le départ d'un membre de l'équipe n'orpheline plus rien.
- La classe de bugs « `LoadScene` vers une scène absente du build » disparaît **par construction**. Ce n'est plus une erreur qu'on peut faire.
- Le renommage de scènes prévu par l'ADR-008 devient sûr : aucune chaîne littérale à traquer.

**Négatives / coûts**

- Il faut auditer les 20 scènes des dossiers-prénoms et leur trouver un sujet. Certaines n'en auront pas — ce sont des scènes mortes, et c'est l'occasion de les supprimer.
- Migrer les appels `LoadScene("…")` existants demande d'introduire un `SceneReference` et de le câbler. C'est du travail ponctuel, une fois.
- Un déplacement de scène produit un gros diff sur des fichiers YAML volumineux (ADR-011). Les déplacements de scènes se font dans un commit **dédié**, sans autre changement, et en une passe — pas au fil de l'eau.

**Application**

- Un dossier de scènes portant un prénom bloque la PR.
- Un `SceneManager.LoadScene("` (littéral) bloque la PR. Détectable par un simple grep en CI.
- Une scène de `Sandbox/` ajoutée aux Build Settings bloque la PR.

## Alternatives écartées

**Garder `Proto/<Prénom>/` pour les prototypes seulement.**
Écarté. C'est l'état actuel, et il représente 38 % des scènes du projet. « C'est juste pour mes protos » est exactement ce qui a été dit avant d'arriver à 20 scènes. Un dossier par personne n'a pas de mécanisme d'expiration : personne ne range le dossier de quelqu'un d'autre. `Sandbox/` avec des noms de sujet a le même usage et reste nettoyable par n'importe qui.

**Autoriser `LoadScene(string)` avec une constante centralisée (`const string GAMEPLAY = "Gameplay";`).**
Écarté. Cela supprime la faute de frappe, mais **pas** le bug observé : la constante compile parfaitement même si la scène a été renommée, déplacée, ou retirée des Build Settings. C'est exactement ce qui s'est produit avec `LV_Main`. Une constante rassure sans protéger.

**Mettre les 53 scènes dans les Build Settings pour que `LoadScene` marche toujours.**
Écarté, et c'est le contraire de ce qu'il faut faire. Chaque scène du build est du poids embarqué — critique sur Android et surtout WebGL. Et cela ferait entrer les bacs à sable, dont `BrushTest.unity` (40 Mo, ADR-011), dans le jeu livré.

**Classer les scènes par équipe (`Art/`, `LD/`, `Prog/`) plutôt que par personne.**
Écarté. C'est le même défaut à une granularité plus grosse : ça encode l'organisation, pas le jeu. Une scène de level design finit par contenir du VFX et de l'éclairage — la question n'est pas qui la touche, mais si elle est livrée.

**Utiliser les Addressables pour tout le chargement de scènes.**
Écarté **pour l'instant**. Les Addressables résolvent le problème (chargement par référence, pas par nom) mais ajoutent un système entier — groupes, profils, catalogues, build de contenu — à une équipe majoritairement junior. C'est en contradiction directe avec la ligne d'architecture du projet, qui a écarté Zenject pour la même raison. Un `SceneReference` sérialisé apporte 90 % du bénéfice pour une fraction du coût cognitif. Réévaluable quand la taille du contenu l'exigera.
