using System;
using System.Collections.Generic;
using System.IO;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

namespace deck_downloader {
    static internal class PDFCreator {

        const int PPI = 72;
        const float MM_PER_INCH = 25.4f;
        const float CARD_WIDTH_MM = 60f;
        const float CARD_HEIGHT_MM = 85f;
        const float PAGE_WIDTH_MM = 210f;
        const float PAGE_HEIGHT_MM = 297f;

        const float CARD_WIDTH_PX = CARD_WIDTH_MM * (PPI / MM_PER_INCH);
        const float CARD_HEIGHT_PX = CARD_HEIGHT_MM * (PPI / MM_PER_INCH);
        const float PAGE_WIDTH_PX = 595f;
        const float PAGE_HEIGHT_PX = 842f;

        static readonly float CARD_PER_ROW = (float) Math.Floor (PAGE_WIDTH_MM / CARD_WIDTH_MM);
        static readonly float CARD_PER_COLUMN = (float) Math.Floor (PAGE_HEIGHT_MM / CARD_HEIGHT_MM);
        static readonly float CART_PER_PAGE = CARD_PER_COLUMN * CARD_PER_ROW;

        static readonly string pdfPath = System.IO.Path.Join (DownloadHelper.folderPath, "cards.pdf");

        public static void CreatePDF (Card[] cardsData) {
            Console.Write ("Creating PDF... ");
            var imageDict = GetFiles (cardsData);

            PdfDocument pdfDoc = new PdfDocument (new PdfWriter (File.Open (pdfPath, FileMode.OpenOrCreate)));
            var document = new Document (pdfDoc, PageSize.A4);

            int rowIndex = 0;
            int columnIndex = 0;
            int pageIndex = 1;

            foreach (var card in imageDict) {
                var imageData = ImageDataFactory.CreateJpeg (card.Key);

                for (int i = 0; i < card.Value; i++) {
                    var image = new Image (imageData);
                    image.SetWidth (CARD_WIDTH_PX);
                    image.SetHeight (CARD_HEIGHT_PX);
                    image.SetFixedPosition (rowIndex * CARD_WIDTH_PX, PAGE_WIDTH_PX - columnIndex * CARD_HEIGHT_PX);
                    image.SetPageNumber (pageIndex);

                    document.Add (image);

                    rowIndex++;
                    if (rowIndex >= CARD_PER_ROW) {
                        columnIndex++;
                        rowIndex = 0;
                    }
                    if (columnIndex >= CARD_PER_COLUMN) {
                        columnIndex = 0;
                        document.GetPdfDocument ().AddNewPage ();
                        pageIndex++;
                    }
                }

            }
            document.Close ();
            Console.WriteLine ($"Done! File saved at {pdfPath}");
        }

        private static Dictionary<byte[], int> GetFiles (Card[] cards) {
            var result = new Dictionary<byte[], int> ();
            foreach (var card in cards)
                result.Add (File.ReadAllBytes (card.image_path), card.count);
            return result;
        }
    }
}