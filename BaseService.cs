using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;

namespace SharpBot
{
    public class BaseService
    {
        public static IServiceProvider ConfigureServices()
        {
            var map = new ServiceCollection();
            map.AddSingleton(new DiscordClient(new DiscordConfiguration()));
            return map.BuildServiceProvider();
        }
    }
}