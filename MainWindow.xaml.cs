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

namespace thousandWords
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MyThes thesaurus = new MyThes("C:\\th_en_US_new.dat");
        OpenFileDialog fileDialog = new OpenFileDialog();
        BitmapImage bitImage;

        public MainWindow()
        {
            InitializeComponent();

            fileDialog.Multiselect = false;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            string wordString = textBox1.Text.Replace(" ", "");
            string[] wordsArray = wordString.Split(',');
            List<string> words = wordsArray.ToList<string>();

            int l = words.Count;
            int i = 0;

            while (i < l)
            {
                ThesResult result = thesaurus.Lookup(words[i]);

                if(result != null)
                {
                    foreach (ThesMeaning meaning in result.Meanings)
                    {
                        foreach (string word in meaning.Synonyms)
                        {
                            if (!words.Contains(word))
                            {
                                words.Add(word);
                                Debug.WriteLine(word);
                            }
                        }
                    }
                }

                l = words.Count;

                if (l < 1000)
                    i++;
                else
                    break;

            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            bool? IsSelected = fileDialog.ShowDialog();

            if (IsSelected == true)
            {
                // Create image element to set as icon on the menu element
                bitImage = new BitmapImage();
                bitImage.BeginInit();
                bitImage.UriSource = new Uri(fileDialog.FileName, UriKind.Absolute);
                bitImage.EndInit();
                image1.Source = bitImage;
            }
        }
    }
}
