using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ggText
{
    internal class UEXPHeader
    {
        internal int ContainerType;
        internal int Unknown0x0C;
        internal int UexpIndex;
        internal int Unknown0x20;
        internal int UncompressedSize;
        internal int CompressedSize;
        internal int Unknown0x2C;

        internal void WriteNewSize(BinaryWriter writer)
        {
            var savepos = writer.BaseStream.Position;
            int newSize = (int)(writer.BaseStream.Length - 0x38);
            writer.BaseStream.Position = 0x24;
            writer.Write(newSize);
            writer.Write(newSize);
            writer.BaseStream.Position = savepos;
        }
        internal void ReadHeader(BinaryReader reader)
        {
            ContainerType = reader.ReadInt32();
            reader.BaseStream.Position += 8;
            Unknown0x0C = reader.ReadInt32();
            reader.BaseStream.Position += 8;
            UexpIndex = reader.ReadInt32();
            reader.BaseStream.Position += 4;
            Unknown0x20 = reader.ReadInt32();
            UncompressedSize = reader.ReadInt32();
            CompressedSize = reader.ReadInt32();
            Unknown0x2C = reader.ReadInt32();
            reader.BaseStream.Position += 4;
        }
    }

    internal class REDText
    {
        internal int Entries { get; set; }
        internal int DataStartOffset { get; set;}
        internal long ContainerPosition { get; private set; }

        public string[] IdNames { get; set; }
        public int[] Offsets { get; set; }
        public string[] Strings { get; set; }

        public void ExtractLocalization(string filename)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filename)))
            {
                UEXPHeader header = new UEXPHeader();
                header.ReadHeader(reader);
                File.WriteAllBytes(filename + ".txt", reader.ReadBytes(header.UncompressedSize));
            }
            Console.WriteLine("Complete.");
        }

        public void RepackLocalization(string inputFile, string uexpFile)
        {
            byte[] inputText = File.ReadAllBytes(inputFile);
            string newUexp = Path.GetFileNameWithoutExtension(uexpFile) + "_new" + Path.GetExtension(uexpFile);
            string uasset = Path.GetFileNameWithoutExtension(uexpFile) + ".uasset";
            using(BinaryReader reader = new BinaryReader(File.OpenRead(uexpFile)))
            using(BinaryWriter writer = new BinaryWriter(new FileStream(newUexp, FileMode.OpenOrCreate)))
            {
                UEXPHeader header = new UEXPHeader();
                header.ReadHeader(reader);
                reader.Close();
                //запись хидера в новый файл
                writer.Write(header.ContainerType);
                writer.BaseStream.Position += 8;
                writer.Write(header.Unknown0x0C);
                writer.BaseStream.Position += 8;
                writer.Write(header.UexpIndex);
                writer.BaseStream.Position += 4;
                writer.Write(header.Unknown0x20);
                writer.Write(inputText.Length);
                writer.Write(inputText.Length);
                writer.Write(header.Unknown0x2C);
                writer.BaseStream.Position += 4;
                //
                if(writer.BaseStream.Position != 0x34)
                {
                    Console.WriteLine("Позиция врайтера не соответствует концу загаловка UEXP.");
                    Console.WriteLine("Текущая позиция: {0}", writer.BaseStream.Position.ToString("X2"));
                    Console.ReadKey();
                    return;
                }

                //вставляем файл
                writer.Write(inputText);
                writer.Write(new byte[] {0xC1, 0x83, 0x2A, 0x9E });
                writer.Close();
            }
            if (File.Exists(uasset))
            {
                using (BinaryWriter assetWriter = new BinaryWriter(new FileStream(uasset, FileMode.Open)))
                {
                    assetWriter.BaseStream.Position = 0x209;
                    assetWriter.Write(inputText.Length + 0x34);
                    assetWriter.Close();
                    Console.WriteLine("{0} has been changed!", uasset);
                }
            }
            Console.WriteLine("Complete.");
        }


        public void PackLibrary(string inputTxt, string inputUexp)
        {
            string uasset = Path.GetFileNameWithoutExtension(inputUexp) + ".uasset";
            string[] text = File.ReadAllLines(inputTxt);
            UEXPHeader header = new UEXPHeader();
            using(MemoryStream outFile = new MemoryStream())
            using(BinaryWriter writer = new BinaryWriter(outFile))
            using(BinaryReader reader = new BinaryReader(File.OpenRead(inputUexp)))
            {
                header.ReadHeader(reader);
                Entries = reader.ReadInt32();
                DataStartOffset = reader.ReadInt32();
                var tablePos = reader.BaseStream.Position;
                int[] newOffsets = new int[Entries];

                if(text.Length != Entries)
                {
                    Console.WriteLine("Кол-во строк во входящем txt не равно кол-ву вхождений в заголовке UEXP.");
                    Console.WriteLine("TXT/UEXP: {0}/{1}", text.Length, Entries);
                    Console.ReadKey();
                    return;
                }

                //получаем и записываем шапку + таблицу
                reader.BaseStream.Position = 0;
                int tableSize = 0x3C + (0x84 * Entries);
                writer.Write(reader.ReadBytes(tableSize));
                reader.Close();

                for(int i = 0; i < Entries; i++)
                {
                    newOffsets[i] = (int)writer.BaseStream.Position - 0x34;
                    writer.Write(Encoding.Unicode.GetBytes(text[i].Replace("{LF}", "\n")));
                    writer.Write(new short());
                }
                writer.BaseStream.Position = tablePos;
                for (int i = 0; i < Entries; i++)
                {
                    writer.BaseStream.Position += 0x80;
                    writer.Write(newOffsets[i]);
                }

                writer.BaseStream.Position = writer.BaseStream.Length;
                writer.Write(0x9E2A83C1);
                header.WriteNewSize(writer);

                if (File.Exists(uasset))
                {
                    using (BinaryWriter assetWriter = new BinaryWriter(new FileStream(uasset, FileMode.Open)))
                    {
                        if(uasset.Contains("correlation"))
                            assetWriter.BaseStream.Position = 0x224;
                        else
                            assetWriter.BaseStream.Position = 0x218;
                        assetWriter.Write(writer.BaseStream.Length - 4);
                        assetWriter.Close();
                        Console.WriteLine("{0} has been changed!", uasset);
                    }
                }
                else
                {
                    Console.WriteLine("Cannot find .uasset file!");
                }
                writer.Close();
                Console.WriteLine("Complete.");
                File.WriteAllBytes(Path.GetFileNameWithoutExtension(inputUexp) + "_new.uexp", outFile.ToArray());
            }
        }

        public void ExtractLibrary(string filename)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filename)))
            {
                UEXPHeader header = new UEXPHeader();
                header.ReadHeader(reader);
                ContainerPosition = reader.BaseStream.Position;

                Entries = reader.ReadInt32();
                DataStartOffset = reader.ReadInt32();

                if (reader.BaseStream.Position != DataStartOffset + ContainerPosition)
                {
                    Console.WriteLine("Позиция контейнера отличается.");
                    Console.WriteLine("Позиция ридера: {0}", reader.BaseStream.Position);
                    Console.WriteLine("Позиция в контейнере (0x04/обычно равно 8 + шапка UEXP): {0}", DataStartOffset + ContainerPosition);
                    Console.ReadKey();
                    return;
                }
                IdNames = new string[Entries];
                Offsets = new int[Entries];
                Strings = new string[Entries];
                string[] test = new string[Entries];

                //считываем идентификаторы строк (напр. CHARACTER_0) и оффсеты
                for (int i = 0; i < Entries; i++)
                {
                    IdNames[i] = Regex.Replace(Encoding.Unicode.GetString(reader.ReadBytes(128)), "\0", "");
                    Offsets[i] = reader.ReadInt32();
                }

                for (int i = 0; i < Offsets.Length; i++)
                {
                    reader.BaseStream.Position = Offsets[i] + ContainerPosition;
                    if (i != Offsets.Length)
                    {
                        Strings[i] = Helpers.ReadString(reader, Encoding.Unicode).Replace("\n", "{LF}");
                    }
                }

                for (int i = 0; i < Entries; i++)
                {
                    test[i] = $"{Strings[i]}";
                }
                File.WriteAllLines(filename + ".txt", test);
            }
            Console.WriteLine("Complete.");
        }

        public void ExtractStory(string filename)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(filename)))
            {
                ATF atf = new ATF();
                UEXPHeader header = new UEXPHeader();
                header.ReadHeader(reader);
                string[] text = atf.GetText(reader);
                File.WriteAllLines(filename + ".txt", text, Encoding.UTF8);
            }
        }

        public void PackStory(string inputText, string inputUexp)
        {
            ATF atf = new ATF();
            atf.PackText(inputText, inputUexp);
        }
    }
}
