namespace WildHealth.Application.Commands._Base
{
    /// <summary>
    /// Represents validatable object
    /// </summary>
    public interface IValidatabe
    {
        /// <summary>
        /// Returns is object valid
        /// </summary>
        /// <returns></returns>
        bool IsValid();

        /// <summary>
        /// Validates object ans throw exception
        /// </summary>
        void Validate();
    }
}
