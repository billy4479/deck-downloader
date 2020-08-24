using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using BillyUtils.WebHelpers;

namespace deck_downloader {
    public enum DLMode { All, JSON, Image }

    static internal class DownloadHelper {
        const string cardDataURL = "https://db.ygoprodeck.com/api/v7/cardinfo.php?id=";
        public static readonly string folderPath = Path.Join (AppDomain.CurrentDomain.BaseDirectory, "downloaded");
        public static readonly string imagePath = Path.Join (folderPath, "images");
        public static readonly string jsonPath = Path.Join (folderPath, "data.json");
        public static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create (UnicodeRanges.BasicLatin)
        };

        public static Card[] DownloadAll (DLMode mode, Dictionary<int, int> ids) {
            Directory.CreateDirectory (folderPath);
            Directory.CreateDirectory (imagePath);
            Card[] cards = null;
            try {
                var JSON = DownloadJSON (ids);
                Console.WriteLine("Done!                                                         ");
                cards = ParseJSON (JSON);
            } catch (System.Exception) {
                Console.WriteLine ($"Exception while downloading cards data");
            }

            if (mode != DLMode.JSON) {
                foreach (var card in cards) {
                    try {
                        DownloadImage (card);
                        Thread.Sleep (200);
                    } catch (System.Exception) {
                        Console.WriteLine ($"Exception while downloading the image of the card with id {card.id}");
                    }
                }
                Console.WriteLine("Done!                                                        ");
            }

            return cards;
        }

        private static Card[] ParseJSON (Dictionary<string, int> input) {
            var result = new List<Card> ();
            Console.WriteLine ($"Parsing data...");
            foreach (var jsonStr in input) {
                JsonElement obj = JsonSerializer.Deserialize<JsonElement> (jsonStr.Key);
                var data = obj.GetProperty ("data");
                var arr = data.EnumerateArray ();
                arr.MoveNext ();
                var wCard = arr.Current;

                var tmp = new Card ();

                tmp.id = wCard.GetProperty ("id").GetInt32 ();
                tmp.name = wCard.GetProperty ("name").GetString ();
                tmp.cardType = wCard.GetProperty ("type").GetString ();
                tmp.description = wCard.GetProperty ("desc").GetString ();

                tmp.count = jsonStr.Value;

                //Monster-only props
                if (tmp.cardType.Contains ("Monster")) {
                    tmp.atk = wCard.GetProperty ("atk").GetInt32 ();
                    tmp.def = wCard.GetProperty ("def").GetInt32 ();
                    tmp.level = wCard.GetProperty ("level").GetInt32 ();
                    tmp.type = wCard.GetProperty ("race").GetString ();
                    tmp.attribute = wCard.GetProperty ("attribute").GetString ();
                }

                //Image
                var images = wCard.GetProperty ("card_images").EnumerateArray ();
                images.MoveNext ();
                tmp.image_url = images.Current.GetProperty ("image_url").GetString ();
                tmp.image_path = Path.Join (imagePath, $"{tmp.id}.jpg");
                result.Add (tmp);
                arr.Dispose ();
            }
            var json = JsonSerializer.Serialize (result, serializerOptions);
            using (var sw = new StreamWriter (File.Open (jsonPath, FileMode.OpenOrCreate))) {
                sw.WriteLine (json);
            }
            return result.ToArray ();
        }

        //Returns a dictionary with as key the json string and as value the count
        private static Dictionary<string, int> DownloadJSON (Dictionary<int, int> ids) {
            var result = new Dictionary<string, int> ();
            foreach (var id in ids) {
                Console.Write ($"Downloading data of card {id.Key}...   \r");
                result.Add (HTTPRequest.GET (cardDataURL + id.Key), id.Value);
                Thread.Sleep (200);
            }
            return result;
        }

        private static void DownloadImage (Card card) {
            Console.Write ($"Downloading image of card {card.id}...   \r");
            var img = Downloader.DownloadBytes (card.image_url);
            using (var fs = File.Open (card.image_path, FileMode.OpenOrCreate)) {
                fs.Write (img, 0, img.Length);
            }
        }
    }
}