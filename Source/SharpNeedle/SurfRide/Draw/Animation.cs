﻿namespace SharpNeedle.SurfRide.Draw;

public class Animation : List<Motion>, IBinarySerializable<ChunkBinaryOptions>
{
    public string Name { get; set; }
    public int ID { get; set; }
    public uint EndFrame { get; set; }
    public bool IsLooping { get; set; }
    public UserData UserData { get; set; }
    public Layer Layer { get; set; }

    public void Read(BinaryObjectReader reader, ChunkBinaryOptions options)
    {
        Layer = (Layer)options.Data;

        Clear();
        if (options.Version >= 3)
            reader.Align(8);
        
        Name = reader.ReadStringOffset();
        ID = reader.Read<int>();
        Capacity = reader.Read<int>();
        EndFrame = reader.Read<uint>();
        if (options.Version >= 3)
            reader.Align(8);
        
        AddRange(reader.ReadObjectArrayOffset<Motion, ChunkBinaryOptions>(options, Capacity));
        if (options.Version >= 1)
        {
            long userDataOffset = reader.ReadOffsetValue();
            if (userDataOffset != 0)
                UserData = reader.ReadObjectAtOffset<UserData, ChunkBinaryOptions>(userDataOffset, options);

            if (options.Version >= 2)
            {
                IsLooping = reader.Read<bool>();
                if (options.Version >= 3)
                    reader.Align(8);
                else
                    reader.Align(4);
            }
        }
    }

    public void Write(BinaryObjectWriter writer, ChunkBinaryOptions options)
    {
        Layer = (Layer)options.Data;

        if (options.Version >= 3)
            writer.Align(8);
        
        writer.WriteStringOffset(StringBinaryFormat.NullTerminated, Name);
        writer.Write(ID);
        writer.Write(Count);
        writer.Write(EndFrame);
        if (options.Version >= 3)
            writer.Align(8);
        
        if (Count != 0)
            writer.WriteObjectCollectionOffset(options, this);
        else
            writer.WriteOffsetValue(0);
        
        if (options.Version >= 1)
        {
            if (UserData != null)
                writer.WriteObjectOffset(UserData, options);
            else
                writer.WriteOffsetValue(0);

            if (options.Version >= 2)
            {
                writer.Write(IsLooping);
                if (options.Version >= 3)
                    writer.Align(8);
                else
                    writer.Align(4);
            }
        }
    }
}
