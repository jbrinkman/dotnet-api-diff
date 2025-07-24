namespace TestAssembly.NamespaceToBeRenamed
{
    /// <summary>
    /// A class in a namespace that will be renamed in V2
    /// </summary>
    public class ClassInRenamedNamespace
    {
        /// <summary>
        /// A method that will remain unchanged
        /// </summary>
        public void Method()
        {
        }

        /// <summary>
        /// A property that will remain unchanged
        /// </summary>
        public string Property { get; set; } = string.Empty;
    }
}
