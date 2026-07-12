#!/usr/bin/env python3
"""
Joue le manifeste. Unity FERME, arbre propre, un seul commit, QUE des deplacements.

Trois choses qu'un script naif rate, et qui coutent cher :

1. Le .meta doit suivre son fichier, dans le MEME commit. Un asset qui arrive sans son .meta
   se fait regenerer un GUID par Unity — un GUID DIFFERENT sur chaque poste. Toutes les
   references vers lui meurent, et elles meurent differemment chez chacun.

2. Les nouveaux dossiers ont besoin d'un .meta, et il faut le fabriquer NOUS-MEMES. Si on
   laisse Unity le faire, les 25 postes en generent chacun un, tous differents, et le premier
   qui commite gagne pendant que les autres recuperent un conflit. On fige donc un GUID
   deterministe, derive du chemin : meme dossier → meme GUID partout, des le premier clone.

3. Les .meta des dossiers vides restent traces alors que le dossier n'existe plus. Ce sont les
   orphelins. On les supprime dans le meme commit.

Par defaut : SIMULATION. Rien n'est ecrit sans --apply.

Usage :
    python3 .glimmer/reorg/apply.py                    # simulation
    python3 .glimmer/reorg/apply.py --apply            # deplace, sans committer
    python3 .glimmer/reorg/apply.py --apply --commit   # deplace et committe
"""
import argparse
import hashlib
import json
import os
import re
import subprocess
import sys

ICI = os.path.dirname(os.path.abspath(__file__))
MANIFESTE = os.path.join(ICI, "manifest.tsv")

ROUGE, VERT, JAUNE, GRAS, FIN = "\033[31m", "\033[32m", "\033[33m", "\033[1m", "\033[0m"

META_DOSSIER = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:{sp}
  assetBundleName:{sp}
  assetBundleVariant:{sp}
"""


def git(*args, check=True):
    r = subprocess.run(["git", "-c", "core.quotepath=false", *args],
                       capture_output=True, text=True,
                       env=dict(os.environ, GIT_LFS_SKIP_SMUDGE="1"))
    if check and r.returncode != 0:
        print(f"{ROUGE}git {' '.join(args[:2])} a echoue{FIN}\n{r.stderr}")
        sys.exit(1)
    return r.stdout


def lire_manifeste():
    moves = []
    with open(MANIFESTE, encoding="utf-8") as f:
        for n, ligne in enumerate(f, 1):
            ligne = ligne.rstrip("\n")
            if not ligne.strip() or ligne.lstrip().startswith("#"):
                continue
            if "\t" not in ligne:
                print(f"{ROUGE}manifest.tsv ligne {n} : pas de tabulation{FIN}\n  {ligne}")
                sys.exit(1)
            src, dst = ligne.split("\t", 1)
            moves.append((src.strip(), dst.strip()))
    return moves


def guid_de(chemin):
    """GUID deterministe derive du chemin. Meme dossier → meme GUID sur les 25 postes."""
    return hashlib.md5(("glimmer:" + chemin).encode("utf-8")).hexdigest()


def guids_existants():
    g = set()
    for dp, _, fn in os.walk("Assets"):
        for f in fn:
            if not f.endswith(".meta"):
                continue
            try:
                with open(os.path.join(dp, f), encoding="utf-8", errors="replace") as fh:
                    for _ in range(4):
                        m = re.match(r"guid:\s*([0-9a-f]{32})", fh.readline())
                        if m:
                            g.add(m.group(1))
                            break
            except OSError:
                pass
    return g


def valider(moves):
    """On verifie AVANT de toucher au disque. Un deplacement a moitie joue est le pire etat."""
    erreurs = []
    traces = set(git("ls-files", "Assets").splitlines())

    for src, dst in moves:
        if not os.path.isfile(src):
            erreurs.append(f"source absente : {src}")
            continue
        if src not in traces:
            erreurs.append(f"source non suivie par git : {src}")
        if not os.path.isfile(src + ".meta"):
            erreurs.append(f"source SANS .meta : {src}  (Unity lui regenererait un GUID "
                           f"different sur chaque poste)")
        if os.path.exists(dst):
            erreurs.append(f"destination deja occupee : {dst}")

    vus = {}
    for src, dst in moves:
        k = dst.lower()                     # NTFS : "Foo.png" et "foo.png" sont le meme fichier
        if k in vus:
            erreurs.append(f"collision : {src} et {vus[k]} visent {dst}")
        vus[k] = src
    return erreurs


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true", help="ecrire pour de vrai")
    ap.add_argument("--commit", action="store_true", help="committer apres deplacement")
    ap.add_argument("--base", default="Merge")
    args = ap.parse_args()

    repo = git("rev-parse", "--show-toplevel").strip()
    os.chdir(repo)

    moves = lire_manifeste()
    print(f"\n{GRAS}Manifeste : {len(moves)} deplacements{FIN}\n")

    # Rien en vol pendant qu'on travaille : la base a-t-elle bouge depuis le preflight ?
    if git("status", "--porcelain").strip():
        print(f"{ROUGE}L'arbre de travail n'est pas propre. Lancez preflight.py.{FIN}")
        return 1

    erreurs = valider(moves)
    if erreurs:
        print(f"{ROUGE}{GRAS}{len(erreurs)} probleme(s) — rien n'a ete touche.{FIN}\n")
        for e in erreurs[:40]:
            print("   " + e)
        if len(erreurs) > 40:
            print(f"   … et {len(erreurs) - 40} autres")
        return 1
    print(f"  {VERT}ok{FIN}  manifeste valide : sources suivies, .meta presents, "
          f"destinations libres")

    nouveaux_dossiers = sorted({os.path.dirname(d) for _, d in moves}
                               - {p.replace("\\", "/") for p, _, _ in os.walk("Assets")})
    # tous les ancetres, pas seulement le dossier feuille
    tous = set()
    for d in nouveaux_dossiers:
        p = d
        while p and p != "Assets" and not os.path.isdir(p):
            tous.add(p)
            p = os.path.dirname(p)
    nouveaux_dossiers = sorted(tous)

    deja = guids_existants()
    collisions_guid = [d for d in nouveaux_dossiers if guid_de(d) in deja]
    if collisions_guid:
        print(f"{ROUGE}Collision de GUID sur : {collisions_guid}{FIN}")
        return 1
    print(f"  {VERT}ok{FIN}  {len(nouveaux_dossiers)} nouveaux dossiers, "
          f"GUID deterministes sans collision")

    if not args.apply:
        print(f"\n{JAUNE}SIMULATION.{FIN} Rien n'a ete ecrit. Relancez avec --apply.\n")
        for src, dst in moves[:10]:
            print(f"   {src}\n     → {dst}")
        print(f"   … et {len(moves) - 10} autres\n")
        return 0

    # ---------------------------------------------------------------- on ecrit

    # Dernier instant ou l'arbre est intact : on fige la baseline de references cassees.
    # verify.py exigera qu'elle NE MONTE PAS. Mesurer apres coup ne prouverait rien.
    sys.path.insert(0, ICI)
    import verify
    idx, _ = verify.index_guids()
    base_refs = len(verify.refs_cassees(idx))
    with open(os.path.join(ICI, "baseline.json"), "w", encoding="utf-8") as f:
        json.dump({"refs_cassees": base_refs,
                   "sha_avant": git("rev-parse", "HEAD").strip(),
                   "deplacements": len(moves)}, f, indent=2)
    print(f"  {VERT}ok{FIN}  baseline figee : {base_refs} references non resolues avant "
          f"deplacement")

    print(f"\n{GRAS}Deplacement…{FIN}")
    for src, dst in moves:
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        git("mv", src, dst)
        git("mv", src + ".meta", dst + ".meta")
    print(f"  {VERT}ok{FIN}  {len(moves)} fichiers deplaces (avec leur .meta)")

    # .meta des nouveaux dossiers — fabriques par nous, identiques sur les 25 postes
    for d in nouveaux_dossiers:
        chemin = d + ".meta"
        if os.path.exists(chemin):
            continue
        with open(chemin, "w", encoding="utf-8", newline="\n") as f:
            f.write(META_DOSSIER.format(guid=guid_de(d), sp=" "))
        git("add", chemin)
    print(f"  {VERT}ok{FIN}  {len(nouveaux_dossiers)} .meta de dossier crees")

    # Nettoyage des dossiers vides et de leurs .meta. L'ORDRE compte, et il faut ITERER :
    # `git mv` laisse le dossier source vide sur le disque, donc son .meta a toujours une
    # cible et ne passe pas pour orphelin. Il faut d'abord retirer le dossier, ensuite son
    # .meta devient orphelin — ce qui vide le dossier PARENT, et ainsi de suite en remontant.
    # Une seule passe n'en attrape que la premiere couche. (Vecu : 0 orphelin detecte sur 76.)
    total_orphelins = 0
    for _ in range(20):
        for dp, _, _ in sorted(os.walk("Assets"), key=lambda x: -len(x[0])):
            try:
                os.rmdir(dp)
            except OSError:
                pass
        orphelins = [m for m in git("ls-files", "Assets").splitlines()
                     if m.endswith(".meta") and not os.path.exists(m[: -len(".meta")])]
        if not orphelins:
            break
        for m in orphelins:
            git("rm", "-q", "--", m)
        total_orphelins += len(orphelins)
    print(f"  {VERT}ok{FIN}  {total_orphelins} .meta orphelins supprimes, dossiers vides retires")

    if not args.commit:
        print(f"\n  {JAUNE}Non commite.{FIN} Verifiez avec verify.py, puis committez.")
        print(f"  {JAUNE}Attention{FIN} : les constantes de chemin en dur ne sont pas encore "
              f"corrigees.\n")
        return 0

    msg = ("refactor(assets): unifier l'arborescence selon ADR-007\n\n"
           f"{len(moves)} fichiers deplaces, .meta compris. Aucun contenu modifie : les GUID\n"
           "sont preserves, donc aucune scene, aucun prefab, aucun materiau ne change.\n\n"
           "ZPreprod/ etait un second arbre _Project parallele : il est dissous dans l'arbre\n"
           "canonique. Les scripts hors asmdef sont regroupes sous Scripts/Legacy/ sans changer\n"
           "d'assembly — les faire entrer dans un asmdef est une vague de code, pas de rangement.\n\n"
           "Les constantes de chemin ecrites en dur sont corrigees dans le commit suivant : un\n"
           "fichier a la fois deplace et modifie casserait la detection de renommage de git, et\n"
           "donc le rollback.")
    git("commit", "-q", "-m", msg)
    print(f"\n  {VERT}{GRAS}Commit 1 — deplacements.{FIN} "
          f"{git('rev-parse', '--short', 'HEAD').strip()}")

    # Second commit : les chemins ecrits en dur. Separe, et pas par elegance — git ne stocke pas
    # les renommages, il les DETECTE par similarite. Un fichier a la fois deplace et modifie
    # casse la detection, et avec elle le `git revert` qui est notre seul plan de repli.
    n = corriger_chemins()
    b = corriger_build_settings(moves)
    if n or b:
        git("commit", "-q", "-m",
            "fix(tools): repointer les chemins en dur apres le deplacement\n\n"
            f"{n} constantes du code nommaient un dossier qui a bouge. Rien ne l'aurait signale :\n"
            "git reussit, Unity compile, et l'outil renvoie zero resultat en silence. C'est la\n"
            "meme panne muette que FindAssets(searchInFolders) sur un dossier absent.\n\n"
            f"{b} chemin(s) de scene dans EditorBuildSettings. Unity les resout par GUID et\n"
            "reecrit le chemin tout seul a l'ouverture — donc si on ne le fait pas ici, les 25\n"
            "postes le regenerent chacun de leur cote et se disputent le fichier qui decide de ce\n"
            "qui part en build.")
        print(f"  {VERT}{GRAS}Commit 2 — chemins en dur.{FIN} "
              f"{git('rev-parse', '--short', 'HEAD').strip()}  "
              f"({n} constantes, {b} scenes de build)")

    print(f"\n  Etape suivante : python3 .glimmer/reorg/verify.py\n")
    return 0


def corriger_build_settings(moves):
    """EditorBuildSettings.asset garde le GUID ET le chemin de chaque scene du build.

    Unity resout par GUID, puis reecrit le chemin tout seul a la premiere ouverture. Le build
    n'est donc jamais casse — mais si on laisse Unity faire, les 25 postes regenerent chacun le
    fichier de leur cote et se disputent celui qui decide de ce qui part en build. On le fait
    une fois, ici, proprement.
    """
    chemin = "ProjectSettings/EditorBuildSettings.asset"
    if not os.path.exists(chemin):
        return 0
    with open(chemin, encoding="utf-8") as f:
        texte = f.read()
    n = 0
    for src, dst in moves:
        if not src.endswith(".unity"):
            continue
        avant = f"path: {src}\n"
        if avant in texte:
            texte = texte.replace(avant, f"path: {dst}\n")
            n += 1
    if n:
        with open(chemin, "w", encoding="utf-8", newline="\n") as f:
            f.write(texte)
        git("add", chemin)
    return n


def corriger_chemins():
    """Applique fixups.tsv. Chaque correction est verifiee avant d'etre ecrite : si la ligne
    ne contient pas ce qu'on attend, on s'arrete plutot que d'ecrire a l'aveugle."""
    chemin = os.path.join(ICI, "fixups.tsv")
    if not os.path.exists(chemin):
        return 0
    n = 0
    with open(chemin, encoding="utf-8") as f:
        for ligne in f:
            if ligne.startswith("#") or not ligne.strip():
                continue
            fichier, num, avant, apres = ligne.rstrip("\n").split("\t")
            num = int(num)
            with open(fichier, encoding="utf-8") as fh:
                lignes = fh.readlines()
            if avant not in lignes[num - 1]:
                print(f"{ROUGE}{fichier}:{num} ne contient pas \"{avant}\" — "
                      f"le fichier a change depuis le plan. Arret.{FIN}")
                sys.exit(1)
            lignes[num - 1] = lignes[num - 1].replace(avant, apres)
            with open(fichier, "w", encoding="utf-8", newline="") as fh:
                fh.writelines(lignes)
            git("add", fichier)
            n += 1
    return n


if __name__ == "__main__":
    sys.exit(main())
