namespace SharpNeedle.HedgehogEngine.Needle;
using SharpNeedle.HedgehogEngine.Mirage;
using SharpNeedle.IO;
using SharpNeedle.Utilities;
using System.IO;
using System.Text.RegularExpressions;

[NeedleResource("hh/needle", ResourceType.Archive, @"\.model$")]
public class NeedleArchive : ResourceBase, IBinarySerializable
{
    public const string NEDARCSignature = "NEDARCV1";

    public byte[] LodInfo { get; set; }
    public List<Model> Models { get; set; }

    public override void ResolveDependencies(IResourceResolver resolver)
    {
        foreach(var model in Models)
            model.ResolveDependencies(resolver);
    }

    public void Read(BinaryObjectReader reader)
    {
        string nedarcSignature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
        if (nedarcSignature != NEDARCSignature)
            throw new NotImplementedException();

        int arcFileSize = reader.Read<int>();
        string arcMagic = reader.ReadString(StringBinaryFormat.FixedLength, 4);

        string lodInfoSignature = reader.ReadString(StringBinaryFormat.FixedLength, 16);
        int lodInfoSize = reader.Read<int>();
        LodInfo = reader.ReadArray<byte>(lodInfoSize);

        int modelCount = 0;
        long prePos = reader.Position;

        while(reader.Position != arcFileSize + 8)
        {
            string needleModelSignature = reader.ReadString(StringBinaryFormat.FixedLength, 16);
            int needleModelSize = reader.Read<int>();
            reader.Skip(needleModelSize);
            modelCount++;
        }

        Models = new();
        reader.Seek(prePos, System.IO.SeekOrigin.Begin);
        for(int i = 0; i < modelCount; i++)
        {
            string needleModelSignature = reader.ReadString(StringBinaryFormat.FixedLength, 16);
            int needleModelSize = reader.Read<int>();
            long needleModelOffset = reader.Position;
            byte[] modelData = reader.ReadArray<byte>(needleModelSize);
            reader.Seek(needleModelOffset, SeekOrigin.Begin);

            reader.Skip(8);
            uint offsetsPointer = reader.Read<uint>();
            uint offsetsCount = reader.Read<uint>();
            Dictionary<uint, uint> offsets = new();

            reader.Seek(offsetsPointer + needleModelOffset, SeekOrigin.Begin);
            for(int x = 0; x < offsetsCount; x++)
            {
                uint offsetPointer = reader.Read<uint>();
                reader.Seek(offsetPointer + needleModelOffset + 0x10, SeekOrigin.Begin);
                uint offset = reader.Read<uint>();
                offsets.Add(offsetPointer, SwapBytes(offset));
                reader.Seek(offsetsPointer + needleModelOffset + (x + 1)*4, SeekOrigin.Begin);
            }

            MemoryStream stream = new(modelData);
            BinaryObjectWriter tempWriter = new(stream, StreamOwnership.Retain, Endianness.Big);

            foreach(var x in offsets)
            {
                tempWriter.Seek(x.Key + 0x10, SeekOrigin.Begin);
                tempWriter.Write(x.Value + x.Key);
            }

            tempWriter.Dispose();

            BinaryObjectReader tempReader = new(stream, StreamOwnership.Retain, Endianness.Big, Encoding.Default);
            Model model = new Model();
            model.Read(tempReader);
            Models.Add(model);
            tempReader.Dispose();
        }

        reader.Dispose();
    }

    public void Write(BinaryObjectWriter writer)
    {
        throw new NotImplementedException();
    }

    public override void Read(IFile file)
    {
        BaseFile = file;
        Name = Path.GetFileNameWithoutExtension(file.Name);
        using var reader = new BinaryObjectReader(file.Open(), StreamOwnership.Transfer, Endianness.Little);

        Read(reader);
    }

    public override void Write(IFile file)
    {
        throw new NotImplementedException();
    }

    private uint SwapBytes(uint x)
    {
        x = (x >> 16) | (x << 16);
        return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
    }
}