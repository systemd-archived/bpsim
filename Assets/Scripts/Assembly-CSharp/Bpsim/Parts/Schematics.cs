using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Bpsim.Parts
{
	public class Schematics
	{
		public struct Unit
		{
			public int X;

			public int Y;

			public int Type;

			public int Index;

			public int Rotation;

			public bool Flipped;

			public Unit(int x, int y, int type, int index, int rotation, bool flipped)
			{
				X = x;
				Y = y;
				Type = type;
				Index = index;
				Rotation = rotation;
				Flipped = flipped;
			}

			public Unit WithPosition(int x, int y)
			{
				return new Unit(x, y, Type, Index, Rotation, Flipped);
			}

			public Unit WithPartType(int type, int index)
			{
				return new Unit(X, Y, type, index, Rotation, Flipped);
			}

			public Unit WithRotation(int rotation, bool flipped)
			{
				return new Unit(X, Y, Type, Index, rotation, flipped);
			}
		}

		public List<Unit> Units { get; set; }

		public Schematics()
			: this(0)
		{
		}

		public Schematics(int count)
		{
			Units = new List<Unit>(count);
		}

		public NativeArray<Unit> ToNative(Allocator allocator)
		{
			int count = Units.Count;
			NativeArray<Unit> result = new NativeArray<Unit>(Units.Count, allocator, NativeArrayOptions.UninitializedMemory);
			for (int i = 0; i < count; i++)
			{
				result[i] = Units[i];
			}
			return result;
		}

		public static Schematics FromNative(NativeArray<Unit> source)
		{
			int length = source.Length;
			Schematics schematics = new Schematics(length);
			for (int i = 0; i < length; i++)
			{
				schematics.Units[i] = source[i];
			}
			return schematics;
		}

		public static ISchematicsLoader CreateLoader(SchematicsFormat format)
		{
			return format switch
			{
				SchematicsFormat.Csv => SchematicsCsvLoader.Default, 
				SchematicsFormat.Json => SchematicsJsonLoader.Default, 
				SchematicsFormat.Xml => SchematicsXmlLoader.Default, 
				SchematicsFormat.EncryptedXml => SchematicsEncryptedXmlLoader.Default, 
				_ => throw new InvalidOperationException(), 
			};
		}
	}
}
