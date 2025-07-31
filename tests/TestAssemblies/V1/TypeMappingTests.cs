using System;

namespace TestAssembly.TypeMapping
{
    /// <summary>
    /// A value type that will be mapped to NewValue in V2 via type mapping
    /// This simulates the RedisValue -> ValkeyValue scenario
    /// </summary>
    public struct OldValue
    {
        private readonly string _value;

        /// <summary>
        /// Constructor that takes a string
        /// </summary>
        public OldValue(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Constructor that takes an int
        /// </summary>
        public OldValue(int value)
        {
            _value = value.ToString();
        }

        /// <summary>
        /// Property that returns the underlying value
        /// </summary>
        public string Value => _value;

        /// <summary>
        /// Static property that returns an empty value
        /// </summary>
        public static OldValue Empty { get; } = new OldValue(string.Empty);

        /// <summary>
        /// Method that takes another OldValue as parameter
        /// </summary>
        public bool Equals(OldValue other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Method that returns an OldValue
        /// </summary>
        public static OldValue Create(string value)
        {
            return new OldValue(value);
        }
    }

    /// <summary>
    /// A class that uses OldValue in various ways
    /// </summary>
    public class OldValueConsumer
    {
        /// <summary>
        /// Property that returns OldValue
        /// </summary>
        public OldValue Value { get; set; }

        /// <summary>
        /// Constructor that takes OldValue
        /// </summary>
        public OldValueConsumer(OldValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Method that takes OldValue parameter
        /// </summary>
        public void ProcessValue(OldValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Method that returns OldValue
        /// </summary>
        public OldValue GetValue()
        {
            return Value;
        }

        /// <summary>
        /// Method that takes array of OldValue
        /// </summary>
        public void ProcessValues(OldValue[] values)
        {
            if (values.Length > 0)
                Value = values[0];
        }

        /// <summary>
        /// Generic method with OldValue constraint
        /// </summary>
        public T ProcessGeneric<T>(T value) where T : struct
        {
            return value;
        }
    }

    /// <summary>
    /// A generic class that uses OldValue
    /// </summary>
    public class GenericOldValueContainer<T>
    {
        public T? Value { get; set; }

        /// <summary>
        /// Method that works with OldValue when T is OldValue
        /// </summary>
        public void SetOldValue(OldValue value)
        {
            if (typeof(T) == typeof(OldValue))
            {
                Value = (T)(object)value;
            }
        }
    }
}

namespace TestAssembly.NamespaceMapping.Old
{
    /// <summary>
    /// A class that will be in a namespace that gets mapped to New namespace in V2
    /// This simulates namespace renaming scenarios
    /// </summary>
    public class NamespaceMappedClass
    {
        /// <summary>
        /// A method in the old namespace
        /// </summary>
        public void OldNamespaceMethod()
        {
        }


        /// <summary>
        /// Property in the old namespace
        /// </summary>
        public string? OldNamespaceProperty { get; set; }

    }

    /// <summary>
    /// An interface in the old namespace
    /// </summary>
    public interface IOldNamespaceInterface
    {
        void InterfaceMethod();
    }
}
