using System;
using System.IO;

namespace ggText
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                PrintUsage();
            else if (args.Length == 1)
            {
                string ext = Path.GetExtension(args[0]);
                REDText red = new REDText();
                if (ext == ".uexp")
                {
                    Console.WriteLine("Mode: Extraction");
                    if (args[0].Contains("REDGame"))
                        red.ExtractLocalization(args[0]);
                    else if (args[0].Contains("storytext"))
                        red.ExtractStory(args[0]);
                    else
                        red.ExtractLibrary(args[0]);
                }
                else if (ext == ".txt")
                {
                    string filewo = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(args[0]));
                    Console.WriteLine("Mode: Repacking");
                    if (args[0].Contains("REDGame"))
                    {
                            red.RepackLocalization(args[0], filewo + ".uexp");
                    }
                    else if (args[0].Contains("_text_"))
                    {
                            red.PackLibrary(args[0], filewo + ".uexp");
                    }
                    else if (args[0].Contains("storytext_INT"))
                    {
                            red.PackStory(args[0], filewo + ".uexp");
                    }
                    else if (args[0].Contains("storytext"))
                        Console.WriteLine("Story container is not supported yet!");
                    else
                        Console.WriteLine("Unsupported type of file.");
                }
                else
                {
                    if (args.Length == 0)
                        PrintUsage();
                    else if (args.Length == 1)
                        red.ExtractLibrary(args[0]);
                }
            }
            else
            {
                REDText red = new REDText();
                if (args[0] == "-e")
                {
                    Console.WriteLine("Mode: Extraction");
                    if (args[1].Contains("REDGame"))
                        red.ExtractLocalization(args[1]);
                    else if (args[1].Contains("storytext"))
                        red.ExtractStory(args[1]);
                    else
                        red.ExtractLibrary(args[1]);
                }
                else if (args[0] == "-i")
                {
                    Console.WriteLine("Mode: Repacking");
                    if (args[2].Contains("REDGame"))
                    {
                        red.RepackLocalization(args[1], args[2]);
                    }
                    else if (args[2].Contains("_text_"))
                        red.PackLibrary(args[1], args[2]);
                    else if (args[2].Contains("storytext"))
                        Console.WriteLine("Story container is not supported yet!");
                    else
                        Console.WriteLine("Unsupported type of file.");
                }
                else
                {
                    if (args.Length == 0)
                        PrintUsage();
                    else if (args.Length == 1)
                        red.ExtractLibrary(args[0]);
                }

            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Guilty Gear Strive Text Tool by LinkOFF V.0.7");
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("\t-e\t\tExtract text from UEXP.");
            Console.WriteLine("\t-i\t\tImport text from TXT to UEXP.");
            Console.WriteLine("Example:");
            Console.WriteLine("\tggText.exe -e REDGame.uexp\t\t\tExtract text from UEXP file.");
            Console.WriteLine("\tggText.exe -i REDGame.uexp.txt REDGame.uexp\tImport all text from TXT file to a UEXP file.");
            Console.WriteLine("");
            Console.WriteLine("Supported containers (ALL): REDGame, Library, History, Correlation, Story Mode");

            return;
        }
    }
}
