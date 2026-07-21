using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ChemistryLab.Domain;
using ChemistryLab.Presentation;

namespace ChemistryLab.Presentation.UnityViews
{
    /// <summary>
    /// Unity Composition Root. Attach this MonoBehaviour to a GameObject named "GameManager" in your scene.
    /// It initializes AppBootstrap, loads the JSON catalogue, opens the save file, and connects the Presenter to the View.
    /// </summary>
    public sealed class GameBootstrapManager : MonoBehaviour
    {
        [Header("Views in Scene")]
        [SerializeField] private MainLabUnityView mainLabView;

        private AppBootstrap appBootstrap;
        private MainLabPresenter mainLabPresenter;

        private async void Start()
        {
            Debug.Log("[GameBootstrapManager] Starting Chemistry Lab initialization sequence...");
            
            var stateController = new AppStateController(AppState.Boot);
            var catalogueLoader = new ResourcesContentCatalogueLoader("chemistry_catalogue");
            var saveFactory = new UnitySaveRepositoryFactory("player_save.dat");

            appBootstrap = new AppBootstrap(stateController, catalogueLoader, saveFactory);

            appBootstrap.InitializationCompleted += OnInitializationCompleted;
            appBootstrap.InitializationFailed += OnInitializationFailed;

            try
            {
                await appBootstrap.InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError("[GameBootstrapManager] Fatal error during boot: " + ex);
            }
        }

        private void OnInitializationCompleted()
        {
            Debug.Log("[GameBootstrapManager] Initialization SUCCESS! AppState is now: " + appBootstrap.StateController.State);
            
            if (appBootstrap.SaveRepository != null && !appBootstrap.SaveRepository.HasData)
            {
                InitializeNewPlayerSave();
            }

            if (mainLabView != null && appBootstrap.ContentCatalogue != null && appBootstrap.SaveRepository != null)
            {
                mainLabPresenter = new MainLabPresenter(
                    appBootstrap.ContentCatalogue,
                    appBootstrap.SaveRepository,
                    mainLabView);

                mainLabView.BindPresenter(mainLabPresenter);
                Debug.Log("[GameBootstrapManager] Bound MainLabPresenter to MainLabUnityView successfully.");
            }
            else
            {
                Debug.LogWarning("[GameBootstrapManager] mainLabView reference is missing in GameBootstrapManager inspector!");
            }
        }

        private void InitializeNewPlayerSave()
        {
            Debug.Log("[GameBootstrapManager] Initializing new player starter profile and inventory...");
            var profile = new PlayerProfile("player_1", dollars: 500, level: 1, avatar: new AvatarData());
            var starterInventory = new Dictionary<string, decimal>(StringComparer.Ordinal)
            {
                { "water", 100m },
                { "salt", 50m },
                { "hcl", 50m },
                { "naoh", 50m }
            };
            var starterTools = new List<ToolState>
            {
                new ToolState("beaker_100ml", isOwned: true, cleanliness: ToolCleanState.Clean, storageState: ToolStorageState.Stored)
            };

            appBootstrap.SaveRepository.SaveState(profile, starterInventory, starterTools);
            Debug.Log("[GameBootstrapManager] New player save initialized successfully!");
        }

        private void OnInitializationFailed(Exception ex)
        {
            Debug.LogError("[GameBootstrapManager] Initialization completed with failure event: " + ex.Message);
        }
    }
}
