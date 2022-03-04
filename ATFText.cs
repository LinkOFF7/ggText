using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace ggText
{
    internal class ATFHeader
    {
        internal string Signature;
        internal int StringsCount;
        //+=8;
        internal int StringsIdOffset;
        internal int StringsIdCount;
        internal int StringsIdSize;
        //+=4;
        internal int StringsInfoOffset;
        internal int StringsInfoCount;
        internal int StringsInfoSize;
        //+=4;
        internal int StringsTagsOffset;
        internal int StringsTagsSize1;
        internal int StringsTagsSize2;
        //+=4;
        internal int StringsBlockOffset;
        internal int StringBlockSizeUTF8;
        internal int StringsBlockSizeUnicode;
        //+=4;

        public long ATFBegin;
        public void ReadHeader(BinaryReader reader)
        {
            ATFBegin = reader.BaseStream.Position;

            Signature = Helpers.ReadString(reader, Encoding.ASCII);
            StringsCount = reader.ReadInt32();
            reader.BaseStream.Position += 8;

            if(Signature != "ATF")
            {
                Console.WriteLine("Incorrect magic: {0}! ATF was expected.", Signature);
                return;
            }

            StringsIdOffset = reader.ReadInt32();
            StringsIdCount = reader.ReadInt32();
            StringsIdSize = reader.ReadInt32();
            reader.BaseStream.Position += 4;

            StringsInfoOffset = reader.ReadInt32();
            StringsInfoCount = reader.ReadInt32();
            StringsInfoSize = reader.ReadInt32();
            reader.BaseStream.Position += 4;

            StringsTagsOffset = reader.ReadInt32();
            StringsTagsSize1 = reader.ReadInt32();
            StringsTagsSize2 = reader.ReadInt32();
            reader.BaseStream.Position += 4;

            StringsBlockOffset = reader.ReadInt32();
            StringBlockSizeUTF8 = reader.ReadInt32();
            StringsBlockSizeUnicode = reader.ReadInt32();
            reader.BaseStream.Position += 4;
        }
    }
    internal class ATF
    {
        public void PackText(string inputText, string inputUexp)
        {
            string newUexp = Path.GetFileNameWithoutExtension(inputUexp) + "_new" + Path.GetExtension(inputUexp);
            string uasset = Path.GetFileNameWithoutExtension(inputUexp) + ".uasset";

            int c = 0;
            string[] text = File.ReadAllLines(inputText);
            List<int> newSizes = new List<int>();
            List<int> newOffsets = new List<int>();
            using(BinaryReader reader = new BinaryReader(File.OpenRead(inputUexp)))
            using (MemoryStream outMS = new MemoryStream())
            using(BinaryWriter writer = new BinaryWriter(outMS))
            {
                //считываем шапку uexp, затем atf
                UEXPHeader header = new UEXPHeader();
                ATFHeader atf = new ATFHeader();
                header.ReadHeader(reader);
                atf.ReadHeader(reader);


                //копируем uexp до блока с текстом в память и возвращаем на позицию 0
                reader.BaseStream.Position = 0;
                writer.Write(reader.ReadBytes((int)(atf.ATFBegin + atf.StringsBlockOffset)));
                reader.BaseStream.Position = 0;

                //записываем все новые строки
                int tableStart = (int)(atf.ATFBegin + atf.StringsBlockOffset);
                for (int i = 0; i < text.Length; i++)
                {
                    string txt = text[i].Replace("{LF}", "\n").Replace("{CRLF}", "\r\n").Replace("{CR}", "\r");
                    newSizes.Add(txt.Length);
                    newOffsets.Add(c);
                    writer.Write(Encoding.Unicode.GetBytes(txt));
                    writer.Write(new short());
                    c += txt.Length + 1;
                }

                //переходим к таблице и записываем
                writer.BaseStream.Position = atf.ATFBegin + atf.StringsInfoOffset;
                for (int i = 0; i < text.Length; i++)
                {
                    //скипаем значения тегов
                    writer.BaseStream.Position += 16;
                    writer.Write(newOffsets[i]);
                    writer.Write(newSizes[i]);
                    writer.BaseStream.Position += 8;
                }

                writer.BaseStream.Position = writer.BaseStream.Length;
                writer.Write(0x9E2A83C1);
                header.WriteNewSize(writer);
                File.WriteAllBytes(newUexp, outMS.ToArray());

                if (File.Exists(uasset))
                {
                    using (BinaryWriter assetWriter = new BinaryWriter(new FileStream(uasset, FileMode.Open)))
                    {
                        assetWriter.BaseStream.Position = 0x208;
                        assetWriter.Write(writer.BaseStream.Length - 4);
                        assetWriter.Close();
                        Console.WriteLine("{0} has been changed!", uasset);
                    }
                }
                Console.WriteLine("Complete.");
            }
        }
        public string[] GetText(BinaryReader reader)
        {
            ATFHeader header = new ATFHeader();
            header.ReadHeader(reader);
            List<int> ids = new List<int>();
            List<int> tagsOffsets = new List<int>();
            List<int> tagsLength = new List<int>();
            List<int> stringOffsets = new List<int>();
            List<int> stringLength = new List<int>();

            string[] tags = new string[header.StringsCount];
            string[] text = new string[header.StringsCount];

            //считываем id (0, 1, 2, 3 и т.д.)
            reader.BaseStream.Position = header.ATFBegin + header.StringsIdOffset;
            for (int i = 0; i < header.StringsIdCount * 2; i += 2)
            {
                ids.Add(reader.ReadInt32());
                ids.Add(reader.ReadInt32());
                reader.BaseStream.Position += 0x28;
            }

            //считываем таблицу
            reader.BaseStream.Position = header.ATFBegin + header.StringsInfoOffset;
            for (int i = 0; i < header.StringsCount; i++)
            {
                tagsOffsets.Add(reader.ReadInt32());
                tagsLength.Add(reader.ReadInt32());
                reader.BaseStream.Position += 0x08;
                stringOffsets.Add(reader.ReadInt32());
                stringLength.Add(reader.ReadInt32());
                reader.BaseStream.Position += 0x08;
            }

            //тэги (ASCII)
            for (int i = 0; i < header.StringsCount; i++)
            {
                reader.BaseStream.Position = header.ATFBegin + header.StringsTagsOffset + tagsOffsets[i];
                tags[i] = Regex.Replace(Encoding.ASCII.GetString(reader.ReadBytes(tagsLength[i])), "\0", "");
            }

            //строки (Unicode)
            reader.BaseStream.Position = header.ATFBegin + header.StringsBlockOffset;
            for (int i = 0; i < header.StringsCount; i++)
            {
                text[i] = Encoding.Unicode.GetString(reader.ReadBytes(stringLength[i] * 2)).Replace("\r\n", "{CRLF}").Replace("\n", "{LF}").Replace("\r", "{CR}");
                reader.BaseStream.Position += 2;
            }
            return text;
        }
    }
}
