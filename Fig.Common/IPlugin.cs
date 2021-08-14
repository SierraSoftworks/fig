namespace Fig.Common
{
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// The Fig plugin interface, which allows downstream consumers to extend Fig's functionality.
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// Registers the plugin's services.
        /// </summary>
        /// <param name="services"></param>
        void Register(IServiceCollection services);
    }
}
