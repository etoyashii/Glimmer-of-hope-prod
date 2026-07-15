# Plan de reorganisation

**440 fichiers deplaces.** 293 intouchables, 755 deja bien ranges, 0 refuses, 0 collisions.

Le `.meta` de chaque fichier suit son fichier : les GUID ne changent pas, donc **aucune scene, aucun prefab, aucun materiau ne peut changer.** C'est verifiable sans ouvrir Unity : `verify.py` exige un diff YAML **vide**.


## Chemins en dur : a corriger dans un SECOND commit

Ces constantes nomment un dossier que le manifeste deplace. Rien ne va protester : git reussira, Unity compilera, et l'outil renverra **zero resultat sans un mot**. C'est la panne la plus chere du lot, precisement parce qu'elle est muette.

Corriger **apres** le commit de deplacement, jamais dedans : un fichier a la fois deplace et modifie casse la detection de renommage de git, et donc le rollback.

| fichier (a sa nouvelle place) | ligne | avant | apres |
|---|---|---|---|
| `Assets/_Project/Scripts/Editor/Characters/CharacterUIConstants.cs` | 75 | `Assets/_Project/Art/UI/Characters` | `Assets/_Project/Art/Textures/UI/Characters` |
| `Assets/_Project/Scripts/Editor/Characters/CharacterUIStyleGenerator.cs` | 11 | `Assets/_Project/Art/UI/Characters` | `Assets/_Project/Art/Textures/UI/Characters` |
| `Assets/_Project/Scripts/Editor/Tools/LODSettings.cs` | 67 | `Assets/_Project/Art/Mesh/_Generated/LOD` | `Assets/_Project/Art/Models/_Generated/LOD` |
| `Assets/_Project/Scripts/Legacy/Editor/BranchMeshBuilderEditor.cs` | 7 | `Assets/_Project/Art/Mesh` | `Assets/_Project/Art/Models` |

## Ce qui bouge, regle par regle


### `art-mesh-vers-models` — 104 fichiers
*ADR-008 : vocabulaire ferme (Models, pas Mesh)*

- `Assets/_Project/Art/Mesh/Animaux/SM_ZF_Fauna_ButterflyTest2 (1).fbx`  →  `Assets/_Project/Art/Models/Animaux/SM_ZF_Fauna_ButterflyTest2 (1).fbx`
- `Assets/_Project/Art/Mesh/Architecture/Prefab/Incinerator.prefab`  →  `Assets/_Project/Art/Models/Architecture/Prefab/Incinerator.prefab`
- `Assets/_Project/Art/Mesh/Architecture/Prefab/Robot.prefab`  →  `Assets/_Project/Art/Models/Architecture/Prefab/Robot.prefab`
- `Assets/_Project/Art/Mesh/Architecture/Prefab/Storage 1.prefab`  →  `Assets/_Project/Art/Models/Architecture/Prefab/Storage 1.prefab`
- `Assets/_Project/Art/Mesh/Architecture/Prefab/Storage.prefab`  →  `Assets/_Project/Art/Models/Architecture/Prefab/Storage.prefab`
- `Assets/_Project/Art/Mesh/Architecture/Prefab/Tree.prefab`  →  `Assets/_Project/Art/Models/Architecture/Prefab/Tree.prefab`
- *… et 98 autres*

### `tiers-zacxophone` — 35 fichiers
*pack Asset Store — ADR-007 : tout pack externe sous ThirdParty/*

- `Assets/_Project/Proto LD/PlaceHoldersAssets/Zacxophone/Birds/Animators/01BirdAnimator.controller`  →  `Assets/ThirdParty/Zacxophone_LowPolyBirds/Birds/Animators/01BirdAnimator.controller`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Zacxophone/Birds/Animators/03BirdsAnimator.controller`  →  `Assets/ThirdParty/Zacxophone_LowPolyBirds/Birds/Animators/03BirdsAnimator.controller`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Zacxophone/Birds/Animators/05BirdsAnimator.controller`  →  `Assets/ThirdParty/Zacxophone_LowPolyBirds/Birds/Animators/05BirdsAnimator.controller`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Zacxophone/Birds/Animators/10BirdsAnimator.controller`  →  `Assets/ThirdParty/Zacxophone_LowPolyBirds/Birds/Animators/10BirdsAnimator.controller`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Zacxophone/Birds/Animators/15BirdsAnimator.controller`  →  `Assets/ThirdParty/Zacxophone_LowPolyBirds/Birds/Animators/15BirdsAnimator.controller`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Zacxophone/Birds/Built-In/DemoScenes/LowPolyBirdsExampleSceneBuiltIn.unity`  →  `Assets/ThirdParty/Zacxophone_LowPolyBirds/Birds/Built-In/DemoScenes/LowPolyBirdsExampleSceneBuiltIn.unity`
- *… et 29 autres*

### `protold-placeholders` — 33 fichiers
*sort d'un dossier a espace ('Proto LD')*

- `Assets/_Project/Proto LD/PlaceHoldersAssets/47074.jpg`  →  `Assets/_Project/Art/Placeholders/47074.jpg`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/9fdc21df-db09-4d5a-afe0-32b30895a340.jpg`  →  `Assets/_Project/Art/Placeholders/9fdc21df-db09-4d5a-afe0-32b30895a340.jpg`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Box04.prefab`  →  `Assets/_Project/Art/Placeholders/Box04.prefab`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Custom_MagicFire.mat`  →  `Assets/_Project/Art/Placeholders/Custom_MagicFire.mat`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Drum Set.FBX`  →  `Assets/_Project/Art/Placeholders/Drum Set.FBX`
- `Assets/_Project/Proto LD/PlaceHoldersAssets/Flower.prefab`  →  `Assets/_Project/Art/Placeholders/Flower.prefab`
- *… et 27 autres*

### `ld-forest-prefabs` — 31 fichiers
*sort d'un dossier a espace ('LD Forest')*

- `Assets/_Project/LD/LD Forest/PrefabForBrush/AB1.prefab`  →  `Assets/_Project/Prefabs/BrushAssets/Forest/AB1.prefab`
- `Assets/_Project/LD/LD Forest/PrefabForBrush/AB2.prefab`  →  `Assets/_Project/Prefabs/BrushAssets/Forest/AB2.prefab`
- `Assets/_Project/LD/LD Forest/PrefabForBrush/AB3.prefab`  →  `Assets/_Project/Prefabs/BrushAssets/Forest/AB3.prefab`
- `Assets/_Project/LD/LD Forest/PrefabForBrush/AB4.prefab`  →  `Assets/_Project/Prefabs/BrushAssets/Forest/AB4.prefab`
- `Assets/_Project/LD/LD Forest/PrefabForBrush/AB5.prefab`  →  `Assets/_Project/Prefabs/BrushAssets/Forest/AB5.prefab`
- `Assets/_Project/LD/LD Forest/PrefabForBrush/AB6.prefab`  →  `Assets/_Project/Prefabs/BrushAssets/Forest/AB6.prefab`
- *… et 25 autres*

### `terrains-racine` — 25 fichiers
*ADR-007 : aucun fichier a la racine d'Assets/*

- `Assets/AutoLayerTest.terrain.asset`  →  `Assets/_Project/LD/Terrains/AutoLayerTest.terrain.asset`
- `Assets/Border1.terrain.asset`  →  `Assets/_Project/LD/Terrains/Border1.terrain.asset`
- `Assets/Border10.terrain.asset`  →  `Assets/_Project/LD/Terrains/Border10.terrain.asset`
- `Assets/Border2.terrain.asset`  →  `Assets/_Project/LD/Terrains/Border2.terrain.asset`
- `Assets/Border3.terrain.asset`  →  `Assets/_Project/LD/Terrains/Border3.terrain.asset`
- `Assets/Border4.terrain.asset`  →  `Assets/_Project/LD/Terrains/Border4.terrain.asset`
- *… et 19 autres*

### `scenes-bac-a-sable-par-prenom` — 20 fichiers
*ADR-010 : scene classee par auteur → bac a sable*

- `Assets/_Project/Scenes/Proto/Bastien/ReworkedControls.unity`  →  `Assets/_Project/Scenes/Sandbox/Bastien_ReworkedControls.unity`
- `Assets/_Project/Scenes/Proto/Erwan/LV_DestroyBlock.unity`  →  `Assets/_Project/Scenes/Sandbox/Erwan_LV_DestroyBlock.unity`
- `Assets/_Project/Scenes/Proto/Erwan/LV_ElevateFlower.unity`  →  `Assets/_Project/Scenes/Sandbox/Erwan_LV_ElevateFlower.unity`
- `Assets/_Project/Scenes/Proto/Erwan/LV_EnlightenSkill.unity`  →  `Assets/_Project/Scenes/Sandbox/Erwan_LV_EnlightenSkill.unity`
- `Assets/_Project/Scenes/Proto/Erwan/LV_LearnSkill.unity`  →  `Assets/_Project/Scenes/Sandbox/Erwan_LV_LearnSkill.unity`
- `Assets/_Project/Scenes/Proto/Erwan/LV_Lueur.unity`  →  `Assets/_Project/Scenes/Sandbox/Erwan_LV_Lueur.unity`
- *… et 14 autres*

### `tiers-gabriel-aguiar` — 19 fichiers
*pack Asset Store — ADR-007 : tout pack externe sous ThirdParty/*

- `Assets/GabrielAguiarProductions/Documentation - Free Quick Effects.pdf`  →  `Assets/ThirdParty/GabrielAguiarProductions/Documentation - Free Quick Effects.pdf`
- `Assets/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/Circle01_AB.mat`  →  `Assets/ThirdParty/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/Circle01_AB.mat`
- `Assets/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/Circle01_AB_2.mat`  →  `Assets/ThirdParty/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/Circle01_AB_2.mat`
- `Assets/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/DistortedFlare01_AB.mat`  →  `Assets/ThirdParty/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/DistortedFlare01_AB.mat`
- `Assets/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/DistortedFlare01_AB_2.mat`  →  `Assets/ThirdParty/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/DistortedFlare01_AB_2.mat`
- `Assets/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/Flame02_AB.mat`  →  `Assets/ThirdParty/GabrielAguiarProductions/FreeQuickEffectsVol1/Materials/Flame02_AB.mat`
- *… et 13 autres*

### `code-orphelin-zpreprod` — 19 fichiers
*script hors asmdef — regroupe sans changer d'assembly*

- `Assets/ZPreprod/_Project/Scripts/Gameplay/Camera/CameraManager.cs`  →  `Assets/_Project/Scripts/Legacy/Gameplay/Camera/CameraManager.cs`
- `Assets/ZPreprod/_Project/Scripts/Gameplay/Camera/LookAt.cs`  →  `Assets/_Project/Scripts/Legacy/Gameplay/Camera/LookAt.cs`
- `Assets/ZPreprod/_Project/Scripts/Gameplay/Camera/Photo.cs`  →  `Assets/_Project/Scripts/Legacy/Gameplay/Camera/Photo.cs`
- `Assets/ZPreprod/_Project/Scripts/Gameplay/Camera/PicturabelObject.cs`  →  `Assets/_Project/Scripts/Legacy/Gameplay/Camera/PicturabelObject.cs`
- `Assets/ZPreprod/_Project/Scripts/Gameplay/Camera/SO_PhotoTask.cs`  →  `Assets/_Project/Scripts/Legacy/Gameplay/Camera/SO_PhotoTask.cs`
- `Assets/ZPreprod/_Project/Scripts/Gameplay/Camera/ViewController.cs`  →  `Assets/_Project/Scripts/Legacy/Gameplay/Camera/ViewController.cs`
- *… et 13 autres*

### `art-zpreprod` — 17 fichiers
*unification : ZPreprod etait un second arbre _Project parallele*

- `Assets/ZPreprod/_Project/Art/Glyphes/AngryGlyph.png`  →  `Assets/_Project/Art/Legacy_ZPreprod/Glyphes/AngryGlyph.png`
- `Assets/ZPreprod/_Project/Art/Glyphes/CloseGlyph.png`  →  `Assets/_Project/Art/Legacy_ZPreprod/Glyphes/CloseGlyph.png`
- `Assets/ZPreprod/_Project/Art/Glyphes/DisappearGlyph.png`  →  `Assets/_Project/Art/Legacy_ZPreprod/Glyphes/DisappearGlyph.png`
- `Assets/ZPreprod/_Project/Art/Glyphes/GroundGlyph.png`  →  `Assets/_Project/Art/Legacy_ZPreprod/Glyphes/GroundGlyph.png`
- `Assets/ZPreprod/_Project/Art/Glyphes/OpenGlyph.png`  →  `Assets/_Project/Art/Legacy_ZPreprod/Glyphes/OpenGlyph.png`
- `Assets/ZPreprod/_Project/Art/Glyphes/SeedGlyph.png`  →  `Assets/_Project/Art/Legacy_ZPreprod/Glyphes/SeedGlyph.png`
- *… et 11 autres*

### `art-shader-vers-shaders` — 17 fichiers
*ADR-008 : vocabulaire ferme (Shaders, pas Shader)*

- `Assets/_Project/Art/Shader/Cloud.shadergraph`  →  `Assets/_Project/Art/Shaders/Cloud.shadergraph`
- `Assets/_Project/Art/Shader/Movement.shadersubgraph`  →  `Assets/_Project/Art/Shaders/Movement.shadersubgraph`
- `Assets/_Project/Art/Shader/SG_DioramaTerrainLerp.shadergraph`  →  `Assets/_Project/Art/Shaders/SG_DioramaTerrainLerp.shadergraph`
- `Assets/_Project/Art/Shader/SG_FolliageAnim.shadergraph`  →  `Assets/_Project/Art/Shaders/SG_FoliageAnim.shadergraph`
- `Assets/_Project/Art/Shader/SG_Leaf.shadergraph`  →  `Assets/_Project/Art/Shaders/SG_Leaf.shadergraph`
- `Assets/_Project/Art/Shader/SG_Portal.shadergraph`  →  `Assets/_Project/Art/Shaders/SG_Portal.shadergraph`
- *… et 11 autres*

### `code-orphelin-inventory` — 13 fichiers
*script hors asmdef — regroupe sans changer d'assembly*

- `Assets/ZPreprod/Inventory/Scripts/Controller/InventoryController.cs`  →  `Assets/_Project/Scripts/Legacy/Inventory/Controller/InventoryController.cs`
- `Assets/ZPreprod/Inventory/Scripts/Events/EventChannel.cs`  →  `Assets/_Project/Scripts/Legacy/Inventory/Events/EventChannel.cs`
- `Assets/ZPreprod/Inventory/Scripts/Events/InventoryChannels.cs`  →  `Assets/_Project/Scripts/Legacy/Inventory/Events/InventoryChannels.cs`
- `Assets/ZPreprod/Inventory/Scripts/Events/Payloads.cs`  →  `Assets/_Project/Scripts/Legacy/Inventory/Events/Payloads.cs`
- `Assets/ZPreprod/Inventory/Scripts/Model/InventoryModel.cs`  →  `Assets/_Project/Scripts/Legacy/Inventory/Model/InventoryModel.cs`
- `Assets/ZPreprod/Inventory/Scripts/Model/ItemModel.cs`  →  `Assets/_Project/Scripts/Legacy/Inventory/Model/ItemModel.cs`
- *… et 7 autres*

### `art-ui-vers-textures` — 11 fichiers
*ADR-008 : Art/ se range par TYPE, puis par domaine*

- `Assets/_Project/Art/UI/Characters/circle-thumb.png`  →  `Assets/_Project/Art/Textures/UI/Characters/circle-thumb.png`
- `Assets/_Project/Art/UI/Characters/rounded-button.png`  →  `Assets/_Project/Art/Textures/UI/Characters/rounded-button.png`
- `Assets/_Project/Art/UI/Characters/rounded-card-lg.png`  →  `Assets/_Project/Art/Textures/UI/Characters/rounded-card-lg.png`
- `Assets/_Project/Art/UI/Characters/rounded-card.png`  →  `Assets/_Project/Art/Textures/UI/Characters/rounded-card.png`
- `Assets/_Project/Art/UI/Characters/rounded-panel.png`  →  `Assets/_Project/Art/Textures/UI/Characters/rounded-panel.png`
- `Assets/_Project/Art/UI/Characters/selection-border.png`  →  `Assets/_Project/Art/Textures/UI/Characters/selection-border.png`
- *… et 5 autres*

### `protold-light` — 9 fichiers
*sort d'un dossier a espace ('Proto LD')*

- `Assets/_Project/Proto LD/Light/Dappled light - 04 - for URP render texture.jpg`  →  `Assets/_Project/LD/Lighting/Dappled light - 04 - for URP render texture.jpg`
- `Assets/_Project/Proto LD/Light/Directional Light (2).prefab`  →  `Assets/_Project/LD/Lighting/Directional Light (2).prefab`
- `Assets/_Project/Proto LD/Light/Directional Light.prefab`  →  `Assets/_Project/LD/Lighting/Directional Light.prefab`
- `Assets/_Project/Proto LD/Light/Level1/LightingData.asset`  →  `Assets/_Project/LD/Lighting/Level1/LightingData.asset`
- `Assets/_Project/Proto LD/Light/Level1/ReflectionProbe-0.exr`  →  `Assets/_Project/LD/Lighting/Level1/ReflectionProbe-0.exr`
- `Assets/_Project/Proto LD/Light/Materials/Sans titre 3.mat`  →  `Assets/_Project/LD/Lighting/Materials/Sans titre 3.mat`
- *… et 3 autres*

### `code-orphelin-protold` — 9 fichiers
*script hors asmdef — regroupe sans changer d'assembly*

- `Assets/_Project/Proto LD/Scripts/Cinematics/BandeNoir/CinematicBarsEffect.cs`  →  `Assets/_Project/Scripts/Legacy/Cinematics/BandeNoir/CinematicBarsEffect.cs`
- `Assets/_Project/Proto LD/Scripts/Cinematics/BandeNoir/CinematicBarsFeature.cs`  →  `Assets/_Project/Scripts/Legacy/Cinematics/BandeNoir/CinematicBarsFeature.cs`
- `Assets/_Project/Proto LD/Scripts/Cinematics/BandeNoir/CinematicBarsPass.cs`  →  `Assets/_Project/Scripts/Legacy/Cinematics/BandeNoir/CinematicBarsPass.cs`
- `Assets/_Project/Proto LD/Scripts/Cinematics/WakeUpEffect.cs`  →  `Assets/_Project/Scripts/Legacy/Cinematics/WakeUpEffect.cs`
- `Assets/_Project/Proto LD/Scripts/DialogueZoneTrigger.cs`  →  `Assets/_Project/Scripts/Legacy/DialogueZoneTrigger.cs`
- `Assets/_Project/Proto LD/Scripts/Floking/FlockManager.cs`  →  `Assets/_Project/Scripts/Legacy/Floking/FlockManager.cs`
- *… et 3 autres*

### `scenes-restantes-racine` — 7 fichiers
*ADR-010 : hors build → bac a sable*

- `Assets/_Project/Scenes/BrushTest.unity`  →  `Assets/_Project/Scenes/Sandbox/BrushTest.unity`
- `Assets/_Project/Scenes/CameraManager TEST.unity`  →  `Assets/_Project/Scenes/Sandbox/CameraManager TEST.unity`
- `Assets/_Project/Scenes/CharacterCreator.unity`  →  `Assets/_Project/Scenes/Sandbox/CharacterCreator.unity`
- `Assets/_Project/Scenes/KCharacterCreator.unity`  →  `Assets/_Project/Scenes/Sandbox/KCharacterCreator.unity`
- `Assets/_Project/Scenes/MergeScene.unity`  →  `Assets/_Project/Scenes/Sandbox/MergeScene.unity`
- `Assets/_Project/Scenes/TestOdin.unity`  →  `Assets/_Project/Scenes/Sandbox/TestOdin.unity`
- *… et 1 autres*

### `code-orphelin-editor` — 5 fichiers
*script editeur hors asmdef — regroupe sans changer d'assembly*

- `Assets/Editor/BranchMeshBuilderEditor.cs`  →  `Assets/_Project/Scripts/Legacy/Editor/BranchMeshBuilderEditor.cs`
- `Assets/Editor/Brush/AssetContextMenuWithCollisions.cs`  →  `Assets/_Project/Scripts/Legacy/Editor/Brush/AssetContextMenuWithCollisions.cs`
- `Assets/Editor/Brush/AssetContextMenuWithoutCollisions.cs`  →  `Assets/_Project/Scripts/Legacy/Editor/Brush/AssetContextMenuWithoutCollisions.cs`
- `Assets/Editor/Brush/AssetsBrush.cs`  →  `Assets/_Project/Scripts/Legacy/Editor/Brush/AssetsBrush.cs`
- `Assets/Editor/ConsoleFilter.cs`  →  `Assets/_Project/Scripts/Legacy/Editor/ConsoleFilter.cs`

### `materiaux-zpreprod` — 5 fichiers
*unification : ZPreprod etait un second arbre _Project parallele*

- `Assets/ZPreprod/Materials/M_Grass 1.mat`  →  `Assets/_Project/Art/Materials/Legacy_ZPreprod/M_Grass 1.mat`
- `Assets/ZPreprod/Materials/M_Plank.mat`  →  `Assets/_Project/Art/Materials/Legacy_ZPreprod/M_Plank.mat`
- `Assets/ZPreprod/Materials/M_PrimaryPlateform.mat`  →  `Assets/_Project/Art/Materials/Legacy_ZPreprod/M_PrimaryPlateform.mat`
- `Assets/ZPreprod/Materials/M_SecondaryPlatform 1.mat`  →  `Assets/_Project/Art/Materials/Legacy_ZPreprod/M_SecondaryPlatform 1.mat`
- `Assets/ZPreprod/Materials/M_Trunk.mat`  →  `Assets/_Project/Art/Materials/Legacy_ZPreprod/M_Trunk.mat`

### `scenes-zpreprod` — 5 fichiers
*ADR-010 : prototype → bac a sable*

- `Assets/ZPreprod/_Project/Scene/Prototypes/LV_Codex.unity`  →  `Assets/_Project/Scenes/Sandbox/LV_Codex.unity`
- `Assets/ZPreprod/_Project/Scene/Prototypes/LV_DialogueProto.unity`  →  `Assets/_Project/Scenes/Sandbox/LV_DialogueProto.unity`
- `Assets/ZPreprod/_Project/Scene/Prototypes/LV_Glyphes.unity`  →  `Assets/_Project/Scenes/Sandbox/LV_Glyphes.unity`
- `Assets/ZPreprod/_Project/Scene/Prototypes/LV_Main.unity`  →  `Assets/_Project/Scenes/Sandbox/LV_Main.unity`
- `Assets/ZPreprod/_Project/Scene/Prototypes/LV_Spells.unity`  →  `Assets/_Project/Scenes/Sandbox/LV_Spells.unity`

### `terrainlayers-partout` — 5 fichiers
*les terrain layers etaient eparpilles sur 5 dossiers*

- `Assets/_Project/LD/TerrainLayer/EcorceLayer1.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/EcorceLayer1.terrainlayer`
- `Assets/_Project/LD/TerrainLayer/WoodLayer.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/WoodLayer.terrainlayer`
- `Assets/_Project/Proto LD/MossTerrainLayer.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/MossTerrainLayer.terrainlayer`
- `Assets/_Project/Scenes/TestAutoLayerTerrain/Layer 1.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/Layer 1.terrainlayer`
- `Assets/_Project/Scenes/TestAutoLayerTerrain/Layer 2.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/Layer 2.terrainlayer`

### `protold-prefabs` — 5 fichiers
*sort d'un dossier a espace ('Proto LD')*

- `Assets/_Project/Proto LD/prefabs/=========UI==============.prefab`  →  `Assets/_Project/Prefabs/Environment/=========UI==============.prefab`
- `Assets/_Project/Proto LD/prefabs/========CHARACTER=========.prefab`  →  `Assets/_Project/Prefabs/Environment/========CHARACTER=========.prefab`
- `Assets/_Project/Proto LD/prefabs/Cloth.prefab`  →  `Assets/_Project/Prefabs/Environment/Cloth.prefab`
- `Assets/_Project/Proto LD/prefabs/Glimmer.prefab`  →  `Assets/_Project/Prefabs/Environment/Glimmer.prefab`
- `Assets/_Project/Proto LD/prefabs/SubBranch.prefab`  →  `Assets/_Project/Prefabs/Environment/SubBranch.prefab`

### `vrac-racine` — 4 fichiers
*ADR-007 : aucun fichier a la racine d'Assets/ — a trier*

- `Assets/GameObject 1.prefab`  →  `Assets/_Project/_ATrier/GameObject 1.prefab`
- `Assets/GameObject.prefab`  →  `Assets/_Project/_ATrier/GameObject.prefab`
- `Assets/d34a659c5dc947bd1cd36411d0618d926a1e94ac.png`  →  `Assets/_Project/_ATrier/d34a659c5dc947bd1cd36411d0618d926a1e94ac.png`
- `Assets/pack_fleurs_lowpoly (11).prefab`  →  `Assets/_Project/_ATrier/pack_fleurs_lowpoly (11).prefab`

### `terrainlayers-homonymes` — 3 fichiers
*trois homonymes auto-nommes par Unity — distingues par provenance*

- `Assets/NewLayer.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/NewLayer_Racine.terrainlayer`
- `Assets/_Project/Art/Shader/NewLayer.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/NewLayer_Shader.terrainlayer`
- `Assets/_Project/Scenes/TestAutoLayerTerrain/NewLayer.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/NewLayer_AutoLayer.terrainlayer`

### `terrainlayers-forest` — 3 fichiers
*les terrain layers etaient eparpilles sur 5 dossiers*

- `Assets/_Project/LD/LD Forest/Layer_Grass.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/Layer_Grass.terrainlayer`
- `Assets/_Project/LD/LD Forest/Layer_Road.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/Layer_Road.terrainlayer`
- `Assets/_Project/LD/LD Forest/Layer_Water.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/Layer_Water.terrainlayer`

### `scenes-protold` — 3 fichiers
*ADR-010 : prototype LD → bac a sable*

- `Assets/_Project/Proto LD/Level 2.unity`  →  `Assets/_Project/Scenes/Sandbox/Level 2.unity`
- `Assets/_Project/Proto LD/Level1.unity`  →  `Assets/_Project/Scenes/Sandbox/Level1.unity`
- `Assets/_Project/Proto LD/New Scene.unity`  →  `Assets/_Project/Scenes/Sandbox/New Scene.unity`

### `scenes-build` — 3 fichiers
*ADR-010 : scene du build*

- `Assets/_Project/Scenes/Gameplay.unity`  →  `Assets/_Project/Scenes/Core/Gameplay.unity`
- `Assets/_Project/Scenes/MainMenu.unity`  →  `Assets/_Project/Scenes/Core/MainMenu.unity`
- `Assets/_Project/Scenes/_Bootstrap.unity`  →  `Assets/_Project/Scenes/Core/_Bootstrap.unity`

### `code-orphelin-brush` — 3 fichiers
*script hors asmdef — regroupe sans changer d'assembly*

- `Assets/_Project/Scripts/Brush/AssetTemplate.cs`  →  `Assets/_Project/Scripts/Legacy/Brush/AssetTemplate.cs`
- `Assets/_Project/Scripts/Brush/AssetsStruct.cs`  →  `Assets/_Project/Scripts/Legacy/Brush/AssetsStruct.cs`
- `Assets/_Project/Scripts/Brush/BrushManager.cs`  →  `Assets/_Project/Scripts/Legacy/Brush/BrushManager.cs`

### `code-orphelin-dev` — 3 fichiers
*script hors asmdef — regroupe sans changer d'assembly*

- `Assets/_Project/Scripts/Dev/Guide.cs`  →  `Assets/_Project/Scripts/Legacy/Dev/Guide.cs`
- `Assets/_Project/Scripts/Dev/TestComponent.cs`  →  `Assets/_Project/Scripts/Legacy/Dev/TestComponent.cs`
- `Assets/_Project/Scripts/Dev/TestEnumSection.cs`  →  `Assets/_Project/Scripts/Legacy/Dev/TestEnumSection.cs`

### `input-racine` — 2 fichiers
*ADR-007 : aucun fichier a la racine d'Assets/*

- `Assets/IA_CustomInput.inputactions`  →  `Assets/_Project/Settings/Input/IA_CustomInput.inputactions`
- `Assets/InputSystem_Actions.inputactions`  →  `Assets/_Project/Settings/Input/InputSystem_Actions.inputactions`

### `resources-fusion` — 2 fichiers
*fusion des deux racines Resources*

- `Assets/ZPreprod/Resources/Skybox Cubemap Extended/Demo/Materials/Polyverse Skies - Blue Sky.mat`  →  `Assets/Resources/Skybox Cubemap Extended/Demo/Materials/Polyverse Skies - Blue Sky.mat`
- `Assets/ZPreprod/Resources/Skybox Cubemap Extended/Demo/Textures/Polyverse Skies - Blue Sky.png`  →  `Assets/Resources/Skybox Cubemap Extended/Demo/Textures/Polyverse Skies - Blue Sky.png`

### `ld-racine-art` — 2 fichiers
*ADR-007 : Art/ se range par type*

- `Assets/_Project/LD/SM_ZF_Folliage_Moss01.fbx`  →  `Assets/_Project/Art/Models/Foliage/SM_ZF_Foliage_Moss01.fbx`
- `Assets/_Project/LD/SM_ZF_Folliage_Moss02.fbx`  →  `Assets/_Project/Art/Models/Foliage/SM_ZF_Foliage_Moss02.fbx`

### `ld-forest-reste` — 2 fichiers
*sort d'un dossier a espace ('LD Forest')*

- `Assets/_Project/LD/LD Forest/Forest_Terrain_v2.terrain.asset`  →  `Assets/_Project/LD/Forest/Forest_Terrain_v2.terrain.asset`
- `Assets/_Project/LD/LD Forest/New Material.mat`  →  `Assets/_Project/LD/Forest/New Material.mat`

### `scenes-ld-forest` — 2 fichiers
*ADR-010 : niveau jouable*

- `Assets/_Project/LD/LD Forest/LD-Forest-V2.unity`  →  `Assets/_Project/Scenes/Levels/LD-Forest-V2.unity`
- `Assets/_Project/LD/LD Forest/LD-Forest.unity`  →  `Assets/_Project/Scenes/Levels/LD-Forest.unity`

### `scenes-art` — 2 fichiers
*ADR-010 : scene d'essai → bac a sable*

- `Assets/_Project/Scenes/Art/CloudTest.unity`  →  `Assets/_Project/Scenes/Sandbox/CloudTest.unity`
- `Assets/_Project/Scenes/Art/Diorama.unity`  →  `Assets/_Project/Scenes/Sandbox/Diorama.unity`

### `scenes-prototypes` — 2 fichiers
*ADR-010 : prototype → bac a sable*

- `Assets/_Project/Scenes/Prototypes/Base.unity`  →  `Assets/_Project/Scenes/Sandbox/Base.unity`
- `Assets/_Project/Scenes/Prototypes/testInitialSetup.unity`  →  `Assets/_Project/Scenes/Sandbox/testInitialSetup.unity`

### `brush-racine` — 1 fichiers
*ADR-007 : aucun fichier a la racine d'Assets/*

- `Assets/NewBrush.brush`  →  `Assets/_Project/Data/BrushAssets/NewBrush.brush`

### `terrainlayers-racine` — 1 fichiers
*ADR-007 : aucun fichier a la racine d'Assets/*

- `Assets/NewLayer 1.terrainlayer`  →  `Assets/_Project/LD/TerrainLayers/NewLayer 1.terrainlayer`

### `import-zpreprod` — 1 fichiers
*unification : ZPreprod etait un second arbre _Project parallele*

- `Assets/ZPreprod/Import/Ramp.fbx`  →  `Assets/_Project/Art/Models/Legacy_ZPreprod/Ramp.fbx`

### `scene-import-art` — 1 fichiers
*ADR-010 : une scene ne vit pas dans Art/*

- `Assets/_Project/Art/ImportAsset.unity`  →  `Assets/_Project/Scenes/Sandbox/ImportAsset.unity`

### `art-racine-vrac` — 1 fichiers
*ADR-007 : Art/ se range par type*

- `Assets/_Project/Art/SM_ZF_TreeTrunk_Brich_Medium.fbx`  →  `Assets/_Project/Art/Models/SM_ZF_TreeTrunk_Birch_Medium.fbx`

### `audio-vers-art` — 1 fichiers
*ADR-007 : Audio est une categorie d'Art*

- `Assets/_Project/Audio/file_example_WAV_5MG.wav`  →  `Assets/_Project/Art/Audio/file_example_WAV_5MG.wav`

### `ld-racine-mat` — 1 fichiers
*ADR-007 : Art/ se range par type*

- `Assets/_Project/LD/MossMatLD.mat`  →  `Assets/_Project/Art/Materials/MossMatLD.mat`

### `protold-level1` — 1 fichiers
*sort d'un dossier a espace ('Proto LD')*

- `Assets/_Project/Proto LD/Level1/OcclusionCullingData.asset`  →  `Assets/_Project/LD/Lighting/Level1/OcclusionCullingData.asset`

### `shader-hlsl-protold` — 1 fichiers
*shader range avec les shaders*

- `Assets/_Project/Proto LD/Scripts/Cinematics/BandeNoir/CinematicBars.shader`  →  `Assets/_Project/Art/Shaders/CinematicBars.shader`

### `scene-terrain-test` — 1 fichiers
*ADR-010 : scene de test → bac a sable*

- `Assets/_Project/Scenes/TestAutoLayerTerrain/AutoTerrainLayerTEst.unity`  →  `Assets/_Project/Scenes/Sandbox/AutoTerrainLayerTEst.unity`

## Sanctuaires — jamais deplaces

| chemin | fichiers |
|---|---|
| `Assets/AddressableAssetsData/` | 14 |
| `Assets/Plugins/` | 216 |
| `Assets/ScriptTemplates/` | 4 |
| `Assets/Settings/` | 7 |
| `Assets/StreamingAssets/` | 14 |
| `Assets/TextMesh Pro/` | 37 |
| `Resources/ (dossier magique Unity)` | 1 |
