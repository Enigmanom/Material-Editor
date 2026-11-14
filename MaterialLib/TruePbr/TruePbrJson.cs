using System.Runtime.Serialization;

namespace MaterialLib
{
    [Serializable]
    public sealed class TruePbrNamedValues : ISerializable
    {
        private readonly Dictionary<string, object> _values = [];

        public TruePbrNamedValues()
        {
        }

        private TruePbrNamedValues(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
                _values[entry.Name] = entry.Value!;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (var (name, value) in _values)
                info.AddValue(name, value is float[] floats ? floats.Cast<object>().ToArray() : value);
        }

        public void Set(string name, object value)
        {
            _values[name] = value;
        }

        public bool TryGetBool(string name, out bool value)
        {
            value = false;
            if (!_values.TryGetValue(name, out var raw))
                return false;

            value = Convert.ToBoolean(raw);
            return true;
        }

        public bool TryGetString(string name, out string value)
        {
            value = "";
            if (!_values.TryGetValue(name, out var raw))
                return false;

            value = Convert.ToString(raw) ?? "";
            return true;
        }

        public bool TryGetFloats(string name, int count, out float[] value)
        {
            value = [];
            if (!_values.TryGetValue(name, out var raw))
                return false;

            value = raw is System.Collections.IEnumerable items && raw is not string
                ? items.Cast<object>().Select(Convert.ToSingle).ToArray()
                : [Convert.ToSingle(raw)];

            if (value.Length != count)
                throw new SerializationException($"'{name}' needs {count} value(s).");

            return true;
        }
    }
}
