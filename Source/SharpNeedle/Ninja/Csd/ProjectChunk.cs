﻿namespace SharpNeedle.Ninja.Csd;
using System.IO;
using FontCollection = CsdDictionary<Font>;

public class ProjectChunk : IChunk
{
    public static readonly uint BinSignature = BinaryHelper.MakeSignature<uint>("nCPJ");
    public uint Signature { get; private set; } = BinSignature;
    public string Name { get; set; }
    public TextureFormat TextureFormat { get; set; }
    public uint Field08 { get; set; }
    public uint Field0C { get; set; }
    public SceneNode Root { get; set; }
    public FontCollection Fonts { get; set; }

    public void Read(BinaryObjectReader reader, ChunkBinaryOptions options)
    {
        options.Header ??= reader.ReadObject<ChunkHeader>();
        Signature = options.Header.Value.Signature;
        Field08 = reader.Read<uint>();
        Field0C = reader.Read<uint>();
        Root = reader.ReadObjectOffset<SceneNode>();
        Name = reader.ReadStringOffset();

        TextureFormat = reader.Read<TextureFormat>();
        Fonts = reader.ReadObjectOffset<FontCollection>();
    }

    public void Write(BinaryObjectWriter writer, ChunkBinaryOptions options)
    {
        writer.WriteLittle(Signature);
        writer.Write(0); // Size

        var start = writer.At();
        writer.Write(Field08);
        writer.Write(Field0C);
        writer.WriteObjectOffset(Root);
        writer.WriteStringOffset(StringBinaryFormat.NullTerminated, Name);
        writer.Write(TextureFormat);
        writer.WriteObjectOffset(Fonts);
        writer.Flush();
        writer.Align(16);
        var end = writer.At();

        var size = (long)end - (long)start;
        writer.At((long)start - sizeof(int), SeekOrigin.Begin);
        writer.Write((int)size);
        
        end.Dispose();
    }
}

public enum TextureFormat
{
    Unknown = 0,
    DirectX = 0x2E44584C,
    RenderWare = 0x2E525754,
}