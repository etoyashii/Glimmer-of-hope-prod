# ADR-011 — Poids des scènes : découpage, pas LFS

## Statut

Accepté.

## Contexte

**`LD-Forest.unity` pèse 85 Mo.** Quatre-vingt-cinq mégaoctets de YAML, en **texte brut**, dans un fichier unique. `BrushTest.unity` en pèse 40.

Il faut mesurer ce que ça signifie concrètement, parce que le chiffre seul n'alarme personne :

- **Chaque modification de la scène réécrit une part significative de ces 85 Mo.** Git stocke la nouvelle version. Un dépôt sur lequel 25 personnes touchent une scène de 85 Mo grossit d'un ordre de grandeur en quelques semaines. Le clone initial devient une épreuve.
- **Deux personnes qui touchent la scène en même temps produisent un conflit sur un fichier de 85 Mo.** Personne ne résout ça à la main. Personne ne *peut* résoudre ça à la main. En pratique, l'un des deux perd son travail — et c'est le comportement effectivement observé sur les projets qui en arrivent là.
- Ce conflit est d'autant plus certain que le driver de merge Unity, `unityyamlmerge`, est **déclaré dans `.gitattributes` mais configuré sur aucun poste** (ADR-012). Git tente donc un merge **textuel** sur du YAML Unity — l'opération qui a déjà produit un `.meta` commité avec des marqueurs de conflit et deux GUID.
- Ouvrir la scène, la sauvegarder, la recharger : tout est lent, pour tout le monde, tout le temps.

Le réflexe naturel, face à un fichier de 85 Mo dans Git, est de le mettre en **Git LFS**. C'est ce réflexe que cet ADR existe pour arrêter.

## Décision

### 1. Les scènes ne vont **PAS** en LFS. Jamais.

C'est une interdiction, pas une préférence.

Git LFS remplace le contenu du fichier par un **pointeur** dans le dépôt, et stocke les octets ailleurs. Pour un `.fbx`, une `.png`, une banque FMOD — des fichiers **binaires, opaques, non mergeables par nature** — c'est exactement le bon outil : on ne perd rien, puisqu'il n'y avait rien à merger.

Pour une scène Unity, c'est une catastrophe, et pour une raison précise : **un fichier en LFS devient non-mergeable**. Git ne voit plus qu'un pointeur. Le driver `unityyamlmerge` n'a plus rien à merger. Toute modification concurrente devient un conflit binaire **« l'un ou l'autre »**, résolu par écrasement pur : quelqu'un perd tout son travail sur la scène, intégralement, sans possibilité de récupération partielle.

Or une scène Unity **est** mergeable. C'est même précisément à ça que sert la sérialisation Force Text (`m_SerializationMode: 2`, ADR-012) : produire un YAML que `unityyamlmerge` sait fusionner sémantiquement, GameObject par GameObject. Le projet a déjà payé le prix de cette mergeabilité — un fichier texte gros et verbeux. Passer les scènes en LFS reviendrait à payer ce prix **et** à jeter le bénéfice.

Le fait que les scènes soient aujourd'hui **hors LFS est correct**. On le documente ici pour que personne ne le « corrige » en voyant les 85 Mo.

Récapitulatif de la ligne :

| Type | LFS ? | Pourquoi |
|---|---|---|
| `.fbx`, `.png`, `.tga`, `.wav`, banques FMOD | **Oui** | Binaire, non mergeable de toute façon. Rien à perdre. |
| `.unity`, `.prefab`, `.asset`, `.meta`, `.cs` | **Non** | Texte mergeable. LFS détruirait le merge. |

### 2. Le vrai remède : découper

Une scène de 85 Mo n'est pas un problème de **stockage**. C'est un problème de **granularité**. On ne compresse pas le symptôme, on supprime la cause : la scène contient tout, donc tout le monde doit la toucher.

**a. Extraire en prefabs.**
Tout groupe d'objets cohérent (un village, un rocher habillé, une clairière, un ensemble de props) devient un **prefab**. Le prefab vit dans `_Project/Prefabs/<Domaine>/` (ADR-007), il est versionné séparément, et la scène ne contient plus qu'une **instance** — c'est-à-dire quelques lignes de YAML avec un GUID et une transform, au lieu de la hiérarchie complète des objets.

C'est le levier principal, et de loin. L'essentiel des 85 Mo est de la géométrie de placement dupliquée dans le YAML. Deux personnes peuvent alors travailler sur deux prefabs différents **sans jamais toucher le même fichier** — le conflit disparaît, il n'est pas résolu.

**b. Découper en scènes additives.**
Une grosse zone se décompose en scènes chargées additivement, par responsabilité :

```
LV_Forest_01.unity              # scène racine, légère : orchestration
LV_Forest_01_Terrain.unity      # terrain, sol
LV_Forest_01_Props.unity        # végétation, décor
LV_Forest_01_Lighting.unity     # lumières, volumes, réglages
LV_Forest_01_Gameplay.unity     # spawns, triggers, logique
```

Un artiste travaille sur `_Props`, un level designer sur `_Gameplay`, un éclairagiste sur `_Lighting` — **en parallèle, sans conflit**, parce qu'ils écrivent dans des fichiers différents. C'est le bénéfice réel : pas la taille, la **concurrence**.

Ce découpage sert aussi le budget mémoire d'Android et de WebGL, où charger une zone entière d'un bloc est de toute façon intenable.

**c. Sortir les données du YAML.**
Ce qui est de la **donnée** (réglages de gameplay, tables d'équilibrage, listes de spawns) sort de la scène et va dans des ScriptableObjects (`_Project/Data/`, ADR-007). Une donnée dans un ScriptableObject se modifie sans ouvrir la scène — donc sans conflit sur la scène.

### 3. Plafond indicatif

| Poids d'une scène | Statut |
|---|---|
| **< 5 Mo** | Sain. |
| **5 – 20 Mo** | À surveiller. Une extraction de prefabs est probablement due. |
| **> 20 Mo** | **Anormal.** Ne doit pas passer en revue sans justification explicite. |

Ce sont des ordres de grandeur, pas une porte automatique — une scène de terrain dense justifiera d'être plus lourde qu'un menu. Mais un franchissement du seuil de 20 Mo doit **déclencher une conversation**, pas passer inaperçu.

`LD-Forest.unity` est à **85 Mo**, soit plus de quatre fois le seuil d'anomalie. `BrushTest.unity` est à 40 Mo — et c'est un **bac à sable** (ADR-010), donc 40 Mo que tout le monde clone pour rien.

### 4. Marche à suivre

Le découpage de `LD-Forest.unity` est un chantier à part entière, pas une tâche de fond :

1. **Geler la scène.** Annoncer à l'équipe que `LD-Forest.unity` n'est touchée par personne pendant l'opération. Sur un fichier de 85 Mo sans driver de merge, une modification concurrente pendant le découpage est une perte de travail garantie.
2. **Une seule personne**, sur une branche dédiée, sans autre changement dans le lot.
3. **Extraire les prefabs** par groupes cohérents. Commiter par lots — pas un commit unique de 85 Mo remaniés.
4. **Découper en scènes additives** selon le schéma ci-dessus.
5. **Mesurer.** Le poids après découpage doit être documenté dans la PR. Si la scène racine dépasse encore 5 Mo, le découpage n'est pas terminé.
6. **Merger vite.** Une branche qui porte un remaniement de scène ne doit pas vivre longtemps : chaque jour de retard est un risque de divergence irréconciliable.

`BrushTest.unity` (40 Mo, bac à sable) est traité plus simplement : on vérifie qu'il sert encore, et s'il ne sert plus, **on le supprime**. Un bac à sable est jetable par définition (ADR-010) — c'est ce qui le rend supportable.

**Prévention.** Le découpage est un cycle, pas une opération unique : une scène redevient grosse si rien ne la surveille. Le poids des `.unity` est vérifié à chaque PR ; un fichier de scène qui franchit 20 Mo est signalé.

## Conséquences

**Positives**

- Les scènes **restent mergeables**. C'est la propriété qu'il faut protéger, et LFS l'aurait détruite.
- Le découpage supprime la contention : plusieurs personnes travaillent sur une même zone sans se marcher dessus, parce qu'elles écrivent dans des fichiers différents. Sur une équipe de ~25 personnes, c'est la seule façon de tenir.
- Les prefabs extraits sont **réutilisables** entre niveaux — bénéfice qui dépasse le poids.
- Le chargement additif sert directement les budgets mémoire d'Android et de WebGL.
- Le dépôt cesse de grossir d'un ordre de grandeur à chaque itération de la forêt.

**Négatives / coûts**

- Le découpage de `LD-Forest.unity` est un chantier lourd, manuel, et il gèle la scène pendant sa durée. Il n'y a pas de raccourci.
- Le chargement additif ajoute de la complexité au code de chargement (plusieurs scènes à orchestrer, un état « tout est chargé » à gérer). Sur une équipe junior, cette complexité doit être **encapsulée une fois** dans le service de scènes (ADR-010) et jamais réécrite au cas par cas.
- L'éclairage baké se comporte différemment sur des scènes additives : c'est un piège classique, et il faut le tester tôt, pas le découvrir à la fin.
- Les 85 Mo déjà commités **restent dans l'historique** de Git. Le découpage arrête l'hémorragie, il ne la rembobine pas. Réécrire l'historique d'un dépôt partagé par 25 personnes n'est pas envisagé.

## Alternatives écartées

**Mettre les `.unity` en LFS.**
Écarté, et **formellement interdit**. Le fichier devient un pointeur opaque : le merge sémantique via `unityyamlmerge` devient impossible, et toute modification concurrente devient un « l'un ou l'autre » qui détruit le travail d'une des deux personnes. On échangerait une lenteur contre une perte de données. La scène est grosse **parce qu'**elle est en texte, et elle est en texte **pour** être mergeable : c'est un coût assumé, pas un accident à corriger.

**Passer les scènes en sérialisation binaire (`m_SerializationMode` autre que 2).**
Écarté, et c'est la même erreur que LFS par un autre chemin. Le binaire est plus compact et plus rapide à charger, mais il est **non mergeable et non diffable**. On perdrait à la fois le merge et la capacité de comprendre ce qu'un commit a changé. Force Text reste obligatoire (ADR-012).

**Compresser ou nettoyer le YAML de la scène (supprimer les objets inutiles, dégraisser).**
Écarté comme **solution** — accepté comme hygiène. Cela peut gagner quelques mégaoctets, mais ça ne change pas la structure : la scène reste un fichier unique que 25 personnes doivent se partager. Le conflit reste. C'est traiter le symptôme.

**Ne rien faire et interdire à plus d'une personne à la fois de toucher la scène (verrou social).**
Écarté. C'est de facto la situation actuelle, et elle ne tient pas : elle sérialise le travail de l'équipe sur son contenu principal, elle repose sur la mémoire de chacun, et elle échoue silencieusement — on ne découvre le double travail qu'au moment du merge, quand il est trop tard. Un verrou qu'aucun outil n'applique n'est pas un verrou.

**Verrouiller les scènes via `git lfs lock` sans stocker le contenu en LFS.**
Écarté. Le mécanisme de verrouillage de LFS suppose un serveur qui l'applique et une discipline d'outillage que le projet n'a pas — rappel : `unityyamlmerge` est déclaré dans `.gitattributes` et configuré sur **zéro poste**. Ajouter un mécanisme que personne ne configurera reproduit exactement l'échec qu'on est en train de documenter.
