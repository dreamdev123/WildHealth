namespace WildHealth.Application.Utils.ApplicationVersionProvider
{
    /// <summary>
    /// <see cref="IApplicationVersionProvider"/>
    /// </summary>
    public class ApplicationVersionProvider : IApplicationVersionProvider
    {
        /// <summary>
        /// <see cref="IApplicationVersionProvider.Get"/>
        /// </summary>
        /// <returns></returns>
        public string? Get()
        {
            return typeof(ApplicationVersionProvider).Assembly.GetName().Version?.ToString();
        }
    }
}