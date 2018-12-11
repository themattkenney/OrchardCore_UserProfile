using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Media.Settings;
using OrchardCore.Media.ViewModels;
using OrchardCore.Settings;

namespace OrchardCore.Media.Drivers
{
    public class MediaSiteSettingsDisplayDriver : SectionDisplayDriver<ISite, MediaSiteSettings>
    {
        public const string GroupId = "media";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthorizationService _authorizationService;

        public MediaSiteSettingsDisplayDriver(
            IHttpContextAccessor httpContextAccessor,
            IAuthorizationService authorizationService)
        {
            _httpContextAccessor = httpContextAccessor;
            _authorizationService = authorizationService;
        }

        public override async Task<IDisplayResult> EditAsync(MediaSiteSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageMedia))
            {
                return null;
            }

            return Initialize<MediaSiteSettingsViewModel>("MediaSiteSettings_Edit", model =>
                {
                    model.LimitedEditorDeleteTempFilesOlderThan = settings.LimitedEditorDeleteTempFilesOlderThan;
                }).Location("Content:3").OnGroup(GroupId);
        }

        public override async Task<IDisplayResult> UpdateAsync(MediaSiteSettings settings, BuildEditorContext context)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageMedia))
            {
                return null;
            }

            if (context.GroupId == GroupId)
            {
                var model = new MediaSiteSettingsViewModel();

                await context.Updater.TryUpdateModelAsync(model, Prefix);

                settings.LimitedEditorDeleteTempFilesOlderThan = model.LimitedEditorDeleteTempFilesOlderThan;                
            }

            return await EditAsync(settings, context);
        }
    }
}
