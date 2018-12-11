using System;
using System.Threading;
using OrchardCore.BackgroundTasks;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using OrchardCore.Media.Services;

namespace OrchardCore.Media.Tasks
{
    /// <summary>
    /// This background task will delete old files in media fields temporary folder
    /// </summary>    
    [BackgroundTask(Schedule = "* * * * *", Description = "Clean old temporary assets.")]
    public class PurgeTempFilesBackgroundTask : IBackgroundTask
    {
        public Task DoWorkAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var purgeTempsFileService = serviceProvider.GetService<PurgeTempFilesService>();
            return purgeTempsFileService.PurgeAsync();

        }
    }
}
