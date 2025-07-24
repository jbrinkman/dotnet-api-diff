namespace TestAssembly
{
    /// <summary>
    /// A public class that remains unchanged between versions
    /// </summary>
    public class PublicClass
    {
        /// <summary>
        /// A public method that remains unchanged
        /// </summary>
        public void UnchangedMethod()
        {
        }

        // RemovedMethod is intentionally removed

        /// <summary>
        /// A public method with a changed signature (parameter type changed)
        /// </summary>
        /// <param name="value">An integer parameter (was string in V1)</param>
        public void ChangedSignatureMethod(int value)
        {
        }

        /// <summary>
        /// A new method added in V2
        /// </summary>
        public void NewMethod()
        {
        }

        /// <summary>
        /// A public property that remains unchanged
        /// </summary>
        public string UnchangedProperty { get; set; } = string.Empty;

        // RemovedProperty is intentionally removed

        /// <summary>
        /// A public property with changed type (was string in V1)
        /// </summary>
        public int ChangedTypeProperty { get; set; }

        /// <summary>
        /// A new property added in V2
        /// </summary>
        public DateTime NewProperty { get; set; }

        /// <summary>
        /// A public field that remains unchanged
        /// </summary>
        public readonly int UnchangedField = 42;

        // RemovedField is intentionally removed

        /// <summary>
        /// A new field added in V2
        /// </summary>
        public readonly double NewField = 3.14;

        /// <summary>
        /// A public event that remains unchanged
        /// </summary>
        public event EventHandler? UnchangedEvent;

        // RemovedEvent is intentionally removed

        /// <summary>
        /// A new event added in V2
        /// </summary>
        public event EventHandler<EventArgs>? NewEvent;

        /// <summary>
        /// A method that had its visibility changed from protected to public
        /// </summary>
        public void VisibilityChangedMethod()
        {
        }
    }
}
