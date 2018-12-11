using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Liquid;
using OrchardCore.Media.Fields;
using OrchardCore.Media.Services;
using OrchardCore.Media.Settings;
using OrchardCore.Media.ViewModels;

namespace OrchardCore.Media.Drivers
{
    public class MediaFieldDisplayDriver : ContentFieldDisplayDriver<MediaField>
    {
        private readonly MediaFieldLimitedEditorFileService _mediaFieldLimitedEditorFileService;

        public MediaFieldDisplayDriver(IMediaFileStore fileStore,
            MediaFieldLimitedEditorFileService mediaFieldLimitedEditorFileService,
            ILiquidTemplateManager liquidTemplateManager,
            IStringLocalizer<MediaFieldDisplayDriver> localizer)
        {
            S = localizer;
            _mediaFieldLimitedEditorFileService = mediaFieldLimitedEditorFileService;
        }

        public IStringLocalizer S { get; set; }

        public override IDisplayResult Display(MediaField field, BuildFieldDisplayContext context)
        {
            return Initialize<DisplayMediaFieldViewModel>(GetDisplayShapeType(context), model =>
            {
                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;
            })
            .Location("Content")
            .Location("SummaryAdmin", "");
        }

        public override IDisplayResult Edit(MediaField field, BuildFieldEditorContext context)
        {
            var itemPaths = field.Paths?.ToList().Select(p => new EditMediaFieldItemInfo { Path = p }) ?? new EditMediaFieldItemInfo[] { };

            return Initialize<EditMediaFieldViewModel>(GetEditorShapeType(context), model =>
            {
                model.Paths = JsonConvert.SerializeObject(itemPaths);
                model.TempUploadFolder = _mediaFieldLimitedEditorFileService.MediaFieldsTempSubFolder;
                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(MediaField field, IUpdateModel updater, UpdateFieldEditorContext context)
        {

            var model = new EditMediaFieldViewModel();


            if (await updater.TryUpdateModelAsync(model, Prefix, f => f.Paths))
            {
                var items = JsonConvert.DeserializeObject<EditMediaFieldItemInfo[]>(model.Paths).ToList();

                // If it's a limited editor the files are automatically handled by _mediaFieldLimitedEditorFileService
                if (string.Equals(context.PartFieldDefinition.Editor(), "Limited", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await _mediaFieldLimitedEditorFileService.HandleFilesOnFieldUpdateAsync(items, context.ContentPart.ContentItem.ContentItemId);
                    }
                    catch (Exception)
                    {
                        updater.ModelState.AddModelError(Prefix, S["{0}: There was an error handling the files.", context.PartFieldDefinition.DisplayName()]);

                    }
                }

                field.Paths = items.Where(p => !p.IsRemoved).Select(p => p.Path).ToArray() ?? new string[] { };

                var settings = context.PartFieldDefinition.Settings.ToObject<MediaFieldSettings>();

                if (settings.Required && field.Paths.Length < 1)
                {
                    updater.ModelState.AddModelError(Prefix, S["{0}: A media is required.", context.PartFieldDefinition.DisplayName()]);
                }

                if (field.Paths.Length > 1 && !settings.Multiple)
                {
                    updater.ModelState.AddModelError(Prefix, S["{0}: Selecting multiple media is forbidden.", context.PartFieldDefinition.DisplayName()]);
                }
            }

            return Edit(field, context);
        }
    }
}
