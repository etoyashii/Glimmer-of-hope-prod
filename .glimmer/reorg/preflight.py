#!/usr/bin/env python3
"""
Controle avant reorganisation. Repond a UNE question : peut-on deplacer les fichiers
maintenant, sans casser le travail de quelqu'un ?

Aucune verification n'est "informative". Chacune bloque. Un preflight qui avertit sans
bloquer se fait ignorer le jour ou tout le monde est presse, c'est-a-dire le seul jour
ou il sert.

Usage :  python3 .glimmer/reorg/preflight.py [--base Merge]
Sortie  :  0 = on peut y aller. 1 = on ne peut pas, et on dit pourquoi.
"""
import argparse
import os
import subprocess
import sys

ROUGE = "\033[31m"
VERT = "\033[32m"
JAUNE = "\033[33m"
GRAS = "\033[1m"
FIN = "\033[0m"

echecs = []
avertissements = []


def git(*args, check=True):
    env = dict(os.environ, GIT_LFS_SKIP_SMUDGE="1")
    r = subprocess.run(["git", "-c", "core.quotepath=false", *args],
                       capture_output=True, text=True, env=env)
    if check and r.returncode != 0:
        return None
    return r.stdout.strip()


def bloque(titre, detail):
    echecs.append((titre, detail))
    print(f"  {ROUGE}BLOQUE{FIN}  {titre}")
    for ligne in detail.strip().splitlines():
        print(f"          {ligne}")


def ok(titre, detail=""):
    print(f"  {VERT}ok{FIN}      {titre}" + (f"  ({detail})" if detail else ""))


def alerte(titre, detail):
    avertissements.append((titre, detail))
    print(f"  {JAUNE}note{FIN}    {titre}")
    for ligne in detail.strip().splitlines():
        print(f"          {ligne}")


# --------------------------------------------------------------------------------------

def unity_ferme(repo):
    """Unity ouvert = il reecrit des .meta pendant qu'on deplace. Corruption garantie."""
    lock = os.path.join(repo, "Temp", "UnityLockfile")
    if not os.path.exists(lock):
        ok("Unity est ferme")
        return
    procs = subprocess.run(["tasklist.exe"], capture_output=True, text=True)
    tourne = any(l.lower().startswith("unity.exe") for l in procs.stdout.splitlines())
    if tourne:
        bloque("Unity est OUVERT",
               "Unity reecrit les .meta pendant qu'il tourne. Un deplacement fait sous son nez\n"
               "produit des GUID regeneres et des references mortes. Fermez Unity.")
    else:
        alerte("Lockfile Unity orphelin",
               f"{lock}\nAucun process Unity ne tourne : c'est un reliquat d'un crash. Sans danger.")


def arbre_propre():
    sale = git("status", "--porcelain")
    if sale:
        n = len(sale.splitlines())
        bloque(f"L'arbre de travail n'est pas propre ({n} fichier(s))",
               "Un deplacement doit etre le SEUL contenu de son commit. Git ne stocke pas les\n"
               "renommages, il les DETECTE par similarite : si du contenu change en meme temps que\n"
               "le chemin, la detection echoue et le rollback devient impossible.\n"
               "Committez ou remisez votre travail d'abord.")
    else:
        ok("Arbre de travail propre")


def a_jour_sur_base(base):
    git("fetch", "--quiet", "origin", check=False)
    tete = git("rev-parse", "HEAD")
    cible = git("rev-parse", f"origin/{base}")
    if tete is None or cible is None:
        bloque(f"Impossible de resoudre origin/{base}", "La branche de base n'existe pas sur le remote.")
        return
    if tete != cible:
        derriere = git("rev-list", "--count", f"HEAD..origin/{base}") or "?"
        devant = git("rev-list", "--count", f"origin/{base}..HEAD") or "?"
        bloque(f"HEAD n'est pas sur origin/{base}",
               f"{derriere} commit(s) de retard, {devant} d'avance.\n"
               f"Reorganiser un arbre perime, c'est reorganiser un fantome : le manifeste sera\n"
               f"genere a partir de fichiers qui ne sont plus la ou vous croyez.\n"
               f"Faites : git checkout -B reorg origin/{base}")
    else:
        ok(f"HEAD est exactement sur origin/{base}", tete[:7])


def rien_en_vol(base):
    """LE controle du jour J. Toute branche distante en avance sur la base = du travail
    qui devra etre rebase par-dessus le deplacement. C'est exactement ce qu'on veut eviter."""
    brut = git("for-each-ref", "--format=%(refname:short)", "refs/remotes/origin")
    if brut is None:
        bloque("Impossible de lister les branches distantes", "")
        return

    # %(refname:short) rend refs/remotes/origin/HEAD comme "origin" tout court, pas
    # "origin/HEAD" : sans le filtre sur la barre oblique, on compte deux fois la branche
    # par defaut sous un nom qui ne ressemble a rien.
    ignorees = {"origin/HEAD", f"origin/{base}"}
    en_vol = []
    for br in brut.splitlines():
        br = br.strip()
        if not br or br in ignorees or "/" not in br:
            continue
        devant = git("rev-list", "--count", f"origin/{base}..{br}")
        if devant is None:
            continue
        n = int(devant)
        if n > 0:
            auteur = git("log", "-1", "--format=%an, %cr", br) or "?"
            en_vol.append((br, n, auteur))

    if not en_vol:
        ok("Aucune branche en vol", f"toutes sont contenues dans origin/{base}")
        return

    lignes = [f"{n:>4} commit(s)  {br}   ({auteur})" for br, n, auteur in
              sorted(en_vol, key=lambda x: -x[1])]
    bloque(f"{len(en_vol)} branche(s) ont du travail non fusionne dans {base}",
           "\n".join(lignes) + "\n\n"
           "Chacune de ces branches devra etre rebasee PAR-DESSUS le deplacement. Git suit les\n"
           "renommages, donc en theorie ca passe. En pratique, nos scenes font jusqu'a 85 Mo de\n"
           "YAML et sont mergees en texte brut tant que unityyamlmerge n'est pas installe.\n\n"
           "Faites atterrir ces branches dans " + base + " AVANT de deplacer quoi que ce soit.\n"
           "C'est tout l'objet de la fenetre de gel.")


def driver_de_merge():
    drv = git("config", "--get", "merge.unityyamlmerge.driver", check=False)
    if drv:
        ok("unityyamlmerge est configure", drv.split()[0])
        return
    bloque("unityyamlmerge n'est PAS configure sur ce poste",
           "Le driver est declare dans .gitattributes mais il n'est branche nulle part : nos\n"
           "scenes et nos .meta sont donc mergees en TEXTE BRUT.\n"
           "On en a deja la preuve : un .meta a ete commite avec ses marqueurs de conflit et\n"
           "deux GUID dedans.\n"
           "Marche a suivre : Docs/setup-git.md")


def objets_lfs_presents(repo):
    """Un checkout avec des objets LFS manquants ecrit des POINTEURS de 130 octets a la
    place des textures. Le deplacement paraitrait reussi, et le jeu serait detruit."""
    brut = git("lfs", "ls-files", "-l", "HEAD", check=False)
    if brut is None:
        alerte("git-lfs indisponible", "Impossible de verifier les objets LFS.")
        return
    manquants = 0
    total = 0
    for ligne in brut.splitlines():
        oid = ligne.split(" ", 1)[0].strip()
        if len(oid) < 4:
            continue
        total += 1
        chemin = os.path.join(repo, ".git", "lfs", "objects", oid[0:2], oid[2:4], oid)
        if not os.path.exists(chemin):
            manquants += 1
    if manquants:
        bloque(f"{manquants} objet(s) LFS manquant(s) sur {total}",
               "Un checkout ecrirait des pointeurs texte a la place des vraies textures.\n"
               "Lancez : git lfs fetch origin HEAD")
    else:
        ok(f"Les {total} objets LFS sont locaux", "un checkout est sur")


def pas_de_marqueurs_de_conflit():
    brut = git("grep", "-l", "-E", r"^<{7}|^={8}$|^>{7}", "--", "Assets/", check=False)
    if brut:
        bloque("Des fichiers contiennent des marqueurs de conflit Git",
               brut + "\n\nUn fichier a deux GUID n'a pas d'identite. Le deplacer propagerait\n"
                      "l'ambiguite au lieu de la resoudre.")
    else:
        ok("Aucun marqueur de conflit dans Assets/")


# --------------------------------------------------------------------------------------

def main():
    ap = argparse.ArgumentParser(description="Controle avant reorganisation des dossiers.")
    ap.add_argument("--base", default="Merge", help="branche de reference (defaut : Merge)")
    args = ap.parse_args()

    repo = git("rev-parse", "--show-toplevel")
    if not repo:
        print("Pas dans un depot git.")
        return 2
    os.chdir(repo)

    print(f"\n{GRAS}Controle avant reorganisation{FIN}   base = origin/{args.base}\n")

    rien_en_vol(args.base)
    a_jour_sur_base(args.base)
    arbre_propre()
    unity_ferme(repo)
    driver_de_merge()
    objets_lfs_presents(repo)
    pas_de_marqueurs_de_conflit()

    print()
    if echecs:
        print(f"{ROUGE}{GRAS}NE PAS DEPLACER.{FIN} {len(echecs)} controle(s) bloquent.\n")
        return 1

    if avertissements:
        print(f"{VERT}{GRAS}Feu vert.{FIN} ({len(avertissements)} note(s) sans gravite)\n")
    else:
        print(f"{VERT}{GRAS}Feu vert.{FIN} Tout est en ordre.\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
