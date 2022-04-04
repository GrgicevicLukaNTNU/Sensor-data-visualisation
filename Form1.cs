using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.IO.Ports;
using System.Windows.Forms.DataVisualization.Charting;

namespace Seminarski_rad
{
    public partial class Form1 : Form
    {
        //pomoćne varijable
        private SerialPort myport; 
        private DateTime datetime;
        private string s;
        private double dx = 0;

        //Liste za crtanje grafova

        List <string> vrijeme = new List<string>() ;
        List <double> temp = new List<double>() ;
        List<double> vla = new List<double>();
        List<double> lux = new List<double>();
        List<double> filtrirano = new List<double>();

        public Form1()
        {
            InitializeComponent();
            
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            //inicializacija birača datuma na sadašnje vrijeme
            dateTimePicker3.Value = DateTime.Now;
            dateTimePicker4.Value = DateTime.Now;
            dateTimePicker1.Value = DateTime.Now;


        }

        private void button4_Click(object sender, EventArgs e)
        {
            //na Start se otvara serijska komunikacija

            myport = new SerialPort();
            myport.BaudRate = 9600;
            myport.PortName = "COM4";
            myport.Parity = Parity.None;
            myport.DataBits = 8;
            myport.StopBits = StopBits.One;
            myport.DataReceived += serialPort1_DataReceived;
            try
            {
                myport.Open();
            }
            catch (Exception)
            {
                MessageBox.Show("Pločica nije priključena na port COM4");
                return;
            }


        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            s = myport.ReadExisting();


            Invoke(new EventHandler(Displaydata_event));

        }

        private void Displaydata_event(object sender, EventArgs args)
        {
            //priprema datuma u string radi kasnije usporedbe
            datetime = DateTime.Now;
            string time = datetime.Hour.ToString("d2") + ":" + datetime.Minute.ToString("d2") + ":" + datetime.Second.ToString("d2");
            string datum = datetime.Year.ToString("d4") + "/" + datetime.Month.ToString("d2") + "/" + datetime.Day.ToString("d2");

            // slaganje podataka iz serijskog porta u listu (vlažnost,temperatura,lux sadašnji,lux predhodni podatak)
            var sen = s.Split(',').ToList();
            if (sen.Count == 4)
            {
                //slanje podataka u vertikalne tkstualne okvire temperaturu i vlažnost
                textBox1.AppendText(time + " " + sen[0] + "\r\n");
                textBox5.AppendText(time + " " + sen[1] + "\r\n");

                //slanje podataka u tekstualne datoteke
                StreamWriter sw = new StreamWriter(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Temp.txt"), true);
                sw.WriteLine(datum + " " + time + " " + sen[1].TrimEnd("\r\n".ToCharArray()));
                sw.Close();
                StreamWriter SW = new StreamWriter(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Vlažnost.txt"), true);
                SW.WriteLine(datum + " " + time + " " + sen[0]);
                SW.Close();

                //pretvaranje podataka sa senzore u liste tipa double ,y vrijednosti na grafovima
                temp.Add(Convert.ToDouble(sen[1])/100);
                vla.Add(Convert.ToDouble(sen[0])/100);
                lux.Add(Convert.ToDouble(sen[2]) / 100);

                //x vrijednost na grafu
                string vr = datetime.Minute.ToString("d2") + ":" + datetime.Second.ToString("d2");
                vrijeme.Add(vr);

                //pomoćna lista i varijable za graf filtriranih podataka osvjetljenja ,sen[2] sadašnji podatak,sen[3] prijašnji
                double[] list = new double[2];
                double tocno = Convert.ToDouble(sen[2]) / 100;
                double prosla = Convert.ToDouble(sen[3]) / 100;

                list[1] = tocno;
                list[0] = prosla;

                //petlja za izbjegavanje da je prijašnja vrijednost luxa nula

                if (list[0] != 0)
                {
                   // predviđanje buduće vrijedosti lux
                    double predikcija = list[0] + dx * 2;

                    // razlika stvarne i predviđene vrijednosti
                    double rezidual = list[1] - predikcija;

                    // skalirana derivacija (sa 'h' vrijednosti) , 2 je vremenski razmak podataka
                    dx = dx + 0.02 * (rezidual / 2);

                    // procjena osvjetljenja je negdje između predikcije i mjerenja
                    double procjena = predikcija + 0.2 * rezidual;

                    //dodavanje procjene (filtriranih podataka u listu)
                    filtrirano.Add(procjena);

                    //ispisivanje u tekstualne okvire
                    textBox6.Text = tocno.ToString("F2");
                    textBox8.Text = procjena.ToString("F2");
                    textBox7.Text = predikcija.ToString("F2");
                }

                // crtanje(ponovno crtavanje) grafova
                chart1.Series["temperatura"].Points.DataBindXY(vrijeme, temp);
                chart2.Series["vlažnost"].Points.DataBindXY(vrijeme,vla);
                chart3.Series["osvjetljenje"].Points.DataBindXY(vrijeme,lux);
                chart3.Series["filtrirano"].Points.DataBindXY(vrijeme, filtrirano);

                //brisanje grafova
                chart1.Invalidate();
                chart2.Invalidate();
                chart3.Invalidate();
                

            }

        }


        private void button1_Click(object sender, EventArgs e)
        {
            // tipka 'ucitaj podatke' , provjera koji se senzor odabrao 
            if (comboBox1.SelectedIndex == 0)
            {
                //brisanje prostora gdje se ispisuje
                textBox4.Clear();

                //citanje iz datoteke
                var f = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Temp.txt"));
                //ocitanje prvog vremenskog birača (kalendara)
                string a = dateTimePicker3.Value.ToString("yyyy'/'MM'/'dd' 'HH':'mm");
                
                //određivanje duljine liste (datoteke)
                int c = f.Length;

                //razlika u datumima(vremenu)
                TimeSpan tspan = dateTimePicker4.Value - dateTimePicker3.Value;
               
                //pretvorba u minute za prikaz u tekstualnom okviru
                double dani = tspan.TotalMinutes;

                //usporedba daje -1 ako je datum 'do' veći od 'od'
                int value = DateTime.Compare(dateTimePicker3.Value, dateTimePicker4.Value);

                //ograničavanje pretrage zbog brzine
                if (tspan.TotalDays > 15)
                {
                    MessageBox.Show("Nije dozvoljeno traženje u periodu većem od 15 dana, zbog brzine izvedbe programa.");
                    value = 1;
                }

                //lista napravljena od datuma
                List<DateTime> allDates = new List<DateTime>();

               
                //dodavanje u listu i pretvorba u strigove
                for (DateTime date = dateTimePicker3.Value; date <= dateTimePicker4.Value; date = date.AddMinutes(1))
                { allDates.Add(date); }
                DateTime[] adate = allDates.ToArray();
                string[] v = Array.ConvertAll(adate, element => element.ToString("yyyy'/'MM'/'dd' 'HH':'mm"));

                //pisanje minuta u tektualni okvir
                textBox3.Text = dani.ToString();
                
                //usporedba stringova iz datoteke i liste napravljen od datuma te ispisivanje korektnih vrijenosti u okvir
                if (value == -1)
                {
                    for (int i = 0; i < c; i++)
                    {
                        for (int j = 0; j < v.Length; j++)
                        {
                            if (f[i].StartsWith(v[j]))
                                textBox4.AppendText(f[i] + "\r\n");

                        }
                    }
                    
                }
                //ako je odabran isti datum
                else if (value == 0)
                {
                    for (int i = 0; i < c; i++)
                    {
                        if (f[i].StartsWith(a))
                            textBox4.Text = f[i];

                    }
                }
                else MessageBox.Show("Ispravno unesi datume");
            }
            // isti postupak za odabrani senzor vlažnosti
            if (comboBox1.SelectedIndex == 1)
            {
                textBox4.Clear();
                var F = File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Vlažnost.txt"));
                string A = dateTimePicker3.Value.ToString("yyyy'/'MM'/'dd' 'HH':'mm");
                int C = F.Length;

                TimeSpan tspan = dateTimePicker4.Value - dateTimePicker3.Value;

                double Dani = tspan.TotalMinutes;

                int Value = DateTime.Compare(dateTimePicker3.Value, dateTimePicker4.Value);


                List<DateTime> allDates = new List<DateTime>();

                for (DateTime date = dateTimePicker3.Value; date <= dateTimePicker4.Value; date = date.AddMinutes(1))
                { allDates.Add(date); }
                DateTime[] adate = allDates.ToArray();
                string[] V = Array.ConvertAll(adate, element => element.ToString("yyyy'/'MM'/'dd' 'HH':'mm"));


                textBox3.Text = Dani.ToString();

                if (Value == -1)
                {
                    for (int i = 0; i < C; i++)
                    {
                        for (int j = 0; j < V.Length; j++)
                        {
                            if (F[i].StartsWith(V[j]))
                                textBox4.AppendText(F[i] + "\r\n");

                        }
                    }

                }
                else if (Value == 0)
                {
                    for (int i = 0; i < C; i++)
                    {
                        if (F[i].StartsWith(A))
                            textBox4.Text = F[i];

                    }
                }

                else MessageBox.Show("Ispravno unesi datume");
            }
           
        }
        //tipka za slanje podataka u datoteku (podatci koji se upisiju ne budu sortirani)
        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 1)
            {
                
                StreamWriter A = new StreamWriter(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Vlažnost.txt"), true);
                A.WriteLine("\r\n" + dateTimePicker1.Value.ToString(("yyyy'/'MM'/'dd' 'HH':'mm':'ss ")) + textBox2.Text);
                A.Close();
                textBox2.Clear();

                //pozivanje funkcije 'učitaj podatke' ,da se upisani podatak odmah prikaze u okviru
                button1_Click(sender, e);
            }

            // isti postupak u slucaju da je odabran senzor temperature
            if (comboBox1.SelectedIndex == 0)
            {

                StreamWriter B = new StreamWriter(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Temp.txt"), true);
                B.WriteLine("\r\n" + dateTimePicker1.Value.ToString(("yyyy'/'MM'/'dd' 'HH':'mm':'ss ")) + textBox2.Text);
                B.Close();
                textBox2.Clear();
                button1_Click(sender, e);
            }
            // 'provjera' da je odabran sezor
            if (comboBox1.SelectedIndex != 0 && comboBox1.SelectedIndex != 1) { MessageBox.Show("Odaberi senzor za prikaz podataka."); }
        }

        // 'save' opcija za spremanje podataka
        private void button3_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0 || comboBox1.SelectedIndex == 1)
            {
                saveFileDialog1.ShowDialog();
            }
            else { MessageBox.Show("Odaberi senzor za spremanje podataka."); }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
           
            if (comboBox1.SelectedIndex == 0)
            {
                string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string temp = Path.Combine(executableLocation, "Temp.txt");
                string temp1 = saveFileDialog1.FileName;
                File.Copy(temp, temp1);
                MessageBox.Show("Datoteka je kreirana.");
            }
            if (comboBox1.SelectedIndex == 1)
            {
                string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string temp = Path.Combine(executableLocation, "Vlažnost.txt");
                string temp1 = saveFileDialog1.FileName;
                File.Copy(temp, temp1);
                MessageBox.Show("Datoteka je kreirana.");
            }
            
        }
        //zaustavljanje dotoka podataka, zatvaranje serijske komunikacije
        private void button5_Click(object sender, EventArgs e)
        {
            if (serialPort1 != null)
            {
                if (myport != null)
                {
                    myport.Close();
                }
                MessageBox.Show("Pokreni dotok podataka na 'Start'.");
            }
        }

        // omogućavanje samo brojeva za vrijednosti za pohranu u tekstualnu datoteku
        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }

    }
}