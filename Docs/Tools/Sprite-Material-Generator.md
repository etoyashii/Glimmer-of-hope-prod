# Sprite Material Generator

Outil éditeur qui crée un material par texture et assigne chaque texture à une propriété
Texture2D d'un Shader Graph.

`Tools > GlimmerOfHope > Sprite Material Generator`

Code : `Assets/_Project/Scripts/Editor/Tools/SpriteMaterial*.cs`

---

## 1. D'où vient cet outil

La première version vivait dans `Assets/_Project/Art/VFX/Transparent/Sprite_Material_Generator.cs`.
L'idée et la mécanique étaient bonnes — c'est la même approche qui est reprise ici. Trois points
la rendaient inutilisable en l'état.

**Elle cassait le build.** Un script qui utilise `UnityEditor` doit vivre sous un dossier `Editor`
ou dans une assembly marquée `includePlatforms: ["Editor"]`. Posé au milieu des assets, il
compilait dans `Assembly-CSharp`, donc dans le runtime : l'éditeur l'acceptait, le build échouait.
Le check de conventions le signalait déjà comme erreur bloquante (règle `code_roots`).

**L'option « Overwrite » détruisait les références.** Elle faisait `DeleteAsset` puis `CreateAsset`,
ce qui donne un GUID neuf. Tout ce qui pointait sur le material — particle systems, renderers,
prefabs, scènes — se retrouvait avec une référence morte, donc un material rose. Régénérer après
une retouche de shader revenait à casser le travail déjà câblé.

**Les noms de sortie n'étaient pas conformes.** Le material prenait le nom de la texture, donc
`T_SP_cone_a.png` produisait `T_SP_cone_a.mat`, là où l'ADR-008 impose le préfixe `M_`. Il fallait
renommer chaque fichier à la main derrière l'outil.

---

## 2. Ce que fait la version actuelle

- Crée un material par texture, avec le nom conforme : `T_SP_cone_a` donne `M_cone_a`.
- Met à jour un material existant **sur place**, sans le supprimer : le GUID survit, les VFX
  câblés dessus continuent de fonctionner.
- Affiche un plan avant d'écrire quoi que ce soit : ce qui sera créé, mis à jour, ignoré.
- Retrouve la sortie correspondant à une texture même si la texture a été renommée, grâce à un
  manifeste indexé par GUID.
- Refuse de s'exécuter si le dossier de sortie est à l'intérieur du dossier scanné.
- Refuse de s'exécuter si le dossier de sortie n'existe pas, au lieu de se replier silencieusement.
- Vérifie que la propriété visée existe bien sur le shader, et liste les propriétés disponibles
  quand ce n'est pas le cas.
- Conserve ses réglages entre deux sessions de l'éditeur.

---

## 3. Mode d'emploi

1. Ouvrir `Tools > GlimmerOfHope > Sprite Material Generator`.
2. **Shader Graph** : le shader à appliquer, par exemple `Art/VFX/Shader_Sprite.shadergraph`.
3. **Dossier textures** : par exemple `Art/VFX/Transparent/Textures_Sprite`.
4. **Dossier materials** : le dossier de destination. Il doit déjà exister.
5. **Propriété texture** : la `Reference` de la propriété Texture2D dans le Blackboard du Shader
   Graph. Pour `Shader_Sprite`, c'est `_Sprite`. Si le nom ne correspond à rien, la fenêtre
   affiche la liste des propriétés que le shader expose réellement.
6. **Mettre à jour l'existant** : décoché, un material déjà présent est laissé tel quel. Coché, il
   est réassigné sur place.
7. **Suffixes exclus** : vide par défaut, donc tout est traité. Renseigner `_noise,_blur` par
   exemple pour écarter les variantes de masque.
8. Cliquer **Prévisualiser**. Rien n'est écrit à ce stade. Lire le résumé et le tableau, décocher
   les lignes non voulues.
9. Cliquer **Générer**.

Le bouton **Générer** reste désactivé tant qu'aucun plan n'a été calculé, ou tant que la propriété
texture ne correspond à rien sur le shader.

---

## 4. Fonctionnement

### Le principe : deux temps séparés

L'outil ne fait jamais « scanner et écrire » en un seul geste. Il calcule d'abord un **plan**,
l'affiche, et n'écrit que si on le lui demande ensuite. C'est ce qui rend la génération relisable
avant qu'elle ait lieu, et c'est la seule protection réelle : `AssetDatabase.CreateAsset` n'est pas
annulable par `Ctrl+Z`.

```
  Previsualiser                          Generer
  -------------                          -------
  lit les champs                         revalide la requete
        |                                      |
  Scanner.Validate()  --- refus --->     StartAssetEditing()
        |                                      |
  charge le manifeste                    pour chaque ligne cochee :
        |                                        Create  -> new Material + CreateAsset
  Scanner.Scan()                                 Update  -> charge, reassigne, SetDirty
        |                                      |
  liste de SpriteMaterialEntry           StopAssetEditing / SaveAssets / Refresh
        |                                      |
  affichage du tableau                   ecriture du manifeste
  RIEN N'EST ECRIT                       ecriture reelle sur disque
```

### Le chemin d'une texture, ligne par ligne

Pour `Textures_Sprite/T_SP_cone_a.png` :

1. **Le scan la trouve.** `AssetDatabase.FindAssets("t:Texture2D", ...)` rend un GUID. Le filtre
   `t:Texture2D` écarte au passage tout ce qui n'est pas une texture — le `.preset` du dossier, par
   exemple, n'est jamais candidat.
2. **Le filtre de portée décide si on la garde.** Sous-dossiers exclus si « Inclure les
   sous-dossiers » est décoché ; suffixe exclu si le nom finit par un des suffixes saisis.
3. **Le nom du material est calculé.** `T_SP_cone_a` devient `M_cone_a` : le préfixe texture le
   plus long est retiré, `M_` est ajouté.
4. **Le manifeste est consulté.** S'il connaît déjà un material pour ce GUID de texture, c'est
   ce chemin-là qui est retenu, et non le nom calculé. C'est ce qui permet de retrouver la sortie
   même après un renommage de la texture.
5. **La destination est réservée.** Si une autre texture visait déjà ce même chemin, la ligne
   passe en collision, décochée, avec le nom de celle qui occupe la place. Aucune écrasure
   silencieuse.
6. **L'action est décidée** — voir ci-dessous.
7. **À la génération**, la ligne est appliquée si elle est cochée et si son action n'est pas
   `Skip`.

### Les trois actions possibles

| Action | Quand | Ce qui se passe |
|---|---|---|
| `Create` | aucun material à cette destination | `new Material(shader)`, assignation de la texture, `CreateAsset` |
| `Update` | un material existe **et** « Mettre à jour l'existant » est coché | le material est **chargé**, son shader et sa texture réassignés, `SetDirty` |
| `Skip` | un material existe et l'option est décochée, ou collision de nom | rien |

La distinction `Create` / `Update` est tout l'enjeu du patch. La version précédente traitait le
second cas par `DeleteAsset` puis `CreateAsset`, ce qui produit un fichier neuf, donc un GUID neuf,
donc des références mortes partout où le material était câblé.

### Pourquoi charger plutôt que supprimer

Un asset Unity est référencé par le GUID inscrit dans son `.meta`, jamais par son chemin ni par son
nom. Un particle system qui utilise `M_cone_a` stocke ce GUID.

- Supprimer puis recréer le fichier : nouveau `.meta`, nouveau GUID, la référence pointe dans le
  vide, le material devient rose.
- Charger l'asset et le modifier : le fichier survit, le `.meta` survit, le GUID survit, la
  référence continue de fonctionner.

C'est aussi ce qui rend l'outil **idempotent** : relancé sans rien changer, il réassigne les mêmes
valeurs et ne produit aucune modification de fichier. `git status` reste vide. L'ADR-009 en fait un
critère d'acceptation avant merge d'un outil.

### Le manifeste, concrètement

`SpriteMaterials.manifest.json`, écrit dans le dossier de sortie :

```json
{
    "entries": [
        { "sourceGuid": "06cd7ade848504d48adae06a99830005", "materialGuid": "…" }
    ]
}
```

Il associe le GUID d'une **texture** au GUID de son **material**. Deux GUID, aucun chemin, aucun
nom : les deux bouts de la relation restent valides après n'importe quel déplacement ou renommage.

Ce qu'il change en pratique : renomme `T_SP_cone_a.png` en `T_SP_cone_alpha.png`, relance l'outil.
Sans manifeste, il verrait une texture inconnue, créerait `M_cone_alpha` et laisserait `M_cone_a`
orphelin. Avec, il reconnaît la source, retrouve `M_cone_a` et le met à jour.

Le fichier est trié par GUID source et n'est réécrit que si son contenu a changé, pour ne pas
apparaître dans le diff à chaque exécution.

### Les quatre refus

L'outil s'arrête au lieu de deviner, dans quatre cas :

| Refus | Pourquoi |
|---|---|
| dossier de textures invalide | rien à scanner |
| dossier de sortie inexistant | quand un chemin de sauvegarde n'existe pas, Unity se replie **silencieusement** sur `Assets/`. C'est l'origine documentée d'une partie des fichiers de la racine du projet (ADR-009) |
| sortie à l'intérieur du dossier scanné | un générateur qui écrit dans l'espace qu'il relit finit par se relire. C'est le bug vécu du générateur de LOD |
| propriété absente du shader | sans ce contrôle, l'outil produirait des materials tous vides, sans la moindre erreur |

Le quatrième est le seul que la version de Marius faisait déjà — l'intention était juste, c'est
l'API appelée qui n'existait pas.

### Ce que `StartAssetEditing` fait, et sa limite

`AssetDatabase.StartAssetEditing()` suspend le traitement des imports : au lieu de réimporter après
chaque asset écrit, Unity accumule et traite tout à la fermeture du bloc. Sur 152 materials, la
différence est de l'ordre de la minute.

La contrepartie est qu'entre l'ouverture et la fermeture du bloc, l'AssetDatabase n'est pas dans un
état interrogeable normalement : un asset créé à l'intérieur n'a pas encore de GUID résolu. C'est
pour cette raison que l'écriture du manifeste a lieu **après** `StopAssetEditing`, et non pendant la
boucle. Le bloc est ouvert dans un `try` dont le `finally` referme toujours — une exception en
plein milieu ne peut pas laisser l'éditeur avec ses imports suspendus.

### Où vivent les réglages

Les champs de la fenêtre portent `[SerializeField]`, ce qui les fait survivre au rechargement de
domaine — la recompilation d'un script, ou l'entrée en Play mode, ne vide plus les trois champs
d'objet. Au-delà, ils sont enregistrés dans les `EditorPrefs` à la fermeture et relus à
l'ouverture, sous forme de **GUID** et non de chemins, pour qu'un dossier déplacé reste retrouvé.

Les `EditorPrefs` sont locaux à la machine : ils ne sont pas versionnés et n'imposent rien aux
autres membres de l'équipe.

---

## 5. Décisions de conception

### La mise à jour ne supprime jamais

Un material est identifié par son GUID, stocké dans son `.meta`. Supprimer puis recréer le fichier
produit un GUID différent, et toutes les références existantes pointent alors dans le vide. Mettre
à jour l'asset en place conserve le GUID, donc les références.

C'est aussi ce qui rend l'outil idempotent : relancé sans rien changer, il ne produit aucune
modification de fichier. L'ADR-009 en fait un test d'acceptation.

### La clé est le GUID de la texture, pas son nom

Le manifeste `SpriteMaterials.manifest.json`, écrit dans le dossier de sortie, associe le GUID de
chaque texture au GUID du material produit. Le nom du fichier sert à l'humain, la clé sert à
l'outil.

Conséquence concrète : si une texture est renommée, l'outil retrouve son material et le met à jour
au lieu d'en créer un second à côté. Sans cette table, chaque renommage de source laisserait un
orphelin.

Le manifeste est trié par GUID source et n'est réécrit que si son contenu change, pour ne pas
polluer l'historique Git à chaque exécution.

### La sortie ne peut pas être dans l'espace scanné

Un générateur qui écrit dans le dossier qu'il relit finit par se relire lui-même. L'ADR-009
documente le cas vécu sur le générateur de LOD. L'outil refuse donc la configuration au lieu de
faire confiance à l'utilisateur.

### Un dossier de sortie inexistant bloque l'exécution

Quand un chemin de sauvegarde n'existe pas, Unity se replie silencieusement sur `Assets/`. C'est
l'origine documentée d'une partie des fichiers à la racine du projet. L'outil vérifie le dossier
et s'arrête plutôt que de laisser ce repli se produire.

### La règle de nommage

Le préfixe texture le plus long est retiré (`T_SP_`, puis `T_`), et `M_` est ajouté. Un nom qui
commence déjà par `M_` est laissé tel quel, ce qui rend la règle stable si on la réapplique.

| Texture | Material |
|---|---|
| `T_SP_cone_a` | `M_cone_a` |
| `T_SP_cone_a_blur_noise` | `M_cone_a_blur_noise` |
| `T_ZF_Dirt_Normal` | `M_ZF_Dirt_Normal` |
| `Smoke_ATLAS` | `M_Smoke_ATLAS` |
| `M_deja_prefixe` | `M_deja_prefixe` |

Les quatre materials déjà en place (`M_cone_a` à `M_cone_d`) correspondent exactement à ce que la
règle produit. L'outil les reconnaîtra donc comme existants et proposera de les mettre à jour, pas
d'en créer des doublons.

### Deux textures qui donneraient le même material

Le cas se présente en mode récursif, quand deux sous-dossiers contiennent un fichier homonyme. La
seconde texture est marquée en collision, décochée, et la raison indique quelle texture occupe
déjà la place. Aucune écriture silencieuse.

---

## 6. Découpage des fichiers

| Fichier | Rôle |
|---|---|
| `SpriteMaterialSettings.cs` | Constantes, structures de données partagées |
| `SpriteMaterialNaming.cs` | Règle de nommage texture vers material |
| `SpriteMaterialShaderProbe.cs` | Lecture des propriétés Texture2D d'un shader |
| `SpriteMaterialManifest.cs` | Table GUID source vers GUID material |
| `SpriteMaterialScanner.cs` | Construction et validation du plan |
| `SpriteMaterialApplier.cs` | Écriture des assets |
| `SpriteMaterialWindow.cs` | Interface |
| `SpriteMaterialWindowPrefs.cs` | Persistance des réglages |

La règle de nommage et la lecture du shader ne dépendent ni de l'interface ni de l'AssetDatabase :
elles se testent isolément.

---

## 7. Vérification avant de pousser

**Compilation.** Ouvrir Unity et attendre la fin de la compilation. La console doit être vide.

**Test d'idempotence, demandé par l'ADR-009.** Lancer la génération trois fois de suite sans rien
changer. Les deuxième et troisième passages doivent laisser `git status` vide et le nombre de
materials identique. Si le compte augmente, l'outil se relit.

**Conventions.** Rejouer le check en local :

```
python3 .glimmer/check-conventions.py --diff origin/Maain
```

---

## 8. Points restés ouverts

**Le dossier de sortie.** L'ADR-009 range les assets produits par un outil sous
`Assets/_Generated/<Type>/`, en les déclarant jetables et jamais édités à la main. Un material VFX
ne rentre pas franchement dans cette case : il est amorcé par l'outil, puis réglé à la main par
l'artiste. L'outil n'impose donc rien et signale seulement le cas dans son interface. La question
se tranche en équipe.

**Nommage des assets voisins.** Le check de conventions relève, en avertissement :
`Shader_Sprite.shadergraph` attend le préfixe `SG_`, `VFX_Test.unity` attend `SC_` ou `LV_`,
`Smoke_ATLAS.png` et `Smoke_RedATLAS.png` attendent `T_`. Ces renommages doivent se faire depuis
la fenêtre Project d'Unity pour que les `.meta` suivent. Ils ne sont pas inclus ici : ce sont les
fichiers d'un autre, et les renommer depuis cette branche mélangerait deux sujets.
