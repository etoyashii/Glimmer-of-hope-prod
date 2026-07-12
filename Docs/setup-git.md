# Configurer unityyamlmerge (5 minutes, une seule fois)

À faire par **chaque personne de l'équipe**, sur **chaque machine** où le projet est cloné.

## Le problème

Le `.gitattributes` du dépôt déclare déjà un outil de fusion pour les fichiers Unity :

```
*.unity  text eol=lf merge=unityyamlmerge
*.prefab text eol=lf merge=unityyamlmerge
*.meta   text eol=lf merge=unityyamlmerge
*.mat    text eol=lf merge=unityyamlmerge
*.anim   text eol=lf merge=unityyamlmerge
```

Mais `.gitattributes` ne fait que **nommer** le driver `unityyamlmerge`. Il ne le fournit pas.
Le driver, lui, se configure **poste par poste**, dans le `.gitconfig` de chacun. Il n'est
versionné nulle part, et git ne peut pas le distribuer : c'est une commande à lancer sur ta
machine, qui pointe vers ton installation d'Unity.

Aujourd'hui, il n'est configuré sur **aucun poste**. Et quand git ne trouve pas un driver
qu'on lui a nommé, il **ne dit rien** : il retombe silencieusement sur la fusion texte
ligne à ligne, celle qu'il utilise pour du code.

Un fichier Unity n'est pas du code. C'est du YAML dont l'ordre des lignes n'a pas de sens,
et où un identifiant doit être unique. Fusionné ligne à ligne, il ne casse pas bruyamment :
il produit un fichier qui a l'air valide et qui ne l'est pas. C'est déjà arrivé sur ce
projet — un `.meta` a été commité avec des marqueurs de conflit et **deux GUID**. Un `.meta`
à deux GUID, c'est un asset dont Unity ne sait plus lequel est le bon : les références qui
pointent dessus cassent, chez tout le monde, et le coupable est un fichier que personne n'a
ouvert.

Configurer le driver, c'est ce qui empêche ça. Il ne se contente pas de mieux fusionner :
quand il ne sait pas résoudre, il **refuse** et te laisse le conflit en main, au lieu
d'inventer un fichier corrompu.

## L'installer

Trois commandes. Copie-colle le bloc de ton système, en **remplaçant le chemin** si ton
Unity n'est pas installé au chemin par défaut. La version du projet est **6000.3.15f1**.

### Windows (PowerShell ou Git Bash, les deux marchent)

```
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p --force %O %B %A %A'
git config --global merge.unityyamlmerge.recursive binary
```

Note les **barres obliques normales** (`/`) et les guillemets autour du chemin : il contient
un espace (`Program Files`), et sans eux git coupe la commande en deux.

### macOS

Sur macOS l'outil est rangé dans le bundle de l'application, pas sous `Editor/Data/` :

```
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/Tools/UnityYAMLMerge" merge -p --force %O %B %A %A'
git config --global merge.unityyamlmerge.recursive binary
```

### Linux

```
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"$HOME/Unity/Hub/Editor/6000.3.15f1/Editor/Data/Tools/UnityYAMLMerge" merge -p --force %O %B %A %A'
git config --global merge.unityyamlmerge.recursive binary
```

Remplace `$HOME/Unity` par le dossier d'installation de ton Hub si tu l'as changé.

Dans les trois cas, `%O %B %A %A` dit à l'outil : voici l'ancêtre commun, voici leur
version, voici la mienne, écris le résultat ici. `recursive = binary` évite que git
fabrique lui-même un faux ancêtre YAML quand l'historique a plusieurs points de fusion.

## Vérifier que ça marche vraiment

Ne te fie pas au fait que les commandes soient passées sans erreur : un chemin faux ne
produit aucune erreur au moment du `git config`. Il ne se verra qu'au premier merge, et
il se verra sous la forme d'un fichier corrompu.

### 1. Le driver est bien déclaré et le chemin existe

```
git config --get merge.unityyamlmerge.driver
```

Copie le chemin qui s'affiche et vérifie que le fichier existe (`ls` sur mac/Linux,
`Test-Path` en PowerShell). **Le lance-toi une fois à la main** pour confirmer qu'il
démarre :

```
"C:/Program Files/Unity/Hub/Editor/6000.3.15f1/Editor/Data/Tools/UnityYAMLMerge.exe"
```

Il doit afficher son aide. S'il répond « commande introuvable », ton chemin est faux :
ouvre Unity Hub, onglet Installs, clique sur la roue dentée de la version 6000.3.15f1 puis
« Show in Explorer / Reveal in Finder », et reconstruis le chemin depuis là.

### 2. Git applique bien le driver aux fichiers Unity

Depuis le dépôt :

```
git check-attr merge -- Assets/_Project/Scenes/Boot.unity
```

Doit répondre `merge: unityyamlmerge`. Si la réponse est `merge: unspecified`, c'est le
`.gitattributes` qui n'est pas là où tu crois — tu n'es pas à la racine du dépôt.

### 3. Le test qui prouve tout : provoquer un vrai conflit

Les deux vérifications précédentes disent que la configuration est en place. Celle-ci dit
qu'elle **fonctionne**. Sur une branche jetable, dans un clone du projet :

```
git checkout -b test-merge-driver
```

Ouvre une scène dans Unity, déplace un objet, sauvegarde, commite.
Puis reviens sur ta branche de base, et sur la **même scène**, déplace un **autre** objet,
sauvegarde, commite. Enfin :

```
git merge test-merge-driver
```

- **Le driver marche** : git annonce `Auto-merging` sur la scène et les deux déplacements
  sont là quand tu rouvres la scène dans Unity. Un fichier YAML fusionné intelligemment.
- **Le driver n'est pas actif** : la scène ressort avec des `<<<<<<<` dedans, ou Unity
  refuse de l'ouvrir. Reprends l'étape 1, ton chemin est faux.

Nettoie derrière toi :

```
git merge --abort
git checkout -
git branch -D test-merge-driver
```

## Si tu tombes sur un fichier déjà cassé

Pour repérer les dégâts déjà commités (marqueurs de conflit oubliés dans un fichier
Unity) :

```
git grep -l "<<<<<<<" -- "*.meta" "*.unity" "*.prefab" "*.mat" "*.anim"
```

Et pour un `.meta` suspect, il ne doit contenir **qu'un seul** `guid:` :

```
git grep -c "^guid:" -- "*.meta" | grep -v ":1$"
```

Tout ce qui remonte ici est à réparer à la main, et **pas** en supprimant le `.meta` :
un `.meta` supprimé, c'est Unity qui régénère un GUID neuf et toutes les références vers
cet asset qui cassent. Le bon geste est de récupérer la version saine du fichier depuis
l'historique (`git log --oneline -- <fichier>` puis `git checkout <commit> -- <fichier>`),
et de prévenir l'équipe avant de pousser.
