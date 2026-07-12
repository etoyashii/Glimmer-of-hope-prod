#!/usr/bin/env python3
"""
Prouve qu'on n'a rien casse. Sans ouvrir Unity, et sans faire confiance a l'absence d'erreur.

L'invariant central est gratuit et binaire : UN DEPLACEMENT CORRECT NE MODIFIE AUCUN OCTET DE
YAML. Les references Unity sont par GUID, le GUID vit dans le .meta, le .meta a suivi son
fichier — donc aucune scene, aucun prefab, aucun materiau ne PEUT avoir change. Si une seule
ligne de YAML bouge, c'est que des GUID ont ete reecrits : on arrete tout.

Les autres controles couvrent les pannes qui ne font aucun bruit :
  - un GUID en double (deux .meta identiques = Unity choisit au hasard) ;
  - un .meta orphelin, ou un asset sans .meta ;
  - une reference cassee de plus qu'avant (le compte ne doit JAMAIS monter) ;
  - un chemin ecrit en dur dans le code qui ne resout plus rien. Celui-la est le pire :
    git reussit, Unity compile, l'outil renvoie zero resultat et se tait.

Usage :  python3 .glimmer/reorg/verify.py [--base Merge]
"""
import argparse
import json
import os
import re
import subprocess
import sys

ICI = os.path.dirname(os.path.abspath(__file__))
BASELINE = os.path.join(ICI, "baseline.json")

ROUGE, VERT, JAUNE, GRAS, FIN = "\033[31m", "\033[32m", "\033[33m", "\033[1m", "\033[0m"

YAML_EXT = (".unity", ".prefab", ".mat", ".asset", ".controller", ".playable",
            ".anim", ".terrainlayer", ".shadergraph", ".shadersubgraph", ".overrideController")

GUID_RE = re.compile(rb"guid: ([0-9a-f]{32})")
META_GUID_RE = re.compile(r"guid:\s*([0-9a-f]{32})")
LITTERAL = re.compile(r'"(Assets/[^"]*)"')

echecs = []


def git(*args, check=True):
    r = subprocess.run(["git", "-c", "core.quotepath=false", *args],
                       capture_output=True, text=True,
                       env=dict(os.environ, GIT_LFS_SKIP_SMUDGE="1"))
    if check and r.returncode != 0:
        return None
    return r.stdout


def bloque(titre, detail=""):
    echecs.append(titre)
    print(f"  {ROUGE}ECHEC{FIN}   {titre}")
    for l in str(detail).strip().splitlines()[:25]:
        if l:
            print(f"          {l}")


def ok(titre, detail=""):
    print(f"  {VERT}ok{FIN}      {titre}" + (f"  ({detail})" if detail else ""))


# ---------------------------------------------------------------------------------------

def index_guids():
    """guid → asset. Construit depuis les .meta, seule source de verite des GUID."""
    idx, doublons = {}, []
    for dp, _, fn in os.walk("Assets"):
        d = dp.replace("\\", "/")
        for f in fn:
            if not f.endswith(".meta"):
                continue
            m = d + "/" + f
            try:
                with open(m, encoding="utf-8", errors="replace") as fh:
                    tete = "".join(fh.readline() for _ in range(4))
            except OSError:
                continue
            g = META_GUID_RE.search(tete)
            if not g:
                continue
            g = g.group(1)
            if g in idx:
                doublons.append((g, idx[g], m))
            idx[g] = m
    return idx, doublons


def refs_cassees(idx):
    """Compte les GUID cites dans le YAML que personne ne definit.

    Le nombre absolu n'a aucun sens (une bonne partie sont des GUID de packages, hors du
    depot). Ce qui a un sens, c'est qu'il NE MONTE PAS.
    """
    connus = set(idx)
    manquants = set()
    for dp, _, fn in os.walk("Assets"):
        d = dp.replace("\\", "/")
        for f in fn:
            if not f.lower().endswith(tuple(e.lower() for e in YAML_EXT)):
                continue
            try:
                with open(d + "/" + f, "rb") as fh:
                    for g in GUID_RE.findall(fh.read()):
                        g = g.decode()
                        if g not in connus:
                            manquants.add(g)
            except OSError:
                pass
    return manquants


def diff_yaml_vide(base):
    """L'invariant. `git diff -M --numstat` : un renommage pur affiche 0 ajout, 0 suppression.

    La base n'est PAS Merge : la branche est empilee sur des PR qui, elles, ont legitimement
    modifie du YAML. La seule base qui a un sens ici est le commit juste AVANT le deplacement.

    Le -z n'est pas une coquetterie. Sans lui, git compacte les renommages en `a/{x => y}/f.asset`
    — et l'accolade fermante peut tomber APRES l'extension (`{x.meta => y.meta}`). Un endswith()
    sur cette chaine renvoie alors False, et le fichier est saute EN SILENCE : le controle
    annonce "aucun YAML modifie" sans avoir regarde. Avec -z, git emet source et destination
    comme deux champs separes, bruts. On ne se fie pas a un formatage d'affichage pour decider
    si le jeu est casse. (Vecu, sur ce script meme.)
    """
    brut = subprocess.run(["git", "-c", "core.quotepath=false", "diff", "-M", "--numstat", "-z",
                           base],
                          capture_output=True,
                          env=dict(os.environ, GIT_LFS_SKIP_SMUDGE="1"))
    if brut.returncode != 0:
        bloque(f"impossible de comparer a {base}")
        return

    champs = brut.stdout.decode("utf-8", "replace").split("\0")
    coupables = []
    i = 0
    while i < len(champs):
        c = champs[i]
        if not c.strip():
            i += 1
            continue
        plus, moins, reste = c.split("\t", 2)
        if reste == "":                               # renommage : src puis dst, champs a part
            chemin = champs[i + 2]
            i += 3
        else:
            chemin = reste
            i += 1
        if plus == "-" and moins == "-":
            continue                                  # binaire (LFS) : le diff textuel n'a pas de sens
        # L'invariant porte sur les ASSETS. ProjectSettings/EditorBuildSettings.asset change
        # legitimement (les chemins des scenes du build) et a son propre controle plus bas ;
        # le reste de ProjectSettings/ est verifie juste apres. On ne mélange pas les deux :
        # un fourre-tout finirait par tout excuser.
        if not chemin.startswith("Assets/"):
            continue
        if not chemin.lower().endswith(tuple(e.lower() for e in YAML_EXT)):
            continue
        if plus != "0" or moins != "0":
            coupables.append(f"+{plus} -{moins}  {chemin}")

    if coupables:
        bloque(f"{len(coupables)} fichier(s) YAML ont CHANGE DE CONTENU",
               "\n".join(coupables) + "\n\n"
               "Un deplacement pur ne peut pas modifier un octet de YAML : les GUID ne bougent\n"
               "pas. Si du contenu a change, c'est que des GUID ont ete reecrits — ou qu'un\n"
               "commit de contenu s'est glisse dans le commit de deplacement. On arrete tout.")
    else:
        ok("aucun YAML n'a change de contenu", "l'invariant tient")


OPAQUES = (".bundle/", ".framework/", ".app/", ".plugin/")


def attend_un_meta(chemin):
    """Unity ne cree PAS de .meta pour tout. Deux exceptions, et il faut les connaitre sous
    peine de crier au loup :
      - les fichiers caches (.gitkeep) : Unity les ignore ;
      - l'interieur d'un .bundle / .framework macOS : Unity voit le bundle comme UN asset
        opaque, un seul .meta a sa racine, rien dedans.
    """
    nom = chemin.rsplit("/", 1)[-1]
    if nom.startswith("."):
        return False
    return not any(o in chemin + "/" for o in OPAQUES)


def metas_coherents():
    orphelins, sans_meta = [], []
    traces = set((git("ls-files", "Assets") or "").splitlines())
    for chemin in traces:
        if chemin.endswith(".meta"):
            if not os.path.exists(chemin[: -len(".meta")]):
                orphelins.append(chemin)
        elif attend_un_meta(chemin):
            if not os.path.exists(chemin + ".meta"):
                sans_meta.append(chemin)
    if orphelins:
        bloque(f"{len(orphelins)} .meta orphelin(s)", "\n".join(orphelins[:10]))
    else:
        ok("aucun .meta orphelin")
    if sans_meta:
        bloque(f"{len(sans_meta)} asset(s) SANS .meta",
               "\n".join(sans_meta[:10]) + "\n\nUnity leur regenererait un GUID DIFFERENT sur "
               "chaque poste.")
    else:
        ok("chaque asset a son .meta")


def chemins_en_dur_resolvent(base):
    """Le controle qui attrape la panne muette. Rien d'autre ne la detecte : git reussit,
    Unity compile, et l'outil renvoie zero resultat sans un mot.

    Le bon critere n'est PAS "ce chemin existe-t-il" — le depot contient deja des chemins
    morts, anterieurs a nous (SO_PhotoTask pointe sur un Prefabs/UI/Camera/ qui n'a jamais
    existe). Les signaler ici noierait le vrai signal.

    Le critere est : L'ANCRE A-T-ELLE PERDU DE LA PROFONDEUR. L'ancre d'un litteral, c'est
    son plus long prefixe qui existe vraiment. On la calcule AVANT (depuis git) et APRES
    (depuis le disque). Si elle recule, c'est nous qui venons de lui couper le sol sous les
    pieds. Si elle est deja courte des le depart, le chemin etait deja mort : ce n'est pas
    notre affaire, et on ne bloque pas la reorg pour ca.
    """
    avant = set((git("ls-tree", "-d", "-r", "--name-only", base, "--", "Assets") or "").split("\n"))
    avant.discard("")
    maintenant = {dp.replace("\\", "/") for dp, _, _ in os.walk("Assets")}

    def profondeur_ancre(lit, dossiers):
        segs = lit.rstrip("/").split("/")
        for n in range(len(segs), 0, -1):
            if "/".join(segs[:n]) in dossiers:
                return n
        return 0

    morts, deja_morts = [], 0
    for dp, _, fn in os.walk("Assets"):
        d = dp.replace("\\", "/")
        if d.startswith("Assets/Plugins"):
            continue                                   # code tiers : on ne le patche jamais
        for f in fn:
            if not f.endswith(".cs"):
                continue
            chemin = d + "/" + f
            try:
                lignes = open(chemin, encoding="utf-8", errors="replace").readlines()
            except OSError:
                continue
            for i, ligne in enumerate(lignes, 1):
                for lit in LITTERAL.findall(ligne):
                    if "{" in lit:
                        continue                       # chaine interpolee : indecidable ici
                    pa = profondeur_ancre(lit, avant)
                    pm = profondeur_ancre(lit, maintenant)
                    if pa > pm:
                        morts.append(f"{chemin}:{i}  \"{lit}\"   ancre {pa} → {pm}")
                    elif pm <= 1 and len(lit.split("/")) > 2 and lit.rstrip("/") not in maintenant:
                        deja_morts += 1

    if morts:
        bloque(f"{len(morts)} chemin(s) en dur ont perdu leur ancre", "\n".join(morts) +
               "\n\nCes constantes nomment un dossier qu'on vient de deplacer. L'outil "
               "renverra zero\nresultat sans un mot. Appliquez fixups.tsv.")
    else:
        ok("aucun chemin en dur n'a perdu son ancre",
           f"{deja_morts} deja mort(s) avant nous — hors perimetre")


def project_settings_intacts(base):
    """Un seul fichier de ProjectSettings/ a le droit de bouger : EditorBuildSettings, et
    seulement pour les chemins des scenes du build. Tout le reste — les couches physiques, la
    qualite, le pipeline de rendu — n'a AUCUNE raison de changer dans une reorganisation. Si
    ca bouge, quelque chose s'est glisse dans le commit."""
    brut = git("diff", "--numstat", base, "--", "ProjectSettings") or ""
    coupables = [l for l in brut.splitlines()
                 if l and not l.endswith("ProjectSettings/EditorBuildSettings.asset")]
    if coupables:
        bloque(f"{len(coupables)} fichier(s) de ProjectSettings/ ont change", "\n".join(coupables))
    else:
        ok("ProjectSettings/ intact", "hors les chemins des scenes du build")


def scenes_du_build():
    """Les 3 scenes du build. Si l'une ne resout plus, il n'y a plus de jeu a livrer."""
    chemin = "ProjectSettings/EditorBuildSettings.asset"
    if not os.path.exists(chemin):
        return
    manquantes, total = [], 0
    for ligne in open(chemin, encoding="utf-8"):
        m = re.match(r"\s*path:\s*(Assets/.+\.unity)\s*$", ligne)
        if not m:
            continue
        total += 1
        if not os.path.exists(m.group(1)):
            manquantes.append(m.group(1))
    if manquantes:
        bloque(f"{len(manquantes)} scene(s) du build introuvable(s)", "\n".join(manquantes))
    else:
        ok(f"les {total} scenes du build resolvent")


def racine_assets_propre():
    vrac = [f for f in os.listdir("Assets")
            if os.path.isfile("Assets/" + f) and not f.endswith(".meta")]
    if vrac:
        bloque(f"{len(vrac)} fichier(s) a la racine d'Assets/ (ADR-007)", "\n".join(vrac[:12]))
    else:
        ok("aucun fichier a la racine d'Assets/", "ADR-007 respecte")


# ---------------------------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default=None,
                    help="commit d'avant le deplacement (defaut : lu dans baseline.json)")
    args = ap.parse_args()

    repo = (git("rev-parse", "--show-toplevel") or "").strip()
    if not repo:
        return 2
    os.chdir(repo)

    socle = {}
    if os.path.exists(BASELINE):
        with open(BASELINE, encoding="utf-8") as f:
            socle = json.load(f)

    base = args.base or socle.get("sha_avant")
    if not base:
        print(f"{ROUGE}Pas de base.{FIN} Lancez apply.py d'abord (il fige baseline.json), "
              f"ou passez --base <sha>.\n")
        return 2

    print(f"\n{GRAS}Verification apres reorganisation{FIN}   base = {base[:12]}\n")

    diff_yaml_vide(base)
    metas_coherents()

    idx, doublons = index_guids()
    if doublons:
        bloque(f"{len(doublons)} GUID en double",
               "\n".join(f"{g} : {a}  ET  {b}" for g, a, b in doublons[:8]))
    else:
        ok(f"{len(idx)} GUID, tous uniques")

    manquants = refs_cassees(idx)
    ref = socle.get("refs_cassees")
    if ref is None:
        print(f"  {JAUNE}note{FIN}    {len(manquants)} references non resolues "
              f"(pas de baseline : lancez apply.py avant)")
    elif len(manquants) > ref:
        bloque(f"les references cassees ont AUGMENTE : {ref} → {len(manquants)}",
               "Une reorganisation ne peut pas en creer : les GUID sont preserves.\n"
               "Chaque nouvelle reference cassee est un asset qu'on vient de perdre.")
    else:
        ok(f"references non resolues : {len(manquants)}",
           f"baseline {ref} — ne monte pas")

    chemins_en_dur_resolvent(base)
    project_settings_intacts(base)
    scenes_du_build()
    racine_assets_propre()

    print()
    if echecs:
        print(f"{ROUGE}{GRAS}NE PAS MERGER.{FIN} {len(echecs)} controle(s) en echec.\n")
        return 1
    print(f"{VERT}{GRAS}Tout tient.{FIN} Aucune reference perdue, aucun YAML touche.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
