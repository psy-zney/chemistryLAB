using System;
using System.Threading.Tasks;
using ChemistryLab.Application;
using ChemistryLab.Infrastructure;

namespace ChemistryLab.Presentation
{
    /// <summary>Boundary implemented by Addressables, Resources, or a test double.</summary>
    public interface IContentCatalogueLoader
    {
        Task<ContentCatalogue> LoadAsync();
    }

    /// <summary>Boundary implemented by the platform-specific persistence composition root.</summary>
    public interface ISaveRepositoryFactory
    {
        Task<ISaveRepository> CreateAsync();
    }

    /// <summary>
    /// Initializes mandatory services once and exposes retryable error recovery.
    /// It deliberately has no MonoBehaviour or Unity UI dependency, so edit-mode
    /// tests can exercise the complete state-transition flow.
    /// </summary>
    public sealed class AppBootstrap
    {
        private readonly IContentCatalogueLoader catalogueLoader;
        private readonly ISaveRepositoryFactory saveRepositoryFactory;
        private Task initializeTask;

        public AppBootstrap(
            AppStateController stateController,
            IContentCatalogueLoader catalogueLoader,
            ISaveRepositoryFactory saveRepositoryFactory)
        {
            StateController = stateController ?? throw new ArgumentNullException(nameof(stateController));
            this.catalogueLoader = catalogueLoader ?? throw new ArgumentNullException(nameof(catalogueLoader));
            this.saveRepositoryFactory = saveRepositoryFactory ?? throw new ArgumentNullException(nameof(saveRepositoryFactory));
        }

        public AppStateController StateController { get; private set; }
        public ContentCatalogue ContentCatalogue { get; private set; }
        public ISaveRepository SaveRepository { get; private set; }
        public Exception LastError { get; private set; }
        public bool IsReady { get { return StateController.State == AppState.MainLab; } }

        public event Action<Exception> InitializationFailed;
        public event Action InitializationCompleted;

        public Task InitializeAsync()
        {
            if (IsReady)
            {
                return Task.FromResult(0);
            }

            if (initializeTask != null && !initializeTask.IsCompleted)
            {
                return initializeTask;
            }

            if (StateController.State != AppState.Boot && StateController.State != AppState.ErrorRecovery)
            {
                throw new InvalidOperationException("Bootstrap can start only from Boot or ErrorRecovery.");
            }

            initializeTask = InitializeCoreAsync();
            return initializeTask;
        }

        public Task RetryAsync()
        {
            if (StateController.State != AppState.ErrorRecovery)
            {
                throw new InvalidOperationException("Retry is available only while recovering from an initialization error.");
            }

            return InitializeAsync();
        }

        private async Task InitializeCoreAsync()
        {
            StateController.TransitionTo(AppState.Loading);
            LastError = null;

            try
            {
                var loadedCatalogue = await catalogueLoader.LoadAsync();
                if (loadedCatalogue == null)
                {
                    throw new InvalidOperationException("The content catalogue loader returned no catalogue.");
                }

                var repository = await saveRepositoryFactory.CreateAsync();
                if (repository == null)
                {
                    throw new InvalidOperationException("The save repository factory returned no repository.");
                }

                ContentCatalogue = loadedCatalogue;
                SaveRepository = repository;
                StateController.TransitionTo(AppState.MainLab);
                var completed = InitializationCompleted;
                if (completed != null)
                {
                    completed();
                }
            }
            catch (Exception exception)
            {
                ContentCatalogue = null;
                SaveRepository = null;
                LastError = exception;
                StateController.TransitionTo(AppState.ErrorRecovery);
                var failed = InitializationFailed;
                if (failed != null)
                {
                    failed(exception);
                }
            }
        }
    }
}
