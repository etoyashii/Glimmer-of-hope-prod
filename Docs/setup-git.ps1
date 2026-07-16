param([string]$UnityPath = '')

$ErrorActionPreference = 'Stop'
$script:Searched = @()

function Say($msg)  { Write-Host $msg }
function Ok($msg)   { Write-Host "[OK]        $msg" -ForegroundColor Green }
function Warn($msg) { Write-Host "[ATTENTION] $msg" -ForegroundColor Yellow }
function Fail($msg)
{
    Write-Host ""
    Write-Host "[ECHEC]     $msg" -ForegroundColor Red
    Write-Host ""
    Write-Host "Rien n'a ete casse. Corrige le point ci-dessus et relance ce script."
    Write-Host "Si tu bloques, envoie une capture de cette fenetre sur le Discord."
    Write-Host ""
    exit 1
}

function Get-RepoRoot
{
    Push-Location $PSScriptRoot
    $root = & git rev-parse --show-toplevel 2>$null
    Pop-Location
    if (-not $root)
    {
        Fail "Git ne trouve pas de depot ici. Laisse ce script dans le dossier du projet Glimmer."
    }
    return $root.Trim()
}

function Get-ProjectUnityVersion($root)
{
    $file = Join-Path $root 'ProjectSettings\ProjectVersion.txt'
    if (-not (Test-Path $file))
    {
        Fail "ProjectVersion.txt introuvable. Ce dossier n'est pas le projet Unity."
    }
    $line = Select-String -Path $file -Pattern 'm_EditorVersion:\s*(\S+)' | Select-Object -First 1
    if (-not $line)
    {
        Fail "Impossible de lire la version d'Unity dans ProjectVersion.txt."
    }
    return $line.Matches[0].Groups[1].Value
}

function From-UnityExe($unityExe)
{
    if (-not $unityExe) { return $null }
    $candidate = Join-Path (Split-Path $unityExe -Parent) 'Data\Tools\UnityYAMLMerge.exe'
    if (Test-Path $candidate) { return $candidate }
    return $null
}

function Get-FromRunningUnity
{
    $script:Searched += "Unity en cours d'execution"
    $p = Get-Process -Name 'Unity' -ErrorAction SilentlyContinue |
         Where-Object { $_.Path } | Select-Object -First 1
    if ($p) { return (From-UnityExe $p.Path) }
    return $null
}

function Get-FromEditorLog
{
    $logs = @(
        (Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor.log')
        (Join-Path $env:LOCALAPPDATA 'Unity\Editor\Editor-prev.log')
    )
    foreach ($log in $logs)
    {
        $script:Searched += $log
        if (-not (Test-Path $log)) { continue }
        $hit = Select-String -Path $log -Pattern '([A-Za-z]:\\[^\r\n"]*?\\Unity\.exe)' -List -ErrorAction SilentlyContinue
        if ($hit)
        {
            $exe = From-UnityExe $hit.Matches[0].Groups[1].Value
            if ($exe) { return $exe }
        }
    }
    return $null
}

function Get-HubRoots
{
    $roots = @()
    $sec = Join-Path $env:APPDATA 'UnityHub\secondaryInstallPath.json'
    if (Test-Path $sec)
    {
        $p = (Get-Content $sec -Raw).Trim().Trim('"')
        if ($p) { $roots += $p }
    }
    foreach ($d in [System.IO.DriveInfo]::GetDrives())
    {
        if ($d.DriveType -ne 'Fixed' -or -not $d.IsReady) { continue }
        $r = $d.RootDirectory.FullName
        foreach ($sub in @('Program Files\Unity', 'Program Files (x86)\Unity', 'Unity', 'UnityHub', 'Editors'))
        {
            $roots += [System.IO.Path]::Combine($r, $sub)
        }
    }
    return $roots | Select-Object -Unique
}

function Get-FromHubRoots
{
    $found = @()
    foreach ($root in (Get-HubRoots))
    {
        $script:Searched += $root
        if (-not (Test-Path $root)) { continue }
        $found += Get-ChildItem -Path $root -Filter 'UnityYAMLMerge.exe' -Recurse -Depth 7 -ErrorAction SilentlyContinue |
                  Select-Object -ExpandProperty FullName
    }
    return $found
}

function Resolve-Given($given)
{
    $given = $given.Trim('"').Trim()
    if (-not (Test-Path $given))
    {
        Fail "Le chemin que tu m'as donne n'existe pas : $given"
    }
    if ($given -like '*UnityYAMLMerge.exe') { return $given }
    if ($given -like '*Unity.exe')
    {
        $e = From-UnityExe $given
        if ($e) { return $e }
    }
    foreach ($suffix in @('Data\Tools\UnityYAMLMerge.exe', 'Editor\Data\Tools\UnityYAMLMerge.exe'))
    {
        $c = [System.IO.Path]::Combine($given, $suffix)
        if (Test-Path $c) { return $c }
    }
    $deep = Get-ChildItem -Path $given -Filter 'UnityYAMLMerge.exe' -Recurse -Depth 7 -ErrorAction SilentlyContinue |
            Select-Object -ExpandProperty FullName -First 1
    if ($deep) { return $deep }
    Fail "Rien trouve sous le chemin que tu m'as donne : $given"
}

function Show-SearchedAndFail($version)
{
    Say ""
    Say "  J'ai cherche ici :"
    foreach ($s in ($script:Searched | Select-Object -Unique)) { Say "    - $s" }
    Say ""
    Say "  Si Unity est ailleurs, relance en me donnant le chemin. Exemple :"
    Say "    setup-git.bat `"D:\Unity\Hub\Editor\$version`""
    Say ""
    Say "  Pour trouver ce chemin : Unity Hub > Installs > roue dentee de la version"
    Say "  $version > Show in Explorer. Copie le chemin de la barre d'adresse."
    Fail "UnityYAMLMerge.exe introuvable automatiquement."
}

function Find-Merger($version)
{
    $exe = Get-FromRunningUnity
    if ($exe) { return $exe }

    $exe = Get-FromEditorLog
    if ($exe) { return $exe }

    $found = @(Get-FromHubRoots)
    if ($found.Count -eq 0) { Show-SearchedAndFail $version }

    $exact = $found | Where-Object { $_ -like "*\$version\*" } | Select-Object -First 1
    if ($exact) { return $exact }

    $fallback = $found | Sort-Object -Descending | Select-Object -First 1
    Warn "Unity $version (la version du projet) n'est pas installee ici."
    Warn "J'utilise : $fallback"
    Warn "Ca marche dans la plupart des cas. Installe quand meme $version des que possible."
    return $fallback
}

function Build-DriverValue($exe)
{
    $path = $exe.Replace('\', '/')
    return '"' + $path + '" merge -h -p --force %O %B %A %A'
}

function Set-Driver($exe)
{
    $path = $exe.Replace('\', '/')
    $escaped = '\"' + $path + '\" merge -h -p --force %O %B %A %A'
    & git config --global --replace-all merge.unityyamlmerge.name "Unity SmartMerge"
    & git config --global --replace-all merge.unityyamlmerge.driver $escaped
    & git config --global --replace-all merge.unityyamlmerge.recursive binary
}

function Test-Exe($exe)
{
    if (-not (Test-Path $exe))
    {
        Fail "UnityYAMLMerge.exe introuvable : $exe"
    }
    $info = (Get-Item $exe).VersionInfo
    if ($info.FileDescription -notmatch 'YAML' -and $info.ProductName -notmatch 'Unity')
    {
        Fail "Le fichier trouve n'est pas UnityYAMLMerge : $exe"
    }
    Ok "UnityYAMLMerge trouve et valide."
}

function Test-StoredValue($expected)
{
    $lines = @(& git config --global --get-all merge.unityyamlmerge.driver)
    if ($lines.Count -eq 0)
    {
        Fail "Le driver n'a pas ete enregistre dans ta config git."
    }
    if ($lines.Count -gt 1)
    {
        Fail "Ta config git contient $($lines.Count) drivers en double. Lance : git config --global --unset-all merge.unityyamlmerge.driver puis relance ce script."
    }
    if ($lines[0] -ne $expected)
    {
        Say ""
        Say "  Attendu : $expected"
        Say "  Trouve  : $($lines[0])"
        Fail "La valeur enregistree ne correspond pas. Ne t'en sers pas : elle fusionnerait mal."
    }
    Ok "Valeur enregistree correcte (verifiee caractere par caractere)."
}

function Test-Attributes($root)
{
    Push-Location $root
    try
    {
        $scene = & git ls-files "*.unity" | Select-Object -First 1
        if (-not $scene)
        {
            Warn "Aucune scene trouvee pour tester - verification partielle."
            return
        }
        $attr = & git check-attr merge -- $scene
        if ($attr -notmatch 'merge:\s*unityyamlmerge')
        {
            Fail "Git n'applique pas le driver aux fichiers Unity ($attr). Le .gitattributes manque a la racine du depot."
        }
        Ok "Git applique bien le driver aux scenes, prefabs et .meta."
    }
    finally
    {
        Pop-Location
    }
}

Say ""
Say "=================================================="
Say "  Configuration git pour Unity - Glimmer of Hope"
Say "=================================================="
Say ""

$root = Get-RepoRoot
Ok "Depot : $root"

$version = Get-ProjectUnityVersion $root
Ok "Version Unity du projet : $version"

if ($UnityPath) { $exe = Resolve-Given $UnityPath } else { $exe = Find-Merger $version }
Test-Exe $exe
Ok "Chemin : $exe"

Set-Driver $exe

Say ""
Say "Verification..."
Test-StoredValue (Build-DriverValue $exe)
Test-Attributes $root

Say ""
Say "=================================================="
Write-Host "  C'est bon. Ta machine est configuree." -ForegroundColor Green
Say "=================================================="
Say ""
Say "A savoir :"
Say " - A faire UNE FOIS par machine (pas par projet, pas par branche)."
Say " - Sans ca, git fusionne les scenes et les .meta comme du texte,"
Say "   et fabrique des fichiers casses sans rien afficher."
Say " - Si tu reinstalles ou deplaces Unity, relance ce script."
Say ""
