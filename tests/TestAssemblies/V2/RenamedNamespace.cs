namespace TestAssembly.RenamedNamespace
{
    /// <summary>
    /// A class in a namespace that was renamed from NamespaceToBeRenamed in V1
    /// </summary>
    public class ClassInRenamedNamespace
    {
        /// <summary>
        /// A method that remains unchanged
        /// </summary>
        public void Method()
        {
        }

        /// <summary>
        /// A property that remains unchanged
        /// </summary>
        public string Property { get; set; } = string.Empty;
    }
}
