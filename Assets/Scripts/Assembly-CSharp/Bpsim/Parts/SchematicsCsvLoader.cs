using System;
using System.Globalization;
using System.IO;

namespace Bpsim.Parts
{
	internal class SchematicsCsvLoader : ISchematicsLoader
	{
		public static SchematicsCsvLoader Default { get; } = new SchematicsCsvLoader();

		public Schematics Read(Stream stream)
		{
			using StreamReader reader = new StreamReader(stream);
			return Read(reader);
		}

		public Schematics Read(TextReader reader)
		{
			IFormatProvider invariantInfo = NumberFormatInfo.InvariantInfo;
			Schematics schematics = new Schematics();
			while (reader.Peek() >= 0)
			{
				string text = reader.ReadLine();
				if (!string.IsNullOrEmpty(text))
				{
					string[] array = text.Split(',');
					int type = int.Parse(array[0], invariantInfo);
					int index = int.Parse(array[1], invariantInfo);
					int x = int.Parse(array[2], invariantInfo);
					int y = int.Parse(array[3], invariantInfo);
					int rotation = int.Parse(array[4], invariantInfo);
					bool flipped = Convert.ToBoolean(int.Parse(array[5], invariantInfo));
					schematics.Units.Add(new Schematics.Unit(x, y, type, index, rotation, flipped));
				}
			}
			return schematics;
		}

		public void Write(Stream stream, Schematics schematics)
		{
			using StreamWriter writer = new StreamWriter(stream);
			Write(writer, schematics);
		}

		public void Write(TextWriter writer, Schematics schematics)
		{
			char value = ',';
			foreach (Schematics.Unit unit in schematics.Units)
			{
				writer.Write(unit.Type);
				writer.Write(value);
				writer.Write(unit.Index);
				writer.Write(value);
				writer.Write(unit.X);
				writer.Write(value);
				writer.Write(unit.Y);
				writer.Write(value);
				writer.Write(unit.Rotation);
				writer.Write(value);
				writer.Write(Convert.ToInt32(unit.Flipped));
				writer.WriteLine();
			}
		}
	}
}
