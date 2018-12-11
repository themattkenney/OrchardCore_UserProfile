using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.Media.Drivers;
using OrchardCore.Navigation;

namespace OrchardCore.Media
{
    public class AdminMenu : INavigationProvider
    {
        public AdminMenu(IStringLocalizer<AdminMenu> localizer)
        {
            S = localizer;
        }

        public IStringLocalizer S { get; set; }

        public Task BuildNavigationAsync(string name, NavigationBuilder builder)
        {
            if (!String.Equals(name, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }

            builder
                .Add(S["Configuration"], configuration => configuration
                    .Add(S["Settings"], settings => settings
                        .Add(S["Media"], S["Media"], layers => layers
                            .Action("Index", "Admin", new { area = "OrchardCore.Settings", groupId = MediaSiteSettingsDisplayDriver.GroupId })
                            .Permission(Permissions.ManageMedia)
                            .LocalNav()
                        )));

            builder
                .Add(S["Content"], content => content
                    .Add(S["Assets"], "3", layers => layers
                        .Permission(Permissions.ManageOwnMedia)
                        .Action("Index", "Admin", new { area = "OrchardCore.Media" })
                        .LocalNav()
                    ));

            return Task.CompletedTask;
        }
    }
}
