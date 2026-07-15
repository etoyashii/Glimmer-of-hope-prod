#!/usr/bin/env python3
"""
Derive le manifeste de deplacement a partir de l'arbre REEL et de rules.json.

Pourquoi deriver plutot que figer un manifeste tout fait : entre aujourd'hui et le jour du
gel, l'arbre va bouger (6 branches en vol). Un manifeste fige serait perime avant d'etre
joue, et il echouerait en silence sur les chemins disparus. Les REGLES, elles, survivent.

Ce que ce script refuse de mettre dans le manifeste :
  - tout .cs dont l'ASSEMBLY changerait (un asmdef ne peut pas referencer Assembly-CSharp :
    le fichier compilerait plus, et aucun git mv ne le dirait) ;
  - toute collision (deux sources pour une meme destination), y compris les collisions qui
    n'existent que sur un systeme insensible a la casse — le depot vit sur NTFS ;
  - tout ce qui touche un sanctuaire Unity (Resources/, StreamingAssets/, Plugins/…).

Et ce qu'il signale sans pouvoir le refuser : les CHEMINS ECRITS EN DUR dans le C#. Deplacer
un dossier nomme dans une chaine litterale ne casse ni git, ni la compilation, ni Unity —
l'outil se contente de ne plus rien trouver, en silence. C'est la panne la plus couteuse du
lot parce que rien ne la signale. Ces constantes doivent etre corrigees dans un SECOND commit
(jamais le meme : git ne stocke pas les renommages, il les DETECTE, et un fichier a la fois
deplace et modifie casse la detection).

Sortie :  .glimmer/reorg/manifest.tsv   (source <TAB> destination — editable a la main)
          .glimmer/reorg/report.md      (ce qu'on deplace, ce qu'on refuse, et pourquoi)
          .glimmer/reorg/fixups.tsv     (les constantes de code a corriger apres coup)
"""
import json
import os
import re
import subprocess
import sys
from collections import defaultdict

ICI = os.path.dirname(os.path.abspath(__file__))
REGLES = os.path.join(ICI, "rules.json")
MANIFESTE = os.path.join(ICI, "manifest.tsv")
RAPPORT = os.path.join(ICI, "report.md")
FIXUPS = os.path.join(ICI, "fixups.tsv")

LITTERAL = re.compile(r'"(Assets/[^"]*)"')


def chemins_en_dur():
    """Les chaines "Assets/..." ecrites en dur dans le C#.

    Piege : le C# d'Unity est plein de chaines qui RESSEMBLENT a des chemins d'assets sans en
    etre — [MenuItem("Assets/Brush Tool/...")], [CreateAssetMenu(menuName = "Assets/Create/...")].
    Les distinguer par mots-cles serait fragile. Le seul critere fiable est materiel : un chemin
    d'ASSET s'ancre sur un dossier qui EXISTE. "Assets/Brush Tool/" n'existe nulle part sur le
    disque — c'est un menu. "Assets/_Project/Art/Mesh" existe — c'est une ancre.

    Rend : (fichier, ligne, litteral, ancre) ou ancre = le plus long prefixe qui existe vraiment.
    """
    trouves = []
    for dp, _, fn in os.walk("Assets"):
        d = dp.replace("\\", "/")
        for f in fn:
            if not f.endswith(".cs"):
                continue
            chemin = d + "/" + f
            try:
                with open(chemin, encoding="utf-8", errors="replace") as fh:
                    lignes = fh.readlines()
            except OSError:
                continue
            for i, ligne in enumerate(lignes, 1):
                for lit in LITTERAL.findall(ligne):
                    segs = lit.rstrip("/").split("/")
                    ancre = None
                    for n in range(len(segs), 1, -1):
                        cand = "/".join(segs[:n])
                        if os.path.isdir(cand):
                            ancre = cand
                            break
                    if ancre and ancre != "Assets":
                        trouves.append((chemin, i, lit, ancre))
    return trouves


# ---------------------------------------------------------------------------- assemblies

class Assemblies:
    """Qui compile quoi. Modele minimal mais fidele des deux regles Unity qui nous concernent :

    1. Le dossier contenant un .asmdef, et tout ce qui est dessous, forme une assembly nommee.
    2. HORS asmdef, un dossier nomme "Editor" (a n'importe quelle profondeur) bascule ses
       scripts dans Assembly-CSharp-Editor ; le reste va dans Assembly-CSharp.

    La consequence qui commande tout ce script : Assembly-CSharp voit les asmdef, mais un
    asmdef ne peut PAS voir Assembly-CSharp. Deplacer un script de l'un vers l'autre n'est
    donc pas un deplacement, c'est une migration de code.
    """

    def __init__(self, racine):
        self.dossiers = []
        for dp, _, fn in os.walk(racine):
            for f in fn:
                if f.endswith(".asmdef"):
                    d = dp.replace("\\", "/")
                    self.dossiers.append((d, f[: -len(".asmdef")]))
        self.dossiers.sort(key=lambda x: -len(x[0]))

    def de(self, chemin):
        for d, nom in self.dossiers:
            if chemin == d or chemin.startswith(d + "/"):
                return nom
        segments = chemin.split("/")[:-1]
        if "Editor" in segments:
            return "Assembly-CSharp-Editor"
        return "Assembly-CSharp"


# ---------------------------------------------------------------------------------- regles

class Regles:
    def __init__(self, chemin):
        with open(chemin, encoding="utf-8") as f:
            d = json.load(f)
        s = d["sanctuaires"]
        self.sanct_prefixes = tuple(s["prefixes"])
        self.sanct_magiques = set(s["dossiers_magiques"])
        self.regles = d["regles"]
        for r in self.regles:
            if "source_regex" in r:
                r["_re"] = re.compile(r["source_regex"])
        ren = d["renommages"]
        self.ren_segments = ren["segments_chemin"]
        self.ren_noms = ren["noms_fichiers"]

    def est_sanctuaire(self, chemin, exemptions):
        for e in exemptions:
            if chemin.startswith(e):
                return None
        for p in self.sanct_prefixes:
            if chemin.startswith(p):
                return p
        segments = chemin.split("/")[1:-1]
        for seg in segments:
            if seg in self.sanct_magiques:
                return seg + "/ (dossier magique Unity)"
        return None

    def destination(self, chemin):
        for r in self.regles:
            if "map" in r:
                if chemin in r["map"]:
                    return r["map"][chemin], r
                continue
            if "source_exact" in r:
                if chemin in r["source_exact"]:
                    return r["destination_dir"] + chemin.rsplit("/", 1)[1], r
                continue
            m = r["_re"].match(chemin)
            if m:
                return r["destination"].format(**m.groupdict()), r
        return None, None

    def renomme(self, chemin):
        """ADR-008 : uniquement les fautes d'orthographe. Aucun renommage de masse."""
        d, _, nom = chemin.rpartition("/")
        segs = d.split("/")
        segs = [self.ren_segments.get(s, s) for s in segs]
        for faux, vrai in self.ren_noms.items():
            if faux in nom:
                nom = nom.replace(faux, vrai)
        return "/".join(segs) + "/" + nom


# ------------------------------------------------------------------------------------ main

def main():
    repo = subprocess.run(["git", "rev-parse", "--show-toplevel"],
                          capture_output=True, text=True).stdout.strip()
    if not repo:
        print("Pas dans un depot git.")
        return 2
    os.chdir(repo)

    regles = Regles(REGLES)
    asm = Assemblies("Assets")

    # Les regles qui SORTENT d'un sanctuaire de facon deliberee et justifiee (fusion des
    # deux racines Resources : le chemin apres "Resources/" est preserve a l'octet pres,
    # donc Resources.Load() resout exactement pareil).
    exemptions = ("Assets/ZPreprod/Resources/",)

    deplacements = []          # (source, destination, regle)
    refus_assembly = []        # (source, destination, asm_avant, asm_apres)
    sanctuaires = defaultdict(int)
    immobiles = []

    for dp, _, fn in os.walk("Assets"):
        d = dp.replace("\\", "/")
        for f in sorted(fn):
            if f.endswith(".meta") or f.startswith("."):
                continue
            src = d + "/" + f

            garde = regles.est_sanctuaire(src, exemptions)
            if garde:
                sanctuaires[garde] += 1
                continue

            dst, regle = regles.destination(src)
            if not dst:
                immobiles.append(src)
                continue
            dst = regles.renomme(dst)
            if dst == src:
                immobiles.append(src)
                continue

            if src.endswith(".cs"):
                avant, apres = asm.de(src), asm.de(dst)
                if avant != apres:
                    refus_assembly.append((src, dst, avant, apres))
                    continue

            deplacements.append((src, dst, regle))

    # --- collisions. Deux sources pour une meme destination = on ne devine pas, on refuse.
    #     Insensible a la casse : le depot vit sur NTFS, "Foo.png" et "foo.png" y sont le
    #     MEME fichier. Un manifeste valide sous Linux peut donc detruire un asset sous
    #     Windows. Le comparateur doit etre celui du systeme de fichiers, pas celui de Python.
    par_dest = defaultdict(list)
    for src, dst, _ in deplacements:
        par_dest[dst.lower()].append((src, dst))

    existants = set()
    for dp, _, fn in os.walk("Assets"):
        for f in fn:
            existants.add((dp.replace("\\", "/") + "/" + f).lower())

    sources_qui_bougent = {s.lower() for s, _, _ in deplacements}

    collisions = []
    ecrasements = []
    for cle, lot in par_dest.items():
        if len(lot) > 1:
            collisions.append(lot)
        # une destination qui existe deja ET qui ne bouge pas = on ecraserait un asset vivant
        elif cle in existants and cle not in sources_qui_bougent:
            ecrasements.append(lot[0])

    bloques = {s for lot in collisions for s, _ in lot} | {s for s, _ in ecrasements}
    retenus = [(s, dd, r) for s, dd, r in deplacements if s not in bloques]

    # --- chemins en dur : le mode de defaillance qui ne fait AUCUN bruit.
    #     Un dossier nomme dans une chaine litterale et deplace par le manifeste : git reussit,
    #     Unity compile, l'outil renvoie zero resultat et ne dit rien. Rien ne le signale.
    #     On calcule, pour chaque ancre citee dans le code, ou elle atterrit.
    mouvement = {s: d for s, d, _ in retenus}
    fixups = []
    for fichier, ligne, lit, ancre in chemins_en_dur():
        # On ne patche JAMAIS du code tiers. FMOD cherche "Assets/Editor/FMODMigrationUtil.cs"
        # (un reliquat de ses propres migrations, absent de ce depot) : corriger cette ligne
        # serait modifier le code d'un vendor, que sa prochaine mise a jour ecraserait.
        if regles.est_sanctuaire(fichier, ()):
            continue
        # l'ancre bouge-t-elle ? on le sait par ses fichiers : si tous partent ailleurs, oui.
        dedans = [(s, d) for s, d in mouvement.items() if s.startswith(ancre + "/")]
        if not dedans:
            continue
        prefixes = {d[: len(d) - len(s) + len(ancre)] for s, d in dedans
                    if s[len(ancre):] == d[len(d) - len(s) + len(ancre):]}
        if len(prefixes) != 1:
            continue                      # l'ancre eclate en plusieurs destinations : a la main
        neuf = prefixes.pop()
        if neuf == ancre:
            continue
        fixups.append((fichier, ligne, lit, lit.replace(ancre, neuf, 1),
                       mouvement.get(fichier, fichier)))

    with open(FIXUPS, "w", encoding="utf-8", newline="\n") as f:
        f.write("# Constantes de chemin a corriger APRES le commit de deplacement.\n")
        f.write("# fichier(apres deplacement) <TAB> ligne <TAB> avant <TAB> apres\n")
        f.write("# Jamais dans le meme commit que les deplacements : un fichier a la fois\n")
        f.write("# deplace ET modifie casse la detection de renommage de git.\n\n")
        for _, ligne, avant, apres, apres_fichier in sorted(fixups, key=lambda x: (x[4], x[1])):
            f.write(f"{apres_fichier}\t{ligne}\t{avant}\t{apres}\n")

    # --- ecriture du manifeste
    with open(MANIFESTE, "w", encoding="utf-8", newline="\n") as f:
        f.write("# Manifeste de deplacement — genere par plan.py, RELU PAR UN HUMAIN.\n")
        f.write("# Un deplacement par ligne :  source <TAB> destination\n")
        f.write("# Supprimer une ligne = ne pas deplacer ce fichier. Commenter avec #.\n")
        f.write("# Le .meta suit son fichier automatiquement : ne PAS le lister ici.\n")
        f.write(f"# {len(retenus)} deplacements.\n\n")
        for src, dst, r in sorted(retenus, key=lambda x: (x[2]["id"], x[0])):
            f.write(f"{src}\t{dst}\n")

    # --- rapport
    par_regle = defaultdict(list)
    for src, dst, r in retenus:
        par_regle[r["id"]].append((src, dst, r["raison"]))

    L = []
    L.append("# Plan de reorganisation\n")
    L.append(f"**{len(retenus)} fichiers deplaces.** "
             f"{sum(sanctuaires.values())} intouchables, {len(immobiles)} deja bien ranges, "
             f"{len(refus_assembly)} refuses, {len(collisions)} collisions.\n")
    L.append("Le `.meta` de chaque fichier suit son fichier : les GUID ne changent pas, donc "
             "**aucune scene, aucun prefab, aucun materiau ne peut changer.** C'est verifiable "
             "sans ouvrir Unity : `verify.py` exige un diff YAML **vide**.\n")

    if refus_assembly:
        L.append("\n## Refuses : le fichier changerait d'assembly\n")
        L.append("Un `.asmdef` **ne peut pas** referencer `Assembly-CSharp` — c'est une regle "
                 "Unity, pas un reglage. Deplacer ces fichiers vers un dossier couvert par un "
                 "asmdef ne les rangerait pas : ca les **decompilerait**. Ce n'est pas du "
                 "rangement, c'est une migration de code, et elle merite sa propre PR.\n")
        L.append("| fichier | assembly aujourd'hui | assembly a l'arrivee |")
        L.append("|---|---|---|")
        for src, dst, a, b in sorted(refus_assembly):
            L.append(f"| `{src}` | {a} | **{b}** |")

    if fixups:
        L.append("\n## Chemins en dur : a corriger dans un SECOND commit\n")
        L.append("Ces constantes nomment un dossier que le manifeste deplace. Rien ne va "
                 "protester : git reussira, Unity compilera, et l'outil renverra **zero "
                 "resultat sans un mot**. C'est la panne la plus chere du lot, precisement "
                 "parce qu'elle est muette.\n")
        L.append("Corriger **apres** le commit de deplacement, jamais dedans : un fichier a la "
                 "fois deplace et modifie casse la detection de renommage de git, et donc le "
                 "rollback.\n")
        L.append("| fichier (a sa nouvelle place) | ligne | avant | apres |")
        L.append("|---|---|---|---|")
        for _, ln, avant, apres, apres_f in sorted(fixups, key=lambda x: (x[4], x[1])):
            L.append(f"| `{apres_f}` | {ln} | `{avant}` | `{apres}` |")

    if collisions:
        L.append("\n## Refuses : collision de destination\n")
        L.append("Deux fichiers atterriraient au meme endroit. On ne devine pas lequel garder.\n")
        for lot in collisions:
            L.append(f"\n**→ `{lot[0][1]}`**")
            for src, _ in lot:
                L.append(f"- `{src}`")

    if ecrasements:
        L.append("\n## Refuses : la destination est deja occupee\n")
        for src, dst in ecrasements:
            L.append(f"- `{src}` → `{dst}` *(existe deja et ne bouge pas)*")

    L.append("\n## Ce qui bouge, regle par regle\n")
    for rid in sorted(par_regle, key=lambda k: -len(par_regle[k])):
        lot = par_regle[rid]
        L.append(f"\n### `{rid}` — {len(lot)} fichiers")
        L.append(f"*{lot[0][2]}*\n")
        for src, dst, _ in sorted(lot)[:6]:
            L.append(f"- `{src}`  →  `{dst}`")
        if len(lot) > 6:
            L.append(f"- *… et {len(lot) - 6} autres*")

    L.append("\n## Sanctuaires — jamais deplaces\n")
    L.append("| chemin | fichiers |")
    L.append("|---|---|")
    for k in sorted(sanctuaires):
        L.append(f"| `{k}` | {sanctuaires[k]} |")

    with open(RAPPORT, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(L) + "\n")

    print(f"\n  {len(retenus):>4}  deplacements retenus       → {os.path.relpath(MANIFESTE, repo)}")
    print(f"  {len(refus_assembly):>4}  refuses (changement d'assembly)")
    print(f"  {len(collisions):>4}  refuses (collision)")
    print(f"  {len(ecrasements):>4}  refuses (destination occupee)")
    print(f"  {sum(sanctuaires.values()):>4}  sanctuarises")
    print(f"  {len(immobiles):>4}  deja bien ranges")
    print(f"  {len(fixups):>4}  chemins en dur a corriger  → {os.path.relpath(FIXUPS, repo)}")
    print(f"\n  Rapport : {os.path.relpath(RAPPORT, repo)}\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
