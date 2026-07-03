using NUnit.Framework;
using ThreadRace.App.Installers;
using ThreadRace.Gameplay.Config;
using ThreadRace.Infrastructure.Config;
using ThreadRace.Presentation.Animation;
using ThreadRace.Presentation.Navigation;
using ThreadRace.Presentation.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace ThreadRace.Tests.EditMode
{
    public sealed class ThreadRaceSceneValidationTests
    {
        private const string ScenePath = "Assets/Scenes/ThreadRace_Main.unity";
        private const string EntryPrefabPath = "Assets/Prefabs/UI/ThreadRaceEntryPopup.prefab";
        private const string HudPrefabPath = "Assets/Prefabs/UI/ThreadRaceRaceHud.prefab";
        private const string LevelPrefabPath = "Assets/Prefabs/UI/ThreadRacePlaceholderLevel.prefab";
        private const string ResultPrefabPath = "Assets/Prefabs/UI/ThreadRaceResultPanel.prefab";
        private const string ProjectContextPath = "Assets/Resources/ProjectContext.prefab";
        private const string UiKitSpritesRoot = "Assets/2D Game UI Kit/Sprites";
        private const string ThreadFeverMainMenuSpritesRoot = "Assets/Sprites/MainMenu_";
        private const string HoleCrazeArtRoot = "Assets/Sprites/comingsoon_bubble.png";
        private const string PrimaryFontAssetPath = "Assets/Fonts/LilitaOne-Regular SDF.asset";
        [Test]
        public void DemoSceneExistsAndContainsRequiredLayers()
        {
            Assert.IsTrue(System.IO.File.Exists(ScenePath));
            var scene = OpenDemoScene();

            Assert.NotNull(FindRoot(scene, "SceneContext"));
            Assert.NotNull(FindRoot(scene, "ThreadRaceSceneInstaller"));
            Assert.NotNull(FindRoot(scene, "Main Camera"));
            Assert.NotNull(FindRoot(scene, "EventSystem"));

            var canvas = FindRoot(scene, "Canvas");
            Assert.NotNull(canvas);
            var safeArea = FindChild(canvas, "SafeArea");
            Assert.NotNull(safeArea);
            Assert.NotNull(FindChild(safeArea, "MainMenuLayer"));
            Assert.NotNull(FindChild(safeArea, "EntryLayer"));
            Assert.NotNull(FindChild(safeArea, "RaceHudLayer"));
            Assert.NotNull(FindChild(safeArea, "LevelResultLayer"));
            Assert.NotNull(FindChild(safeArea, "ResultLayer"));
            Assert.NotNull(FindChild(safeArea, "OverlayLayer"));
        }

        [Test]
        public void SafeAreaHasFitterAndCanvasScalerIsPreserved()
        {
            var scene = OpenDemoScene();
            var canvas = FindRoot(scene, "Canvas");
            var safeArea = FindChild(canvas, "SafeArea");
            var scaler = canvas.GetComponent<CanvasScaler>();

            Assert.NotNull(safeArea.GetComponent<SafeAreaFitter>());
            Assert.NotNull(scaler);
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
            Assert.AreEqual(new Vector2(1080f, 1920f), scaler.referenceResolution);
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight);
        }

        [Test]
        public void PresentationLayersAreSavedHiddenAndNonInteractive()
        {
            var scene = OpenDemoScene();
            var canvas = FindRoot(scene, "Canvas");
            var safeArea = FindChild(canvas, "SafeArea");

            AssertLayerSavedHidden(safeArea, "EntryLayer");
            AssertLayerSavedHidden(safeArea, "RaceHudLayer");
            AssertLayerSavedHidden(safeArea, "LevelResultLayer");
            AssertLayerSavedHidden(safeArea, "ResultLayer");
            AssertLayerSavedHidden(safeArea, "OverlayLayer");
        }

        [Test]
        public void RequiredUiPrefabsExistAndHudContainsFiveRows()
        {
            var entryPrefab = AssertPrefabComponent<EntryPopupView>(EntryPrefabPath);
            var hudPrefab = AssertPrefabComponent<RaceHudView>(HudPrefabPath);
            AssertPrefabComponent<PlaceholderLevelView>(LevelPrefabPath);
            AssertPrefabComponent<RaceResultView>(ResultPrefabPath);

            Assert.AreEqual(5, hudPrefab.GetComponentsInChildren<RacerHudRowView>(true).Length);
            var countdownText = FindChild(hudPrefab, "CountdownText");
            Assert.NotNull(countdownText == null
                ? null
                : countdownText.GetComponent<TMPro.TMP_Text>());
            Assert.NotNull(entryPrefab.GetComponentsInChildren<TMPro.TMP_Text>(true));
            Assert.NotNull(FindChild(entryPrefab, "CloseButton").GetComponent<Button>());
            AssertSpriteName(FindChild(entryPrefab, "CloseButton"), "UI-pack_Sprite_1_79");
        }

        [Test]
        public void GeneratedPresentationTextsUsePrimaryThreadRaceFont()
        {
            var expectedFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(PrimaryFontAssetPath);
            Assert.NotNull(expectedFont, "Primary ThreadRace TMP font asset");
            Assert.NotNull(expectedFont.material, "Primary ThreadRace TMP font material");

            var scene = OpenDemoScene();
            var canvas = FindRoot(scene, "Canvas");
            var safeArea = FindChild(canvas, "SafeArea");
            AssertAllTextsUseFont(FindChild(safeArea, "MainMenuLayer"), expectedFont);
            AssertAllTextsUseFont(AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath), expectedFont);
            AssertAllTextsUseFont(AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath), expectedFont);
            AssertAllTextsUseFont(AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabPath), expectedFont);
            AssertAllTextsUseFont(AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath), expectedFont);
        }

        [Test]
        public void PlaceholderLevelPrefabSupportsHostGameplayOutcomePanels()
        {
            var levelPrefab = AssertPrefabComponent<PlaceholderLevelView>(LevelPrefabPath);

            Assert.NotNull(FindChild(levelPrefab, "HostGameplayRoot"));
            Assert.NotNull(FindChild(levelPrefab, "ChallengePanel"));
            Assert.NotNull(FindChild(levelPrefab, "LevelWinPanel"));
            Assert.NotNull(FindChild(levelPrefab, "LevelFailPanel"));
            Assert.NotNull(FindChild(levelPrefab, "SuccessButton").GetComponent<Button>());
            Assert.NotNull(FindChild(levelPrefab, "FailButton").GetComponent<Button>());
            Assert.NotNull(FindChild(levelPrefab, "ClaimButton").GetComponent<Button>());
            Assert.NotNull(FindChild(levelPrefab, "BackHomeButton").GetComponent<Button>());
            AssertSpriteName(FindChild(levelPrefab, "SuccessButton"), "UI-pack_Sprite_1_36");
            AssertSpriteName(FindChild(levelPrefab, "SuccessButton/Icon"), "UI-pack_Sprite_1_5");
            AssertSpriteName(FindChild(levelPrefab, "FailButton"), "UI-pack_Sprite_1_37");
            AssertSpriteName(FindChild(levelPrefab, "FailButton/Icon"), "UI-pack_Sprite_1_79");
            AssertSpriteName(FindChild(levelPrefab, "LevelWinPanel/CoinIcon"), "UI-pack_Sprite_1_12");
            AssertSpriteName(FindChild(levelPrefab, "ClaimButton"), "UI-pack_Sprite_1_36");
            AssertSpriteName(FindChild(levelPrefab, "LevelFailPanel/FailBadge"), "UI-pack_Sprite_1_79");
            AssertSpriteName(FindChild(levelPrefab, "BackHomeButton"), "UI-pack_Sprite_1_36");
            var challengeTitle = FindChild(levelPrefab, "ChallengePanel/Title").GetComponent<TMPro.TMP_Text>();
            Assert.NotNull(challengeTitle);
            Assert.AreEqual("LEVEL 1", challengeTitle.text);
            StringAssert.DoesNotContain("/", challengeTitle.text);

            var failTitle = FindChild(levelPrefab, "LevelFailPanel/Title").GetComponent<TMPro.TMP_Text>();
            Assert.NotNull(failTitle);
            Assert.AreEqual("TRY AGAIN!", failTitle.text);

            var failBody = FindChild(levelPrefab, "LevelFailPanel/Body").GetComponent<TMPro.TMP_Text>();
            Assert.NotNull(failBody);
            Assert.AreEqual("Try again and keep going.", failBody.text);
            StringAssert.DoesNotContain("race", failBody.text.ToLowerInvariant());
            StringAssert.DoesNotContain("progress", failBody.text.ToLowerInvariant());

            var serialized = new SerializedObject(levelPrefab.GetComponent<PlaceholderLevelView>());
            AssertReference<CanvasGroup>(serialized, "_challengeGroup");
            AssertReference<CanvasGroup>(serialized, "_levelWinGroup");
            AssertReference<CanvasGroup>(serialized, "_levelFailGroup");
            AssertReference<TMPro.TMP_Text>(serialized, "_titleText");
            AssertReference<TMPro.TMP_Text>(serialized, "_instructionText");
            AssertReference<TMPro.TMP_Text>(serialized, "_coinRewardText");
            AssertReference<Button>(serialized, "_successButton");
            AssertReference<Button>(serialized, "_failButton");
            AssertReference<Button>(serialized, "_levelWinClaimButton");
            AssertReference<Button>(serialized, "_levelFailReturnButton");
        }

        [Test]
        public void SceneInstallerHasConfigAndPresentationReferences()
        {
            var scene = OpenDemoScene();
            var installer = FindRoot(scene, "ThreadRaceSceneInstaller").GetComponent<ThreadRaceSceneInstaller>();
            var serialized = new SerializedObject(installer);

            AssertReference<RaceEventConfigAsset>(serialized, "_raceEventConfigAsset");
            AssertReference<MainMenuView>(serialized, "_mainMenuView");
            AssertReference<EntryPopupView>(serialized, "_entryPopupView");
            AssertReference<RaceHudView>(serialized, "_raceHudView");
            AssertReference<PlaceholderLevelView>(serialized, "_placeholderLevelView");
            AssertReference<RaceResultView>(serialized, "_raceResultView");
            AssertReference<ThreadRace.App.RaceApplicationLifecycleObserver>(serialized, "_lifecycleObserver");
        }

        [Test]
        public void TimedEventPrefabReferencesAreAssigned()
        {
            var entryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath);
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);

            AssertReference<TMPro.TMP_Text>(new SerializedObject(entryPrefab.GetComponent<EntryPopupView>()), "_durationText");
            AssertReference<Button>(new SerializedObject(entryPrefab.GetComponent<EntryPopupView>()), "_closeButton");
            AssertReference<TMPro.TMP_Text>(new SerializedObject(hudPrefab.GetComponent<RaceHudView>()), "_countdownText");
            AssertReference<Button>(new SerializedObject(hudPrefab.GetComponent<RaceHudView>()), "_closeButton");
        }

        [Test]
        public void TimedEventConfigUsesSchemaV3RewardTiersAndDynamicAiPacingValues()
        {
            var config = AssetDatabase.LoadAssetAtPath<RaceEventConfigAsset>("Assets/ScriptableObjects/RaceEventConfigAsset.asset");
            var serialized = new SerializedObject(config);

            Assert.AreEqual(3, serialized.FindProperty("_saveSchemaVersion").intValue);
            Assert.AreEqual("ThreadRace.Save.V3", serialized.FindProperty("_saveKey").stringValue);
            Assert.AreEqual(1800L, serialized.FindProperty("_eventDurationSeconds").longValue);
            Assert.AreEqual(1, serialized.FindProperty("_countdownUpdateIntervalSeconds").intValue);
            AssertRewardTier(serialized, 0, 1, "thread_race_rank_1_coins", 1000, "1000 Coins", "coin_stack");
            AssertRewardTier(serialized, 1, 2, "thread_race_rank_2_coins", 500, "500 Coins", "coin_stack");
            AssertRewardTier(serialized, 2, 3, "thread_race_rank_3_coins", 250, "250 Coins", "coin_stack");
            AssertAiTiming(serialized, 1, "ai_01", 6.8f, 10.2f, AiPacingStyle.Steady, 0.52f, 0.9f, 0.16f, 0.02f, 0.08f, 0.04f, 0.03f, 0.08f);
            AssertAiTiming(serialized, 2, "ai_02", 6.4f, 10.8f, AiPacingStyle.Sprinter, 0.52f, 0.64f, 0.38f, 0.62f, -0.18f, 0.13f, 0.08f, 0.04f);
            AssertAiTiming(serialized, 3, "ai_03", 6.5f, 10.6f, AiPacingStyle.Closer, 0.52f, 0.72f, 0.3f, -0.18f, 0.64f, 0.08f, 0.07f, 0.2f);
            AssertAiTiming(serialized, 4, "ai_04", 6.1f, 11.2f, AiPacingStyle.Wildcard, 0.51f, 0.45f, 0.76f, 0.04f, 0.16f, 0.22f, 0.16f, 0.14f);
        }

        private static void AssertRewardTier(
            SerializedObject serialized,
            int index,
            int rank,
            string rewardId,
            int amount,
            string displayText,
            string iconId)
        {
            var tiers = serialized.FindProperty("_rewardTiers");
            Assert.IsNotNull(tiers);
            Assert.Greater(tiers.arraySize, index);
            var tier = tiers.GetArrayElementAtIndex(index);
            Assert.AreEqual(rank, tier.FindPropertyRelative("_rank").intValue);
            Assert.AreEqual(rewardId, tier.FindPropertyRelative("_rewardId").stringValue);
            Assert.AreEqual(0, tier.FindPropertyRelative("_rewardType").enumValueIndex);
            Assert.AreEqual(amount, tier.FindPropertyRelative("_amount").intValue);
            Assert.AreEqual(displayText, tier.FindPropertyRelative("_displayText").stringValue);
            Assert.AreEqual(iconId, tier.FindPropertyRelative("_iconId").stringValue);
        }

        [Test]
        public void LifecycleObserverExistsExactlyOnce()
        {
            OpenDemoScene();

            Assert.AreEqual(1, Object.FindObjectsOfType<ThreadRace.App.RaceApplicationLifecycleObserver>(true).Length);
        }

        [Test]
        public void SceneContextReferencesSceneInstallerAndEventSystemIsNotDuplicated()
        {
            var scene = OpenDemoScene();
            var sceneContext = FindRoot(scene, "SceneContext").GetComponent<SceneContext>();
            var serialized = new SerializedObject(sceneContext);
            var monoInstallers = serialized.FindProperty("_monoInstallers");

            Assert.NotNull(sceneContext);
            Assert.NotNull(monoInstallers);
            Assert.AreEqual(1, monoInstallers.arraySize);
            Assert.NotNull(monoInstallers.GetArrayElementAtIndex(0).objectReferenceValue as ThreadRaceSceneInstaller);
            Assert.AreEqual(1, Object.FindObjectsOfType<EventSystem>(true).Length);
        }

        [Test]
        public void ProjectContextRemainsValid()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectContextPath);

            Assert.NotNull(prefab);
            Assert.NotNull(prefab.GetComponent<ProjectContext>());
            Assert.NotNull(prefab.GetComponent<ThreadRaceProjectInstaller>());
        }

        [Test]
        public void SceneAndUiPrefabsHaveNoMissingScripts()
        {
            var scene = OpenDemoScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                Assert.AreEqual(0, CountMissingScripts(roots[i]), roots[i].name);
            }

            Assert.AreEqual(0, CountMissingScripts(AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath)));
            Assert.AreEqual(0, CountMissingScripts(AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath)));
            Assert.AreEqual(0, CountMissingScripts(AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabPath)));
            Assert.AreEqual(0, CountMissingScripts(AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath)));
        }

        [Test]
        public void ProjectOwnedUiPrefabsReferenceUiKitSprites()
        {
            AssertReferencesUiKitSprite(EntryPrefabPath);
            AssertReferencesUiKitSprite(HudPrefabPath);
            AssertReferencesUiKitSprite(LevelPrefabPath);
            AssertReferencesUiKitSprite(ResultPrefabPath);
        }

        [Test]
        public void DemoSceneMainMenuReferencesUiKitSprites()
        {
            var dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            for (var i = 0; i < dependencies.Length; i++)
            {
                if (dependencies[i].StartsWith(UiKitSpritesRoot, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            Assert.Fail("ThreadRace_Main main menu does not reference required UI Kit sprites.");
        }

        [Test]
        public void DemoSceneMainMenuReferencesThreadFeverSprites()
        {
            var dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            for (var i = 0; i < dependencies.Length; i++)
            {
                if (dependencies[i].StartsWith(ThreadFeverMainMenuSpritesRoot, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            Assert.Fail("ThreadRace_Main main menu does not reference Thread Fever main-menu sprites.");
        }

        [Test]
        public void DemoSceneMainMenuReferencesHoleCrazeComingSoonTooltipArt()
        {
            var dependencies = AssetDatabase.GetDependencies(ScenePath, true);
            for (var i = 0; i < dependencies.Length; i++)
            {
                if (dependencies[i].StartsWith(HoleCrazeArtRoot, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            Assert.Fail("ThreadRace_Main main menu does not reference HoleCraze coming-soon tooltip art.");
        }

        private static Scene OpenDemoScene()
        {
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static GameObject AssertPrefabComponent<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, path);
            Assert.NotNull(prefab.GetComponent<T>(), path);
            return prefab;
        }

        private static void AssertReference<T>(SerializedObject serialized, string propertyName) where T : Object
        {
            var property = serialized.FindProperty(propertyName);
            Assert.NotNull(property, propertyName);
            Assert.NotNull(property.objectReferenceValue as T, propertyName);
        }

        private static void AssertLayerSavedHidden(GameObject safeArea, string layerName)
        {
            var layer = FindChild(safeArea, layerName);
            Assert.NotNull(layer, layerName);

            var canvasGroup = layer.GetComponent<CanvasGroup>();
            Assert.NotNull(canvasGroup, layerName + " requires a CanvasGroup.");
            Assert.AreEqual(0f, canvasGroup.alpha, layerName);
            Assert.IsFalse(canvasGroup.interactable, layerName);
            Assert.IsFalse(canvasGroup.blocksRaycasts, layerName);
        }

        private static void AssertAiTiming(
            SerializedObject serialized,
            int racerIndex,
            string expectedId,
            float expectedMinimum,
            float expectedMaximum,
            AiPacingStyle expectedPacingStyle,
            float expectedSkill,
            float expectedConsistency,
            float expectedVolatility,
            float expectedEarlyPaceBias,
            float expectedLatePaceBias,
            float expectedBurstChance,
            float expectedSlumpChance,
            float expectedFinalPushChance)
        {
            var racers = serialized.FindProperty("_racers");
            var racer = racers.GetArrayElementAtIndex(racerIndex);

            Assert.AreEqual(expectedId, racer.FindPropertyRelative("_racerId").stringValue);
            Assert.AreEqual(expectedMinimum, racer.FindPropertyRelative("_minimumAiStepDelaySeconds").floatValue);
            Assert.AreEqual(expectedMaximum, racer.FindPropertyRelative("_maximumAiStepDelaySeconds").floatValue);
            Assert.AreEqual((int)expectedPacingStyle, racer.FindPropertyRelative("_aiPacingStyle").enumValueIndex);
            Assert.IsTrue(racer.FindPropertyRelative("_usesDynamicAiPlanning").boolValue);
            Assert.That(racer.FindPropertyRelative("_aiSkill").floatValue, Is.EqualTo(expectedSkill).Within(0.001f));
            Assert.That(racer.FindPropertyRelative("_aiConsistency").floatValue, Is.EqualTo(expectedConsistency).Within(0.001f));
            Assert.That(racer.FindPropertyRelative("_aiVolatility").floatValue, Is.EqualTo(expectedVolatility).Within(0.001f));
            Assert.That(racer.FindPropertyRelative("_aiEarlyPaceBias").floatValue, Is.EqualTo(expectedEarlyPaceBias).Within(0.001f));
            Assert.That(racer.FindPropertyRelative("_aiLatePaceBias").floatValue, Is.EqualTo(expectedLatePaceBias).Within(0.001f));
            Assert.That(racer.FindPropertyRelative("_aiBurstChance").floatValue, Is.EqualTo(expectedBurstChance).Within(0.001f));
            Assert.That(racer.FindPropertyRelative("_aiSlumpChance").floatValue, Is.EqualTo(expectedSlumpChance).Within(0.001f));
            Assert.That(racer.FindPropertyRelative("_aiFinalPushChance").floatValue, Is.EqualTo(expectedFinalPushChance).Within(0.001f));
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static GameObject FindChild(GameObject parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            if (name.IndexOf('/') >= 0)
            {
                var separator = name.IndexOf('/');
                var rootName = name.Substring(0, separator);
                var childPath = name.Substring(separator + 1);
                var transforms = parent.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    if (transforms[i].name != rootName)
                    {
                        continue;
                    }

                    var child = transforms[i].Find(childPath);
                    if (child != null)
                    {
                        return child.gameObject;
                    }
                }

                return null;
            }

            var children = parent.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return children[i].gameObject;
                }
            }

            return null;
        }

        private static void AssertSpriteName(GameObject gameObject, string expectedSpriteName)
        {
            Assert.NotNull(gameObject, expectedSpriteName);
            var image = gameObject.GetComponent<Image>();
            Assert.NotNull(image, expectedSpriteName);
            Assert.NotNull(image.sprite, expectedSpriteName);
            Assert.AreEqual(expectedSpriteName, image.sprite.name);
        }

        private static void AssertViewportMaskWritesStencil(GameObject mainMenuLayer)
        {
            var viewport = FindChild(mainMenuLayer, "Viewport");
            Assert.NotNull(viewport, "Viewport");

            var mask = viewport.GetComponent<Mask>();
            Assert.NotNull(mask, "Viewport Mask");
            Assert.IsTrue(mask.enabled, "Viewport Mask must remain enabled for swipe-page clipping.");
            Assert.IsFalse(mask.showMaskGraphic, "Viewport Mask graphic should stay hidden.");

            var image = viewport.GetComponent<Image>();
            Assert.NotNull(image, "Viewport Image");
            Assert.AreEqual(1f, image.color.a, "Viewport Image alpha must stay opaque so the Mask writes stencil.");
        }

        private static void AssertPageMasksClipBleedingBackgrounds(GameObject mainMenuLayer)
        {
            AssertPageMask(mainMenuLayer, "COMING_SOON_LEFTPage");
            AssertPageMask(mainMenuLayer, "SHOPPage");
            AssertPageMask(mainMenuLayer, "HOMEPage");
            AssertPageMask(mainMenuLayer, "LEADERBOARDPage");
            AssertPageMask(mainMenuLayer, "COMING_SOON_RIGHTPage");
        }

        private static void AssertPageMask(GameObject mainMenuLayer, string pageName)
        {
            var page = FindChild(mainMenuLayer, pageName);
            Assert.NotNull(page, pageName);
            Assert.NotNull(page.GetComponent<RectMask2D>(), pageName + " must clip its oversized background.");
        }

        private static void AssertNavbarFitsBottomEdge(GameObject mainMenuLayer)
        {
            var navbar = FindChild(mainMenuLayer, "BottomNavbar");
            Assert.NotNull(navbar, "BottomNavbar");

            var rect = navbar.GetComponent<RectTransform>();
            Assert.NotNull(rect, "BottomNavbar RectTransform");
            Assert.AreEqual(new Vector2(0f, 0f), rect.anchorMin);
            Assert.AreEqual(new Vector2(1f, 0f), rect.anchorMax);
            Assert.AreEqual(Vector2.zero, rect.offsetMin);
            Assert.AreEqual(new Vector2(0f, 190f), rect.offsetMax);

            Assert.IsNull(FindChild(mainMenuLayer, "SelectionIndicator"), "SelectionIndicator is not used by the current navbar design.");

            var itemsRoot = FindChild(mainMenuLayer, "NavbarItems");
            Assert.NotNull(itemsRoot, "NavbarItems");
            var itemsRect = itemsRoot.GetComponent<RectTransform>();
            Assert.NotNull(itemsRect, "NavbarItems RectTransform");
            Assert.AreEqual(Vector2.zero, itemsRect.offsetMin);
            Assert.AreEqual(Vector2.zero, itemsRect.offsetMax);

            var layout = itemsRoot.GetComponent<HorizontalLayoutGroup>();
            Assert.NotNull(layout, "NavbarItems HorizontalLayoutGroup");
            Assert.AreEqual(0f, layout.spacing);

            var navbarItems = itemsRoot.GetComponentsInChildren<NavbarItem>(true);
            Assert.AreEqual(5, navbarItems.Length);
            for (var i = 0; i < navbarItems.Length; i++)
            {
                var layoutElement = navbarItems[i].GetComponent<LayoutElement>();
                Assert.NotNull(layoutElement, navbarItems[i].name + " LayoutElement");
                Assert.AreEqual(190f, layoutElement.preferredHeight);

                var itemCanvas = navbarItems[i].GetComponent<Canvas>();
                Assert.NotNull(itemCanvas, navbarItems[i].name + " Canvas");
                Assert.IsFalse(itemCanvas.overrideSorting, navbarItems[i].name + " inactive Canvas overrideSorting");
                Assert.AreEqual(0, itemCanvas.sortingOrder, navbarItems[i].name + " inactive sorting order");

                var itemRaycaster = navbarItems[i].GetComponent<GraphicRaycaster>();
                Assert.NotNull(itemRaycaster, navbarItems[i].name + " GraphicRaycaster");

                var itemImage = navbarItems[i].GetComponent<Image>();
                Assert.NotNull(itemImage, navbarItems[i].name + " root Image");
                Assert.IsTrue(itemImage.raycastTarget, navbarItems[i].name + " root Image raycastTarget");

                var background = FindChild(navbarItems[i].gameObject, "Background");
                Assert.NotNull(background, navbarItems[i].name + " Background");
                var backgroundRect = background.GetComponent<RectTransform>();
                Assert.NotNull(backgroundRect, navbarItems[i].name + " Background RectTransform");
                Assert.AreEqual(new Vector2(0f, 0f), backgroundRect.anchorMin);
                Assert.AreEqual(new Vector2(1f, 1f), backgroundRect.anchorMax);
                Assert.AreEqual(new Vector2(0.5f, 0f), backgroundRect.pivot);

                var backgroundImage = background.GetComponent<Image>();
                Assert.NotNull(backgroundImage, navbarItems[i].name + " Background Image");
                Assert.IsFalse(backgroundImage.raycastTarget, navbarItems[i].name + " Background raycastTarget");

                var icon = FindChild(navbarItems[i].gameObject, "Icon");
                Assert.NotNull(icon, navbarItems[i].name + " Icon");
                var iconImage = icon.GetComponent<Image>();
                Assert.NotNull(iconImage, navbarItems[i].name + " Icon Image");
                Assert.IsFalse(iconImage.raycastTarget, navbarItems[i].name + " Icon raycastTarget");

                var serialized = new SerializedObject(navbarItems[i]);
                Assert.AreSame(itemCanvas, serialized.FindProperty("_sortingCanvas").objectReferenceValue);
                Assert.AreSame(backgroundRect, serialized.FindProperty("_backgroundTransform").objectReferenceValue);
                AssertVector2(new Vector2(232f / 166f, 219f / 173f), serialized.FindProperty("_activeBackgroundScale").vector2Value);
                AssertVector2(Vector2.one, serialized.FindProperty("_inactiveBackgroundScale").vector2Value);
                Assert.AreEqual(3f, serialized.FindProperty("_activeBackgroundYOffset").floatValue);
                Assert.AreEqual(30, serialized.FindProperty("_activeSortingOrder").intValue);
                Assert.AreEqual(0, serialized.FindProperty("_inactiveSortingOrder").intValue);
            }
        }

        private static void AssertFakeLeaderboard(GameObject mainMenuLayer)
        {
            var leaderboardPage = FindChild(mainMenuLayer, "LEADERBOARDPage");
            Assert.NotNull(leaderboardPage, "LEADERBOARDPage");

            var panel = FindChild(leaderboardPage, "FakeLeaderboardPanel");
            Assert.NotNull(panel, "FakeLeaderboardPanel");
            AssertSpriteName(panel, "UI-pack_Sprite_2_0");

            for (var i = 1; i <= 8; i++)
            {
                var row = FindChild(panel, "FakeLeaderboardRow_" + i.ToString());
                Assert.NotNull(row, "FakeLeaderboardRow_" + i.ToString());
                AssertSpriteName(row, "UI-pack_Sprite_1_46");
                Assert.NotNull(FindChild(row, "Rank"), "Rank text " + i.ToString());
                Assert.NotNull(FindChild(row, "Name"), "Name text " + i.ToString());
                Assert.NotNull(FindChild(row, "Score"), "Score text " + i.ToString());
                AssertFakeLeaderboardTextColors(row, i == 3);
            }
        }

        private static void AssertFakeLeaderboardTextColors(GameObject row, bool isPlayerRow)
        {
            var expectedNameColor = isPlayerRow
                ? new Color(0.55f, 0.12f, 0.78f, 1f)
                : new Color(0.18f, 0.22f, 0.42f, 1f);
            var expectedScoreColor = isPlayerRow
                ? new Color(0.55f, 0.12f, 0.78f, 1f)
                : new Color(0.06f, 0.27f, 0.65f, 1f);

            AssertColor(FindChild(row, "Name").GetComponent<TMPro.TMP_Text>().color, expectedNameColor, row.name + " name color");
            AssertColor(FindChild(row, "ScoreLabel").GetComponent<TMPro.TMP_Text>().color, new Color(0.10f, 0.42f, 0.84f, 1f), row.name + " score label color");
            AssertColor(FindChild(row, "Score").GetComponent<TMPro.TMP_Text>().color, expectedScoreColor, row.name + " score color");
        }

        private static void AssertShopShowcase(GameObject mainMenuLayer)
        {
            var shopPage = FindChild(mainMenuLayer, "SHOPPage");
            Assert.NotNull(shopPage, "SHOPPage");

            var panel = FindChild(shopPage, "ShopShowcasePanel");
            Assert.NotNull(panel, "ShopShowcasePanel");

            var currencyRow = FindChild(panel, "ShopCurrencyRow");
            Assert.NotNull(currencyRow, "ShopCurrencyRow");
            AssertSpriteName(FindChild(currencyRow, "CoinCurrency/Icon"), "UI-pack_Sprite_1_22");
            AssertSpriteName(FindChild(currencyRow, "GemCurrency/Icon"), "UI-pack_Sprite_1_16");

            AssertShopPack(panel, "ShopPack_0", "UI-pack_Sprite_1_56", "UI-pack_Sprite_1_30");
            AssertShopPack(panel, "ShopPack_1", "UI-pack_Sprite_1_57", "UI-pack_Sprite_1_47");
            AssertShopPack(panel, "ShopPack_2", "UI-pack_Sprite_1_56", "UI-pack_Sprite_1_31");
            AssertShopPack(panel, "ShopPack_3", "UI-pack_Sprite_1_57", "UI-pack_Sprite_1_11");
            AssertShopPack(panel, "ShopPack_4", "UI-pack_Sprite_1_56", "UI-pack_Sprite_1_13");
            AssertShopPack(panel, "ShopPack_5", "UI-pack_Sprite_1_57", "UI-pack_Sprite_1_42");
        }

        private static void AssertShopPack(GameObject panel, string packName, string backgroundSprite, string itemSprite)
        {
            var pack = FindChild(panel, packName);
            Assert.NotNull(pack, packName);
            AssertSpriteName(pack, backgroundSprite);
            AssertSpriteName(FindChild(pack, "ItemIcon"), itemSprite);
            Assert.NotNull(FindChild(pack, "Title"), packName + " title");
            Assert.NotNull(FindChild(pack, "Subtitle"), packName + " subtitle");
            Assert.NotNull(FindChild(pack, "PriceTag"), packName + " price tag");
            Assert.NotNull(FindChild(pack, "PriceTag/Price"), packName + " price");
        }

        private static void AssertSkyRaceHudRow(
            GameObject hudPrefab,
            string rowName,
            string expectedRacerId,
            string expectedNamePlateSprite,
            string expectedAvatarSprite,
            string expectedMarkerSprite)
        {
            var row = FindChild(hudPrefab, rowName);
            Assert.NotNull(row, rowName);

            var rowView = row.GetComponent<RacerHudRowView>();
            Assert.NotNull(rowView, rowName);

            var serialized = new SerializedObject(rowView);
            Assert.AreEqual(expectedRacerId, serialized.FindProperty("_racerId").stringValue, rowName);
            AssertReference<RectTransform>(serialized, "_rectTransform");
            AssertReference<TMPro.TMP_Text>(serialized, "_rankText");
            AssertReference<TMPro.TMP_Text>(serialized, "_nameText");
            AssertReference<TMPro.TMP_Text>(serialized, "_progressText");
            AssertReference<TMPro.TMP_Text>(serialized, "_finishText");
            AssertReference<Image>(serialized, "_progressFill");
            AssertReference<Image>(serialized, "_playerAccent");
            AssertReference<RectTransform>(serialized, "_progressTrack");
            AssertReference<RectTransform>(serialized, "_progressMover");
            AssertReference<Image>(serialized, "_leaderCrown");

            var namePlate = FindChild(row, "NamePlate");
            var rankBadge = FindChild(row, "RankBadge");
            var leaderCrown = FindChild(row, "RankBadge/LeaderCrown");
            var progressMover = FindChild(row, "ProgressMover");
            var finishText = FindChild(row, "FinishText");

            Assert.NotNull(namePlate, rowName + " NamePlate");
            Assert.NotNull(leaderCrown, rowName + " LeaderCrown");
            Assert.NotNull(FindChild(row, "ProgressTrack"), rowName + " ProgressTrack");
            Assert.NotNull(progressMover, rowName + " ProgressMover");
            Assert.NotNull(finishText, rowName + " FinishText");
            AssertSpriteName(namePlate, expectedNamePlateSprite);
            AssertSpriteName(rankBadge, expectedAvatarSprite);
            AssertSpriteName(leaderCrown, "raceHudElementsSheet_6");
            AssertSpriteName(progressMover, expectedMarkerSprite);
            Assert.IsFalse(leaderCrown.activeSelf, rowName + " LeaderCrown saved hidden");

            var namePlateRect = namePlate.GetComponent<RectTransform>();
            var rankBadgeRect = rankBadge.GetComponent<RectTransform>();
            var leaderCrownRect = leaderCrown.GetComponent<RectTransform>();
            var progressMoverRect = progressMover.GetComponent<RectTransform>();
            var finishTextRect = finishText.GetComponent<RectTransform>();
            var finishTextLabel = finishText.GetComponent<TMPro.TMP_Text>();
            AssertVector2(new Vector2(188f, 24f), namePlateRect.anchoredPosition);
            AssertVector2(new Vector2(360f, 106f), namePlateRect.sizeDelta);
            AssertVector2(new Vector2(58f, 0f), rankBadgeRect.anchoredPosition);
            AssertVector2(new Vector2(96f, 96f), rankBadgeRect.sizeDelta);
            AssertVector2(new Vector2(6f, 8f), leaderCrownRect.anchoredPosition);
            AssertVector2(new Vector2(42f, 32f), leaderCrownRect.sizeDelta);
            Assert.IsFalse(leaderCrown.GetComponent<Image>().raycastTarget, rowName + " LeaderCrown raycast target");
            AssertVector2(new Vector2(-230f, 24f), progressMoverRect.anchoredPosition);
            AssertVector2(new Vector2(178f, 100f), progressMoverRect.sizeDelta);
            AssertVector2(new Vector2(70f, 44f), finishTextRect.anchoredPosition);
            AssertVector2(new Vector2(260f, 100f), finishTextRect.sizeDelta);
            Assert.NotNull(finishTextLabel, rowName + " FinishText label");
            Assert.AreEqual(76f, finishTextLabel.fontSize);
            AssertColor(finishTextLabel.color, new Color(1f, 0.91f, 0.56f, 1f), rowName + " FinishText color");
        }

        private static void AssertFloatingChest(
            GameObject hudPrefab,
            string chestName,
            float expectedPixels,
            float expectedCycleDuration,
            float expectedPhaseOffset)
        {
            var chest = FindChild(hudPrefab, chestName);
            Assert.NotNull(chest, chestName);

            var animator = chest.GetComponent<FloatingUiElementAnimator>();
            Assert.NotNull(animator, chestName + " floating animator");

            var serialized = new SerializedObject(animator);
            Assert.AreSame(chest.GetComponent<RectTransform>(), serialized.FindProperty("_target").objectReferenceValue);
            Assert.That(serialized.FindProperty("_floatPixels").floatValue, Is.EqualTo(expectedPixels).Within(0.001f));
            Assert.That(serialized.FindProperty("_cycleDurationSeconds").floatValue, Is.EqualTo(expectedCycleDuration).Within(0.001f));
            Assert.That(serialized.FindProperty("_phaseOffsetSeconds").floatValue, Is.EqualTo(expectedPhaseOffset).Within(0.001f));
        }

        private static void AssertAllTextsUseFont(GameObject root, TMPro.TMP_FontAsset expectedFont)
        {
            Assert.NotNull(root, "Text root");
            var texts = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
            Assert.Greater(texts.Length, 0, root.name + " TMP_Text collection");

            foreach (var text in texts)
            {
                Assert.AreSame(expectedFont, text.font, root.name + "/" + text.name);
                Assert.AreSame(expectedFont.material, text.fontSharedMaterial, root.name + "/" + text.name + " material");
            }
        }

        private static void AssertMainMenuStartsAtHomeAndLocksOuterPages(SwipePageController swipeController)
        {
            var serialized = new SerializedObject(swipeController);

            Assert.IsFalse(serialized.FindProperty("_startAtMiddlePage").boolValue);
            Assert.AreEqual(2, serialized.FindProperty("_startPageIndex").intValue);
            Assert.IsTrue(serialized.FindProperty("_useAllowedPageRange").boolValue);
            Assert.AreEqual(1, serialized.FindProperty("_minAllowedPageIndex").intValue);
            Assert.AreEqual(3, serialized.FindProperty("_maxAllowedPageIndex").intValue);
        }

        private static void AssertComingSoonNavItem(GameObject mainMenuLayer, string itemName)
        {
            var item = FindChild(mainMenuLayer, itemName);
            Assert.NotNull(item, itemName);
            var navbarItem = item.GetComponent<NavbarItem>();
            Assert.NotNull(navbarItem, itemName);
            Assert.IsTrue(navbarItem.IsComingSoon, itemName);
        }

        private static void AssertComingSoonTooltip(GameObject mainMenuLayer)
        {
            var tooltip = FindChild(mainMenuLayer, "ComingSoonTooltip");
            Assert.NotNull(tooltip, "ComingSoonTooltip");
            var animation = tooltip.GetComponent<LockedNavbarTooltip>();
            Assert.NotNull(animation, "ComingSoonTooltip animation");
            Assert.IsNull(tooltip.GetComponent<Image>(), "ComingSoonTooltip root must not mirror the label with the bubble background.");

            var canvasGroup = tooltip.GetComponent<CanvasGroup>();
            Assert.NotNull(canvasGroup, "ComingSoonTooltip CanvasGroup");
            Assert.AreEqual(0f, canvasGroup.alpha, "ComingSoonTooltip alpha");
            Assert.IsFalse(canvasGroup.interactable, "ComingSoonTooltip interactable");
            Assert.IsFalse(canvasGroup.blocksRaycasts, "ComingSoonTooltip blocksRaycasts");

            var background = FindChild(tooltip, "Background");
            Assert.NotNull(background, "ComingSoonTooltip Background");
            AssertSpriteName(background, "comingsoon_bubble");

            var serialized = new SerializedObject(animation);
            Assert.AreSame(background.GetComponent<RectTransform>(), serialized.FindProperty("_backgroundRoot").objectReferenceValue);
        }

        private static void AssertColor(Color actual, Color expected, string label)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.001f), label + " r");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.001f), label + " g");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.001f), label + " b");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.001f), label + " a");
        }

        private static void AssertVector2(Vector2 expected, Vector2 actual)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f), "Vector2 x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f), "Vector2 y");
        }

        private static int CountMissingScripts(GameObject root)
        {
            if (root == null)
            {
                return 0;
            }

            var missingCount = 0;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                missingCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);
            }

            return missingCount;
        }

        private static void AssertReferencesUiKitSprite(string prefabPath)
        {
            var dependencies = AssetDatabase.GetDependencies(prefabPath, true);
            for (var i = 0; i < dependencies.Length; i++)
            {
                if (dependencies[i].StartsWith(UiKitSpritesRoot, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            Assert.Fail(prefabPath + " does not reference a required UI Kit sprite.");
        }
    }
}
