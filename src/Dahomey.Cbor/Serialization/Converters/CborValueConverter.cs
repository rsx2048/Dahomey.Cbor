﻿using Dahomey.Cbor.ObjectModel;

namespace Dahomey.Cbor.Serialization.Converters
{
    public class CborValueConverter : 
        ICborConverter<CborValue>,
        ICborMapReader<CborValueConverter.MapReaderContext>,
        ICborMapWriter<CborValueConverter.MapWriterContext>,
        ICborArrayReader<CborValueConverter.ArrayReaderContext>,
        ICborArrayWriter<CborValueConverter.ArrayWriterContext>
    {
        public struct MapReaderContext
        {
            public CborObject obj;
        }

        public struct MapWriterContext
        {
            public CborObject obj;
            public int index;
        }

        public struct ArrayReaderContext
        {
            public CborArray array;
        }

        public struct ArrayWriterContext
        {
            public CborArray array;
            public int index;
        }

        public CborValue Read(ref CborReader reader)
        {
            switch (reader.GetCurrentDataItemType())
            {
                case CborDataItemType.Boolean:
                    return reader.ReadBoolean();

                case CborDataItemType.Null:
                    reader.ReadNull();
                    return CborValue.Null;

                case CborDataItemType.Signed:
                    return reader.ReadInt64();

                case CborDataItemType.Unsigned:
                    return reader.ReadUInt64();

                case CborDataItemType.Single:
                    return reader.ReadSingle();

                case CborDataItemType.Double:
                    return reader.ReadDouble();

                case CborDataItemType.String:
                    return reader.ReadString();

                case CborDataItemType.Array:
                    if (reader.ReadNull())
                    {
                        return null;
                    }

                    ArrayReaderContext arrayContext = new ArrayReaderContext();
                    reader.ReadArray(this, ref arrayContext);
                    return arrayContext.array;

                case CborDataItemType.Map:
                    if (reader.ReadNull())
                    {
                        return null;
                    }

                    MapReaderContext mapContext = new MapReaderContext();
                    reader.ReadMap(this, ref mapContext);
                    return mapContext.obj;

                default:
                    throw reader.BuildException("Unexpected data item type");
            }
        }

        public void Write(ref CborWriter writer, CborValue value)
        {
            switch (value.Type)
            {
                case CborValueType.Object:
                    MapWriterContext mapWriterContext = new MapWriterContext
                    {
                        obj = (CborObject)value
                    };
                    writer.WriteMap(this, ref mapWriterContext);
                    break;

                case CborValueType.Array:
                    ArrayWriterContext arrayWriterContext = new ArrayWriterContext
                    {
                        array = (CborArray)value
                    };
                    writer.WriteArray(this, ref arrayWriterContext);
                    break;

                case CborValueType.Positive:
                    writer.WriteUInt64(value.Value<ulong>());
                    break;

                case CborValueType.Negative:
                    writer.WriteInt64(value.Value<long>());
                    break;

                case CborValueType.Single:
                    writer.WriteSingle(value.Value<float>());
                    break;

                case CborValueType.Double:
                    writer.WriteDouble(value.Value<double>());
                    break;

                case CborValueType.String:
                    writer.WriteString(value.Value<string>());
                    break;

                case CborValueType.Boolean:
                    writer.WriteBoolean(value.Value<bool>());
                    break;

                case CborValueType.Null:
                    writer.WriteNull();
                    break;
            }
        }

        void ICborMapReader<MapReaderContext>.ReadBeginMap(int size, ref MapReaderContext context)
        {
            context.obj = new CborObject();
        }

        void ICborMapReader<MapReaderContext>.ReadMapItem(ref CborReader reader, ref MapReaderContext context)
        {
            string key = reader.ReadString();
            CborValue value = Read(ref reader);
            context.obj.Pairs.Add(new CborPair(key, value));
        }

        int ICborMapWriter<MapWriterContext>.GetMapSize(ref MapWriterContext context)
        {
            return context.obj.Pairs.Count;
        }

        void ICborMapWriter<MapWriterContext>.WriteMapItem(ref CborWriter writer, ref MapWriterContext context)
        {
            CborPair pair = context.obj.Pairs[context.index++];
            writer.WriteString(pair.Name);
            Write(ref writer, pair.Value);
        }

        void ICborArrayReader<ArrayReaderContext>.ReadBeginArray(int size, ref ArrayReaderContext context)
        {
            context.array = new CborArray();

            if (size != -1)
            {
                context.array.Capacity = size;
            }
        }

        void ICborArrayReader<ArrayReaderContext>.ReadArrayItem(ref CborReader reader, ref ArrayReaderContext context)
        {
            context.array.Add(Read(ref reader));
        }

        int ICborArrayWriter<ArrayWriterContext>.GetArraySize(ref ArrayWriterContext context)
        {
            return context.array.Count;
        }

        void ICborArrayWriter<ArrayWriterContext>.WriteArrayItem(ref CborWriter writer, ref ArrayWriterContext context)
        {
            Write(ref writer, context.array[context.index++]);
        }
    }
}