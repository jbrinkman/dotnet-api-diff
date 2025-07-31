using System;

namespace TestAssembly.TypeMapping
{
    /// <summary>
    /// A value type that maps from OldValue in V1 via type mapping
    /// This simulates the RedisValue -> ValkeyValue scenario
    /// </summary>
    public struct NewValue
    {
        private readonly string _value;

        /// <summary>
        /// Constructor that takes a string
        /// </summary>
        public NewValue(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Constructor that takes an int
        /// </summary>
        public NewValue(int value)
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
        public static NewValue Empty { get; } = new NewValue(string.Empty);

        /// <summary>
        /// Method that takes another NewValue as parameter
        /// </summary>
        public bool Equals(NewValue other)
        {
            return _value == other._value;
        }

        /// <summary>
        /// Method that returns a NewValue
        /// </summary>
        public static NewValue Create(string value)
        {
            return new NewValue(value);
        }
    }

    /// <summary>
    /// A class that uses NewValue in various ways
    /// </summary>
    public class OldValueConsumer
    {
        /// <summary>
        /// Property that returns NewValue (was OldValue in V1)
        /// </summary>
        public NewValue Value { get; set; }

        /// <summary>
        /// Constructor that takes NewValue (was OldValue in V1)
        /// </summary>
        public OldValueConsumer(NewValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Method that takes NewValue parameter (was OldValue in V1)
        /// </summary>
        public void ProcessValue(NewValue value)
        {
            Value = value;
        }

        /// <summary>
        /// Method that returns NewValue (was OldValue in V1)
        /// </summary>
        public NewValue GetValue()
        {
            return Value;
        }

        /// <summary>
        /// Method that takes array of NewValue (was OldValue in V1)
        /// </summary>
        public void ProcessValues(NewValue[] values)
        {
            if (values.Length > 0)
                Value = values[0];
        }

        /// <summary>
        /// Generic method with NewValue constraint
        /// </summary>
        public T ProcessGeneric<T>(T value) where T : struct
        {
            return value;
        }
    }

    /// <summary>
    /// A generic class that uses NewValue
    /// </summary>
    public class GenericOldValueContainer<T>
    {
        public T? Value { get; set; }

        /// <summary>
        /// Method that works with NewValue when T is NewValue (was OldValue in V1)
        /// </summary>
        public void SetOldValue(NewValue value)
        {
            if (typeof(T) == typeof(NewValue))
            {
                Value = (T)(object)value;
            }
        }
    }
}

namespace TestAssembly.NamespaceMapping.New
{
    /// <summary>
    /// A class that was in Old namespace in V1, now mapped to New namespace in V2
    /// This simulates namespace renaming scenarios
    /// </summary>
    public class NamespaceMappedClass
    {
        /// <summary>
        /// A method in the new namespace (was in old namespace in V1)
        /// </summary>
        public void OldNamespaceMethod()
        {
        }

        /// <summary>
        /// Property in the new namespace (was in old namespace in V1)
        /// </summary>
        public string? OldNamespaceProperty { get; set; }
    }

    /// <summary>
    /// An interface in the new namespace (was in old namespace in V1)
    /// </summary>
    public interface IOldNamespaceInterface
    {
        void InterfaceMethod();
    }
}
