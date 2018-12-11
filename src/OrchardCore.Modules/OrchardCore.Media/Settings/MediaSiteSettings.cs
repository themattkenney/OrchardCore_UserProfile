namespace OrchardCore.Media.Settings
{
    public class MediaSiteSettings
    {
        // In minutes. 0 = no delete
        public int LimitedEditorDeleteTempFilesOlderThan { get; set; } = 1440;
    }
}
