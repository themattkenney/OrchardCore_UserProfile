using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.FileStorage;
using OrchardCore.Security;

namespace OrchardCore.Media.Services
{    /// <summary>
     /// Check if the path passed as resource is inside the MediaFieldsFolder, and in case it is, It checks if the user has ManageMediaFieldsFolder permission
     /// </summary>     
    public class MediaFieldsFolderAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly MediaFieldLimitedEditorFileService _mediaFieldLimitedEditorFileService;
        private readonly IMediaFileStore _fileStore;

        public MediaFieldsFolderAuthorizationHandler(IServiceProvider serviceProvider,
            MediaFieldLimitedEditorFileService mediaFieldLimitedEditorFileService,
            IMediaFileStore fileStore)
        {
            _serviceProvider = serviceProvider;
            _mediaFieldLimitedEditorFileService = mediaFieldLimitedEditorFileService;
            _fileStore = fileStore;
        }


        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.HasSucceeded)
            {
                // This handler is not revoking any pre-existing grants.
                return;
            }

            if (requirement.Permission.Name != Permissions.ManageMediaFieldsFolder.Name)
            {
                return;
            }

            var path = context.Resource as string;
                        
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!IsDescendantOfMediaFieldsFolder(path))
            {
                context.Succeed(requirement);
            }

            // If we get to here, the path is on the media fields folder and the user must have the ManageMediaFieldsFolder permission.
            // Lazy load to prevent circular dependencies
            var authorizationService = _serviceProvider.GetService<IAuthorizationService>();

            if (await authorizationService.AuthorizeAsync(context.User, Permissions.ManageMediaFieldsFolder))
            {
                context.Succeed(requirement);
            }
        }


        private bool IsDescendantOfMediaFieldsFolder(string childPath)
        {
            childPath = _fileStore.NormalizePath(childPath);
            var parentPath = _fileStore.NormalizePath(_mediaFieldLimitedEditorFileService.MediaFieldsFolder);
            
            // A way to discover the path's separator on the current IFileStorage implementation
            var separator = _fileStore.Combine("a","b").Contains("/") ? "/" : "\\";

            var parentSegments = parentPath.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            var childSegments = childPath.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parentSegments.Length; i++)
            {
                if (!string.Equals( parentSegments[i], childSegments[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
