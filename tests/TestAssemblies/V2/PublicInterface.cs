namespace TestAssembly
{
    /// <summary>
    /// A public interface with members added and removed
    /// </summary>
    public interface IPublicInterface
    {
        /// <summary>
        /// A method that remains unchanged
        /// </summary>
        void UnchangedMethod();

        // RemovedMethod is intentionally removed

        /// <summary>
        /// A new method added in V2
        /// </summary>
        void NewMethod();

        /// <summary>
        /// A property that remains unchanged
        /// </summary>
        string UnchangedProperty { get; set; }

        // RemovedProperty is intentionally removed

        /// <summary>
        /// A new property added in V2
        /// </summary>
        DateTime NewProperty { get; }

        /// <summary>
        /// An event that remains unchanged
        /// </summary>
        event EventHandler UnchangedEvent;

        // RemovedEvent is intentionally removed

        /// <summary>
        /// A new event added in V2
        /// </summary>
        event EventHandler<EventArgs> NewEvent;
    }
}
