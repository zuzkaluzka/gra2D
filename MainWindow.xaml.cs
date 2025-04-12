using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // Stałe reprezentujące rodzaje terenu
        public const int LAS = 1;     // las
        public const int LAKA = 2;     // łąka
        public const int SKALA = 3;   // skały
        public const int SARNA = 4;  //sarna
        public const int SIEKIERA = 5;  //siekiera
        public const int SCIETE_DRZEWO = 6;  //ścięte drzewo
        public const int ILE_TERENOW = 7;   // ile terenów
        // Mapa i jej oryginalna wersja przechowywana jako tablica dwuwymiarowa int
        private int[,] mapa;
        private int[,] oryginalnaMapa;
        //rozmiar mapy
        private int szerokoscMapy;
        private int wysokoscMapy;

        // Dwuwymiarowa tablica kontrolek Image reprezentujących segmenty mapy
        private Image[,] tablicaTerenu;
        // Rozmiar jednego segmentu mapy w pikselach
        private const int RozmiarSegmentu = 32;

        // Tablica obrazków terenu – indeks odpowiada rodzajowi terenu
        // Indeks 1: las, 2: łąka, 3: skały
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];

        // Pozycja gracza na mapie
        private int pozycjaGraczaX = 0;
        private int pozycjaGraczaY = 0;
        // Obrazek reprezentujący gracza
        private Image obrazGracza;
        // Licznik zgromadzonego drewna
        private int iloscDrewna = 0;
        //zmienna sprawdzająca czy gracz ma przy sobie siekierę
        private bool maSiekiere = false;
        //zmienna sprawdzająca czy sarna uciekła
        private bool czySarnaUciekla = false;

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();

            // Inicjalizacja obrazka gracza
            obrazGracza = new Image
            {
                Width = RozmiarSegmentu,
                Height = RozmiarSegmentu
            };
            BitmapImage bmpGracza = new BitmapImage(new Uri("gracz.png", UriKind.Relative));
            obrazGracza.Source = bmpGracza;
        }
        private void WczytajObrazyTerenu()
        {
            // Zakładamy, że tablica jest indeksowana od 0, ale używamy indeksów 1-3
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
            obrazyTerenu[SARNA] = new BitmapImage(new Uri("sarna.png", UriKind.Relative));
            obrazyTerenu[SIEKIERA] = new BitmapImage(new Uri("siekiera.png", UriKind.Relative));
            obrazyTerenu[SCIETE_DRZEWO] = new BitmapImage(new Uri("sciete-drzewo.png", UriKind.Relative));
        }

        // Wczytuje mapę z pliku tekstowego i dynamicznie tworzy tablicę kontrolek Image
        private void WczytajMape(string sciezkaPliku)
        {
            try
            {
                var linie = File.ReadAllLines(sciezkaPliku);//zwraca tablicę stringów, np. linie[0] to pierwsza linia pliku
                wysokoscMapy = linie.Length;
                szerokoscMapy = linie[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;//zwraca liczbę elementów w tablicy
                mapa = new int[wysokoscMapy, szerokoscMapy];
                oryginalnaMapa = (int[,])mapa.Clone();

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    var czesci = linie[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);//zwraca tablicę stringów np. czesci[0] to pierwszy element linii
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        mapa[y, x] = int.Parse(czesci[x]);//wczytanie mapy z pliku
                        oryginalnaMapa[y, x] = mapa[y, x];
                    }
                }

                // Przygotowanie kontenera SiatkaMapy – czyszczenie elementów i definicji wierszy/kolumn
                SiatkaMapy.Children.Clear();
                SiatkaMapy.RowDefinitions.Clear();
                SiatkaMapy.ColumnDefinitions.Clear();

                for (int y = 0; y < wysokoscMapy; y++)
                {
                    SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });
                }
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });
                }

                // Tworzenie tablicy kontrolk Image i dodawanie ich do siatki
                tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
                for (int y = 0; y < wysokoscMapy; y++)
                {
                    for (int x = 0; x < szerokoscMapy; x++)
                    {
                        Image obraz = new Image
                        {
                            Width = RozmiarSegmentu,
                            Height = RozmiarSegmentu
                        };

                        int rodzaj = mapa[y, x];
                        if (rodzaj >= 1 && rodzaj < ILE_TERENOW)
                        {
                            obraz.Source = obrazyTerenu[rodzaj];//wczytanie obrazka terenu
                        }
                        else
                        {
                            obraz.Source = null;
                        }

                        Grid.SetRow(obraz, y);
                        Grid.SetColumn(obraz, x);
                        SiatkaMapy.Children.Add(obraz);//dodanie obrazka do siatki na ekranie
                        tablicaTerenu[y, x] = obraz;
                    }
                }

                // Dodanie obrazka gracza – ustawiamy go na wierzchu
                SiatkaMapy.Children.Add(obrazGracza);
                Panel.SetZIndex(obrazGracza, 1);//ustawienie obrazka gracza na wierzchu
                pozycjaGraczaX = 0;
                pozycjaGraczaY = 0;
                AktualizujPozycjeGracza();

                iloscDrewna = 0;
                EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
            }//koniec try
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wczytywania mapy: " + ex.Message);
            }
        }

        // Aktualizuje pozycję obrazka gracza w siatce
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(obrazGracza, pozycjaGraczaY);
            Grid.SetColumn(obrazGracza, pozycjaGraczaX);
        }

        //funkcja resetująca grę gdy gracz wejdzie na skałę
        private void ResetujGre()
        {
            //pętla do resetowania obrazków na mapie
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    //ustawienie odpowiednich obrazów na mapie
                    if (mapa[y, x] == LAS)
                    {
                        tablicaTerenu[y, x].Source = obrazyTerenu[LAS]; 
                    }
                    else if (mapa[y, x] == LAKA)
                    {
                        tablicaTerenu[y, x].Source = obrazyTerenu[LAKA];
                    }
                    else if (mapa[y, x] == SKALA)
                    {
                        tablicaTerenu[y, x].Source = obrazyTerenu[SKALA];
                    }
                }
            }
            //pętla do przywrócenia oryginalnego stanu mapy
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    if (oryginalnaMapa[y, x] == LAS) 
                    {
                        tablicaTerenu[y, x].Source = obrazyTerenu[LAS];
                        mapa[y, x] = LAS;
                    }
                    if (oryginalnaMapa[y, x] == SARNA)
                    {
                        tablicaTerenu[y, x].Source = obrazyTerenu[SARNA];
                        mapa[y, x] = SARNA;
                    }
                    if (oryginalnaMapa[y, x] == SIEKIERA)
                    {
                        tablicaTerenu[y, x].Source = obrazyTerenu[SIEKIERA];
                        mapa[y, x] = SIEKIERA;
                    }
                }
            }
            oryginalnaMapa = new int[wysokoscMapy, szerokoscMapy];
            //resetowanie zmiennych 
            maSiekiere = false;
            czySarnaUciekla = false;


            //reset pozycji gracza i drewna
            pozycjaGraczaX = 0;
            pozycjaGraczaY = 0;
            iloscDrewna = 0;
            EtykietaDrewna.Content = "Drewno: " + iloscDrewna;

            //przywrócenie pozycji gracza
            AktualizujPozycjeGracza();

            MessageBox.Show("Gracz stanął na skale! Gra zrestartowana.");
        }
        //funkcja losująca nową pozycje na mapie
        private (int, int) LosowaPozycja()
        {
            Random rnd = new Random();
            int x, y;
            do
            {
                x = rnd.Next(0, szerokoscMapy);
                y = rnd.Next(0, wysokoscMapy);
            }
            while (mapa[y, x] != LAS); //sarna może się pojawić tylko na polu las

            return (x, y);  
        }

        // Obsługa naciśnięć klawiszy – ruch gracza oraz wycinanie lasu
        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            int nowyX = pozycjaGraczaX;
            int nowyY = pozycjaGraczaY;
            //zmiana pozycji gracza
            if (e.Key == Key.W) nowyY--;
            else if (e.Key == Key.S) nowyY++;
            else if (e.Key == Key.A) nowyX--;
            else if (e.Key == Key.D) nowyX++;
            //Gracz nie może wyjść poza mapę
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
            {
                //reset gry gdy gracz stanie na skale
                if (mapa[nowyY, nowyX] == SKALA)
                {
                    ResetujGre();
                    return;
                }
                if (mapa[nowyY, nowyX] == LAS) //jeśli gracz stanął na pole las
                {
                    if (maSiekiere == true) //sprawdzenie czy gracz ma siekierę
                    {
                        UzytkownikMaSiekiere(nowyY, nowyX);

                    }
                }

                // Gracz nie może wejść na pole ze skałami
                if (mapa[nowyY, nowyX] != SKALA)
                {
                    pozycjaGraczaX = nowyX;
                    pozycjaGraczaY = nowyY;
                    AktualizujPozycjeGracza();
                }
                if (mapa[nowyY, nowyX] == SARNA)
                {
                    NowaPozycjaSarny(nowyY, nowyX);
                }
                if (mapa[nowyY, nowyX] == SIEKIERA)
                {
                    ZmianaNaLake(nowyY, nowyX);
                }
                if (iloscDrewna == 7)
                {
                    MessageBox.Show("Przeszedłeś grę!");
                }
            }


            // Obsługa wycinania lasu – naciskamy klawisz C
            if (e.Key == Key.C)
            {
                WycinanieLasu(nowyY, nowyX);
            }
        }
        private void UzytkownikMaSiekiere(int nowyY,int nowyX)
            {
                tablicaTerenu[nowyY, nowyX].Source = obrazyTerenu[SCIETE_DRZEWO];
                oryginalnaMapa[nowyY, nowyX] = LAS; //zamiana poprzedniego pola na ścięte drzewo
                iloscDrewna++;
                EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
            }
        private void NowaPozycjaSarny(int nowyY,int nowyX)
        {
            MessageBox.Show("Sarna uciekła!");
            var (nowyYSarny, nowyXSarny) = LosowaPozycja(); //wylosowanie nowej pozycji dla sarny
            oryginalnaMapa[nowyYSarny, nowyXSarny] = mapa[nowyYSarny, nowyXSarny]; //zapisanie w oryginalnej mapie co było przed ucieczką sarny
            tablicaTerenu[nowyYSarny, nowyXSarny].Source = obrazyTerenu[SARNA];
            mapa[nowyYSarny, nowyXSarny] = SARNA; //zapisanie sarny w mapie
            if (czySarnaUciekla == false)
            {
                oryginalnaMapa[nowyY, nowyX] = SARNA;
            }
            //ustawienie w starym miejscu sarny
            tablicaTerenu[nowyY, nowyX].Source = obrazyTerenu[LAS];
            mapa[nowyY, nowyX] = LAS;
            //oznaczenie że sarna uciekła
            czySarnaUciekla = true;
        }
        private void ZmianaNaLake(int nowyY,int nowyX)
        {
            maSiekiere = true; //garcz ma siekierę
            tablicaTerenu[nowyY, nowyX].Source = obrazyTerenu[LAKA]; //zamiana obrazka terenu na łąkę
            mapa[nowyY, nowyX] = LAKA; //aktualizacja mapy
            oryginalnaMapa[nowyY, nowyX] = SIEKIERA; //zapisanie w oryginalnej mapie, że na tym polu jest siekiera
        }
        private void WycinanieLasu(int nowyY, int nowyX)
        {
            if (mapa[pozycjaGraczaY, pozycjaGraczaX] == LAS)//jeśli gracz stoi na polu lasu
            {
                mapa[pozycjaGraczaY, pozycjaGraczaX] = LAKA; //zamiana pola lasu na łakę
                if (oryginalnaMapa[pozycjaGraczaY, pozycjaGraczaX] == SARNA) //jeśli gracz stoi na polu sarna 
                {
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA]; //obraz zmienia sie w łąkę
                    iloscDrewna++;
                    EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
                }
                else
                {
                    oryginalnaMapa[pozycjaGraczaY, pozycjaGraczaX] = LAS; // na pozycję gracza zostaje przypisane pole las
                    tablicaTerenu[pozycjaGraczaY, pozycjaGraczaX].Source = obrazyTerenu[LAKA]; //obraz zmienia się na łąkę
                    iloscDrewna++;
                    EtykietaDrewna.Content = "Drewno: " + iloscDrewna;
                }
            }
        }

        // Obsługa przycisku "Wczytaj mapę"
        private void WczytajMape_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oknoDialogowe = new OpenFileDialog();
            oknoDialogowe.Filter = "Plik mapy (*.txt)|*.txt";
            oknoDialogowe.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory; // Ustawienie katalogu początkowego
            bool? czyOtwartoMape = oknoDialogowe.ShowDialog();
            if (czyOtwartoMape == true)
            {
                WczytajMape(oknoDialogowe.FileName);
            }
        }
    }
}


