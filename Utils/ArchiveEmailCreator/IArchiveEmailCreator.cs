namespace WildHealth.Application.Utils.ArchiveEmailCreator
{
    /// <summary>
    /// Represents archive email generator
    /// </summary>
    public interface IArchiveEmailCreator
    {
        /// <summary>
        /// Generates archive email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="oldPracticeId"></param>
        /// <returns></returns>
        string GenerateArchivedEmailNameForOldPractice(string email, int oldPracticeId);
    }
}
