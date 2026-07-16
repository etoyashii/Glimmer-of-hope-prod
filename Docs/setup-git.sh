#!/usr/bin/env bash
set -uo pipefail

ok()   { printf '[OK]        %s\n' "$1"; }
warn() { printf '[ATTENTION] %s\n' "$1"; }
fail()
{
    printf '\n[ECHEC]     %s\n\n' "$1"
    printf "Rien n'a ete casse. Corrige le point ci-dessus et relance ce script.\n"
    printf "Si tu bloques, envoie une capture de ce terminal sur le Discord.\n\n"
    exit 1
}

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here" && git rev-parse --show-toplevel 2>/dev/null)" \
    || fail "Git ne trouve pas de depot ici. Laisse ce script dans le dossier du projet Glimmer."

pv="$root/ProjectSettings/ProjectVersion.txt"
[ -f "$pv" ] || fail "ProjectVersion.txt introuvable. Ce dossier n'est pas le projet Unity."
version="$(sed -n 's/^m_EditorVersion: *\(.*\)$/\1/p' "$pv" | tr -d '\r' | head -1)"
[ -n "$version" ] || fail "Impossible de lire la version d'Unity dans ProjectVersion.txt."

roots=()
case "$(uname -s)" in
    Darwin) roots=("$HOME/Applications/Unity/Hub/Editor" "/Applications/Unity/Hub/Editor") ;;
    *)      roots=("$HOME/Unity/Hub/Editor" "/opt/unity/Hub/Editor" "$HOME/.local/share/unity3d/Hub/Editor") ;;
esac

found=()
for r in "${roots[@]}"
do
    [ -d "$r" ] || continue
    while IFS= read -r line
    do
        found+=("$line")
    done < <(find "$r" -name 'UnityYAMLMerge' -type f 2>/dev/null)
done

[ "${#found[@]}" -gt 0 ] \
    || fail "Aucun UnityYAMLMerge trouve. Installe Unity $version depuis Unity Hub, puis relance."

exe=""
for f in "${found[@]}"
do
    case "$f" in
        */"$version"/*) exe="$f"; break ;;
    esac
done

if [ -z "$exe" ]
then
    exe="$(printf '%s\n' "${found[@]}" | sort -r | head -1)"
    warn "Unity $version (la version du projet) n'est pas installee ici."
    warn "J'utilise : $exe"
    warn "Ca marche dans la plupart des cas. Installe quand meme $version des que possible."
fi

[ -x "$exe" ] || fail "UnityYAMLMerge trouve mais non executable : $exe"
ok "UnityYAMLMerge trouve : $exe"
ok "Depot : $root"
ok "Version Unity du projet : $version"

value="\"$exe\" merge -h -p --force %O %B %A %A"
git config --global --replace-all merge.unityyamlmerge.name "Unity SmartMerge"
git config --global --replace-all merge.unityyamlmerge.driver "$value"
git config --global --replace-all merge.unityyamlmerge.recursive binary

printf '\nVerification...\n'

mapfile -t stored < <(git config --global --get-all merge.unityyamlmerge.driver)
[ "${#stored[@]}" -ne 0 ] || fail "Le driver n'a pas ete enregistre dans ta config git."
if [ "${#stored[@]}" -gt 1 ]
then
    fail "Ta config contient ${#stored[@]} drivers en double. Lance : git config --global --unset-all merge.unityyamlmerge.driver puis relance."
fi
if [ "${stored[0]}" != "$value" ]
then
    printf '\n  Attendu : %s\n  Trouve  : %s\n' "$value" "${stored[0]}"
    fail "La valeur enregistree ne correspond pas. Ne t'en sers pas : elle fusionnerait mal."
fi
ok "Valeur enregistree correcte (verifiee caractere par caractere)."

scene="$(cd "$root" && git ls-files '*.unity' | head -1)"
if [ -n "$scene" ]
then
    attr="$(cd "$root" && git check-attr merge -- "$scene")"
    case "$attr" in
        *"merge: unityyamlmerge") ok "Git applique bien le driver aux scenes, prefabs et .meta." ;;
        *) fail "Git n'applique pas le driver aux fichiers Unity ($attr). Le .gitattributes manque a la racine du depot." ;;
    esac
else
    warn "Aucune scene trouvee pour tester - verification partielle."
fi

cat <<'EOF'

==================================================
  C'est bon. Ta machine est configuree.
==================================================

A savoir :
 - A faire UNE FOIS par machine (pas par projet, pas par branche).
 - Sans ca, git fusionne les scenes et les .meta comme du texte,
   et fabrique des fichiers casses sans rien afficher.
 - Si tu reinstalles ou deplaces Unity, relance ce script.

EOF
