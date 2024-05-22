using System;

namespace WildHealth.Application.Utils.ArchiveEmailCreator
{
    /// <summary>
    /// <see cref="IArchiveEmailCreator"/>
    /// </summary>
    public class ArchiveEmailCreator : IArchiveEmailCreator
    {
        private const string AppendString = "practice";

        /// <summary>
        /// <see cref="IArchiveEmailCreator.GenerateArchivedEmailNameForOldPractice"/>
        /// </summary>
        /// <param name="email"></param>
        /// <param name="oldPracticeId"></param>
        /// <returns></returns>
        public string GenerateArchivedEmailNameForOldPractice(string email, int oldPracticeId)
        {
            var splitEmail = email.Split("@");
            
            var appendTimestamp = DateTime.Now.ToString("yyyy-MM-dd");

            return $"{splitEmail[0]}+{AppendString}_{oldPracticeId}-{appendTimestamp}@{splitEmail[1]}";
        }
    }
}
