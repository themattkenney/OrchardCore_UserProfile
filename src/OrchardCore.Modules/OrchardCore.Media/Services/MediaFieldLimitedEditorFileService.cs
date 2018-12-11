using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.FileStorage;
using OrchardCore.Media.ViewModels;

namespace OrchardCore.Media.Services
{
    /// <summary>
    /// Handles file management operations related to the limited media field editor
    /// </summary>    
    public class MediaFieldLimitedEditorFileService
    {
        private readonly IMediaFileStore _fileStore;
        private readonly IStringLocalizer<MediaFieldLimitedEditorFileService> T;

        public MediaFieldLimitedEditorFileService(IMediaFileStore fileStore,
            IStringLocalizer<MediaFieldLimitedEditorFileService> stringLocalizer)
        {
            _fileStore = fileStore;
            T = stringLocalizer;

            MediaFieldsFolder = "mediafields";
            MediaFieldsTempSubFolder = _fileStore.Combine(MediaFieldsFolder, "temp");
            MediaFieldsTrashSubFolder = _fileStore.Combine(MediaFieldsFolder, "trash");
        }

        public string MediaFieldsFolder { get; }
        public string MediaFieldsTempSubFolder { get; }
        public string MediaFieldsTrashSubFolder { get; }



        public async Task HandleFilesOnFieldUpdateAsync(List<EditMediaFieldItemInfo> items, string contentItemId)
        {
            if (items == null)
            {
                throw new System.ArgumentNullException(nameof(items));
            }

            await EnsureGlobalDirectoriesExist();

            RemoveTemporary(items);

            MoveDeletedToTrash(items, contentItemId);

            MoveUsedToContentItemDirAndUpdateTheirPaths(items, contentItemId);


        }

        private async Task EnsureGlobalDirectoriesExist()
        {
            await _fileStore.TryCreateDirectoryAsync(MediaFieldsFolder);
            await _fileStore.TryCreateDirectoryAsync(MediaFieldsTempSubFolder);
            await _fileStore.TryCreateDirectoryAsync(MediaFieldsTrashSubFolder);
        }

        // Remove temp files: Files that are just uploaded and then inmediately discarded.
        private void RemoveTemporary(List<EditMediaFieldItemInfo> items)
        {
            items.Where(x => x.IsRemoved && x.IsNew).ToList().ForEach(async x => await _fileStore.TryDeleteFileAsync(x.Path));
        }


        // Files that where used and now are being deleted are moved to a trash folder.
        private void MoveDeletedToTrash(List<EditMediaFieldItemInfo> items, string contentItemId)
        {
            items.Where(x => x.IsRemoved && !x.IsNew).ToList()
                .ForEach(async x =>
                {
                    var fileName = (await _fileStore.GetFileInfoAsync(x.Path)).Name;
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        throw new FileNotFoundException(T["Can't find the file for {0}", x.Path]);
                    }
                    var newPath = _fileStore.Combine(new string[] { MediaFieldsTrashSubFolder, contentItemId + fileName });
                    await _fileStore.MoveFileAsync(x.Path, newPath);
                });
        }


        // Files used are moved from temp to the content item folder, and the path is updated accordingly.
        private void MoveUsedToContentItemDirAndUpdateTheirPaths(List<EditMediaFieldItemInfo> items, string contentItemId)
        {
            // todo: performance; we are getting fileinfo twice.
            items.Where(x => !x.IsRemoved && x.IsNew).ToList()
                .ForEach(async x =>
                {
                    var targetDir = _fileStore.Combine(MediaFieldsFolder, GetSplittedDirName(contentItemId));
                    await _fileStore.TryCreateDirectoryAsync(targetDir);
                    var uploadFileName = (await _fileStore.GetFileInfoAsync(x.Path)).Name;
                    var fileName = await BuildUniqueNameFromTemporaryGuidName(uploadFileName, targetDir);
                    var newPath = _fileStore.Combine(new string[] { targetDir, fileName });
                    await _fileStore.MoveFileAsync(x.Path, newPath);

                    // update the path with the new name and folder
                    x.Path = newPath;
                });
        }



        private async Task<string> BuildUniqueNameFromTemporaryGuidName(string uploadFileName, string targetDir)
        {
            // remove-guid.
            var noGuid = uploadFileName.Substring(36);

            // get extension.
            var lastPoint = noGuid.LastIndexOf(".");
            var ext = lastPoint > -1 ? noGuid.Substring(lastPoint) : "";

            // get filename-without-extension
            var fileNameWithoutExtension = lastPoint > -1 ? noGuid.Substring(0, lastPoint) : noGuid;

            // does this filename exist? -> build another one .
            if (await IsFileNameUnique(fileNameWithoutExtension, ext, targetDir) == false)
            {
                return await GenerateUniqueNameAsync(fileNameWithoutExtension, ext, targetDir);
            }

            return fileNameWithoutExtension + ext;
        }

        private async Task<string> GenerateUniqueNameAsync(string fileNameWithoutExtension, string ext, string targetDir)
        {
            var fileName = fileNameWithoutExtension;
            var version = 1;
            var unversionedFileName = fileName;

            var versionSeparatorPosition = fileName.LastIndexOf("-v-");
            if (versionSeparatorPosition > -1)
            {
                int.TryParse(fileName.Substring(versionSeparatorPosition).TrimStart(new char[] { '-', 'v', '-' }), out version);
                unversionedFileName = fileName.Substring(0, versionSeparatorPosition);
            }

            while (true)
            {
                var versionedFileName = $"{unversionedFileName}-v-{version++}";
                if (await IsFileNameUnique(versionedFileName, ext, targetDir))
                {
                    return versionedFileName + ext;
                }
            }
        }

        private async Task<bool> IsFileNameUnique(string fileName, string fileExtension, string directory)
        {
            return await _fileStore.GetFileInfoAsync(_fileStore.Combine(directory, fileName + fileExtension)) == null;
        }


        /// <summary>
        /// Converts an content item id string into a path composed of several dirs. 
        /// It is a mechanism to avoid having a too long list of dirs in the root dir.
        /// </summary>
        private string GetSplittedDirName(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("Name to split can't be null");
            }

            if (id.Length != 26)
            {
                throw new ArgumentException("Name to split must be 26 characters long.");
            }

            var result = new StringBuilder();
            for (int i = 0; i < 13; i++)
            {
                result.Append(id.Substring(i * 2, 2));
                result.Append("\\");
            }

            return result.ToString().TrimEnd('\\');
        }
    }
}
