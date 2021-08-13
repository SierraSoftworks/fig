namespace Fig.Common
{
    using System.Threading.Tasks;

    /// <summary>
    /// Reports the status of healthchecks that are run by the agent.
    /// </summary>
    public interface IHealthcheckReporter
    {
        /// <summary>
        /// Reports the status of a healthcheck that has been run by the agent.
        /// </summary>
        /// <param name="healthcheck">The healthcheck which was executed.</param>
        /// <param name="result">The result of the healthcheck's execution.</param>
        /// <returns></returns>
        Task ReportAsync(Healthcheck healthcheck, HealthcheckResult result);
    }
}
