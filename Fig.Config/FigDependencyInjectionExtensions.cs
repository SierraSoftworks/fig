namespace Microsoft.Extensions.DependencyInjection
{
    using Fig.Common;
    using Fig.Config;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Extensions which simplify the registration of Fig's services with
    /// a dependency injection container.
    /// </summary>
    public static class FigDependencyInjectionExtensions
    {
        public static IServiceCollection AddFigConfiguration(this IServiceCollection services)
        {
            services
                .AddOptions<ConfigOptions>();

            var healthCheckers = Assembly.GetAssembly(typeof(HealthcheckerBase))
                ?.GetTypes()
                ?.Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(HealthcheckerBase)))
                ?? Enumerable.Empty<Type>();

            foreach (var healthChecker in healthCheckers)
            {
                services.AddSingleton(typeof(HealthcheckerBase), healthChecker);
            }

            return services
                .AddSingleton<ConfigClient>()
                .AddSingleton<HealthcheckRunner>();
        }
    }
}
