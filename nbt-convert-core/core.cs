using System.Dynamic;
using System.IO.Compression;
using System.Text;

namespace nbt_convert_core
{
    public class Json
    {
        // JSON not yet implemented.
    }
    public class Compiler
    {
        // Compiler not yet implemented.
    }
    public class Decompiler
    {
        private const byte GZIP_SIGNATURE_1 = 0x1F;
        private const byte GZIP_SIGNATURE_2 = 0x8B;

        private const byte TAG_END = 0;
        private const byte TAG_BYTE = 1;
        private const byte TAG_SHORT = 2;
        private const byte TAG_INT = 3;
        private const byte TAG_LONG = 4;
        private const byte TAG_FLOAT = 5;
        private const byte TAG_DOUBLE = 6;
        private const byte TAG_BYTE_ARRAY = 7;
        private const byte TAG_STRING = 8;
        private const byte TAG_LIST = 9;
        private const byte TAG_COMPOUND = 10;
        private const byte TAG_INT_ARRAY = 11;
        private const byte TAG_LONG_ARRAY = 12;

        public static readonly ExpandoObject NBT_DATA = new ExpandoObject();

        public static (ExpandoObject nbtData, string report) Decompile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    if (new FileInfo(filePath).Length == 0)
                    {
                        throw new ArgumentException("File is empty.");
                    }

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        using (BinaryReader binaryReader = new BinaryReader(fileStream))
                        {
                            if (binaryReader.ReadByte() == GZIP_SIGNATURE_1 && binaryReader.ReadByte() == GZIP_SIGNATURE_2)
                            {
                                using (GZipStream gZipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
                                {
                                    gZipStream.Seek(0, SeekOrigin.Begin);
                                    gZipStream.CopyToAsync(memoryStream);
                                }
                            }
                            else
                            {
                                fileStream.Seek(0, SeekOrigin.Begin);
                                fileStream.CopyToAsync(memoryStream);
                            }
                        }
                        for (memoryStream.Position = 0; memoryStream.Position < memoryStream.Length;)
                        {
                            byte type = (byte)memoryStream.ReadByte();
                            DecompileCycle(type, memoryStream, NBT_DATA);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return (new ExpandoObject(), $"Decompilation of {new FileInfo(filePath).Name} failed.\n" +
                        $"Error: {ex}");
                }
            }
            return (NBT_DATA, $"Decompilation of {new FileInfo(filePath).Name} successful.");
        }
        private struct Add
        {
            public static MemoryStream memoryStream = new MemoryStream();
            public static BinaryReader buffer = new BinaryReader(memoryStream);
            public static ExpandoObject nbtData = new ExpandoObject();
            public static void Byte()
            {
                byte value = buffer.ReadByte();
                nbtData.TryAdd("Byte", value);
                return;
            }
            public static void Short()
            {
                short value = buffer.ReadInt16();
                nbtData.TryAdd("Short", value);
                return;
            }
            public static void Int()
            {
                int value = buffer.ReadInt32();
                nbtData.TryAdd("Int", value);
                return;
            }
            public static void Long()
            {
                long value = buffer.ReadInt64();
                nbtData.TryAdd("Long", value);
                return;
            }
            public static void Float()
            {
                float value = buffer.ReadSingle();
                nbtData.TryAdd("Float", value);
                return;
            }
            public static void Double()
            {
                double value = buffer.ReadDouble();
                nbtData.TryAdd("Double", value);
                return;
            }
            public static void ByteArray()
            {
                int length = buffer.ReadInt32();
                byte[] value = buffer.ReadBytes(length);
                nbtData.TryAdd("ByteArray", value);
                return;
            }
            public static void String()
            {
                short length = buffer.ReadInt16();
                string value = Encoding.UTF8.GetString(buffer.ReadBytes(length));
                nbtData.TryAdd("String", value);
                return;
            }
            public static void List()
            {
                byte type = buffer.ReadByte();
                short nameLength = buffer.ReadInt16();
                string name = Encoding.UTF8.GetString(buffer.ReadBytes(nameLength));
                int length = buffer.ReadInt32();
                ExpandoObject list = new ExpandoObject();
                for (int i = 0; i < length; i++)
                {
                    DecompileCycle(type, memoryStream,list);
                }
                nbtData.TryAdd($"List {name}", list);
                return;
            }
            public static void Compound()
            {
                ExpandoObject compound = new ExpandoObject();
                short nameLength = buffer.ReadInt16();
                string name = Encoding.UTF8.GetString(buffer.ReadBytes(nameLength));
                while (true)
                {
                    byte type = buffer.ReadByte();
                    if (type == TAG_END)
                    {
                        break;
                    }
                    DecompileCycle(type, memoryStream, compound);
                }
                nbtData.TryAdd($"Compound {name}", compound);
                return;
            }
            public static void IntArray()
            {
                int length = buffer.ReadInt32();
                int[] value = new int[length];
                for (int i = 0; i < length; i++)
                {
                    value[i] = buffer.ReadInt32();
                }
                nbtData.TryAdd("IntArray", value);
                return;
            }
            public static void LongArray()
            {
                int length = buffer.ReadInt32();
                long[] value = new long[length];
                for (int i = 0; i < length; i++)
                {
                    value[i] = buffer.ReadInt64();
                }
                nbtData.TryAdd("LongArray", value);
                return;
            }
        }
        private static void DecompileCycle(byte type, MemoryStream memoryStream, ExpandoObject nbtData)
        {
            Add.memoryStream = memoryStream;
            Add.nbtData = nbtData;
            switch (type)
            {
                case TAG_END:
                    return;
                case TAG_BYTE:
                    Add.Byte();
                    return;
                case TAG_SHORT:
                    Add.Short();
                    return;
                case TAG_INT:
                    Add.Int();
                    return;
                case TAG_LONG:
                    Add.Long();
                    return;
                case TAG_FLOAT:
                    Add.Float();
                    return;
                case TAG_DOUBLE:
                    Add.Double();
                    return;
                case TAG_BYTE_ARRAY:
                    Add.ByteArray();
                    return;
                case TAG_STRING:
                    Add.String();
                    return;
                case TAG_LIST:
                    Add.List();
                    return;
                case TAG_COMPOUND:
                    Add.Compound();
                    return;
                case TAG_INT_ARRAY:
                    Add.IntArray();
                    return;
                case TAG_LONG_ARRAY:
                    Add.LongArray();
                    return;
                default:
                    throw new Exception($"Unknown type {type} at {memoryStream.Position}.");
            }
        }
    }
}
