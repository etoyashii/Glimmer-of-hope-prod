# Glimmer of Hopes

## 🚀 Quick Start

1. **Clone** le repo
2. **Ouvrir** avec **Unity 6000.0.30f1**
3. **Ouvrir le projet** : `Glimmer-of-Hope/Glimmer-of-Hope/`
4. **Attendre** la compilation (longue la première fois)
5. **Ouvrir** la scène `_Project/Scenes/_Bootstrap`
6. **Play** → Console affiche `[GameBootstrapper] Services initialized.`

---

## 📁 Structure du Projet

```
Assets/_Project/
├── Scripts/
│   ├── Core/           # Systèmes bootstrap (NE PAS MODIFIER)
│   │   ├── Services/   # ServiceLocator
│   │   ├── Events/     # EventChannels
│   │   ├── Save/       # SaveManager
│   │   ├── Localization/
│   │   ├── Audio/
│   │   └── Bootstrap/
│   ├── Examples/       # Exemples d'utilisation des systèmes
│   ├── Gameplay/       # [À REMPLIR] Code gameplay
│   ├── UI/             # [À REMPLIR] Code UI
│   └── Editor/         # Outils Editor
├── Data/
│   └── Events/         # EventChannel assets (.asset)
├── Scenes/
│   ├── _Bootstrap      # Point d'entrée (init services)
│   ├── MainMenu        # [À REMPLIR]
│   └── Gameplay        # [À REMPLIR]
├── Art/                # [À REMPLIR] Assets graphiques
├── Audio/              # [À REMPLIR] Assets audio
├── Prefabs/            # [À REMPLIR] Prefabs gameplay
└── UI/                 # [À REMPLIR] Assets UI
```

---

## 🔧 Systèmes Disponibles

### EventChannels (Communication découplée)
Les scripts communiquent via des ScriptableObjects `.asset` sans se connaître.

**Créer un EventChannel :**
1. `Data/Events/` → Right-click → Create > Glimmer > Events > [Type]
2. Renommer (ex: `OnEnemySpawned`)
3. Drag sur les scripts qui en ont besoin

**Utilisation :**
```csharp
// Déclencher un event
[SerializeField] private VoidEventChannel _onPlayerDeath;
_onPlayerDeath.Raise();

// Écouter un event
private void OnEnable() => _onPlayerDeath.Subscribe(HandleDeath);
private void OnDisable() => _onPlayerDeath.Unsubscribe(HandleDeath);
```

**Voir les exemples :** `Scripts/Examples/`

### ServiceLocator (Accès aux services)
```csharp
var audioManager = ServiceLocator.Get<AudioManager>();
var saveManager = ServiceLocator.Get<SaveManager>();
```

### Localization (Multilingue)
```csharp
string text = LocalizationManager.Get("menu.play");
// Français: "Jouer" | English: "Play" | Español: "Jugar"
```

**Ajouter des traductions :** `StreamingAssets/Localization/[lang]/`

### SaveManager (Sauvegarde JSON)
```csharp
SaveManager.Save("player", playerData);
var data = SaveManager.Load<PlayerData>("player");
```

---

## 📦 EventChannels Disponibles

| Catégorie | Assets | Type |
|-----------|--------|------|
| **Game** | OnGameStart, OnGamePause, OnGameResume, OnGameOver, OnLevelComplete | Void |
| **Player** | OnPlayerDeath, OnPlayerRespawn, OnPlayerHit | Void |
| **Player** | OnScoreChanged, OnLivesChanged | Int |
| **Player** | OnHealthChanged | Float |
| **Audio** | OnMusicVolumeChanged, OnSFXVolumeChanged | Float |
| **Dialogue** | OnDialogueLine | String |
| **Dialogue** | OnDialogueEnd | Void |
| **Progression** | OnCheckpointReached | Void |
| **Progression** | OnPauseToggled | Bool |

---

## 🌍 Langues Supportées

- 🇫🇷 Français (`fr`)
- 🇬🇧 English (`en`)
- 🇪🇸 Español (`es`)

---

## 📌 Règles

1. **Ne pas modifier `Scripts/Core/`** — C'est le bootstrap
2. **Utiliser les EventChannels** — Pas de références directes entre systèmes
3. **Gameplay dans `Scripts/Gameplay/`**
4. **UI dans `Scripts/UI/`**

---

## 🛠️ Plugins Installés

- **UniTask** — Async/await performant
- **DOTween** — Animations fluides
- **NaughtyAttributes** — Inspector enrichi
- **FMOD** — Audio professionnel

---

*Bootstrap terminé — Repo prêt pour le développement*
