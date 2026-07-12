#!/usr/bin/env python3
"""Verificateur des conventions d'assets de Glimmer of Hope.

Lit .glimmer/conventions.json, prend une liste de chemins, applique les regles.
Ne lit JAMAIS le contenu d'un fichier : uniquement des noms de chemins. Aucun octet
d'asset n'est ouvert, donc aucun telechargement Git LFS n'est declenche.

Stdlib uniquement, Python 3.8+.

Usage courant
-------------
  # Verifier ce que change ma branche par rapport a Maain (usage local)
  python3 .glimmer/check-conventions.py --diff origin/Maain

  # Verifier des fichiers precis
  python3 .glimmer/check-conventions.py Assets/_Project/Art/SM_Rock.fbx

  # Verifier tout le depot, sans la baseline : c'est la dette totale
  python3 .glimmer/check-conventions.py --all --no-baseline

  # Regenerer la baseline apres une campagne de rangement
  python3 .glimmer/check-conventions.py --baseline
"""

import argparse
import json
import os
import re
import sys

DEFAULT_CONFIG = ".glimmer/conventions.json"

# Dossiers qu'on ne traverse jamais : generes par Unity ou par l'outillage.
SKIPPED_DIRS = {".git", ".github", ".glimmer", ".idea", ".vs", "Library", "Temp",
                "Obj", "obj", "Build", "Builds", "Logs", "UserSettings", "MemoryCaptures"}

ERROR = "error"
WARNING = "warning"


class Violation:
    def __init__(self, path, rule, severity, message, fix):
        self.path = path
        self.rule = rule
        self.severity = severity
        self.message = message
        self.fix = fix


# --------------------------------------------------------------------------
# Lecture des chemins
# --------------------------------------------------------------------------

def unquote_git_path(line):
    """Git peut renvoyer un chemin entre guillemets avec des octets echappes (\\303\\251).

    La CI passe core.quotepath=false pour l'eviter, mais un poste mal configure peut
    encore produire cette forme : on la decode ici plutot que de laisser passer en
    silence un chemin accentue, c'est-a-dire exactement le chemin qu'on veut attraper.
    """
    if len(line) < 2 or not line.startswith('"') or not line.endswith('"'):
        return line
    body = line[1:-1]
    out = bytearray()
    i = 0
    while i < len(body):
        char = body[i]
        if char != "\\":
            out.extend(char.encode("utf-8"))
            i += 1
            continue
        nxt = body[i + 1] if i + 1 < len(body) else ""
        if nxt.isdigit():                      # octet en octal : \303
            out.append(int(body[i + 1:i + 4], 8))
            i += 4
        else:                                  # \" \\ \t ...
            out.extend({"n": b"\n", "t": b"\t"}.get(nxt, nxt.encode("utf-8")))
            i += 2
    return out.decode("utf-8", errors="replace")


def read_lines(source):
    """Lit un fichier de chemins ('-' = stdin). Tolere tout encodage bancal."""
    if source == "-":
        raw = sys.stdin.buffer.read()
    else:
        with open(source, "rb") as handle:
            raw = handle.read()
    text = raw.decode("utf-8", errors="surrogateescape")
    return [unquote_git_path(line.strip()) for line in text.splitlines() if line.strip()]


def git_diff_paths(base):
    """Chemins ajoutes/copies/renommes par la branche courante, et renommages/suppressions.

    core.quotepath=false est indispensable : sans lui git echappe les chemins accentues
    en \\303\\251 et ils passeraient au travers du controle, precisement sur les fichiers
    qu'on veut attraper.
    """
    import subprocess
    common = ["git", "-c", "core.quotepath=false", "diff", "-M"]
    added = subprocess.run(
        common + ["--name-only", "--diff-filter=ACR", "%s...HEAD" % base],
        capture_output=True, check=True).stdout
    moved = subprocess.run(
        common + ["--name-status", "--diff-filter=RD", "%s...HEAD" % base],
        capture_output=True, check=True).stdout
    decode = lambda blob: blob.decode("utf-8", errors="surrogateescape").splitlines()
    return ([unquote_git_path(x.strip()) for x in decode(added) if x.strip()],
            [unquote_git_path(x) for x in decode(moved) if x.strip()])


def walk_repo(root, assets_root):
    """Liste tous les chemins suivis sous Assets/, sans lire un seul octet de contenu."""
    paths = []
    base = os.path.join(root, assets_root)
    for current, dirs, files in os.walk(base):
        dirs[:] = sorted(d for d in dirs if d not in SKIPPED_DIRS)
        for name in sorted(files):
            full = os.path.join(current, name)
            paths.append(os.path.relpath(full, root).replace(os.sep, "/"))
    return paths


# --------------------------------------------------------------------------
# Baseline
# --------------------------------------------------------------------------

class Baseline:
    """Dette figee.

    Deux formes d'entree, deux effets differents :

    - chemin exact : ce fichier existe deja et viole deja les regles. Il est exempte
      de tout. On ne casse pas le travail de quelqu'un pour une regle arrivee apres.

    - chemin terminant par '/' : sous-arbre gele. Le dossier lui-meme porte deja une
      faute dans son nom ("Proto LD", "Erwan", racine legacy). Un fichier ajoute demain
      la-dedans n'est PAS exempte : seuls les segments de chemin dont il HERITE cessent
      de lui etre reproches. Son propre nom, son propre prefixe, sa propre place restent
      controles. On ne reproche a personne le nom d'un dossier qu'il n'a pas cree, et on
      ne laisse pour autant pas la zone en dette servir de tapis sous lequel balayer.
    """

    def __init__(self, entries):
        self.exact = set(e for e in entries if not e.endswith("/"))
        self.prefixes = tuple(sorted((e for e in entries if e.endswith("/")),
                                     key=len, reverse=True))

    def exempts(self, path):
        return path in self.exact

    def frozen_prefix(self, path):
        """Le plus long sous-arbre gele qui contient ce chemin, sinon ''."""
        for prefix in self.prefixes:
            if path.startswith(prefix):
                return prefix
        return ""


# --------------------------------------------------------------------------
# Regles
# --------------------------------------------------------------------------

class Checker:
    def __init__(self, config, baseline, asmdef_dirs=None, root="."):
        self.rules = config["rules"]
        self.assets_root = config.get("assets_root", "Assets")
        self.baseline = baseline
        self.exempt = tuple(p + "/" for p in config["naming_exempt_prefixes"]["paths"])
        self.asmdef_dirs = asmdef_dirs          # None = on cherche sur le disque
        self.root = root
        self.default_names = self._compile_default_names()
        self.duplicate = re.compile(self.rules["duplicate_suffix"]["pattern"])
        self.people = [p.lower() for p in self.rules["person_folders"]["people"]]

    def _compile_default_names(self):
        # "New Terrain" attrape aussi "New Terrain", "New Terrain 4", "new terrain 12".
        return [(name, re.compile(r"^%s(\s*\d+)?$" % re.escape(name), re.IGNORECASE))
                for name in self.rules["default_names"]["names"]]

    def _rule(self, name):
        return self.rules[name]

    def _add(self, out, path, rule_name, message, fix=None):
        rule = self._rule(rule_name)
        out.append(Violation(path, rule_name, rule["severity"], message,
                             fix or rule.get("fix", "")))

    # -- regles de structure ------------------------------------------------

    def check_root_and_layout(self, path, parts, out):
        if len(parts) == 2:                      # Assets/<fichier>
            self._add(out, path, "assets_root_files",
                      "fichier pose a la racine d'Assets/")
            return False
        root_dir = parts[1]
        if root_dir not in self._rule("allowed_roots")["allowed"]:
            self._add(out, path, "allowed_roots",
                      "racine \"%s\" non autorisee dans Assets/" % root_dir)
            return False
        return True

    def check_names(self, path, parts, out, first_segment=1):
        """Regles de nommage, sur les segments a partir de first_segment.

        first_segment saute les dossiers dont le fichier HERITE (sous-arbre gele) :
        leur nom n'est pas de son fait et il ne peut pas le corriger seul.
        """
        if path.startswith(self.exempt):
            return
        forbidden = self._rule("forbidden_characters")["forbidden"]

        for index, segment in enumerate(parts[first_segment:], start=first_segment):
            is_last = (index == len(parts) - 1)

            if self.duplicate.search(segment):
                self._add(out, path, "duplicate_suffix",
                          "\"%s\" porte un suffixe de duplication" % segment)

            offenders, fixes = [], []
            for spec in forbidden.values():
                if re.search(spec["pattern"], segment):
                    # une parenthese deja signalee comme duplication ne l'est pas deux fois
                    if spec["label"] == "parenthese" and self.duplicate.search(segment):
                        continue
                    offenders.append(spec["label"])
                    fixes.append(spec["fix"])
            if offenders:
                self._add(out, path, "forbidden_characters",
                          "\"%s\" contient : %s" % (segment, ", ".join(offenders)),
                          fix=" ".join(fixes))

            stem = os.path.splitext(segment)[0] if is_last else segment
            for name, pattern in self.default_names:
                if pattern.match(stem):
                    self._add(out, path, "default_names",
                              "\"%s\" est un nom par defaut d'Unity" % segment)
                    break

    def check_person(self, path, parts, out):
        """Volontairement appliquee a TOUS les dossiers du chemin, meme herites.

        Un dossier-prenom qui existe deja reste dans la baseline, mais y AJOUTER un
        fichier est un acte neuf, et son auteur peut le corriger seul : il lui suffit
        de deposer son fichier ailleurs. La zone en dette ne doit pas continuer a
        grossir en silence.
        """
        if path.startswith(self.exempt):
            return
        for segment in parts[1:-1]:
            if self._is_person(segment):
                self._add(out, path, "person_folders",
                          "le dossier \"%s\" est nomme d'apres une personne" % segment)

    def _is_person(self, segment):
        low = segment.lower()
        return any(low == p or re.match(r"^%s[\s_\-0-9]" % re.escape(p), low)
                   for p in self.people)

    def check_prefix(self, path, parts, out):
        if path.startswith(self.exempt):
            return
        rule = self._rule("asset_prefix")
        filename = parts[-1]
        extension = os.path.splitext(filename)[1].lower()
        expected = rule["prefixes"].get(extension)
        if not expected or filename.startswith(tuple(expected)):
            return
        described = ", ".join("%s (%s)" % (p, rule["labels"].get(p, "")) for p in expected)
        self._add(out, path, "asset_prefix",
                  "\"%s\" devrait commencer par : %s" % (filename, described))

    def check_code(self, path, parts, out):
        if not path.lower().endswith(".cs"):
            return
        allowed = tuple(p + "/" for p in self._rule("code_roots")["allowed"])
        if not path.startswith(allowed):
            self._add(out, path, "code_roots",
                      "script C# hors des dossiers de code declares")
            return
        if not self._has_asmdef_above(path):
            self._add(out, path, "asmdef_required",
                      "aucun .asmdef ne couvre ce script")

    def _has_asmdef_above(self, path):
        """Remonte les dossiers parents jusqu'a Assets/ en cherchant un .asmdef.

        Ne lit que des noms de fichiers (scandir), jamais un contenu.
        """
        directory = os.path.dirname(path)
        while directory and directory != self.assets_root:
            if self.asmdef_dirs is not None:
                if directory in self.asmdef_dirs:
                    return True
            else:
                absolute = os.path.join(self.root, directory)
                try:
                    if any(e.name.endswith(".asmdef") for e in os.scandir(absolute)):
                        return True
                except OSError:
                    pass                          # dossier absent (fichier supprime) : on remonte
            directory = os.path.dirname(directory)
        return False

    # -- point d'entree par chemin ------------------------------------------

    def check(self, path):
        out = []
        if not path.startswith(self.assets_root + "/"):
            return out
        if path.endswith(".meta"):
            return out       # un .meta porte le nom de son asset : corriger l'asset suffit
        if self.baseline.exempts(path):
            return out       # fichier deja en dette avant la mise en place des regles

        parts = path.split("/")
        frozen = self.baseline.frozen_prefix(path)

        if not frozen:
            if not self.check_root_and_layout(path, parts, out):
                return out   # mal range : inutile d'empiler le nommage par-dessus

        # Sous un sous-arbre gele, on ne controle que ce que ce fichier introduit.
        inherited = frozen.count("/") if frozen else 1
        self.check_names(path, parts, out, first_segment=max(inherited, 1))
        self.check_person(path, parts, out)
        self.check_prefix(path, parts, out)
        self.check_code(path, parts, out)
        return out

    def check_moved(self, status_lines):
        """Regle des dossiers sanctuarises, sur les renommages et suppressions."""
        out = []
        rule = self._rule("sanctuary_paths")
        for line in status_lines:
            fields = line.split("\t")
            status, old = fields[0], fields[1]
            new = fields[2] if len(fields) > 2 else None
            for sanctuary in rule["paths"]:
                inside = old == sanctuary or old.startswith(sanctuary + "/")
                if not inside:
                    continue
                if status.startswith("R") and new and not (
                        new == sanctuary or new.startswith(sanctuary + "/")):
                    self._add(out, new, "sanctuary_paths",
                              "\"%s\" sort du dossier sanctuarise %s" % (old, sanctuary))
                elif status == "D" and old == sanctuary:
                    self._add(out, old, "sanctuary_paths",
                              "suppression du dossier sanctuarise %s" % sanctuary)
        return out


def build_asmdef_index(paths):
    """Dossiers contenant un .asmdef, deduits d'une liste de chemins (pas du disque)."""
    return set(os.path.dirname(p) for p in paths if p.endswith(".asmdef"))


# --------------------------------------------------------------------------
# Sorties
# --------------------------------------------------------------------------

def report_text(violations):
    if not violations:
        print("Conventions : aucune violation sur les fichiers verifies.")
        return
    for violation in violations:
        marker = "ERREUR " if violation.severity == ERROR else "WARNING"
        print("%s %s" % (marker, violation.path))
        print("        regle    : %s" % violation.rule)
        print("        probleme : %s" % violation.message)
        print("        corriger : %s" % violation.fix)
        print()


def report_github(violations):
    """Annotations GitHub : la violation s'affiche dans le diff de la pull request."""
    for violation in violations:
        level = "error" if violation.severity == ERROR else "warning"
        # %0A = saut de ligne dans une annotation ; les :,% du message sont echappes.
        message = "[%s] %s%%0A%%0AComment corriger : %s" % (
            violation.rule,
            violation.message.replace("%", "%25").replace("\n", " "),
            violation.fix.replace("%", "%25").replace("\n", " "))
        print("::%s file=%s,line=1::%s" % (level, violation.path, message))


def summarize(violations, checked):
    errors = sum(1 for v in violations if v.severity == ERROR)
    warnings = len(violations) - errors
    print("")
    print("Conventions : %d fichier(s) verifie(s), %d erreur(s), %d avertissement(s)."
          % (checked, errors, warnings))
    if errors:
        print("Regles et corrections detaillees : .glimmer/conventions.json")
        print("Une violation legitime se discute en equipe et se corrige dans les regles,")
        print("elle ne s'ajoute pas a la baseline : la baseline ne fait que retrecir.")
    return errors


# --------------------------------------------------------------------------
# Mode baseline
# --------------------------------------------------------------------------

def generate_baseline(config, paths):
    """Fige l'etat actuel : retourne la liste des chemins en dette.

    Passe 1 : reperer les DOSSIERS deja fautifs par leur propre nom ("Proto LD", un
    dossier-prenom, une racine legacy comme ZPreprod). Ils deviennent des sous-arbres
    geles, pour qu'un fichier ajoute demain la-dedans ne se voie pas reprocher le nom
    d'un dossier qu'il n'a pas cree.

    Passe 2 : rejouer toutes les regles avec ces sous-arbres actifs, et figer nommement
    chaque fichier qui viole encore quelque chose. Un fichier sous un sous-arbre gele
    reste donc controle sur son propre nom : la passe 2 le figera s'il est deja fautif
    aujourd'hui, mais un fichier NEUF ajoute demain au meme endroit, lui, sera signale.
    """
    assets_root = config.get("assets_root", "Assets")
    asmdef_dirs = build_asmdef_index(paths)
    naked = Checker(config, Baseline([]), asmdef_dirs=asmdef_dirs)

    faulty_dir_rules = ("allowed_roots", "person_folders", "duplicate_suffix",
                        "forbidden_characters", "default_names")
    frozen_dirs = []
    for directory in sorted(set(os.path.dirname(p) for p in paths)):
        if directory == assets_root or any(directory.startswith(f) for f in frozen_dirs):
            continue
        # On sonde le dossier avec un nom de fichier neutre : les fautes qui remontent
        # ne peuvent venir que des segments du dossier lui-meme.
        faults = naked.check(directory + "/_probe.txt")
        basename = os.path.basename(directory)
        if any(v.rule in faulty_dir_rules
               and (v.rule == "allowed_roots" or basename in v.message)
               for v in faults):
            frozen_dirs.append(directory + "/")

    frozen = Checker(config, Baseline(frozen_dirs), asmdef_dirs=asmdef_dirs)
    entries = set(frozen_dirs)
    for path in paths:
        if frozen.check(path):
            entries.add(path)
    return sorted(entries)


def write_baseline(config_path, config, entries):
    config["baseline"] = entries
    with open(config_path, "w", encoding="utf-8") as handle:
        json.dump(config, handle, indent=2, ensure_ascii=False)
        handle.write("\n")


# --------------------------------------------------------------------------
# Entree
# --------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Verifie les conventions d'assets (noms de fichiers uniquement).")
    parser.add_argument("paths", nargs="*", help="chemins a verifier, relatifs a la racine")
    parser.add_argument("--config", default=DEFAULT_CONFIG)
    parser.add_argument("--root", default=".", help="racine du depot")
    parser.add_argument("--paths-from", help="fichier listant les chemins ('-' = stdin)")
    parser.add_argument("--moved-from", help="fichier listant les renommages (git --name-status)")
    parser.add_argument("--diff", metavar="BASE",
                        help="verifier le diff de la branche courante contre BASE")
    parser.add_argument("--all", action="store_true", help="verifier tout Assets/")
    parser.add_argument("--baseline", action="store_true",
                        help="regenerer la baseline depuis l'etat actuel du depot")
    parser.add_argument("--no-baseline", action="store_true",
                        help="ignorer la baseline (montre la dette totale)")
    parser.add_argument("--format", choices=["text", "github"], default="text")
    args = parser.parse_args()

    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    config_path = args.config if os.path.isabs(args.config) \
        else os.path.join(args.root, args.config)
    with open(config_path, encoding="utf-8") as handle:
        config = json.load(handle)
    assets_root = config.get("assets_root", "Assets")

    if args.baseline:
        paths = read_lines(args.paths_from) if args.paths_from \
            else walk_repo(args.root, assets_root)
        entries = generate_baseline(config, paths)
        write_baseline(config_path, config, entries)
        print("Baseline regeneree : %d entree(s) figee(s) dans %s"
              % (len(entries), config_path))
        return 0

    moved = []
    if args.diff:
        paths, moved = git_diff_paths(args.diff)
    elif args.all:
        paths = walk_repo(args.root, assets_root)
    elif args.paths_from:
        paths = read_lines(args.paths_from)
    else:
        paths = args.paths

    if args.moved_from:
        moved += read_lines(args.moved_from)

    baseline = Baseline([] if args.no_baseline else config["baseline"])
    asmdef_dirs = build_asmdef_index(paths) if (args.all or args.baseline) else None
    checker = Checker(config, baseline, asmdef_dirs=asmdef_dirs, root=args.root)

    violations = []
    for path in paths:
        violations.extend(checker.check(path))
    violations.extend(checker.check_moved(moved))
    violations.sort(key=lambda v: (v.severity != ERROR, v.path, v.rule))

    if args.format == "github":
        report_github(violations)
    else:
        report_text(violations)
    errors = summarize(violations, len(paths))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
