using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Media.Fields;

namespace OrchardCore.Media.ViewModels
{
    public class EditMediaFieldViewModel
    {
        // A Json serialized array of EditMediaFieldItemInfos
        public string Paths { get; set; }
        public MediaField Field { get; set; }
        public ContentPart Part { get; set; }
        public ContentPartFieldDefinition PartFieldDefinition { get; set; }

        // This will be used by the uploader of a limited media field editor
        public string TempUploadFolder { get; set; }
    }


    public class EditMediaFieldItemInfo
    {        
        public string Path { get; set; }

        // It will be true if the media item is a new upload from a limited media field editor.
        public bool IsNew { get; set; }

        // It will be true if the media item has marked for deletion using a limited media field editor.
        public bool IsRemoved { get; set; }
    }
}
