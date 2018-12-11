using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.Entities;
using OrchardCore.FileStorage;
using OrchardCore.Media.Settings;
using OrchardCore.Settings;

namespace OrchardCore.Media.Services
{
    public class PurgeTempFilesService
    {
        private readonly IMediaFileStore _mediaFileStore;
        private readonly ISiteService _siteService;
        private readonly MediaFieldLimitedEditorFileService _mediaFieldLimitedEditorFileService;

        /// <summary>
        /// Deletes old temporary files from the Media Fields Temp Directory
        /// </summary>        
        public PurgeTempFilesService(
            IMediaFileStore mediaFileStore,
            ISiteService siteService,
            MediaFieldLimitedEditorFileService mediaFieldLimitedEditorFileService,
            ILogger<PurgeTempFilesService> logger,
            IStringLocalizer<PurgeTempFilesService> stringLocalizer)
        {
            _mediaFileStore = mediaFileStore;
            _siteService = siteService;
            _mediaFieldLimitedEditorFileService = mediaFieldLimitedEditorFileService;

            Logger = logger;
            S = stringLocalizer;
        }

        public ILogger Logger { get; }
        public IStringLocalizer<PurgeTempFilesService> S { get; }


        public async Task PurgeAsync()
        {
            try
            {
                var maxAge = await GetMaxAgeSettingAsync();
                if (maxAge == 0) // By convention, 0 means never purge
                {
                    return;
                }

                // delete old files
                (await GetOldFilesAsync(maxAge)).ForEach(async x => await _mediaFileStore.TryDeleteFileAsync(x.Path));

                // delete old empty dirs
                (await GetOldEmptyDirsAsync(maxAge)).ForEach(async x => await _mediaFileStore.TryDeleteDirectoryAsync(x.Path));

            }
            catch (Exception exception)
            {
                Logger.LogError(exception, S["an error occurred while cleaning old temporary asset files."]);
            }

        }


        private async Task<int> GetMaxAgeSettingAsync()
        {
            var siteSettings = await _siteService.GetSiteSettingsAsync();
            return siteSettings.As<MediaSiteSettings>().LimitedEditorDeleteTempFilesOlderThan;
        }


        private async Task<List<IFileStoreEntry>> GetOldFilesAsync(int maxAge)
        {
            var allFiles = await _mediaFileStore.GetDirectoryContentAsync(_mediaFieldLimitedEditorFileService.MediaFieldsTempSubFolder, true);

            var oldFiles = allFiles.ToList()
                .Where(x => !x.IsDirectory && IsOld(x, maxAge));

            return oldFiles.ToList();
        }


        private async Task<List<IFileStoreEntry>> GetOldEmptyDirsAsync(int maxAge)
        {
            var allFiles = await _mediaFileStore.GetDirectoryContentAsync(_mediaFieldLimitedEditorFileService.MediaFieldsTempSubFolder, true);

            var oldAndEmpty = new List<IFileStoreEntry>();

            allFiles.ToList().ForEach(async x =>
            {
                if (x.IsDirectory
                && IsOld(x, maxAge)
                && (await _mediaFileStore.GetDirectoryContentAsync(x.Path)).Count() < 1)
                {
                    oldAndEmpty.Add(x);
                }
            });

            return oldAndEmpty.ToList();
        }


        private bool IsOld(IFileStoreEntry file, int ageRequiredInMinutes)
        {
            var timeLimit = file.LastModifiedUtc.AddMinutes(ageRequiredInMinutes);

            return DateTime.Now.ToUniversalTime().Ticks > timeLimit.Ticks;
        }
    }
}
