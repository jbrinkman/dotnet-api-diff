namespace TestAssembly
{
    /// <summary>
    /// A public interface that will have members added and removed in V2
    /// </summary>
    public interface IPublicInterface
    {
        /// <summary>
        /// A method that will remain unchanged
        /// </summary>
        void UnchangedMethod();

        /// <summary>
        /// A method that will be removed in V2
        /// </summary>
        void RemovedMethod();

        /// <summary>
        /// A property that will remain unchanged
        /// </summary>
        string UnchangedProperty { get; set; }

        /// <summary>
        /// A property that will be removed in V2
        /// </summary>
        int RemovedProperty { get; }

        /// <summary>
        /// An event that will remain unchanged
        /// </summary>
        event EventHandler UnchangedEvent;

        /// <summary>
        /// An event that will be removed in V2
        /// </summary>
        event EventHandler RemovedEvent;
    }
}
