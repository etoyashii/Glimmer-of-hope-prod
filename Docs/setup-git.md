# Configurer unityyamlmerge (1 minute, une seule fois)

À faire par **chaque personne de l'équipe**, sur **chaque machine** où le projet est cloné.
Une fois par machine — pas par projet, pas par branche.

## La procédure

### Windows

Double-clique sur **`Docs/setup-git.bat`**.

### macOS / Linux

Depuis un terminal :

```
bash Docs/setup-git.sh
```

C'est tout. Le script trouve ton installation d'Unity, configure git, et vérifie son propre
travail. Il affiche `C'est bon. Ta machine est configuree.` quand c'est fini.

Rien à copier, rien à adapter : il lit la version d'Unity dans `ProjectSettings/ProjectVersion.txt`,
donc il suivra automatiquement le jour où le projet changera de version.

**Si le script s'arrête sur `[ECHEC]`**, le message dit quoi corriger. Le cas le plus fréquent est
une version d'Unity absente : installe celle du projet depuis Unity Hub et relance. Le script
vérifie ce qu'il a écrit et refuse de te dire que c'est bon si ça ne l'est pas — un `[OK]` final
est fiable. Si tu bloques, envoie une capture de la fenêtre sur le Discord.

## Pourquoi c'est obligatoire

Le `.gitattributes` du dépôt déclare déjà un outil de fusion pour les fichiers Unity :

```
*.unity  text eol=lf merge=unityyamlmerge
*.prefab text eol=lf merge=unityyamlmerge
*.meta   text eol=lf merge=unityyamlmerge
*.mat    text eol=lf merge=unityyamlmerge
*.anim   text eol=lf merge=unityyamlmerge
*.asset  text eol=lf merge=unityyamlmerge
```

Mais `.gitattributes` ne fait que **nommer** le driver `unityyamlmerge`. Il ne le fournit pas. Le
driver se configure **poste par poste**, dans le `.gitconfig` de chacun. Il n'est versionné nulle
part, et git ne peut pas le distribuer : c'est une commande qui pointe vers ton installation
d'Unity, propre à ta machine. C'est exactement ce que le script fait à ta place.

Tant qu'il n'est pas configuré, git **ne dit rien** : quand il ne trouve pas un driver qu'on lui a
nommé, il retombe silencieusement sur la fusion texte ligne à ligne, celle qu'il utilise pour du
code.

Un fichier Unity n'est pas du code. C'est du YAML dont l'ordre des lignes n'a pas de sens, et où un
identifiant doit être unique. Fusionné ligne à ligne, il ne casse pas bruyamment : il produit un
fichier qui a l'air valide et qui ne l'est pas. C'est déjà arrivé sur ce projet — un `.meta` a été
commité avec des marqueurs de conflit et **deux GUID**. Un `.meta` à deux GUID, c'est un asset dont
Unity ne sait plus lequel est le bon : les références qui pointent dessus cassent, chez tout le
monde, et le coupable est un fichier que personne n'a ouvert.

La différence est reproductible en une minute. Deux personnes ajoutent chacune un objet à la même
scène :

| | Sans le driver | Avec le driver |
|---|---|---|
| `git check-attr merge -- <scène>` | `merge: unspecified` | `merge: unityyamlmerge` |
| Résultat du merge | `CONFLICT (content)` | `Auto-merging`, propre |
| Contenu du fichier | des `<<<<<<<` **dedans** | les deux objets, intacts |

Configurer le driver, c'est ce qui empêche ça. Il ne se contente pas de mieux fusionner : quand il
ne sait pas résoudre, il **refuse** et te laisse le conflit en main, au lieu d'inventer un fichier
corrompu.

## Si git ou GitHub Desktop a l'air figé pendant un merge

UnityYAMLMerge affiche ses erreurs dans une **fenêtre modale**, qui peut ne jamais remonter à
l'écran quand elle est déclenchée depuis GitHub Desktop. Git attend alors un clic que personne ne
voit, et l'interface a l'air gelée. Le script configure le driver en mode headless (`-h`) pour
éviter ça. Si tu avais configuré ton poste à la main avant, relance le script : il corrigera.

Plus généralement, pour un merge à gros diff, préfère la ligne de commande : tu vois ce que le
driver fait, GitHub Desktop non.

## Si tu tombes sur un fichier déjà cassé

Pour repérer les dégâts déjà commités (marqueurs de conflit oubliés dans un fichier Unity) :

```
git grep -al "<<<<<<<" -- "*.meta" "*.unity" "*.prefab" "*.mat" "*.anim" "*.asset"
```

Et pour un `.meta` suspect, il ne doit contenir **qu'un seul** `guid:` :

```
git grep -ac "^guid:" -- "*.meta" | grep -v ":1$"
```

Le `-a` n'est pas décoratif : sans lui, git peut considérer un fichier comme binaire et le sauter
en silence — et un résultat vide te ferait croire que tout va bien.

Tout ce qui remonte ici est à réparer à la main, et **pas** en supprimant le `.meta` : un `.meta`
supprimé, c'est Unity qui régénère un GUID neuf et toutes les références vers cet asset qui cassent.
Le bon geste est de récupérer la version saine du fichier depuis l'historique
(`git log --oneline -- <fichier>` puis `git checkout <commit> -- <fichier>`), et de prévenir
l'équipe avant de pousser.
