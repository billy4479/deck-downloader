using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using BillyUtils.WebHelpers;

namespace deck_downloader {
    class Program {
        static bool useDefault = false;

        static void Main(string[] args) {
            Console.Clear();

            Console.WriteLine("*********************************************************************************");
            Console.WriteLine("* Yu-Gi-Oh Deck Downloader - By billy4479                                       *");
            Console.WriteLine("*                                                                               *");
            Console.WriteLine("* You can find the code on Github: https://github.com/billy4479/deck-downloader *");
            Console.WriteLine("* Powered by the awesome YGOPRODeck API: https://ygoprodeck.com                 *");
            Console.WriteLine("* PDF Framework: iText7 https://itextpdf.com                                    *");
            Console.WriteLine("*********************************************************************************\n");

            //Args parsing
            bool cleanMode = false;
            foreach (var arg in args) {
                switch (arg) {
                    case "-y":
                        useDefault = true;
                        break;
                    case "-c":
                        cleanMode = true;
                        break;
                    case "--help":
                        Console.WriteLine("\n\t-y\tRun in batch mode, no input from user is required.\n\t--help\tShow this screen");
                        return;
                    default:
                        Console.WriteLine($"Invalid argumant: {arg}. Use --help for more informations");
                        return;
                }
            }

            DLMode dlMode = default;
            bool pdfOnly = false;
            bool noPdf = false;
            if (!useDefault) {
                Console.Write("Mode: \n\t0. Everything (default)\n\t1. PDF Only (no internet required)\n\t2. No PDF\n\t3. No images and no PDF\nEnter mode: ");
                var choise = Console.ReadKey().KeyChar;
                switch (choise) {
                    case '0':
                        dlMode = DLMode.All;
                        break;
                    case '\r':
                        dlMode = DLMode.All;
                        break;
                    case '\n':
                        dlMode = DLMode.All;
                        break;
                    case '1':
                        pdfOnly = true;
                        break;
                    case '2':
                        dlMode = DLMode.All;
                        noPdf = true;
                        break;
                    case '3':
                        noPdf = true;
                        dlMode = DLMode.JSON;
                        break;
                    default:
                        Console.WriteLine("\nInvalid mode.");
                        Environment.Exit(1);
                        break;
                }
            }
            Console.WriteLine();

            if (cleanMode && Directory.Exists(DownloadHelper.folderPath)) Directory.Delete(DownloadHelper.folderPath, true);

            Card[] cards = null;
            if (!pdfOnly) {
                var ids = LoadFile();
                cards = DownloadHelper.DownloadAll(dlMode, ids);
            } else {
                string json;

                using(var sr = new StreamReader(File.OpenRead(DownloadHelper.jsonPath))) {
                    json = sr.ReadToEnd();
                }

                cards = JsonSerializer.Deserialize<Card[]>(json, DownloadHelper.serializerOptions);
            }

            if (!noPdf)
                PDFCreator.CreatePDF(cards);
        }

        static Dictionary<int, int> LoadFile() {
            string pathToYDK = null;
            if (!useDefault) {
                Console.Write("Enter the path to path to the file containing the IDs (default is ./deck.ydk): ");
                pathToYDK = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(pathToYDK)) {
                pathToYDK = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "deck.ydk");
            }

            var result = new Dictionary<int, int>();
            var valid = ValidateFile(pathToYDK, out int[] ids);
            if (!valid) {
                Console.WriteLine("Invalid File!");
                if (useDefault) {
                    Environment.Exit(1);
                }
                result = LoadFile();
                return result;
            }

            foreach (var id in ids) {
                if (result.ContainsKey(id)) {
                    result[id]++;
                } else {
                    result.Add(id, 1);
                }
            }

            return result;
        }

        static bool ValidateFile(string path, out int[] ids) {
            ids = null;
            if (!File.Exists(path)) {
                return false;
            }

            string content;
            using(StreamReader sr = File.OpenText(path)) {
                content = sr.ReadToEnd();
            }

            var lines = content.Split('\n');
            var idList = new List<int>();

            foreach (var line in lines) {
                if (line.StartsWith('#') || line.StartsWith('!')) {
                    continue;
                }
                line.Replace("\n", "");
                var success = int.TryParse(line, out int tmp);

                if (!success) {
                    return false;
                }

                idList.Add(tmp);
            }
            ids = idList.ToArray();

            return true;
        }

    }
}