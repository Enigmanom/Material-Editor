using System.Text;

namespace MaterialLib
{
    public sealed record MaterialBlock(string Tag, byte[] Payload)
    {
        public static IEnumerable<MaterialBlock> ReadAll(BinaryReader input)
        {
            var stream = input.BaseStream;
            while (stream.Position < stream.Length)
            {
                var tag = Encoding.ASCII.GetString(input.ReadBytes(4));
                var size = input.ReadUInt32();
                if (tag.Length != 4 || size > stream.Length - stream.Position)
                    throw new FormatException($"Truncated block '{tag}'.");

                yield return new MaterialBlock(tag, input.ReadBytes((int)size));
            }
        }

        public static void Write(BinaryWriter output, string tag, Action<BinaryWriter> writePayload)
        {
            using var payload = new MemoryStream();
            using (var writer = new BinaryWriter(payload, Encoding.UTF8, true))
                writePayload(writer);

            new MaterialBlock(tag, payload.ToArray()).Write(output);
        }

        public void Write(BinaryWriter output)
        {
            output.Write(Encoding.ASCII.GetBytes(Tag));
            output.Write((uint)Payload.Length);
            output.Write(Payload);
        }

        public T Parse<T>(Func<BinaryReader, T> read)
        {
            using var stream = new MemoryStream(Payload);
            using var reader = new BinaryReader(stream);
            var value = read(reader);
            if (stream.Position != stream.Length)
                throw new FormatException($"Unexpected data in block '{Tag}'.");

            return value;
        }
    }
}
