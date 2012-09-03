using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NHunspell;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace thousandWords
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MyThes thesaurus = new MyThes("C:\\th_en_US_new.dat");
        OpenFileDialog fileDialog = new OpenFileDialog();
        SaveFileDialog saveDialog = new SaveFileDialog();
        string imagePath;

        public struct Ratio
        {
            public double X
            {
                get
                {
                    return _X;
                }
                set
                {
                    _X = value;
                }
            }
            double _X;

            public double Y
            {
                get
                {
                    return _Y;
                }
                set
                {
                    _Y = value;
                }
            }
            double _Y;

            public float Value
            {
                get
                {
                    return (float)X / (float)Y;
                }
            }

            public Ratio(double Width, double Height)
            {
                _X = Width;
                _Y = Height;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            fileDialog.Multiselect = false;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            //
            //Find the synonyms of the words given
            //
            List<string> words;
            string wordString = textBox1.Text.Replace(" ", "");
            string[] wordsArray = wordString.Split(',');
            words = wordsArray.ToList<string>();
            words = GetSynonyms(words);

            string bigWords = "";
            int totalLetters = 0;
            Ratio targetAR = new Ratio(image1.Source.Width, image1.Source.Height);

            foreach (string word in words)
            {
                bigWords = bigWords + word;
            }

            // Arbitrary artificial enlargement
            // In other words... 1,000 words worth of letters is not very many pixels
            totalLetters = bigWords.Length;

            //Ratio actualAR = GetAspectRatio(1440 * 900, 16f / 10f);
            Ratio actualAR = GetAspectRatio(totalLetters, targetAR.Value);

            BitmapSource bitImage = ResizeBitmap((BitmapImage)image1.Source, actualAR.X, actualAR.Y);


            #region Read Pixel Data

            Color[,] colors = new Color[bitImage.PixelWidth, bitImage.PixelHeight];

            for (int pixely = 0; pixely < bitImage.PixelHeight; pixely++)
            {
                for (int pixelx = 0; pixelx < bitImage.PixelWidth; pixelx++)
                {
                    colors[pixelx, pixely] = GetPixel(bitImage, pixelx, pixely);
                }
            }

            #endregion

            #region Build HTML page

            string pageBuilder = "<html>\n<body style=\"font-family: monospace; background-color: #000000\">\n";
            char[] bigChars = bigWords.ToCharArray();

            for (int pixely = 0; pixely < bitImage.PixelHeight; pixely++)
            {
                for (int pixelx = 0; pixelx < bitImage.PixelWidth; pixelx++)
                {
                    pageBuilder = pageBuilder + "<font color=\"" + colors[pixelx, pixely].ToString() + "\">" + bigChars[pixely * bitImage.PixelWidth + pixelx] + "</font>";
                }

                pageBuilder = pageBuilder + "<br/>\n";
            }

            pageBuilder = pageBuilder + "</body>\n</html>";

            bool? isSelected = saveDialog.ShowDialog(this);

            if (isSelected == true)
            {
                File.WriteAllText(saveDialog.FileName, pageBuilder);
            }

            #endregion

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            bool? IsSelected = fileDialog.ShowDialog();

            if (IsSelected == true)
            {
                imagePath = fileDialog.FileName;

                BitmapImage bitImage = new BitmapImage();
                bitImage.BeginInit();
                bitImage.UriSource = new Uri(fileDialog.FileName, UriKind.Absolute);
                bitImage.EndInit();
                image1.Source = bitImage;
            }
        }

        //
        // Unashamedly copied from online somewhere
        //
        public Color GetPixel(BitmapSource bitmap, int x, int y)
        {
            Debug.Assert(bitmap != null);
            Debug.Assert(x >= 0);
            Debug.Assert(y >= 0);
            Debug.Assert(x < bitmap.PixelWidth);
            Debug.Assert(y < bitmap.PixelHeight);
            Debug.Assert(bitmap.Format.BitsPerPixel >= 24);

            CroppedBitmap cb = new CroppedBitmap(bitmap, new Int32Rect(x, y, 1, 1));
            byte[] pixel = new byte[bitmap.Format.BitsPerPixel / 8];
            cb.CopyPixels(pixel, bitmap.Format.BitsPerPixel / 8, 0);
            return Color.FromRgb(pixel[2], pixel[1], pixel[0]);
        }

        //
        // An arguably crude way to get a possible aspect ratio from 
        // a number of units (pixels) and a target AR
        //
        public Ratio GetAspectRatio(int numberOfUnits, float targetAspectRatio)
        {
            float lastAR = 0f;
            float testAR = 0f;

            for (int w = numberOfUnits; w >= 1; w--)
            {
                int h = numberOfUnits / w;

                lastAR = testAR;
                testAR = (float)w / (float)h;

                if (testAR <= targetAspectRatio && lastAR > targetAspectRatio)
                {
                    return new Ratio(w, h);
                }

            }

            return new Ratio();
        }

        public BitmapSource ResizeBitmap(BitmapImage sourceBitmap, double newWidth, double newHeight)
        {
            var photoDecoder = BitmapDecoder.Create(sourceBitmap.UriSource, 
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.None);
            var photo = photoDecoder.Frames[0];
            
            var target = new TransformedBitmap(photo,
                new ScaleTransform(
                    newWidth / photo.Width * 96 / photo.DpiX,
                    newHeight / photo.Height * 96 / photo.DpiY, 0, 0));

            return target;
        }

        public List<string> GetSynonyms(List<string> Words)
        {
            int l = Words.Count;
            int i = 0;

            while (i < l)
            {
                ThesResult result = thesaurus.Lookup(Words[i]);

                if (result != null)
                {
                    foreach (ThesMeaning meaning in result.Meanings)
                    {
                        foreach (string word in meaning.Synonyms)
                        {
                            if (!Words.Contains(word))
                            {
                                Words.Add(word);
                            }
                        }
                    }
                }

                l = Words.Count;

                if (l < 1000)
                    i++;
                else
                    break;

            }

            return Words;
        }
    }
}
