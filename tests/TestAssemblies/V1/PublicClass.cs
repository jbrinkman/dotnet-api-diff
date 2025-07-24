namespace TestAssembly
{
    /// <summary>
    /// A public class that will remain unchanged between versions
    /// </summary>
    public class PublicClass
    {
        /// <summary>
        /// A public method that will remain unchanged
        /// </summary>
        public void UnchangedMethod()
        {
        }

        /// <summary>
        /// A public method that will be removed in V2
        /// </summary>
        public void RemovedMethod()
        {
        }

        /// <summary>
        /// A public method that will have its signature changed in V2
        /// </summary>
        /// <param name="value">A string parameter</param>
        public void ChangedSignatureMethod(string value)
        {
        }

        /// <summary>
        /// A public property that will remain unchanged
        /// </summary>
        public string UnchangedProperty { get; set; } = string.Empty;

        /// <summary>
        /// A public property that will be removed in V2
        /// </summary>
        public int RemovedProperty { get; set; }

        /// <summary>
        /// A public property that will have its type changed in V2
        /// </summary>
        public string ChangedTypeProperty { get; set; } = string.Empty;

        /// <summary>
        /// A public field that will remain unchanged
        /// </summary>
        public readonly int UnchangedField = 42;

        /// <summary>
        /// A public field that will be removed in V2
        /// </summary>
        public readonly bool RemovedField = true;

        /// <summary>
        /// A public event that will remain unchanged
        /// </summary>
        public event EventHandler? UnchangedEvent;

        /// <summary>
        /// A public event that will be removed in V2
        /// </summary>
        public event EventHandler? RemovedEvent;

        /// <summary>
        /// A protected method that will have its visibility changed to public in V2
        /// </summary>
        protected void VisibilityChangedMethod()
        {
        }
    }
}
