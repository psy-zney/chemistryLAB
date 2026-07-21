using System.IO;
using System.Threading.Tasks;
using ChemistryLab.Infrastructure;
using ChemistryLab.Presentation;

namespace ChemistryLab.Presentation.UnityViews
{
    /// <summary>
    /// Creates a SaveRepository saving to Unity's Application.persistentDataPath directory.
    /// </summary>
    public sealed class UnitySaveRepositoryFactory : ISaveRepositoryFactory
    {
        private readonly string fileName;

        public UnitySaveRepositoryFactory(string fileName = "chemistry_player.dat")
        {
            this.fileName = fileName;
        }

        public Task<ISaveRepository> CreateAsync()
        {
            var savePath = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
            ISaveRepository repository = new SaveRepository(savePath);
            return Task.FromResult(repository);
        }
    }
}
