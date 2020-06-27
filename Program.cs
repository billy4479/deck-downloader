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
        const string cardDataURL = "https://db.ygoprodeck.com/api/v7/cardinfo.php?id=";
        static bool useDefuault = false;
        static bool pdfOnly = false;
        static bool noImg = false;
        static bool noPdf = false;
        static JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin)
        };

        [Serializable]
        class Card {
            public int count { get; set; }
            public int id { get; set; }
            public string name { get; set; }
            public int level { get; set; }
            public string image_url { get; set; }
            public string image_path { get; set; }
            public string cardType { get; set; }
            public string type { get; set; }
            public string attribute { get; set; }
            public string description { get; set; }
            public int atk { get; set; }
            public int def { get; set; }
        }

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
            foreach (var arg in args) {
                switch (arg) {
                    case "-y":
                        useDefuault = true;
                        break;
                    case "--help":
                        Console.WriteLine("\n\t-y\tRun in batch mode, no input from user is required.\n\t--help\tShow this screen");
                        return;
                    default:
                        Console.WriteLine($"Invalid argumant: {arg}. Use --help for more informations");
                        return;
                }
            }

            if (!useDefuault) {
                Console.Write("Mode: \n\t0. Everything (default)\n\t1. PDF Only (no internet required)\n\t2. No PDF\n\t3. No images\nEnter mode: ");
                var choise = Console.ReadKey().KeyChar;
                switch (choise) {
                    case '0':
                        break;
                    case '1':
                        pdfOnly = true;
                        break;
                    case '2':
                        noPdf = true;
                        break;
                    case '3':
                        noImg = true;
                        break;
                    default:
                        Console.WriteLine("\nInvalid mode.");
                        Environment.Exit(5);
                        break;
                }
            }
            Console.WriteLine();
            
            Dictionary<string, int> imageDatas = null;
            if (!pdfOnly) {
                var ids = LoadFile();
                imageDatas = Download(ids);
            } else {
                var jsonPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "downloaded", "card_data.json");
                string json;

                using(var sr = new StreamReader(File.OpenRead(jsonPath))) {
                    json = sr.ReadToEnd();
                }

                var cardData = JsonSerializer.Deserialize<Card[]>(json, serializerOptions);
                imageDatas = new Dictionary<string, int>();

                foreach (var card in cardData) {
                    using(var fs = File.OpenRead(card.image_path)) {
                        byte[] output = new byte[fs.Length];
                        fs.Read(output, 0, (int) fs.Length);
                        imageDatas.Add(Convert.ToBase64String(output), card.count);
                    }
                }

            }

            if (!noPdf && !noImg)
                PDFCreator.CreatePDF(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "downloaded", "document.pdf"), imageDatas);
        }

        static Dictionary<string, int> Download(Dictionary<int, int> ids) {
            string downloadDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "downloaded");
            if (!Directory.Exists(downloadDir)) {
                Directory.CreateDirectory(downloadDir);
            }

            var cardData = DownloadCardData(ids, true, downloadDir);
            if (noImg)
                return null;
            var imgs = DownloadImgs(cardData, true, true, downloadDir);
            var dict = new Dictionary<string, int>();
            int i = 0;
            foreach (var card in ids) {
                dict.Add(imgs[i], card.Value);
                i++;
            }
            return dict;
        }

        static string[] DownloadImgs(Card[] cards, bool returnB64 = true, bool writeToFile = false, string downloadDir = "") {
            List<string> b64 = new List<string>();
            if (writeToFile && string.IsNullOrEmpty(downloadDir)) {
                throw new ArgumentException("If writeToFile is true, downloadDir must be specified");
            }
            downloadDir = Path.Join(downloadDir, "images");
            if (!Directory.Exists(downloadDir)) {
                Directory.CreateDirectory(downloadDir);
            }

            foreach (var card in cards) {
                Console.Write("\r                                                                                                                 ");
                Console.Write($"\rDownloading the image of the card with id {card.id}...");

                var data = Downloader.DownloadBytes(card.image_url);

                if (returnB64) {
                    b64.Add(Convert.ToBase64String(data));
                }
                if (writeToFile) {
                    var dlPath = Path.Join(downloadDir, $"{card.id}.jpg");
                    using(var fs = File.Open(dlPath, FileMode.OpenOrCreate)) {
                        fs.Write(data, 0, data.Length);
                    }
                }

                Thread.Sleep(500);
                Console.Write(" Done!");
            }

            Console.Write("\r                                                                                                                 ");
            Console.Write($"\rImage download finished.");
            if (writeToFile) {
                Console.Write($" {downloadDir}");
            }
            Console.WriteLine();

            if (returnB64)
                return b64.ToArray();
            else
                return null;
        }

        static Card[] DownloadCardData(Dictionary<int, int> ids, bool writeToFile = false, string folderPath = "") {
            if (writeToFile && string.IsNullOrEmpty(folderPath)) {
                throw new ArgumentException("If writeToFile is true, folderPath must be specified");
            }

            var resultData = new List<Card>();

            foreach (var id in ids) {
                Console.Write("\r                                                                                                                 ");
                Console.Write($"\rDownloading data of the card with id {id.Key}...");

                string response;
                try {
                    response = HTTPRequest.GET(cardDataURL + id.Key);
                } catch (System.Exception e) {
                    Console.WriteLine($"Exception generated during the request of the id {id}. Is the id valid?");
                    Console.WriteLine(e);
                    continue;
                }

                JsonElement obj = JsonSerializer.Deserialize<JsonElement>(response);

                var fail = obj.TryGetProperty("error", out var error);
                if (fail) {
                    Console.WriteLine(error.GetString());
                    Thread.Sleep(500);
                    continue;
                }

                var data = obj.GetProperty("data");

                foreach (var wCard in data.EnumerateArray()) {

                    var result = new Card();

                    result.id = wCard.GetProperty("id").GetInt32();
                    result.name = wCard.GetProperty("name").GetString();
                    result.cardType = wCard.GetProperty("type").GetString();
                    result.description = wCard.GetProperty("desc").GetString();

                    result.count = id.Value;

                    //Monster-only props
                    if (result.cardType.Contains("Monster")) {
                        result.atk = wCard.GetProperty("atk").GetInt32();
                        result.def = wCard.GetProperty("def").GetInt32();
                        result.level = wCard.GetProperty("level").GetInt32();
                        result.type = wCard.GetProperty("race").GetString();
                        result.attribute = wCard.GetProperty("attribute").GetString();
                    }

                    //Image
                    var images = wCard.GetProperty("card_images").EnumerateArray();
                    images.MoveNext();
                    result.image_url = images.Current.GetProperty("image_url").GetString();
                    result.image_path = Path.Join(folderPath, "images", $"{result.id}.jpg");

                    //Console.WriteLine(JsonSerializer.Serialize(result));
                    resultData.Add(result);
                    Console.Write(" Done!");

                }

                Thread.Sleep(500);
            }

            if (writeToFile) {
                using(var sw = new StreamWriter(File.Open(Path.Join(folderPath, "card_data.json"), FileMode.OpenOrCreate))) {
                    sw.Write(JsonSerializer.Serialize(resultData.ToArray(), serializerOptions));
                }
            }
            Console.Write("\r                                                                                                                 ");
            Console.Write("\rThe download of card datas has ended.");
            if (writeToFile)
                Console.Write($" {Path.Join(folderPath, "card_data.json")}");
            Console.WriteLine();
            return resultData.ToArray();
        }

        static Dictionary<int, int> LoadFile() {
            string pathToYDK = null;
            if (!useDefuault) {
                Console.Write("Enter the path to path to the file containing the IDs (default is ./deck.ydk): ");
                pathToYDK = Console.ReadLine();
            }
            if (string.IsNullOrEmpty(pathToYDK)) {
                pathToYDK = AppDomain.CurrentDomain.BaseDirectory + "/deck.ydk";
            }

            var result = new Dictionary<int, int>();
            var valid = ValidateFile(pathToYDK, out int[] ids);
            if (!valid) {
                Console.WriteLine("Invalid File!");
                result = LoadFile();
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