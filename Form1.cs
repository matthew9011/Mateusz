using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;

namespace Filtracja
{
    
    public partial class Form1 : Form
    {
        //Zmienna typu Size moze byc przez nas dowolnie 
        //powołana i wykorzystana w kodzie. Dzieki temu
        //mozna zdefiniowac zadany rozmiar obrazku z wewnatrz kodu.
        //Właściwość StrechImage będzie odpowiedzialna za dopasowanie
        //rozmiarow
        private Size desired_image_size;
        Image<Bgr, byte> image_PB1, image_PB2, image_PB3, image_PB4;    /*, image_BUF1, image_BUF2, image_BUF3*/
        Image<Bgr, byte>[] image_buffers;
        double[] filter_coeff;
        VideoCapture camera;


        Queue<Point> pix_tlace = new Queue<Point>();
        Queue<Point> pix_palace = new Queue<Point>();
        Queue<Point> pix_nadpalone = new Queue<Point>();
        Queue<Point> pix_wypalone = new Queue<Point>();

        private MCvScalar aktualnie_klikniety = new MCvScalar(0, 0, 0);
        private MCvScalar cecha_palnosci = new MCvScalar(0xFF, 0xFF, 0xFF);
        private MCvScalar cecha_nadpalenia = new MCvScalar(0, 0, 0);

        private MCvScalar kolor_tlenia = new MCvScalar(51, 153, 255);
        private MCvScalar kolor_palenia = new MCvScalar(0, 0, 204);
        private MCvScalar kolor_nadpalenia = new MCvScalar(51, 204, 51);
        private MCvScalar kolor_wypalenia = new MCvScalar(100, 100, 100);
        private MCvScalar aktualny_kolor_wypalenia = new MCvScalar(100, 100, 100);

        private int nr_pozaru = 0;
        private bool skos = false;
        private bool cecha_dowolna = false;

        private MCvScalar wykryj_kolor = new MCvScalar(0xFF, 0xFF, 0xFF);


        //Konstruktor klasy Form1. Odpowiada za inicjalizację wszystkich
        //komponentów (kontrolki i ich rozmieszczenie i właściwości)
        //na oknie aplikacji
        public Form1()
        {
            InitializeComponent();
            desired_image_size = new Size(320, 240);
            image_PB1 = new Image<Bgr, byte>(desired_image_size);
            image_PB2 = new Image<Bgr, byte>(desired_image_size);
            image_PB3 = new Image<Bgr, byte>(desired_image_size);
            image_PB4 = new Image<Bgr, byte>(desired_image_size);

            filter_coeff = new double[9];

            image_buffers = new Image<Bgr, byte>[3];
            for (int i = 0; i < image_buffers.Length; i++)
            {
                image_buffers[i] = new Image<Bgr, byte>(desired_image_size);
            }

            //Blok try catch aby przechwycić ewentualne niepowodzenie
            //tworzenia nowej instancji obiektu VideoCapture. Potrzebny, gdyz
            //w chwili tworzenia następuje próba połączenia się z kamerą która
            //może zakończyć się niepowodzeniem.
            try
            {
                camera = new VideoCapture();
                camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, desired_image_size.Width);
                camera.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, desired_image_size.Height);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void button_From_CvInvoke_PB1_Click(object sender, EventArgs e)
        {
            CvInvoke.Circle(image_PB1, new Point(200, 150), 50, new MCvScalar(255, 0, 0), -1);
            CvInvoke.Rectangle(image_PB1, new Rectangle(20, 20, 100, 120), new MCvScalar(0, 255, 0), -1);
            CvInvoke.Line(image_PB1, new Point(120, 150), new Point(20, 150), new MCvScalar(0, 0, 255), 7);
            pictureBox1.Image = image_PB1.Bitmap;
        }


        private void button_Clr_PB1_Click(object sender, EventArgs e)
        {
            //Możliwe jest przekazanie obiektów jakie chcemy zmodyfikować
            //jako argumenty metody.
            //Obiekty w C# są domyślnie przekazywane jako referencje. Są to tzw. typy referencyjne
            //Oznacza to, że zmiany dokonane na tak przekazanych obiektach "przeniosą się"
            //poza metodę, w której te modyfikacje były dokonane
            //PS: zmienne typów int, double itd to tzw typy wartościowe, a nie referencyjne.
            //Oznacza to, że te typy są kopiowane do metody.
            clear_image(pictureBox1, image_PB1);
        }

        private void button_Clr_PB2_Click(object sender, EventArgs e)
        {
            clear_image(pictureBox2, image_PB2);
        }

        private void clear_image(PictureBox PB, Image<Bgr, byte> Image)
        {
            //Zmienne typu PictureBox i Image<Bgr, byte> to instancje klas.
            //Zostały zatem "pod maską" przekazane do metody jako referencje.
            Image.SetZero();
            PB.Image = Image.Bitmap;
        }

        private void button_Browse_Files_PB1_Click(object sender, EventArgs e)
        {
            textBox_Image_Path_PB1.Text = get_image_path();
        }



        private string get_image_path()
        {
            string ret = "";
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Obrazy|*.jpg;*.jpeg;*.png;*.bmp";
            openFileDialog1.Title = "Wybierz obrazek.";
            //Jeśli wszystko przebiegło ok to pobiera nazwę pliku
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            { 
                ret = openFileDialog1.FileName;
            }

            return ret;
        }

        private void button_From_File_PB1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = get_image_bitmap_from_file(textBox_Image_Path_PB1.Text, ref image_PB1);
        }


        private Bitmap get_image_bitmap_from_file(string path, ref Image<Bgr, byte> Data)
        {
            Mat temp = CvInvoke.Imread(path);
            CvInvoke.Resize(temp, temp, desired_image_size);
            Data = temp.ToImage<Bgr, byte>();
            return Data.Bitmap;
        }

        private void button_From_Camera_PB1_Click(object sender, EventArgs e)
        {
            Mat temp = camera.QueryFrame();
            CvInvoke.Resize(temp, temp, pictureBox1.Size);
            image_PB1 = temp.ToImage<Bgr, byte>();
            pictureBox1.Image = image_PB1.Bitmap;
        }

        private void button_From_Camera_PB2_Click(object sender, EventArgs e)
        {
            Mat temp = camera.QueryFrame();
            CvInvoke.Resize(temp, temp, pictureBox2.Size);
            image_PB2 = temp.ToImage<Bgr, byte>();
            pictureBox2.Image = image_PB2.Bitmap;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        //Bufory
        private void button_Buf1_To_PB1_Click(object sender, EventArgs e)
        {
            image_PB1.SetZero();
            CopyImage(image_buffers[0].Data, image_PB1.Data, image_PB1.Size);
            pictureBox1.Image = image_PB1.Bitmap;
        }

        private void button_Buf1_From_PB1_Click(object sender, EventArgs e)
        {
            image_buffers[0].SetZero();
            CopyImage(image_PB1.Data, image_buffers[0].Data, image_buffers[0].Size);
            pictureBox_BUF1.Image = image_buffers[0].Bitmap;
        }

        private void button_Buf2_To_PB1_Click(object sender, EventArgs e)
        {
            image_PB1.SetZero();
            CopyImage(image_buffers[1].Data, image_PB1.Data, image_PB1.Size);
            pictureBox1.Image = image_PB1.Bitmap;
        }

        private void button_Buf2_From_PB1_Click(object sender, EventArgs e)
        {
            image_buffers[1].SetZero();
            CopyImage(image_PB1.Data, image_buffers[1].Data, image_buffers[1].Size);
            pictureBox_BUF2.Image = image_buffers[1].Bitmap;
        }

        private void button_Buf3_To_PB1_Click(object sender, EventArgs e)
        {
            image_PB1.SetZero();
            CopyImage(image_buffers[2].Data, image_PB1.Data, image_PB1.Size);
            pictureBox1.Image = image_PB1.Bitmap;
        }

        private void button_Buf3_From_PB1_Click(object sender, EventArgs e)
        {
            image_buffers[2].SetZero();
            CopyImage(image_PB1.Data, image_buffers[2].Data, image_buffers[2].Size);
            pictureBox_BUF3.Image = image_buffers[2].Bitmap;
        }

        private void button_Buf1_To_PB2_Click(object sender, EventArgs e)
        {
            image_PB2.SetZero();
            CopyImage(image_buffers[0].Data, image_PB2.Data, image_PB2.Size);
            pictureBox2.Image = image_PB2.Bitmap;
        }

        private void button_Buf1_From_PB2_Click(object sender, EventArgs e)
        {
            image_buffers[0].SetZero();
            CopyImage(image_PB2.Data, image_buffers[0].Data, image_buffers[0].Size);
            pictureBox_BUF1.Image = image_buffers[0].Bitmap;
        }

        private void button_Buf2_To_PB2_Click(object sender, EventArgs e)
        {
            image_PB2.SetZero();
            CopyImage(image_buffers[1].Data, image_PB2.Data, image_PB2.Size);
            pictureBox2.Image = image_PB2.Bitmap;
        }

        private void button_Buf2_From_PB2_Click(object sender, EventArgs e)
        {
            image_buffers[1].SetZero();
            CopyImage(image_PB2.Data, image_buffers[1].Data, image_buffers[1].Size);
            pictureBox_BUF2.Image = image_buffers[1].Bitmap;
        }

        private void button_Buf3_To_PB2_Click(object sender, EventArgs e)
        {
            image_PB2.SetZero();
            CopyImage(image_buffers[2].Data, image_PB2.Data, image_PB2.Size);
            pictureBox2.Image = image_PB2.Bitmap;
        }

        private void button_Buf3_From_PB2_Click(object sender, EventArgs e)
        {
            image_buffers[2].SetZero();
            CopyImage(image_PB2.Data, image_buffers[2].Data, image_buffers[2].Size);
            pictureBox_BUF3.Image = image_buffers[2].Bitmap;
        }

        private void copy_image_data(Image<Bgr, byte> src, Image<Bgr, byte> dest)
        {
            src.CopyTo(dest);
        }

        private void button_Logical_Operation_Click(object sender, EventArgs e)
        {
            byte[, ,] b1, b2, b3;
            b1 = image_buffers[0].Data;
            b2 = image_buffers[1].Data;
            b3 = image_buffers[2].Data;
            for (int x = 0; x < desired_image_size.Width; x++)
            {
                for (int y = 0; y < desired_image_size.Height; y++)
                {
                    if (radioButton_buf_AND.Checked)
                    {
                        b3[y, x, 0] = (byte)(b1[y, x, 0] & b2[y, x, 0]);
                        b3[y, x, 1] = (byte)(b1[y, x, 1] & b2[y, x, 1]);
                        b3[y, x, 2] = (byte)(b1[y, x, 2] & b2[y, x, 2]);
                    }
                    if (radioButton_buf_OR.Checked)
                    {
                        b3[y, x, 0] = (byte)(b1[y, x, 0] | b2[y, x, 0]);
                        b3[y, x, 1] = (byte)(b1[y, x, 1] | b2[y, x, 1]);
                        b3[y, x, 2] = (byte)(b1[y, x, 2] | b2[y, x, 2]);
                    }
                    if (radioButton_buf_XOR.Checked)
                    {
                        b3[y, x, 0] = (byte)(b1[y, x, 0] ^ b2[y, x, 0]);
                        b3[y, x, 1] = (byte)(b1[y, x, 1] ^ b2[y, x, 1]);
                        b3[y, x, 2] = (byte)(b1[y, x, 2] ^ b2[y, x, 2]);
                    }
                }
            }
            image_buffers[2].Data = b3;
            pictureBox_BUF3.Image = image_buffers[2].Bitmap;
        }

        //Filtry

        private void button_Low_Pass_Coeff_Click(object sender, EventArgs e)
        {
            numericUpDown_Filtr_Param11.Value = 1;
            numericUpDown_Filtr_Param12.Value = 1;
            numericUpDown_Filtr_Param13.Value = 1;
            numericUpDown_Filtr_Param21.Value = 1;
            numericUpDown_Filtr_Param22.Value = 0;
            numericUpDown_Filtr_Param23.Value = 1;
            numericUpDown_Filtr_Param31.Value = 1;
            numericUpDown_Filtr_Param32.Value = 1;
            numericUpDown_Filtr_Param33.Value = 1;

        }

        private void button_High_Pass_Coeff_Click(object sender, EventArgs e)
        {
            numericUpDown_Filtr_Param11.Value = 1;
            numericUpDown_Filtr_Param12.Value = 1;
            numericUpDown_Filtr_Param13.Value = 0;
            numericUpDown_Filtr_Param21.Value = 1;
            numericUpDown_Filtr_Param22.Value = 0;
            numericUpDown_Filtr_Param23.Value = -1;
            numericUpDown_Filtr_Param31.Value = 0;
            numericUpDown_Filtr_Param32.Value = -1;
            numericUpDown_Filtr_Param33.Value = -1;
        }

        private void button_Apply_Filter_Click(object sender, EventArgs e)
        {
            filter();
        }

        private void filter()
        {
            //Dodać kod  WAGI,SUMA
            int[] wagi = {Convert.ToInt32(numericUpDown_Filtr_Param11.Value), Convert.ToInt32(numericUpDown_Filtr_Param12.Value), Convert.ToInt32(numericUpDown_Filtr_Param13.Value),
                          Convert.ToInt32(numericUpDown_Filtr_Param21.Value), Convert.ToInt32(numericUpDown_Filtr_Param22.Value), Convert.ToInt32(numericUpDown_Filtr_Param23.Value),
                          Convert.ToInt32(numericUpDown_Filtr_Param31.Value), Convert.ToInt32(numericUpDown_Filtr_Param32.Value), Convert.ToInt32(numericUpDown_Filtr_Param33.Value)};
            byte[,,] temp1 = image_buffers[1].Data;
            byte[,,] temp2 = image_buffers[2].Data;
            double suma=0;
            for (int i = 0; i < 9; i++)
                suma += wagi[i];

            for (int x = 1; x < desired_image_size.Width - 1; x++)
            {
                for (int y = 1; y < desired_image_size.Height - 1; y++)
                {
                    //Dodać kod     WAGIxSKLADOWE, DZIELENIE, SPRAWDZENIE
                    double R = 0, G = 0, B = 0;

                        B += wagi[0] * temp1[y - 1, x - 1, 0];
                        B += wagi[1] * temp1[y - 1, x, 0];
                        B += wagi[2] * temp1[y - 1, x + 1, 0];
                        B += wagi[3] * temp1[y, x - 1, 0];
                        B += wagi[4] * temp1[y, x, 0];
                        B += wagi[5] * temp1[y, x + 1, 0];
                        B += wagi[6] * temp1[y + 1, x - 1, 0];
                        B += wagi[7] * temp1[y + 1, x, 0];
                        B += wagi[8] * temp1[y + 1, x + 1, 0];

                        G += wagi[0] * temp1[y - 1, x - 1, 1];
                        G += wagi[1] * temp1[y - 1, x, 1];
                        G += wagi[2] * temp1[y - 1, x + 1, 1];
                        G += wagi[3] * temp1[y, x - 1, 1];
                        G += wagi[4] * temp1[y, x, 1];
                        G += wagi[5] * temp1[y, x + 1, 1];
                        G += wagi[6] * temp1[y + 1, x - 1, 1];
                        G += wagi[7] * temp1[y + 1, x, 1];
                        G += wagi[8] * temp1[y + 1, x + 1, 1];

                        R += wagi[0] * temp1[y - 1, x - 1, 2];
                        R += wagi[1] * temp1[y - 1, x, 2];
                        R += wagi[2] * temp1[y - 1, x + 1, 2];
                        R += wagi[3] * temp1[y, x - 1, 2];
                        R += wagi[4] * temp1[y, x, 2];
                        R += wagi[5] * temp1[y, x + 1, 2];
                        R += wagi[6] * temp1[y + 1, x - 1, 2];
                        R += wagi[7] * temp1[y + 1, x, 2];
                        R += wagi[8] * temp1[y + 1, x + 1, 2];

                    if((int)suma != 0)
                    {
                        B /= suma;
                        G /= suma;
                        R /= suma;
                    }
                    if (checkBox_Add_Half.Checked)
                    {
                        B += 128;
                        G += 128;
                        R += 128;
                        B /= 2;
                        G /= 2;
                        R /= 2;

                    }
                    if (B < 0) B = 0;
                    if (G < 0) G = 0;
                    if (R < 0) R = 0;
                    if (B > 255) B = 255;
                    if (G > 255) G = 255;
                    if (R > 255) R = 255;

                    temp2[y, x, 0] = (byte)B;
                    temp2[y, x, 1] = (byte)G;
                    temp2[y, x, 2] = (byte)R;
                }
            }
            image_buffers[2].Data = temp2;
            pictureBox_BUF3.Image = image_buffers[2].Bitmap;
        }

        private void button_Thresh_Click(object sender, EventArgs e)
        {
            Threshold();
        }

        private void Threshold()
        {

        }

        private void button_Dilate_Click(object sender, EventArgs e)
        {
            Dilate();
        }

        private void button_Erode_Click(object sender, EventArgs e)
        {
            Erode();
        }

        private void Dilate()
        {
            double R, G, B;

            byte[, ,] temp1 = image_buffers[1].Data;
            byte[, ,] temp2 = image_buffers[2].Data;

            for (int x = 1; x < desired_image_size.Width - 1; x++)
            {
                for (int y = 1; y < desired_image_size.Height - 1; y++)
                {
                    R = G = B = 0;

                    B = temp1[y - 1, x - 1, 0];
                    B = Math.Max(temp1[y - 1, x, 0],B);
                    B = Math.Max(temp1[y - 1, x + 1, 0], B);
                    B = Math.Max(temp1[y, x - 1, 0], B);
                    B = Math.Max(temp1[y, x, 0], B);
                    B = Math.Max(temp1[y, x + 1, 0], B);
                    B = Math.Max(temp1[y + 1, x - 1, 0], B);
                    B = Math.Max(temp1[y + 1, x, 0], B);
                    B = Math.Max(temp1[y + 1, x + 1, 0], B);

                    G = temp1[y - 1, x - 1, 1];
                    G = Math.Max(temp1[y - 1, x, 1], G);
                    G = Math.Max(temp1[y - 1, x + 1, 1], G);
                    G = Math.Max(temp1[y, x - 1, 1], G);
                    G = Math.Max(temp1[y, x, 1], G);
                    G = Math.Max(temp1[y, x + 1, 1], G);
                    G = Math.Max(temp1[y + 1, x - 1, 1], G);
                    G = Math.Max(temp1[y + 1, x, 1], G);
                    G = Math.Max(temp1[y + 1, x + 1, 1], G);

                    R = temp1[y - 1, x - 1, 2];
                    R = Math.Max(temp1[y - 1, x, 2], R);
                    R = Math.Max(temp1[y - 1, x + 1, 2], R);
                    R = Math.Max(temp1[y, x - 1, 2], R);
                    R = Math.Max(temp1[y, x, 2], R);
                    R = Math.Max(temp1[y, x + 1, 2], R);
                    R = Math.Max(temp1[y + 1, x - 1, 2], R);
                    R = Math.Max(temp1[y + 1, x, 2], R);
                    R = Math.Max(temp1[y + 1, x + 1, 2], R);
                    temp2[y, x, 0] = (byte)B;
                    temp2[y, x, 1] = (byte)G;
                    temp2[y, x, 2] = (byte)R;
                }
            }
            image_buffers[2].Data = temp2;
            pictureBox_BUF3.Image = image_buffers[2].Bitmap;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
        private void CopyImage(byte[,,] src, byte[,,] dst, Size size)
        {
            for (int X = 0; X < size.Width; X++)
            {
                for (int Y = 0; Y < size.Height; Y++)
                {
                    dst[Y, X, 0] = (byte)(src[Y, X, 0] );
                    dst[Y, X, 1] = (byte)(src[Y, X, 1] );
                    dst[Y, X, 2] = (byte)(src[Y, X, 2] );
                }
            }
        }
        private void pictureBox_BUF1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            MouseEventArgs me = e as MouseEventArgs;

            textBox_X.Text = me.X.ToString();
            textBox_Y.Text = me.Y.ToString();

            byte[,,] temp = image_PB1.Data;
            byte R, G, B;
            B = temp[me.Y, me.X, 0];
            G = temp[me.Y, me.X, 1];
            R = temp[me.Y, me.X, 2];

            aktualnie_klikniety.V0 = B;
            aktualnie_klikniety.V1 = G;
            aktualnie_klikniety.V2 = R;

            textBox_B.Text = "0x" + B.ToString("X");
            textBox_G.Text = "0x" + G.ToString("X");
            textBox_R.Text = "0x" + R.ToString("X");
        }

        private void radioButton_buf_AND_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void radioButton_buf_OR_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown_Filtr_Param11_ValueChanged(object sender, EventArgs e)
        {

        }

        private void checkBox_Mono_Thresh_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox_Add_Half_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown_Filtr_Param22_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown_Filtr_Param12_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown_Filtr_Param13_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown_Filtr_Param21_ValueChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {

        }

        private bool Sprawdz_czy_cecha_palnosci(byte B, byte G, byte R)
        {
            if (B == cecha_palnosci.V0 && G == cecha_palnosci.V1 && R == cecha_palnosci.V2)
                return true;
            else
                return false;
        }

        private bool Sprawdz_czy_cecha_nadpalenia(byte B, byte G, byte R)
        {
            if (B == cecha_nadpalenia.V0 && G == cecha_nadpalenia.V1 && R == cecha_nadpalenia.V2)
                return true;
            else
                return false;
        }

        private bool Sprawdz_czy_jakiekolwiek_nadpalenie(byte B, byte G, byte R)
        {
            if (B == cecha_palnosci.V0 && G == cecha_palnosci.V1 && R == cecha_palnosci.V2)
                return false;
            else if (B == cecha_nadpalenia.V0 && G == cecha_nadpalenia.V1 && R == cecha_nadpalenia.V2)
                return true;
            else if (B == kolor_tlenia.V0 && G == kolor_tlenia.V1 && R == kolor_tlenia.V2)
                return false;
            else if (B == kolor_nadpalenia.V0 && G == kolor_nadpalenia.V1 && R == kolor_nadpalenia.V2)
                return false;
            else if (B == kolor_palenia.V0 && G == kolor_palenia.V1 && R == kolor_palenia.V2)
                return false;
            else if (B == aktualny_kolor_wypalenia.V0 && G == aktualny_kolor_wypalenia.V1 && R == aktualny_kolor_wypalenia.V2)
                return false;
            else
                return true;
        }

        private void Wyswietl_dane_pozaru()
        {
            label_Tlace.Text = "Liczba pikseli tlacych: " + pix_tlace.Count();
            label_Palace.Text = "Liczba pikseli palacych: " + pix_palace.Count();
            label_Nadpalone.Text = "Liczba pikseli nadpalonych: " + pix_nadpalone.Count();
            label_Wypalone.Text = "Liczba pikseli wypalonych: " + pix_wypalone.Count();
            label_Liczba_obiektow.Text = "Liczba obiektów: " + nr_pozaru;
        }

        private void Wyczysc_dane_pozaru()
        {
            nr_pozaru = 0;
            pix_nadpalone.Clear();
            pix_palace.Clear();
            pix_tlace.Clear();
            pix_wypalone.Clear();
            Wyswietl_dane_pozaru();
        }



        private void button_Rozpocznij_pozar_Click(object sender, EventArgs e)
        {
            int X, Y;
            byte[,,] temp = image_PB2.Data;
            X = Convert.ToInt32(textBox_X.Text);
            Y = Convert.ToInt32(textBox_Y.Text);

            if (Sprawdz_czy_cecha_palnosci(temp[Y, X, 0], temp[Y, X, 1], temp[Y, X, 2]))
            {
                pix_tlace.Enqueue(new Point(X, Y));
                temp[Y, X, 0] = (byte)kolor_tlenia.V0;
                temp[Y, X, 1] = (byte)kolor_tlenia.V1;
                temp[Y, X, 2] = (byte)kolor_tlenia.V2;
            }

            Wyswietl_dane_pozaru();
            image_PB2.Data = temp;
            pictureBox2.Image = image_PB2.Bitmap;
        }

        private void textBox_Image_Path_PB2_TextChanged(object sender, EventArgs e)
        {

        }

        private void listView_Dane_Mechanika_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void textBox_B_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox_G_TextChanged(object sender, EventArgs e)
        {

        }

        private void button_Ustaw_Jako_Palnosc_Click(object sender, EventArgs e)
        {
            textBox_B_pal.Text = textBox_B.Text;
            textBox_G_pal.Text = textBox_G.Text;
            textBox_R_pal.Text = textBox_R.Text;

            cecha_palnosci.V0 = aktualnie_klikniety.V0;
            cecha_palnosci.V1 = aktualnie_klikniety.V1;
            cecha_palnosci.V2 = aktualnie_klikniety.V2;
        }

        private void button_Ustaw_Jako_Nadpalenie_Click(object sender, EventArgs e)
        {
            textBox_B_nadpal.Text = textBox_B.Text;
            textBox_G_nadpal.Text = textBox_G.Text;
            textBox_R_nadpal.Text = textBox_R.Text;

            cecha_nadpalenia.V0 = aktualnie_klikniety.V0;
            cecha_nadpalenia.V1 = aktualnie_klikniety.V1;
            cecha_nadpalenia.V2 = aktualnie_klikniety.V2;
        }

        private void button_Krok_pozaru_Click(object sender, EventArgs e)
        {
            button_Krok_pozaru.Enabled = false;
            Krok_Pozaru();
            Wyswietl_dane_pozaru();
            button_Krok_pozaru.Enabled = true;
        }

        private void Krok_Pozaru()
        {
            //W języku C# wszystkie tablice są tzw typami referencyjnymi. Oznacza to, że w tym przypadku
            //do metody zostanie przekazana referencja, a nie skopiowana wartość czyli zmiany dokonane w metodzie
            //będą widoczne poza nią, a wydajność nie zostanie pogorszona nadmiarowymi operacjami kopiowania.
            byte[,,] temp = image_PB2.Data;

            Tlace_do_palacych(temp);

            foreach (Point pix in pix_palace)
            {
                Tlenie_od_palacego(temp, pix);
            }

            foreach (Point pix in pix_palace)
            {
                Nadpalenie_palacego(temp, pix);
            }

            Wypalenie_palacego(temp);

            image_PB2.Data = temp;
            pictureBox2.Image = image_PB2.Bitmap;
            Wyswietl_dane_pozaru();
            //Dokańcza kolejkę oczekujących zdarzeń interfejsu graficznego. Dodatkowy opis w "button_Krok_pozaru_Click"
            Application.DoEvents();
        }

        //
        private void Tlace_do_palacych(byte[,,] temp)
        {
            while (pix_tlace.Count > 0)
            {
                Point p = pix_tlace.Dequeue();
                pix_palace.Enqueue(p);
                temp[p.Y, p.X, 0] = (byte)kolor_palenia.V0;
                temp[p.Y, p.X, 1] = (byte)kolor_palenia.V1;
                temp[p.Y, p.X, 2] = (byte)kolor_palenia.V2;
            }
        }

        private void Tlenie_od_palacego(byte[,,] temp, Point pix_in)
        {
            if (Czy_piksel_w_zakresie(pix_in))
            {
                Point[] sasiedzi = Wylicz_wspolrzedne_sasiednich_pikseli(pix_in);
                foreach (Point p in sasiedzi)
                {
                    if (Sprawdz_czy_cecha_palnosci(temp[p.Y, p.X, 0], temp[p.Y, p.X, 1], temp[p.Y, p.X, 2]))
                    {
                        pix_tlace.Enqueue(new Point(p.X, p.Y));
                        temp[p.Y, p.X, 0] = (byte)kolor_tlenia.V0;
                        temp[p.Y, p.X, 1] = (byte)kolor_tlenia.V1;
                        temp[p.Y, p.X, 2] = (byte)kolor_tlenia.V2;
                    }
                }
            }
        }

        private void Nadpalenie_palacego(byte[,,] temp, Point pix_in)
        {
            //Należy zobaczyć co się stanie z rysunkiem innym niż *.bmp i/lub takim na którym została wywołana metoda
            //resize zarówno dla cechy dowolnej (jakiejkolwiek) jak i konkretnej
            //Należy zwrócic uwagę na nieoczekiwane zmiany kolorów na modyfikowanych lub kompresowanych obrazach
            if (Czy_piksel_w_zakresie(pix_in))
            {
                Point[] sasiedzi = Wylicz_wspolrzedne_sasiednich_pikseli(pix_in);
                bool nalezy_nadpalic = false;
                foreach (Point p in sasiedzi)
                {
                    if (cecha_dowolna)
                        nalezy_nadpalic = Sprawdz_czy_jakiekolwiek_nadpalenie(temp[p.Y, p.X, 0], temp[p.Y, p.X, 1], temp[p.Y, p.X, 2]);
                    else
                        nalezy_nadpalic = Sprawdz_czy_cecha_nadpalenia(temp[p.Y, p.X, 0], temp[p.Y, p.X, 1], temp[p.Y, p.X, 2]);
                    if (nalezy_nadpalic)
                    {
                        pix_nadpalone.Enqueue(new Point(p.X, p.Y));
                        temp[p.Y, p.X, 0] = (byte)kolor_nadpalenia.V0;
                        temp[p.Y, p.X, 1] = (byte)kolor_nadpalenia.V1;
                        temp[p.Y, p.X, 2] = (byte)kolor_nadpalenia.V2;
                    }
                }
            }
        }

        private void Wypalenie_palacego(byte[,,] temp)
        {
            while (pix_palace.Count > 0)
            {
                Point p = pix_palace.Dequeue();
                pix_wypalone.Enqueue(p);
                temp[p.Y, p.X, 0] = (byte)(aktualny_kolor_wypalenia.V0);
                temp[p.Y, p.X, 1] = (byte)(aktualny_kolor_wypalenia.V1);
                temp[p.Y, p.X, 2] = (byte)(aktualny_kolor_wypalenia.V2);
            }
        }

        private Point[] Wylicz_wspolrzedne_sasiednich_pikseli(Point pix_in)
        {
            List<Point> sasiedzi = new List<Point>();
            sasiedzi.Add(new Point(pix_in.X - 1, pix_in.Y));
            sasiedzi.Add(new Point(pix_in.X + 1, pix_in.Y));
            sasiedzi.Add(new Point(pix_in.X, pix_in.Y - 1));
            sasiedzi.Add(new Point(pix_in.X, pix_in.Y + 1));

            if (skos)
            {
                sasiedzi.Add(new Point(pix_in.X - 1, pix_in.Y - 1));
                sasiedzi.Add(new Point(pix_in.X + 1, pix_in.Y + 1));
                sasiedzi.Add(new Point(pix_in.X - 1, pix_in.Y + 1));
                sasiedzi.Add(new Point(pix_in.X + 1, pix_in.Y - 1));
            }

            return sasiedzi.ToArray();
        }

        private bool Czy_piksel_w_zakresie(Point pix_in)
        {
            int max_W, max_H;
            max_W = desired_image_size.Width - 1;
            max_H = desired_image_size.Height - 1;
            if (pix_in.X > 0 && pix_in.X < max_W && pix_in.Y > 0 && pix_in.Y < max_H)
                return true;
            else
                return false;
        }

        private void Cykl_Pozaru()
        {
            while (pix_tlace.Count() > 0)
                Krok_Pozaru();
            
        }

        private void Pozar_Calosci()
        {

            //Dokończyć
            byte[,,] temp1 = image_PB2.Data;
            for (int y = 1; y < desired_image_size.Height - 1; y++)
            {
                for (int x = 1; x < desired_image_size.Width - 1; x++)
                {
                    if (Sprawdz_czy_cecha_palnosci(temp1[y, x, 0], temp1[y, x, 1], temp1[y, x, 2]))
                    {
                        nr_pozaru++;
                        pix_tlace.Enqueue(new Point(x, y));
                        aktualny_kolor_wypalenia.V0 = (byte)(kolor_wypalenia.V0 + 1 * nr_pozaru);
                        aktualny_kolor_wypalenia.V1 = (byte)(kolor_wypalenia.V1 + 2 * nr_pozaru);
                        aktualny_kolor_wypalenia.V2 = (byte)(kolor_wypalenia.V2 + 3 * nr_pozaru);
                        Cykl_Pozaru();
                        temp1 = image_PB2.Data;
                    }

                }
            }
            Wyswietl_dane_pozaru();
            image_PB2.Data = temp1;
            pictureBox2.Image = image_PB2.Bitmap;
        }

        private void button_Cykl_pozaru_Click(object sender, EventArgs e)
        {
            button_Cykl_pozaru.Enabled = false;
            Cykl_Pozaru();
            Wyswietl_dane_pozaru();
            button_Cykl_pozaru.Enabled = true;
        }

        private void button_Pozar_calosci_Click(object sender, EventArgs e)
        {
            button_Pozar_calosci.Enabled = false;
            Wyczysc_dane_pozaru();
            Application.DoEvents();

            Pozar_Calosci();
            Wyswietl_dane_pozaru();

            numericUpDown_Numer_obiektu.Value = 1;
            if (nr_pozaru >= 1)
                numericUpDown_Numer_obiektu.Maximum = nr_pozaru;
            else
                numericUpDown_Numer_obiektu.Maximum = 1;

            button_Pozar_calosci.Enabled = true;
        }

        private void button_Pokaz_obiekt_Click(object sender, EventArgs e)
        {
            Narysuj_wybrany_obiekt((int)numericUpDown_Numer_obiektu.Value);
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown_Numer_obiektu_ValueChanged(object sender, EventArgs e)
        {

        }

        private void button_Mechanika_Click(object sender, EventArgs e)
        {
            listView_Dane_Mechanika.Items.Clear();
            image_PB4.Data = image_PB3.Data;

            //Reczne liczenie
            double F, Sx, Sy, x0, y0;
            double Jx0, Jy0, Jx0y0, Jx, Jy, Jxy, Je_0, Jt_0;
            double alfa_e, alfa_t, alfa_e_deg, alfa_t_deg;
            F = Sx = Sy = Jx0 = Jy0 = Jx0y0 = Jx = Jy = Jxy = Je_0 = Jt_0 = alfa_e = alfa_t = alfa_e_deg = alfa_t_deg = x0 = y0 = 0;

            //Odciecie ewentualnego stykania sie z krawedzia obrazu
            CvInvoke.Rectangle(image_PB4, new Rectangle(0, 0, desired_image_size.Width, desired_image_size.Height), new MCvScalar(0, 0, 0), 2);
            pictureBox4.Image = image_PB4.Bitmap;
            Application.DoEvents();

            //Wyliczenie momentow 1 i 2 stopnia
            byte[,,] temp = image_PB4.Data;
            for (int X = 0; X < desired_image_size.Width; X++)
            {
                for (int Y = 0; Y < desired_image_size.Height; Y++)
                {
                    if (temp[Y, X, 0] == 0xFF && temp[Y, X, 1] == 0xFF && temp[Y, X, 2] == 0xFF)
                    {
                        F = F + 1;
                        Sx = Sx + Y;
                        Sy = Sy + X;
                        Jx = Jx + Math.Pow(Y, 2);
                        Jy = Jy + Math.Pow(X, 2);
                        Jxy = Jxy + X * Y;
                    }
                }
            }
            //Obliczenie środka cieżkości
            if (F > 0)
            {
                x0 = Sy / F;
                y0 = Sx / F;
            }
            //Obliczenie momentów centralnych
            Jx0 = Jx - F * Math.Pow(y0, 2);
            Jy0 = Jy - F * Math.Pow(x0, 2);
            Jx0y0 = Jxy - F * x0 * y0;

            Je_0 = (Jx0 + Jy0) / 2 + Math.Sqrt(0.25 * Math.Pow(Jy0 - Jx0, 2) + Math.Pow(Jx0y0, 2));
            Jt_0 = (Jx0 + Jy0) / 2 - Math.Sqrt(0.25 * Math.Pow(Jy0 - Jx0, 2) + Math.Pow(Jx0y0, 2));

            if (Jy0 != Je_0)
                alfa_e = Math.Atan(Jx0y0 / (Jy0 - Je_0));
            else
                alfa_e = Math.PI / 2;

            if (Jy0 != Jt_0)
                alfa_t = Math.Atan(Jx0y0 / (Jy0 - Jt_0));
            else
                alfa_t = Math.PI / 2;


            //Przykład wykorzystania biblioteki Emgu
            //Image<Gray, byte> image_mech = image_PB3.Convert<Gray, byte>();
            //MCvMoments m = CvInvoke.Moments(image_mech, true);
            //
            //Point srodek_ciezkosci = new Point();
            //srodek_ciezkosci.X = (int)(m.M10 / m.M00);
            //srodek_ciezkosci.Y = (int)(m.M01 / m.M00);
            //
            //double moment20 = CvInvoke.cvGetCentralMoment(ref m, 2, 0);
            //double moment02 = CvInvoke.cvGetCentralMoment(ref m, 0, 2);
            //double moment11 = CvInvoke.cvGetCentralMoment(ref m, 1, 1);
            //double tang2alfa;
            //double katObrotuUkladu;
            //
            //tang2alfa = 2 * moment11 / (moment02 - moment20);
            //katObrotuUkladu = Math.Atan(tang2alfa);
            //katObrotuUkladu = katObrotuUkladu / 2;
            //katObrotuUkladu = (Math.PI / 2) - katObrotuUkladu;
            //
            //double[] wektor_czerw = new double[2];
            //double[] wektor_nieb = new double[2];
            //wektor_czerw[0] = Math.Cos(katObrotuUkladu);
            //wektor_czerw[1] = Math.Sin(katObrotuUkladu);
            //
            //wektor_nieb[0] = Math.Cos(katObrotuUkladu - Math.PI / 2);
            //wektor_nieb[1] = Math.Sin(katObrotuUkladu - Math.PI / 2);

            double[] wektor_czerw = new double[2];
            double[] wektor_nieb = new double[2];

            wektor_czerw[0] = Math.Cos(alfa_e);
            wektor_czerw[1] = Math.Sin(alfa_e);

            wektor_nieb[0] = Math.Cos(alfa_t);
            wektor_nieb[1] = Math.Sin(alfa_t);

            //Rysowanie punktów przeciecia
            Point P1, P2, P3, P4, Pc;
            P1 = new Point();
            P2 = new Point();
            P3 = new Point();
            P4 = new Point();
            Pc = new Point((int)x0, (int)y0);
            bool czarny = false;
            int i, zakres;
            zakres = 320;
            i = 0;
            while (czarny == false && i > -zakres && i < zakres)
            {
                int X = (int)(Pc.X + i * wektor_czerw[0]);
                int Y = (int)(Pc.Y + i * wektor_czerw[1]);
                if (temp[Y, X, 0] == 0)
                {
                    P1.X = X;
                    P1.Y = Y;
                    CvInvoke.Circle(image_PB4, P1, 6, new MCvScalar(0, 0, 255), 2);
                    czarny = true;
                }
                i++;
            }

            //Dokończyć
            czarny = false;
            i = 0;
            while (czarny == false && i > -zakres && i < zakres)
            {
                int X = (int)(Pc.X - i * wektor_czerw[0]);
                int Y = (int)(Pc.Y - i * wektor_czerw[1]);
                if (temp[Y, X, 0] == 0)
                {
                    P2.X = X;
                    P2.Y = Y;
                    CvInvoke.Circle(image_PB4, P2, 6, new MCvScalar(255, 0, 255), 2);
                    czarny = true;
                }
                i++;
            }

            czarny = false;
            i = 0;
            while (czarny == false && i > -zakres && i < zakres)
            {
                int X = (int)(Pc.X + i * wektor_nieb[0]);
                int Y = (int)(Pc.Y + i * wektor_nieb[1]);
                if (temp[Y, X, 0] == 0)
                {
                    P3.X = X;
                    P3.Y = Y;
                    CvInvoke.Circle(image_PB4, P3, 6, new MCvScalar(0, 255, 255), 2);
                    czarny = true;
                }
                i++;
            }

            czarny = false;
            i = 0;
            while (czarny == false && i > -zakres && i < zakres)
            {
                int X = (int)(Pc.X - i * wektor_nieb[0]);
                int Y = (int)(Pc.Y - i * wektor_nieb[1]);
                if (temp[Y, X, 0] == 0)
                {
                    P4.X = X;
                    P4.Y = Y;
                    CvInvoke.Circle(image_PB4, P4, 6, new MCvScalar(0, 255, 0), 2);
                    czarny = true;
                }
                i++;
            }

            //Długość
            double d1, d2, dlugosc;
            d1 = d2 = dlugosc = 0;

            if (d1 >= d2)
                dlugosc = d1;
            else
                dlugosc = d2;

            //Dokończyć
            CvInvoke.Circle(image_PB4, Pc, 6, new MCvScalar(255, 0, 0), 2);

            CvInvoke.Line(image_PB4, Pc, new Point((int)(Pc.X + 120), (int)(Pc.Y)), new MCvScalar(0, 255, 0), 2);
            CvInvoke.Line(image_PB4, Pc, new Point((int)(Pc.X + 100 * wektor_czerw[0]), (int)(Pc.Y + 100 * wektor_czerw[1])), new MCvScalar(0, 0, 255), 2);
            CvInvoke.Line(image_PB4, Pc, new Point((int)(Pc.X + 100 * wektor_nieb[0]), (int)(Pc.Y + 100 * wektor_nieb[1])), new MCvScalar(255, 0, 0), 2);

            image_PB4.Data = temp;
            pictureBox4.Image = image_PB4.Bitmap;


            listView_Dane_Mechanika.Items.Add("Powierzchnia: " + F.ToString());
            listView_Dane_Mechanika.Items.Add("Środek ciężkości: " + Pc.ToString());
            listView_Dane_Mechanika.Items.Add("alfa(czerw): " + alfa_e_deg.ToString() + "°");
            listView_Dane_Mechanika.Items.Add("alfa(nieb): " + alfa_t_deg.ToString() + "°");
            //Dokończyć

        }
        private void button_Obrysuj_Click_1(object sender, EventArgs e)
        {
            int ymax, ymin, xmax, xmin;
            byte[,,] temp = image_PB4.Data;
            ymin = xmin = 1000;
            xmax = ymax = 0;
            //Dokończyć

            for (int x = 0; x < desired_image_size.Width; x++)
            {
                for (int y = 0; y < desired_image_size.Height; y++)
                {
                    if (temp[y, x, 0] == 0xFF && temp[y, x, 1] == 0xFF && temp[y, x, 2] == 0xFF)
                    {
                        if (y < ymin)
                        {
                            ymin = y;
                        }
                        if (x < xmin)
                        {
                            xmin = x;
                        }
                        if (y > ymax)
                        {
                            ymax = y;
                        }
                        if (x > xmax)
                        {
                            xmax = x;
                        }
                    }
                }
            }
            //Dokończyć
            double dlugosc = xmax - xmin;
            double wysokosc = ymax - ymin;
            CvInvoke.Rectangle(image_PB4, new Rectangle(xmin, ymin, xmax - xmin, ymax - ymin), new MCvScalar(0, 0, 255));
            pictureBox4.Image = image_PB4.Bitmap;
            CvInvoke.Rectangle(image_PB1, new Rectangle(xmin, ymin, xmax - xmin, ymax - ymin), new MCvScalar(0, 0, 255));
            pictureBox1.Image = image_PB1.Bitmap;

            listView_Dane_Mechanika.Items.Add(" ");
            listView_Dane_Mechanika.Items.Add("Dlugość: " + dlugosc.ToString());
            listView_Dane_Mechanika.Items.Add("Wysokość: " + wysokosc.ToString());
            listView_Dane_Mechanika.Items.Add("xmax: " + xmax.ToString());
            listView_Dane_Mechanika.Items.Add("ymax: " + ymax.ToString());
            listView_Dane_Mechanika.Items.Add("xmin: " + xmin.ToString());
            listView_Dane_Mechanika.Items.Add("ymin: " + ymin.ToString());
        }

        private void button_wykryj_po_kolor_Click(object sender, EventArgs e)
        {
            if(nr_pozaru!=0)
            {
                wykryj_kolor.V0 = cecha_palnosci.V0;
                wykryj_kolor.V1 = cecha_palnosci.V1;
                wykryj_kolor.V2 = cecha_palnosci.V2;
                listView_Dane_Mechanika.Items.Add(" ");
                if (nr_pozaru > 1)
                {
                    listView_Dane_Mechanika.Items.Add("Jest " + nr_pozaru.ToString() + " obiektów w takim kolorze.");
                    if (wykryj_kolor.V0 >=0xA0 && wykryj_kolor.V0 <=0xD0 && wykryj_kolor.V1 >=0xA0 && wykryj_kolor.V1 <=0xD0 && wykryj_kolor.V2 >=0xA0 && wykryj_kolor.V2 <=0xD0)
                    {
                        if(((wykryj_kolor.V0 - wykryj_kolor.V1) + (wykryj_kolor.V0 - wykryj_kolor.V2)) < 0x10)
                        {
                            listView_Dane_Mechanika.Items.Add(" ");
                            listView_Dane_Mechanika.Items.Add("Wybrane obiekty to: Przyciski funkcyjne");
                        }
                    }
                    listView_Dane_Mechanika.Items.Add("Wybierz inną metodę jeżeli chcesz poznać konkretny przycisk.");
                }
                else
                {
                    if (wykryj_kolor.V1 > 0xB0 && wykryj_kolor.V0 < 0xA0 && wykryj_kolor.V2 < 0xA0)
                    {
                        listView_Dane_Mechanika.Items.Add(" ");
                        listView_Dane_Mechanika.Items.Add("Wybrany obiekt to: Przycisk START");
                    }
                    if (wykryj_kolor.V2 > 0xB0 && wykryj_kolor.V0 < 0xA0 && wykryj_kolor.V1 < 0xA0)
                    {
                        listView_Dane_Mechanika.Items.Add(" ");
                        listView_Dane_Mechanika.Items.Add("Wybrany obiekt to: Przycisk STOP");
                    }
                    if (wykryj_kolor.V1 > 0xD0 && wykryj_kolor.V0 < 0xB0 && wykryj_kolor.V2 < 0xC0)
                    {
                        listView_Dane_Mechanika.Items.Add(" ");
                        listView_Dane_Mechanika.Items.Add("Wybrany obiekt to: Wyświetlacz LCD");
                    }
                }
            }
        }

        private void Narysuj_wybrany_obiekt(int nr)
        {
            clear_image(pictureBox3, image_PB3);
            image_PB3.SetZero();
            byte[,,] temp1 = image_PB2.Data;
            byte[,,] temp2 = image_PB3.Data;

            MCvScalar kolor = new MCvScalar();
            kolor.V0 = kolor_wypalenia.V0 + 1 * nr;
            kolor.V1 = kolor_wypalenia.V1 + 2 * nr;
            kolor.V2 = kolor_wypalenia.V2 + 3 * nr;

            for (int y = 1; y < desired_image_size.Height - 2; y++)
            {
                for (int x = 1; x < desired_image_size.Width - 2; x++)
                {
                    if (temp1[y, x, 0] == kolor.V0 && temp1[y, x, 1] == kolor.V1 && temp1[y, x, 2] == kolor.V2)
                    {
                        temp2[y, x, 0] = 0xFF;
                        temp2[y, x, 1] = 0xFF;
                        temp2[y, x, 2] = 0xFF;
                    }
                }
            }
            image_PB3.Data = temp2;
            pictureBox3.Image = image_PB3.Bitmap;
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {

        }

        private void Erode()
        {
            double R, G, B;

            byte[, ,] temp1 = image_buffers[1].Data;
            byte[, ,] temp2 = image_buffers[2].Data;

            for (int x = 1; x < desired_image_size.Width - 1; x++)
            {
                for (int y = 1; y < desired_image_size.Height - 1; y++)
                {
                    R = G = B = 0;

                    B = temp1[y - 1, x - 1, 0];
                    B = Math.Min(temp1[y - 1, x, 0], B);
                    B = Math.Min(temp1[y - 1, x + 1, 0], B);
                    B = Math.Min(temp1[y, x - 1, 0], B);
                    B = Math.Min(temp1[y, x, 0], B);
                    B = Math.Min(temp1[y, x + 1, 0], B);
                    B = Math.Min(temp1[y + 1, x - 1, 0], B);
                    B = Math.Min(temp1[y + 1, x, 0], B);
                    B = Math.Min(temp1[y + 1, x + 1, 0], B);

                    G = temp1[y - 1, x - 1, 1];
                    G = Math.Min(temp1[y - 1, x, 1], G);
                    G = Math.Min(temp1[y - 1, x + 1, 1], G);
                    G = Math.Min(temp1[y, x - 1, 1], G);
                    G = Math.Min(temp1[y, x, 1], G);
                    G = Math.Min(temp1[y, x + 1, 1], G);
                    G = Math.Min(temp1[y + 1, x - 1, 1], G);
                    G = Math.Min(temp1[y + 1, x, 1], G);
                    G = Math.Min(temp1[y + 1, x + 1, 1], G);

                    R = temp1[y - 1, x - 1, 2];
                    R = Math.Min(temp1[y - 1, x, 2], R);
                    R = Math.Min(temp1[y - 1, x + 1, 2], R);
                    R = Math.Min(temp1[y, x - 1, 2], R);
                    R = Math.Min(temp1[y, x, 2], R);
                    R = Math.Min(temp1[y, x + 1, 2], R);
                    R = Math.Min(temp1[y + 1, x - 1, 2], R);
                    R = Math.Min(temp1[y + 1, x, 2], R);
                    R = Math.Min(temp1[y + 1, x + 1, 2], R);
                    temp2[y, x, 0] = (byte)B;
                    temp2[y, x, 1] = (byte)G;
                    temp2[y, x, 2] = (byte)R;
                }
            }
            image_buffers[2].Data = temp2;
            pictureBox_BUF3.Image = image_buffers[2].Bitmap;
        }
    }
}
