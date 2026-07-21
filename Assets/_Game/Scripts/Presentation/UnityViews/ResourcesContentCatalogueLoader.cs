using System;
using System.Threading.Tasks;
using UnityEngine;
using ChemistryLab.Application;
using ChemistryLab.Infrastructure;
using ChemistryLab.Presentation;

namespace ChemistryLab.Presentation.UnityViews
{
    /// <summary>
    /// Loads the JSON content catalogue from Unity's Resources folder using TextAsset.
    /// </summary>
    public sealed class ResourcesContentCatalogueLoader : IContentCatalogueLoader
    {
        private readonly string resourcePath;

        public ResourcesContentCatalogueLoader(string resourcePath = "chemistry_catalogue")
        {
            this.resourcePath = resourcePath;
        }

        public Task<ContentCatalogue> LoadAsync()
        {
            var tcs = new TaskCompletionSource<ContentCatalogue>();
            try
            {
                var textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    tcs.SetException(new InvalidOperationException("Could not find TextAsset at Resources path: " + resourcePath));
                    return tcs.Task;
                }

                var importer = new ContentImporter();
                var catalogue = importer.ImportFromJson(textAsset.text);
                tcs.SetResult(catalogue);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }
    }
}
