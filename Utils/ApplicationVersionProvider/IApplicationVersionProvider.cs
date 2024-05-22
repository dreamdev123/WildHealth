namespace WildHealth.Application.Utils.ApplicationVersionProvider
{
    /// <summary>
    /// Represents interface of application version provider
    /// </summary>
    public interface IApplicationVersionProvider
    {
        /// <summary>
        /// Returns application version
        /// </summary>
        /// <returns></returns>
        string? Get();
    }
}