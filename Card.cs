using System;

namespace deck_downloader {
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
        public int link_level { get; set; }
        public string[] link_markers { get; set; }
    }

}