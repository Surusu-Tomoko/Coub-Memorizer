using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Xml.XPath;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
//using program;

namespace Coub_Memorizer
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();

            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
        }

        List<Tuple<string, string, string, string>> LoadList = new List<Tuple<string, string, string, string>>();
        /*  public string url
            public string dir
            public string new_dir
            public string name
        }*/

        public bool cancel = false;

        public string url_api = "";
        public int api_type = 0;
        public string api_name = "";
        public string api_dir = "";
        public string api_pertmalink = "";

        //Вывод лога в асинхронном коде
        public void textlog (string text)
        {
            bool uiMarshal = textBox2.InvokeRequired;
            if (uiMarshal)
            {
                textBox2.Invoke(new Action(() => { textBox2.Text += text + Environment.NewLine; }));
            }
            else
            {
                textBox2.Text += text + Environment.NewLine;
            }
        }

        public void lableout(Label label,string text)
        {
            bool uiMarshal = label.InvokeRequired;
            if (uiMarshal)
            {
                textBox2.Invoke(new Action(() => { label.Text = text; }));
            }
            else
            {
                label.Text = text;
            }
        }

        public void progresinfo (int info)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                progressBar1.Value = info;
            });
        }

        public void progresinfo2(int info)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                progressBar2.Value = info;
            });
        }

        //Отправка запроса с Cookie (HttpClient)
        public async Task<string> Request(string url,Cookie cookie = null)
        {
            textlog("Выполнение запроса...");

            using (HttpClientHandler handler = new HttpClientHandler())
            using (HttpClient http = new HttpClient(handler))
            {
                http.DefaultRequestHeaders.Add("X-Answer", "42");
                handler.CookieContainer.Add(cookie);

                HttpResponseMessage response = await http.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    textlog("Ответ сервера: " + response.StatusCode.ToString());
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    textlog("Сервер вернул ошибку " + response.StatusCode);
                    // problems handling here
                    return null;
                }

                //await http.GetStringAsync(url);
            }
        }

        //Обычный загрузщик файлов
        public void start_download(List<Tuple<string, string, string, string>> filelist)
        {
            this.backgroundWorker1.RunWorkerAsync(filelist);
            while (this.backgroundWorker1.IsBusy)
            {
                //progressBar1.Increment(1);
                Application.DoEvents();
            }
        }

        //Кнопка сохранить аккаунт
        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            try
            {
                Cookie remember_token = new Cookie("remember_token", textBox1.Text, "/", "coub.com");

                string me_json = await Request("https://coub.com/api/v2/users/me", remember_token);

                if (me_json != null)
                {
                    JsonElement root = JsonDocument.Parse(me_json).RootElement;
                    JsonElement root2;
                    if (!root.TryGetProperty("error",out root2))
                    {
                        if (root.TryGetProperty("current_channel", out root2))
                        {
                                                                                                                                //Вывод имя пользователя
                            label5.Text = root.GetProperty("current_channel").GetProperty("title").ToString();              
                            Properties.Settings.Default.remember_token = textBox1.Text;

                            string permalink = root.GetProperty("current_channel").GetProperty("permalink").ToString();
                            Properties.Settings.Default.permalink = permalink;
                            string channel_id = root.GetProperty("current_channel").GetProperty("id").ToString();
                            Properties.Settings.Default.channel_id = channel_id;

                            System.IO.File.WriteAllText(Properties.Settings.Default.save_dir + @"/me/" + permalink + ".json", me_json);
                            textlog("Пользователь " + label5.Text + " сохранён: " + @"/me/" + permalink + ".json");

                                                                                                                                //Сохранение аккаунта
                            string channel_json = await Request("https://coub.com/api/v2/channels/" + channel_id, remember_token);
                            System.IO.File.WriteAllText(Properties.Settings.Default.save_dir + @"/channel/" + channel_id + ".json", channel_json);
                            JsonElement ch_root = JsonDocument.Parse(channel_json).RootElement;



                            string avatar = root.GetProperty("current_channel").GetProperty("avatar_versions").GetProperty("template").ToString();
                            avatar = avatar.Replace("%{version}", "profile_pic_big_2x");
                            string background_image = ch_root.GetProperty("background_image").ToString();

                            LoadList.Clear();

                            LoadList.Add(Tuple.Create(avatar, @"/avatars/", "", Path.GetFileName(avatar)));
                            LoadList.Add(Tuple.Create(background_image, @"/background_image/", "", Path.GetFileName(background_image)));

                            start_download(LoadList);

                            textlog("Информация о канале " + root.GetProperty("current_channel").GetProperty("title").ToString() + " сохранёна: " + @"/channel/" + channel_id + ".json");

                            Image tempImage = Image.FromFile(Properties.Settings.Default.save_dir + @"/avatars/" + Path.GetFileName(avatar));
                            Bitmap tempBitmap = new Bitmap(tempImage);
                            pictureBox1.Image = tempBitmap;

                            this.Size = new System.Drawing.Size(802, 492);

                            CategoryList();

                        } else textlog("Получены данные ошибочны!");
                    } else textlog("Неверный токен!");
                } else textlog("Данные не получены!");

                button1.Enabled = true;
            }
            catch (HttpRequestException)
            {
                throw;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.save_dir = folderBrowserDialog1.SelectedPath;
                label3.Text = Properties.Settings.Default.save_dir;

                string save_dir = Properties.Settings.Default.save_dir;

                if (!Directory.Exists(save_dir + @"/me")) Directory.CreateDirectory(save_dir + @"/me");
                if (!Directory.Exists(save_dir + @"/avatars")) Directory.CreateDirectory(save_dir + @"/avatars");
                if (!Directory.Exists(save_dir + @"/background_image")) Directory.CreateDirectory(save_dir + @"/background_image");
                if (!Directory.Exists(save_dir + @"/channel")) Directory.CreateDirectory(save_dir + @"/channel");
                if (!Directory.Exists(save_dir + @"/likes")) Directory.CreateDirectory(save_dir + @"/likes");
                if (!Directory.Exists(save_dir + @"/recoubs")) Directory.CreateDirectory(save_dir + @"/recoubs");
                if (!Directory.Exists(save_dir + @"/simples")) Directory.CreateDirectory(save_dir + @"/simples");
                if (!Directory.Exists(save_dir + @"/video")) Directory.CreateDirectory(save_dir + @"/video");
                if (!Directory.Exists(save_dir + @"/tags")) Directory.CreateDirectory(save_dir + @"/tags");
                if (!Directory.Exists(save_dir + @"/all")) Directory.CreateDirectory(save_dir + @"/all");
                if (!Directory.Exists(save_dir + @"/view_info")) Directory.CreateDirectory(save_dir + @"/view_info");
                if (!Directory.Exists(save_dir + @"/bookmarks")) Directory.CreateDirectory(save_dir + @"/bookmarks");
                

                button1.Enabled = true;
                CategoryList();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();
            cancel = true;
            //backgroundWorker1.CancelAsync();
            //backgroundWorkerCookie.CancelAsync();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = Properties.Settings.Default.remember_token;

            if (Properties.Settings.Default.save_dir == "")
            {
                label3.Text = "Не выбран";
                button1.Enabled = false;
                this.Size = new System.Drawing.Size(424, 492);
            } else
            {
                string save_dir = Properties.Settings.Default.save_dir;
                label3.Text = save_dir;

                if (Properties.Settings.Default.permalink != "")
                {
                    if (!Directory.Exists(save_dir + @"/me")) Directory.CreateDirectory(save_dir + @"/me");
                    if (!Directory.Exists(save_dir + @"/avatars")) Directory.CreateDirectory(save_dir + @"/avatars");
                    if (!Directory.Exists(save_dir + @"/background_image")) Directory.CreateDirectory(save_dir + @"/background_image");
                    if (!Directory.Exists(save_dir + @"/channel")) Directory.CreateDirectory(save_dir + @"/channel");
                    if (!Directory.Exists(save_dir + @"/likes")) Directory.CreateDirectory(save_dir + @"/likes");
                    if (!Directory.Exists(save_dir + @"/recoubs")) Directory.CreateDirectory(save_dir + @"/recoubs");
                    if (!Directory.Exists(save_dir + @"/simples")) Directory.CreateDirectory(save_dir + @"/simples");
                    if (!Directory.Exists(save_dir + @"/video")) Directory.CreateDirectory(save_dir + @"/video");
                    if (!Directory.Exists(save_dir + @"/tags")) Directory.CreateDirectory(save_dir + @"/tags");
                    if (!Directory.Exists(save_dir + @"/all")) Directory.CreateDirectory(save_dir + @"/all");
                    if (!Directory.Exists(save_dir + @"/view_info")) Directory.CreateDirectory(save_dir + @"/view_info");
                    if (!Directory.Exists(save_dir + @"/bookmarks")) Directory.CreateDirectory(save_dir + @"/bookmarks");

                    if (File.Exists(Properties.Settings.Default.save_dir + @"/me/" + Properties.Settings.Default.permalink + ".json"))
                    {
                        string readText = File.ReadAllText(Properties.Settings.Default.save_dir + @"/me/" + Properties.Settings.Default.permalink + ".json");

                        JsonElement root = JsonDocument.Parse(readText).RootElement;

                        label5.Text = root.GetProperty("current_channel").GetProperty("title").ToString();
                        string permalink = root.GetProperty("current_channel").GetProperty("permalink").ToString();
                        string avatar = root.GetProperty("current_channel").GetProperty("avatar_versions").GetProperty("template").ToString();
                        avatar = avatar.Replace("%{version}", "profile_pic_big_2x");
                        
                        Image tempImage = Image.FromFile(Properties.Settings.Default.save_dir + @"/avatars/" + Path.GetFileName(avatar));
                        Bitmap tempBitmap = new Bitmap(tempImage);
                        pictureBox1.Image = tempBitmap;

                        button1.Text = "Обновить аккаунт";
                        this.Size = new System.Drawing.Size(802, 492);
                        CategoryList();
                    } else
                    {
                        this.Size = new System.Drawing.Size(424, 492);
                    }
                }

                if (Properties.Settings.Default.second_dir != "")
                {
                    button12.Text = "Отключить";
                    label26.Text = Properties.Settings.Default.second_dir;
                    radioButton2.Visible = true;
                }
            }
            checkBox3.Checked = Properties.Settings.Default.ispublic;
            checkBox2.Checked = Properties.Settings.Default.web_server;
            textBox4.Text = Properties.Settings.Default.port.ToString();
            if (Properties.Settings.Default.web_server)
            {
                string server_url = "";
                if (Properties.Settings.Default.ispublic)
                    server_url = "http://127.0.0.1:";
                else
                    server_url = "http://localhost:";
                server_url += Properties.Settings.Default.port;

                linkLabel1.Text = server_url;
            }
            else
            {
                label28.Visible = false;
                linkLabel1.Visible = false;
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            List<Tuple<string, string, string, string>> filelist = e.Argument as List<Tuple<string, string, string, string>>;

            int count = filelist.Count;

            string save_dir = Properties.Settings.Default.save_dir;
            string second_dir = Properties.Settings.Default.second_dir;
            /*
               public string url;
               public string dir;
               public string new_dir;
               public string name;
            */
            int counter = 0;
            foreach (Tuple<string, string, string, string> link in filelist)
            {
                if (cancel)
                {
                    break;
                } else
                {
                    using (var client = new WebClient())
                    {
                        string url = link.Item1;
                        string dir = link.Item2 + "/";
                        string new_dir = link.Item3;
                        string name = link.Item4;

                        if (new_dir != "")
                        {
                            if (dir == @"/video/")
                            {
                                if (radioButton1.Checked == true)
                                {
                                    if (!Directory.Exists(save_dir + dir + new_dir)) Directory.CreateDirectory(save_dir + dir + new_dir);
                                    new_dir += "/";
                                } else
                                {
                                    if (!Directory.Exists(second_dir + new_dir)) Directory.CreateDirectory(second_dir + new_dir);
                                    new_dir += "/";
                                }
                            } else
                            {
                                if (!Directory.Exists(save_dir + dir + new_dir)) Directory.CreateDirectory(save_dir + dir + new_dir);
                                new_dir += "/";
                            }
                            
                        }

                        //client.DownloadFile(url, save_dir + dir + new_dir + name);

                        //client.Headers.Add(HttpRequestHeader.Cookie, "remember_token=" + Properties.Settings.Default.remember_token);
                        
                        if (dir == @"/video/")
                        {
                            if (radioButton1.Checked == true)
                            {
                                client.DownloadFile(new Uri(url), save_dir + dir + new_dir + name);
                            } else
                            {
                                client.DownloadFile(new Uri(url), second_dir + new_dir + name);
                            }
                        } else
                        {
                            client.DownloadFile(new Uri(url), save_dir + dir + new_dir + name);
                        }

                        textlog("Файл " + name + " загружен");
                        counter++;
                        progresinfo((int)(((float)counter * 100f) / (float)count));
                        //(sender as BackgroundWorker).ReportProgress(rercent, null);
                    }
                }
                  
            }
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progresinfo(100);

            if (e.Error == null)
            {
                textlog("Загрузка файлов завершена!");
                progresinfo(0);
                //cancel = false;
            }
            else
            {
                textlog("Failed to download file!");
                /*MessageBox.Show(
                    "Failed to download file",
                    "Download failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);*/
            }

            // Enable the download button and reset the progress bar.
            //progressBar1.Value = 0;
        }

        

        private void backgroundWorkerCookie_DoWork(object sender, DoWorkEventArgs e)
        {
            //Cookie cookie = new Cookie("remember_token", Properties.Settings.Default.remember_token, "/", "coub.com");

            List<Tuple<string, string, string, string>> filelist = e.Argument as List<Tuple<string, string, string, string>>;
            int count = filelist.Count;

            string save_dir = Properties.Settings.Default.save_dir;
            /*
               public string url;
               public string dir;
               public string new_dir;
               public string name;
            */
            int counter = 0;
            foreach (Tuple<string, string, string, string> link in filelist)
            {
                if (cancel)
                {
                    break;
                }
                else
                {
                    using (var client = new WebClient())
                    {
                        string url = link.Item1;
                        string dir = link.Item2 + "/";
                        string new_dir = link.Item3;
                        string name = link.Item4;


                        if (new_dir != "")
                        {
                            if (!Directory.Exists(save_dir + dir + new_dir)) Directory.CreateDirectory(save_dir + dir + new_dir);
                            new_dir += "/";
                        }

                        client.Headers.Add(HttpRequestHeader.Cookie, "remember_token=" + Properties.Settings.Default.remember_token);
                        client.DownloadFile(new Uri(url), save_dir + dir + new_dir + name);

                        textlog("Файл " + name + " загружен");
                        counter++;
                        progresinfo((int)(((float)counter * 100f) / (float)count));
                    }
                }
                Task.Delay(100);
            }
        }



        private void backgroundWorkerCookie_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //progressBar1.Value = e.ProgressPercentage;

            //textBox2.BeginInvoke(new MyDelegate(InvokeMethod), e.UserState);
        }

        public void start_download_cookie(List<Tuple<string, string, string, string>> filelist)
        {
            this.backgroundWorkerCookie.RunWorkerAsync(filelist);
            while (this.backgroundWorkerCookie.IsBusy)
            {
                //progressBar1.Increment(1);
                // Keep UI messages moving, so the form remains
                // responsive during the asynchronous operation.
                Application.DoEvents();
            }

        }

        private void backgroundWorkerCookie_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progresinfo(100);

            if (e.Error == null)
            {
                textlog("Загрузка файлов завершена!");
                progresinfo(0);
                //cancel = false;
            }
            else
            {
                textlog("Failed to download file!");
                /*MessageBox.Show(
                    "Failed to download file",
                    "Download failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);*/
            }

            // Enable the download button and reset the progress bar.
            
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            textlog("Начинаем скачивание списка лайков");

            string url = "https://coub.com/api/v2/timeline/likes?per_page=30&all=true&order_by=date&type=&scope=all&page=";
            DateTime localDate = DateTime.Now;
            string save_dir = Properties.Settings.Default.save_dir;

            Cookie remember_token = new Cookie("remember_token", textBox1.Text, "/", "coub.com");
            string first_like = await Request(url+"1", remember_token);
            JsonElement fl_root = JsonDocument.Parse(first_like).RootElement;

            int listCount;
            Int32.TryParse(fl_root.GetProperty("total_pages").ToString(), out listCount);

            textlog("Количество страниц с данными: " + listCount + " шт. Примерное количество коубов: менее " + (listCount * 30) + " шт.");

            if (!Directory.Exists(save_dir + @"/likes/" + Properties.Settings.Default.permalink)) Directory.CreateDirectory(save_dir + @"/likes/" + Properties.Settings.Default.permalink);
            LoadList.Clear();

            if (listCount > numericUpDown1.Value)
                listCount = (int)numericUpDown1.Value;

            for (int i = 1; i <= listCount; i++)
                LoadList.Add(Tuple.Create(url + i, @"/likes/" + Properties.Settings.Default.permalink, @"/" + localDate.ToString("dd-MM-yyyy_HH-mm-ss"), Path.GetFileName(i + ".json")));

            start_download_cookie(LoadList);

            textlog("Загрузка списка лайков завершена!");
            CategoryList();
            cancel = false;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            textBox2.SelectionStart = textBox2.Text.Length;
            textBox2.ScrollToCaret();
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            textlog("Начинаем скачивание списка репостов");
            string permalink = Properties.Settings.Default.permalink;

            string url = "https://coub.com/api/v2/timeline/channel/"+ permalink + "?per_page=30&order_by=newest&permalink=" + permalink + "&type=recoubs&scope=all&page=";
            DateTime localDate = DateTime.Now;
            string save_dir = Properties.Settings.Default.save_dir;

            Cookie remember_token = new Cookie("remember_token", textBox1.Text, "/", "coub.com");
            string first_like = await Request(url + "1", remember_token);
            JsonElement fl_root = JsonDocument.Parse(first_like).RootElement;

            int listCount;
            Int32.TryParse(fl_root.GetProperty("total_pages").ToString(), out listCount);

            textlog("Количество страниц с данными: " + listCount + " шт. Примерное количество коубов: менее "+ (listCount * 30) + " шт.");

            if (!Directory.Exists(save_dir + @"/recoubs/" + permalink)) Directory.CreateDirectory(save_dir + @"/recoubs/" + Properties.Settings.Default.permalink);
            LoadList.Clear();

            if (listCount > numericUpDown1.Value)
                listCount = (int)numericUpDown1.Value;

            for (int i = 1; i <= listCount; i++)
                LoadList.Add(Tuple.Create(url + i, @"/recoubs/" + permalink, @"/" + localDate.ToString("dd-MM-yyyy_HH-mm-ss"), Path.GetFileName(i + ".json")));

            //start_download_cookie(LoadList);
            start_download(LoadList);

            textlog("Загрузка списка репостов завершена!");
            CategoryList();
            cancel = false;
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            textlog("Начинаем скачивание списка коубов");
            string permalink = Properties.Settings.Default.permalink;

            string url = "https://coub.com/api/v2/timeline/channel/" + permalink + "?per_page=30&order_by=newest&permalink=" + permalink + "&type=simples&scope=all&page=";
            DateTime localDate = DateTime.Now;
            string save_dir = Properties.Settings.Default.save_dir;

            Cookie remember_token = new Cookie("remember_token", textBox1.Text, "/", "coub.com");
            string first_like = await Request(url + "1", remember_token);
            JsonElement fl_root = JsonDocument.Parse(first_like).RootElement;

            int listCount;
            Int32.TryParse(fl_root.GetProperty("total_pages").ToString(), out listCount);

            textlog("Количество страниц с данными: " + listCount + " шт. Примерное количество коубов: менее " + (listCount * 30) + " шт.");

            if (!Directory.Exists(save_dir + @"/simples/" + permalink)) Directory.CreateDirectory(save_dir + @"/simples/" + Properties.Settings.Default.permalink);
            LoadList.Clear();

            if (listCount > numericUpDown1.Value)
                listCount = (int)numericUpDown1.Value;

            for (int i = 1; i <= listCount; i++)
                LoadList.Add(Tuple.Create(url + i, @"/simples/" + permalink, @"/" + localDate.ToString("dd-MM-yyyy_HH-mm-ss"), Path.GetFileName(i + ".json")));

            start_download_cookie(LoadList);
            //start_download(LoadList);

            textlog("Загрузка списка коубов завершена!");
            CategoryList();
            cancel = false;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            cancel = true;
            textlog("Принудительное прерывание задачи....");
            //backgroundWorker1.CancelAsync();
            //backgroundWorkerCookie.CancelAsync();
        }

        class categories
        {
            public string dir { get; set; }
            public string name { get; set; }
        }

        public void CategoryList()
        {
            List<categories> category = new List<categories> { };

            category.Add(new categories { dir = "likes", name = "Лайки" });
            category.Add(new categories { dir = "recoubs", name = "Репосты" });
            category.Add(new categories { dir = "simples", name = "Коубы" });
            category.Add(new categories { dir = "tags", name = "Теги" });
            category.Add(new categories { dir = "bookmarks", name = "Закладки" });

            comboBox1.DataSource = category;

            comboBox1.DisplayMember = "name";
            comboBox1.ValueMember = "dir";
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string save_dir = Properties.Settings.Default.save_dir;
            string[] dirs = Directory.GetDirectories(save_dir +@"\" + ((categories)comboBox1.SelectedItem).dir);
            List<categories> category = new List<categories> { };
            if (dirs.Length != 0)
            {
                foreach (string dir in dirs)
                    category.Add(new categories { dir = new DirectoryInfo(dir).Name, name = new DirectoryInfo(dir).Name });

                comboBox2.DataSource = category;
                comboBox2.DisplayMember = "name";
                comboBox2.ValueMember = "dir";
            } else
            {
                comboBox2.DataSource = null;
                comboBox2.Items.Clear();
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            string save_dir = Properties.Settings.Default.save_dir;
            if (comboBox2.SelectedItem != null)
            {
                string[] dirs;
                dirs = Directory.GetDirectories(save_dir + @"\" + ((categories)comboBox1.SelectedItem).dir + @"\" + ((categories)comboBox2.SelectedItem).dir);
                List<categories> category = new List<categories> { };
                if (dirs.Length != 0)
                {
                    foreach (string dir in dirs)
                        category.Add(new categories { dir = new DirectoryInfo(dir).Name, name = new DirectoryInfo(dir).Name });

                    comboBox3.DataSource = category;
                    comboBox3.DisplayMember = "name";
                    comboBox3.ValueMember = "dir";
                    button8.Enabled = true;
                }
                else
                {
                    comboBox3.DataSource = null;
                    comboBox3.Items.Clear();
                    button8.Enabled = false;
                }
            } else
            {
                comboBox2.DataSource = null;
                comboBox2.Items.Clear();
                comboBox3.DataSource = null;
                comboBox3.Items.Clear();
                button8.Enabled = false;
            }

            
                
            
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string save_dir = Properties.Settings.Default.save_dir;
            string second_dir = Properties.Settings.Default.second_dir; 
            string[] files = Directory.GetFiles(save_dir + @"\" + ((categories)comboBox1.SelectedItem).dir + @"\" + ((categories)comboBox2.SelectedItem).dir + @"\" + ((categories)comboBox3.SelectedItem).dir); // путь к папке

            LoadList.Clear();

            int count = 0;
            int noload = 0;
            int noaudio = 0;

            int count_files = files.Count();
            int counter_files = 0;

            foreach (var item in files)
            {
                if (cancel)
                {
                    break;
                } else
                {
                    textlog(item);
                    using (StreamReader r = new StreamReader(item))
                    {
                        LoadList.Clear();
                        JsonElement json = JsonDocument.Parse(r.ReadToEnd()).RootElement;
                        for (int i = 0; i < json.GetProperty("coubs").GetArrayLength(); i++)
                        {
                            JsonElement file_versions = json.GetProperty("coubs")[i].GetProperty("file_versions");

                            string permalink = json.GetProperty("coubs")[i].GetProperty("permalink").ToString();



                            permalink = replace(permalink);

                            string videourl;
                            string audiourl = null;

                            JsonElement root2;
                            if (file_versions.GetProperty("html5").GetProperty("video").TryGetProperty("higher", out root2))
                            {
                                videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("higher").GetProperty("url").ToString();
                            }
                            else if (file_versions.GetProperty("html5").GetProperty("video").TryGetProperty("high", out root2))
                            {
                                videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("high").GetProperty("url").ToString();
                            }
                            else
                            {
                                videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("med").GetProperty("url").ToString();
                            }

                            //textlog(videourl);

                            if (file_versions.GetProperty("html5").TryGetProperty("audio", out root2))
                            {
                                if (file_versions.GetProperty("html5").GetProperty("audio").TryGetProperty("high", out root2))
                                {
                                    audiourl = file_versions.GetProperty("html5").GetProperty("audio").GetProperty("high").GetProperty("url").ToString();
                                }
                                else
                                {
                                    audiourl = file_versions.GetProperty("html5").GetProperty("audio").GetProperty("med").GetProperty("url").ToString();
                                }
                            }
                            else
                            {
                                noaudio++;
                            }

                            if (checkBox1.Checked)
                            {
                                if (json.GetProperty("coubs")[i].GetProperty("channel").TryGetProperty("avatar_versions", out root2))
                                {
                                    JsonElement avatar_json = json.GetProperty("coubs")[i].GetProperty("channel").GetProperty("avatar_versions");
                                    if (avatar_json.TryGetProperty("template", out root2))
                                    {
                                        string avatar = avatar_json.GetProperty("template").ToString();
                                        avatar = avatar.Replace("%{version}", "profile_pic_big_2x");
                                        if (!File.Exists(save_dir + @"/avatars/" + Path.GetFileName(avatar)))
                                            LoadList.Add(Tuple.Create(avatar, @"/avatars/", "", Path.GetFileName(avatar)));
                                    }
                                }
                            }

                            if (json.GetProperty("coubs")[i].TryGetProperty("image_versions", out root2))
                            {
                                if (json.GetProperty("coubs")[i].GetProperty("image_versions").TryGetProperty("template", out root2))
                                {
                                    string first_image = json.GetProperty("coubs")[i].GetProperty("image_versions").GetProperty("template").ToString();
                                    first_image = first_image.Replace("%{version}", "big");

                                    if (!File.Exists(save_dir + @"/video/" + permalink + @"/" + Path.GetFileName(first_image)))
                                        if (radioButton1.Checked == true)
                                        {
                                            if (save_dir == "")
                                            {
                                                LoadList.Add(Tuple.Create(first_image, @"/video", @"/" + permalink, Path.GetFileName(first_image)));
                                            }
                                            else if (!File.Exists(second_dir + @"/" + permalink + @"/" + Path.GetFileName(first_image)))
                                                LoadList.Add(Tuple.Create(first_image, @"/video", @"/" + permalink, Path.GetFileName(first_image)));
                                        }
                                        else if (!File.Exists(second_dir + @"/" + permalink + @"/" + Path.GetFileName(first_image)))
                                            LoadList.Add(Tuple.Create(first_image, @"/video", @"/" + permalink, Path.GetFileName(first_image)));

                                    /*if (!File.Exists(save_dir + @"/video/" + permalink + @"/" + Path.GetFileName(first_image)))
                                        LoadList.Add(Tuple.Create(first_image, @"/video", @"/" + permalink, Path.GetFileName(first_image)));
                                    */
                                }
                            }


                            if (!File.Exists(save_dir + @"/video/" + permalink + @"/" + Path.GetFileName(videourl)))
                            {
                                if (radioButton1.Checked == true)
                                {
                                    if (save_dir == "")
                                    {
                                        LoadList.Add(Tuple.Create(videourl, @"/video", @"/" + permalink, Path.GetFileName(videourl)));
                                        count++;
                                        textlog("Найден новый коуб [" + json.GetProperty("coubs")[i].GetProperty("permalink").ToString() + "] : " + json.GetProperty("coubs")[i].GetProperty("title").ToString());
                                    }
                                    else if (!File.Exists(second_dir + @"/" + permalink + @"/" + Path.GetFileName(videourl)))
                                    {
                                        LoadList.Add(Tuple.Create(videourl, @"/video", @"/" + permalink, Path.GetFileName(videourl)));
                                        count++;
                                        textlog("Найден новый коуб [" + json.GetProperty("coubs")[i].GetProperty("permalink").ToString() + "] : " + json.GetProperty("coubs")[i].GetProperty("title").ToString());
                                    } else
                                        noload++;

                                }
                                else if (!File.Exists(second_dir + @"/" + permalink + @"/" + Path.GetFileName(videourl)))
                                {
                                    LoadList.Add(Tuple.Create(videourl, @"/video", @"/" + permalink, Path.GetFileName(videourl)));
                                    count++;
                                    textlog("Найден новый коуб [" + json.GetProperty("coubs")[i].GetProperty("permalink").ToString() + "] : " + json.GetProperty("coubs")[i].GetProperty("title").ToString());
                                } else
                                {
                                    noload++;
                                }
                            } else
                                noload++;

                            /*if (!File.Exists(save_dir + @"/video/" + permalink + @"/" + Path.GetFileName(videourl)))
                            {
                                LoadList.Add(Tuple.Create(videourl, @"/video", @"/" + permalink, Path.GetFileName(videourl)));
                                count++;
                                textlog("Найден новый коуб [" + json.GetProperty("coubs")[i].GetProperty("permalink").ToString() + "] : " + json.GetProperty("coubs")[i].GetProperty("title").ToString());
                            }
                            else
                                noload++;*/

                            if (!String.IsNullOrEmpty(audiourl))
                            {
                                if (!File.Exists(save_dir + @"/video/" + permalink + @"/" + Path.GetFileName(audiourl)))
                                {
                                    if (radioButton1.Checked == true)
                                    {
                                        if (save_dir == "")
                                        {
                                            LoadList.Add(Tuple.Create(audiourl, @"/video", @"/" + permalink, Path.GetFileName(audiourl)));
                                        }
                                        else if (!File.Exists(second_dir + @"/" + permalink + @"/" + Path.GetFileName(audiourl)))
                                        {
                                            LoadList.Add(Tuple.Create(audiourl, @"/video", @"/" + permalink, Path.GetFileName(audiourl)));
                                        }
                                    }
                                    else if (!File.Exists(second_dir + @"/" + permalink + @"/" + Path.GetFileName(audiourl)))
                                    {
                                        LoadList.Add(Tuple.Create(audiourl, @"/video", @"/" + permalink, Path.GetFileName(audiourl)));
                                        
                                    }
                                }
                                /*
                                if (!File.Exists(save_dir + @"/video/" + permalink + @"/" + Path.GetFileName(audiourl)))
                                    LoadList.Add(Tuple.Create(audiourl, @"/video", @"/" + permalink, Path.GetFileName(audiourl)));*/
                            }
                        }
                    }
                    if (LoadList.Count != 0)
                    {
                        start_download(LoadList);
                    } else
                    {
                        if (((categories)comboBox2.SelectedItem).dir == "a")
                        {
                            System.IO.File.Move(item, save_dir + @"\clear\" + Path.GetFileName(item));
                        }
                        //save_dir + @"\" + ((categories)comboBox1.SelectedItem).dir + @"\" + ((categories)comboBox2.SelectedItem).dir + @"\" + ((categories)comboBox3.SelectedItem).dir 
                        // путь к папке
                    }

                    //break;
                    counter_files++;
                    progresinfo2((int)(((float)counter_files * 100f) / (float)count_files));
                }    
            }

            textlog("Загрузка коубов из списка завершена!");
            textlog("Всего было найдено: " + count + " новых коубов");
            if (noload != 0) textlog(noload + " коубов пропущены, так как были скачены ранее");
            if (noaudio != 0) textlog(noaudio + " коубов с заблокированным аудио");

            CategoryList();
            cancel = false;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(Properties.Settings.Default.remember_token);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            string url = textBox3.Text;
            char[] charsToTrim = { '/' };
            url = url.TrimEnd(charsToTrim);
            Uri uriResult;
            if (Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {    
                Uri checkurl = new Uri(url);
                if (checkurl.Host == "coub.com")
                {
                    button10.Text = "Скачать данные";
                    textlog("Анализ URL...");
                    api_type = 0;
                    url_api = "";
                    api_name = "";
                    api_dir = "";
                    api_pertmalink = "";

                    //var result = string.Empty;
                    using (var webClient = new System.Net.WebClient())
                    {
                        try
                        {
                            webClient.Headers.Add(HttpRequestHeader.Cookie, "remember_token=" + Properties.Settings.Default.remember_token);
                            string result = webClient.DownloadString(url);

                            string pattern_api = @"<div[^>]+url\s*=\s*['""]\s*([^'""]+)['""][^>]*>";
                            string pattern_sort = @"<div[^>]+data-sort\s*=\s*['""]\s*([^'""]+)['""][^>]*>";
                            string pattern_user_sort = @"<li[^>]+data-val\s*=\s*['""]\s*([^'""]+)['""][^>]*>";
                            string pattern_type = @"<div[^>]+data-type\s*=\s*['""]\s*([^'""]+)['""][^>]*>";

                            //var rx = new Regex(pattern_api);
                            /*foreach (Match m in Regex.Matches(result, pattern_api))
                            {
                                string name = m.Groups[1].ToString();
                                textlog(m.Groups[1].ToString());
                            }*/

                            url_api = Regex.Match(result, pattern_api).Groups[1].ToString();

                            if (url_api.Split('/').Last() != "category_suggestions")
                            {
                                label20.Visible = true;

                                comboBox4.DataSource = null;
                                comboBox4.Items.Clear();

                                List<categories> category = new List<categories> { };

                                if (Regex.Matches(result, pattern_sort).Count != 0)
                                {
                                    foreach (Match m in Regex.Matches(result, pattern_sort))
                                    {
                                        string name = m.Groups[1].ToString();
                                        //textlog(m.Groups[1].ToString());
                                        switch (m.Groups[1].ToString())
                                        {
                                            case "newest_popular":
                                                name = "Популярное";
                                                break;
                                            case "likes_count":
                                                name = "Топ";
                                                break;
                                            case "views_count":
                                                name = "По просмотрам";
                                                break;
                                            case "newest":
                                                name = "Новый";
                                                break;
                                            case "date":
                                                name = "Недавнее";
                                                break;
                                            case "oldest":
                                                name = "Старые";
                                                break;
                                            case "random":
                                                name = "Вразнобой";
                                                break;
                                        }

                                        category.Add(new categories { dir = m.Groups[1].ToString(), name = name });
                                    }
                                    lableout(label20, "Тип: Поиск/тег");
                                    api_type = 1;
                                }

                                if (Regex.Matches(result, pattern_sort).Count == 0)     //Пользователь или раздел
                                {
                                    if (Regex.Matches(result, pattern_user_sort).Count != 0)
                                    {
                                        foreach (Match m in Regex.Matches(result, pattern_user_sort))
                                        {
                                            string name = m.Groups[1].ToString();
                                            //textlog(m.Groups[1].ToString());
                                            switch (m.Groups[1].ToString())
                                            {
                                                case "newest_popular":
                                                    name = "Популярное";
                                                    break;
                                                case "likes_count":
                                                    name = "Топ";
                                                    break;
                                                case "views_count":
                                                    name = "По просмотрам";
                                                    break;
                                                case "newest":
                                                    name = "Свежее";
                                                    break;
                                                case "date":
                                                    name = "Недавнее";
                                                    break;
                                                case "oldest":
                                                    name = "Старые";
                                                    break;
                                                case "random":
                                                    name = "Вразнобой";
                                                    break;
                                            }
                                            category.Add(new categories { dir = m.Groups[1].ToString(), name = name });
                                        }
                                        //textlog("Тип ссылки: канал!");
                                    }

                                    switch (checkurl.PathAndQuery.Split('/')[1])
                                    {
                                        case "community":
                                            lableout(label20, "Тип: Сообщества");
                                            api_type = 2;
                                            break;
                                        case "feed":
                                            lableout(label20, "Тип: Лента");
                                            api_type = 2;
                                            break;
                                        case "hot":
                                            lableout(label20, "Тип: Горячее");
                                            api_type = 2;
                                            break;
                                        case "rising":
                                            lableout(label20, "В тренде");
                                            api_type = 2;
                                            break;
                                        default:
                                            lableout(label20, "В разработке!");
                                            api_type = 0;
                                            break;
                                    }

                                }
                                else
                                {
                                    //textlog("Тип ссылки: Неподписано 2!");
                                }

                                comboBox4.DataSource = category;
                                comboBox4.DisplayMember = "name";
                                comboBox4.ValueMember = "dir";
                                comboBox4.Visible = true;
                                label11.Visible = true;

                                comboBox5.DataSource = null;
                                comboBox5.Items.Clear();

                                List<categories> category2 = new List<categories> { };

                                if (Regex.Matches(result, pattern_type).Count != 0)
                                {
                                    foreach (Match m in Regex.Matches(result, pattern_type))
                                    {
                                        string name = m.Groups[1].ToString();
                                        //textlog(m.Groups[1].ToString());
                                        switch (m.Groups[1].ToString())
                                        {
                                            case "recoubs":
                                                name = "Репосты";
                                                break;
                                            /*case "stories":
                                                name = "Истории";
                                                break;*/
                                            case "simples":
                                                name = "Коубы";
                                                break;
                                            case "likes":
                                                name = "Лайки";
                                                break;
                                        }

                                        category2.Add(new categories { dir = m.Groups[1].ToString(), name = name });
                                    }

                                    category2.Add(new categories { dir = "", name = "Все" });

                                    lableout(label20, "Тип: Канал");
                                    api_type = 3;
                                    //textlog("Тип ссылки: Пользователь!");
                                }

                                comboBox5.DataSource = category2;
                                comboBox5.DisplayMember = "name";
                                comboBox5.ValueMember = "dir";

                                button10.Enabled = true;
                                if (api_type != 2 && api_type != 1)
                                {
                                    comboBox5.Visible = true;
                                    label18.Visible = true;
                                }

                            }
                            else
                            {
                                lableout(label20, "Тип: коуб");
                                api_type = 4;
                                button10.Text = "Скачать данные и коуб";
                            }
                        }
                        catch (Exception)
                        {
                            
                            throw;
                        }
                        

                    }

                    if (api_type != 0)
                    {
                        switch (api_type)
                        {
                            case 1:
                                comboBox4.Visible = false;
                                if (checkurl.PathAndQuery.Split('/').Length != 4)
                                {
                                    //https://coub.com/tags/vivy/
                                    url_api += "?order_by=newest_popular&type=&scope=all&page=";
                                    label25.Visible = true;
                                    label25.Text = "Популярное";
                                } else if (checkurl.PathAndQuery.Split('/')[3] != "")
                                {
                                    switch (checkurl.PathAndQuery.Split('/')[3])
                                    {
                                        case "likes":
                                            url_api += "?order_by=likes_count&type=&scope=all&page=";
                                            label25.Visible = true;
                                            label25.Text = "Топ";
                                            api_name = "likes";
                                            break;
                                        case "views":
                                            url_api += "?order_by=views_count&type=&scope=all&page=";
                                            label25.Visible = true;
                                            label25.Text = "По просмотрам";
                                            api_name = "views";
                                            break;
                                        case "fresh":
                                            url_api += "?order_by=newest&type=&scope=all&page=";
                                            label25.Visible = true;
                                            label25.Text = "Свежее";
                                            api_name = "fresh";
                                            break;
                                    }
                                }
                                api_dir = "tags";
                                api_pertmalink = checkurl.PathAndQuery.Split('/')[2]+" "+ api_name;
                                api_pertmalink = api_pertmalink.Trim();
                                api_name = "";
                                button10.Enabled = true;
                                break;
                            case 2:
                                textlog("Данный формат ссылок на данный момент не поддерживается!");
                                button10.Enabled = false;
                                break;
                            case 3:
                                button10.Enabled = true;
                                break;
                            case 4:
                                url_api = "/api/v2/coubs/"+ checkurl.PathAndQuery.Split('/')[2];
                                textlog("API: " + url_api);
                                button10.Enabled = true;
                                api_dir = "view";
                                api_pertmalink = checkurl.PathAndQuery.Split('/')[2];
                                break;
                        }



                        //textlog("API: " + url_api);

                        //if (api_type != 2) textlog("API: " + url_api);
                        textlog("Анализ URL завершён!");
                    }
                }
                else
                {
                    textlog("Неверный URL!");
                }
            } else
            {
                textlog("Это не URL!");
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            button10.Enabled = false;
            comboBox5.Visible = false;
            comboBox4.Visible = false;
            label25.Visible = false;
            label18.Visible = false;
            label11.Visible = false;
        }

        private async void button10_Click(object sender, EventArgs e)
        {
            string save_dir = Properties.Settings.Default.save_dir;

            if (!Directory.Exists(save_dir + @"/me")) Directory.CreateDirectory(save_dir + @"/me");
            if (!Directory.Exists(save_dir + @"/avatars")) Directory.CreateDirectory(save_dir + @"/avatars");
            if (!Directory.Exists(save_dir + @"/background_image")) Directory.CreateDirectory(save_dir + @"/background_image");
            if (!Directory.Exists(save_dir + @"/channel")) Directory.CreateDirectory(save_dir + @"/channel");
            if (!Directory.Exists(save_dir + @"/likes")) Directory.CreateDirectory(save_dir + @"/likes");
            if (!Directory.Exists(save_dir + @"/recoubs")) Directory.CreateDirectory(save_dir + @"/recoubs");
            if (!Directory.Exists(save_dir + @"/simples")) Directory.CreateDirectory(save_dir + @"/simples");
            if (!Directory.Exists(save_dir + @"/video")) Directory.CreateDirectory(save_dir + @"/video");
            if (!Directory.Exists(save_dir + @"/tags")) Directory.CreateDirectory(save_dir + @"/tags");
            if (!Directory.Exists(save_dir + @"/all")) Directory.CreateDirectory(save_dir + @"/all");
            if (!Directory.Exists(save_dir + @"/view_info")) Directory.CreateDirectory(save_dir + @"/view_info");
            if (!Directory.Exists(save_dir + @"/bookmarks")) Directory.CreateDirectory(save_dir + @"/bookmarks");

            if (api_type != 0)
            {
                switch (api_type)
                {
                    case 3:
                        ///               /api/v2/timeline/channel/lermontova.vk3024?order_by=newest

                        //https://coub.com/api/v2/timeline/channel/lermontova.vk3024?order_by=newest&permalink=lermontova.vk3024&type=&page=1
                        url_api = url_api.Split('?')[0];
                        url_api += "?order_by=" + ((categories)comboBox4.SelectedItem).dir + "&permalink=" + url_api.Split('/')[5] + "&type=" + ((categories)comboBox5.SelectedItem).dir + "&page=";
                        //https://coub.com/api/v2/timeline/channel/lermontova.vk3024?order_by=newest&permalink=lermontova.vk3024&type=simples&scope=all&page=1
                        textlog("API: " + url_api);

                        if (((categories)comboBox5.SelectedItem).dir != "")
                        {
                            api_dir = ((categories)comboBox5.SelectedItem).dir;
                        } else
                        {
                            api_dir = "all";
                        }
                        
                        api_pertmalink = url_api.Split('/')[5].Split('?')[0];

                        Cookie remember_token = new Cookie("remember_token", textBox1.Text, "/", "coub.com");

                        string channel_json = await Request("https://coub.com/api/v2/channels/" + api_pertmalink, remember_token);
                        System.IO.File.WriteAllText(Properties.Settings.Default.save_dir + @"/channel/" + replace(api_pertmalink) + ".json", channel_json);
                        JsonElement ch_root = JsonDocument.Parse(channel_json).RootElement;

                        JsonElement root2;
                        LoadList.Clear();

                        if (ch_root.TryGetProperty("avatar_versions", out root2))
                        {
                            if (ch_root.GetProperty("avatar_versions").TryGetProperty("template", out root2))
                            {
                                string avatar = ch_root.GetProperty("avatar_versions").GetProperty("template").ToString();
                                avatar = avatar.Replace("%{version}", "profile_pic_big_2x");
                                if (!File.Exists(@"/avatars/" + Path.GetFileName(avatar)))
                                    LoadList.Add(Tuple.Create(avatar, @"/avatars/", "", Path.GetFileName(avatar)));
                            }
                        }

                        if (ch_root.TryGetProperty("background_image", out root2))
                        {
                            string background_image = ch_root.GetProperty("background_image").ToString();
                            if (background_image != "")
                                if (!File.Exists(@"/background_image/" + Path.GetFileName(background_image)))
                                    LoadList.Add(Tuple.Create(background_image, @"/background_image/", "", Path.GetFileName(background_image)));

                        }

                        start_download(LoadList);
                        
                        textlog("Информация о канале " + ch_root.GetProperty("title").ToString() + " сохранёна: " + @"/channel/" + replace(api_pertmalink) + ".json");
                        break;
                }

                if (api_type != 4)
                {
                    textlog("Начинаем скачивание списка");

                    url_api = "https://coub.com" + url_api;
                    //textlog(url_api);
                    DateTime localDate = DateTime.Now;
                    

                    Cookie remember_token = new Cookie("remember_token", textBox1.Text, "/", "coub.com");
                    url_api = url_api.Replace("&#39;", "'");
                    string first_like = await Request(url_api + "1", remember_token);
                    JsonElement fl_root = JsonDocument.Parse(first_like).RootElement;

                    int listCount;
                    Int32.TryParse(fl_root.GetProperty("total_pages").ToString(), out listCount);

                    textlog("Количество страниц с данными: " + listCount + " шт. Примерное количество коубов: менее " + (listCount * 10) + " шт.");

                    if (!Directory.Exists(save_dir + @"/"+api_dir+"/" + replace(api_pertmalink))) Directory.CreateDirectory(save_dir + @"/"+ api_dir + "/" + replace(api_pertmalink));
                    LoadList.Clear();

                    if (listCount > numericUpDown2.Value)
                        listCount = (int)numericUpDown2.Value;

                    for (int i = 1; i <= listCount; i++)
                        LoadList.Add(Tuple.Create(url_api + i, @"/" + api_dir + "/" + replace(api_pertmalink), @"/" + localDate.ToString("dd-MM-yyyy_HH-mm-ss"), Path.GetFileName(i + ".json")));

                    //start_download_cookie(LoadList);
                    start_download(LoadList);

                    textlog("Загрузка списка завершена!");
                    CategoryList();

                    /*LoadList.Clear();

                    int count = 0;
                    int noload = 0;
                    int noaudio = 0;

                    int count_files = files.Count();
                    int counter_files = 0;

                    foreach (var item in files)
                    {
                        textlog(item);
                        using (StreamReader r = new StreamReader(item))
                        {
                            LoadList.Clear();
                            JsonElement json = JsonDocument.Parse(r.ReadToEnd()).RootElement;
                            for (int i = 0; i < json.GetProperty("coubs").GetArrayLength(); i++)
                            {
                                JsonElement file_versions = json.GetProperty("coubs")[i].GetProperty("file_versions");

                                string permalink = json.GetProperty("coubs")[i].GetProperty("permalink").ToString();

                                string videourl;
                                string audiourl = null;

                                JsonElement root2;
                                if (file_versions.GetProperty("html5").GetProperty("video").TryGetProperty("higher", out root2))
                                {
                                    videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("higher").GetProperty("url").ToString();
                                }
                                else if (file_versions.GetProperty("html5").GetProperty("video").TryGetProperty("high", out root2))
                                {
                                    videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("high").GetProperty("url").ToString();
                                }
                                else
                                {
                                    videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("med").GetProperty("url").ToString();
                                }

                                //textlog(videourl);

                                if (file_versions.GetProperty("html5").TryGetProperty("audio", out root2))
                                {
                                    if (file_versions.GetProperty("html5").GetProperty("audio").TryGetProperty("high", out root2))
                                    {
                                        audiourl = file_versions.GetProperty("html5").GetProperty("audio").GetProperty("high").GetProperty("url").ToString();
                                    }
                                    else
                                    {
                                        audiourl = file_versions.GetProperty("html5").GetProperty("audio").GetProperty("med").GetProperty("url").ToString();
                                    }
                                }
                                else
                                {
                                    noaudio++;
                                }



                                if (!File.Exists(save_dir + @"/video/" + permalink + @"/" + Path.GetFileName(videourl)))
                                {
                                    LoadList.Add(Tuple.Create(videourl, @"/video", @"/" + permalink, Path.GetFileName(videourl)));
                                    count++;
                                    textlog("Найден новый коуб [" + json.GetProperty("coubs")[i].GetProperty("permalink").ToString() + "] : " + json.GetProperty("coubs")[i].GetProperty("title").ToString());
                                }
                                else
                                    noload++;

                                if (!String.IsNullOrEmpty(audiourl))
                                {
                                    if (!File.Exists(save_dir + @"/video/" + permalink + @"/" + Path.GetFileName(audiourl)))
                                        LoadList.Add(Tuple.Create(audiourl, @"/video", @"/" + permalink, Path.GetFileName(audiourl)));
                                }
                            }
                        }
                        if (LoadList.Count != 0)
                            start_download(LoadList);
                        //break;
                        counter_files++;
                        progresinfo2((int)(((float)counter_files * 100f) / (float)count_files));
                    }



                    textlog("Загрузка коубов из списка завершена!");
                    textlog("Всего было найдено: " + count + " новых коубов");
                    if (noload != 0) textlog(noload + " коубов пропущены, так как были скачены ранее");
                    if (noaudio != 0) textlog(noaudio + " коубов с заблокированным аудио");

                    CategoryList();*/

                } else
                {
                    LoadList.Clear();
                    url_api = "https://coub.com" + url_api;
                    Cookie remember_token = new Cookie("remember_token", textBox1.Text, "/", "coub.com");
                    string first_view = await Request(url_api, remember_token);
                    
                    System.IO.File.WriteAllText(Properties.Settings.Default.save_dir + @"/view_info/" + replace(api_pertmalink) + ".json", first_view);
                    textlog("данные коуба " + api_pertmalink + " сохранены!");

                    JsonElement fl_root = JsonDocument.Parse(first_view).RootElement;
                    JsonElement file_versions = fl_root.GetProperty("file_versions");
                    string videourl;
                    string audiourl = null;

                    JsonElement root2;
                    if (file_versions.GetProperty("html5").GetProperty("video").TryGetProperty("higher", out root2))
                    {
                        videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("higher").GetProperty("url").ToString();
                    }
                    else if (file_versions.GetProperty("html5").GetProperty("video").TryGetProperty("high", out root2))
                    {
                        videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("high").GetProperty("url").ToString();
                    }
                    else
                    {
                        videourl = file_versions.GetProperty("html5").GetProperty("video").GetProperty("med").GetProperty("url").ToString();
                    }

                    //textlog(videourl);

                    if (file_versions.GetProperty("html5").TryGetProperty("audio", out root2))
                    {
                        if (file_versions.GetProperty("html5").GetProperty("audio").TryGetProperty("high", out root2))
                        {
                            audiourl = file_versions.GetProperty("html5").GetProperty("audio").GetProperty("high").GetProperty("url").ToString();
                        }
                        else
                        {
                            audiourl = file_versions.GetProperty("html5").GetProperty("audio").GetProperty("med").GetProperty("url").ToString();
                        }
                    }
                    else
                    {
                        textlog("Коуб с заблокированным аудио");
                    }



                    if (!File.Exists(save_dir + @"/video/" + replace(api_pertmalink) + @"/" + Path.GetFileName(videourl)))
                    {
                        LoadList.Add(Tuple.Create(videourl, @"/video", @"/" + replace(api_pertmalink), Path.GetFileName(videourl)));
                        textlog("Найден новый коуб [" + api_pertmalink + "] : " + fl_root.GetProperty("title").ToString());
                    }
                    else
                        textlog("Коуб был пропущен, так как был скачен ранее");

                    if (!String.IsNullOrEmpty(audiourl))
                    {
                        if (!File.Exists(save_dir + @"/video/" + replace(api_pertmalink) + @"/" + Path.GetFileName(audiourl)))
                            LoadList.Add(Tuple.Create(audiourl, @"/video", @"/" + replace(api_pertmalink), Path.GetFileName(audiourl)));
                    }

                    if (LoadList.Count != 0)
                        start_download(LoadList);
                    LoadList.Clear();
                }

                textlog("Загружено в папку: " + api_dir);
                textlog("Загружено под именем: " + replace(api_pertmalink));

                url_api = "";
                api_type = 0;
                api_name = "";
                api_dir = "";
                api_pertmalink = "";
                button10.Enabled = false;
                comboBox5.Visible = false;
                comboBox4.Visible = false;
                label18.Visible = false;
                label11.Visible = false;
                label25.Visible = false;
                label20.Visible = false;
                button10.Text = "Скачать данные";
            }
            cancel = false;
        }

        public static long DirSize(DirectoryInfo d, long aLimit = 0)
        {
            long Size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                Size += fi.Length;
                if (aLimit > 0 && Size > aLimit)
                    return Size;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                Size += DirSize(di, aLimit);
                if (aLimit > 0 && Size > aLimit)
                    return Size;
            }
            return (Size);
        }
        private void button11_Click(object sender, EventArgs e)
        {
            label8.Text = (Math.Round((float)((float)DirSize(new System.IO.DirectoryInfo(Properties.Settings.Default.save_dir)) / (float)1073741824), 2)).ToString()+ " ГБ";
            DirectoryInfo di = new DirectoryInfo(Properties.Settings.Default.save_dir+@"\video");
            label9.Text = di.GetDirectories("*", SearchOption.AllDirectories).Length.ToString()+" коубов";

            label8.Visible = true;
            label9.Visible = true;

        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.second_dir != "")
            {
                button12.Text = "Подключить";
                label26.Text = "Не выбран";
                Properties.Settings.Default.second_dir = "";
                radioButton2.Visible = false;
                radioButton1.Checked = true;
            } else
            {
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    Properties.Settings.Default.second_dir = folderBrowserDialog1.SelectedPath;
                    button12.Text = "Отключить";
                    label26.Text = Properties.Settings.Default.second_dir;

                    radioButton2.Visible = true;
                    /*label3.Text = Properties.Settings.Default.save_dir;

                    string save_dir = Properties.Settings.Default.save_dir;

                    if (!Directory.Exists(save_dir + @"/me")) Directory.CreateDirectory(save_dir + @"/me");
                    if (!Directory.Exists(save_dir + @"/avatars")) Directory.CreateDirectory(save_dir + @"/avatars");
                    if (!Directory.Exists(save_dir + @"/background_image")) Directory.CreateDirectory(save_dir + @"/background_image");
                    if (!Directory.Exists(save_dir + @"/channel")) Directory.CreateDirectory(save_dir + @"/channel");
                    if (!Directory.Exists(save_dir + @"/likes")) Directory.CreateDirectory(save_dir + @"/likes");
                    if (!Directory.Exists(save_dir + @"/recoubs")) Directory.CreateDirectory(save_dir + @"/recoubs");
                    if (!Directory.Exists(save_dir + @"/simples")) Directory.CreateDirectory(save_dir + @"/simples");
                    if (!Directory.Exists(save_dir + @"/video")) Directory.CreateDirectory(save_dir + @"/video");
                    if (!Directory.Exists(save_dir + @"/tags")) Directory.CreateDirectory(save_dir + @"/tags");
                    if (!Directory.Exists(save_dir + @"/all")) Directory.CreateDirectory(save_dir + @"/all");
                    if (!Directory.Exists(save_dir + @"/view_info")) Directory.CreateDirectory(save_dir + @"/view_info");

                    button1.Enabled = true;
                    CategoryList();*/
                }
                
            }
        }

        private async void button13_Click(object sender, EventArgs e)
        {
            textlog("Начинаем скачивание списка закладок");

            string url = "https://coub.com/api/v2/timeline/favourites?order_by=date&page=";
            DateTime localDate = DateTime.Now;
            string save_dir = Properties.Settings.Default.save_dir;

            Cookie remember_token = new Cookie("remember_token", textBox1.Text, "/", "coub.com");
            string first_like = await Request(url + "1", remember_token);
            JsonElement fl_root = JsonDocument.Parse(first_like).RootElement;

            int listCount;
            Int32.TryParse(fl_root.GetProperty("total_pages").ToString(), out listCount);

            textlog("Количество страниц с данными: " + listCount + " шт. Примерное количество коубов: менее " + (listCount * 10) + " шт.");

            if (!Directory.Exists(save_dir + @"/bookmarks/" + Properties.Settings.Default.permalink)) Directory.CreateDirectory(save_dir + @"/bookmarks/" + Properties.Settings.Default.permalink);
            LoadList.Clear();

            if (listCount > numericUpDown1.Value)
                listCount = (int)numericUpDown1.Value;

            for (int i = 1; i <= listCount; i++)
                LoadList.Add(Tuple.Create(url + i, @"/bookmarks/" + Properties.Settings.Default.permalink, @"/" + localDate.ToString("dd-MM-yyyy_HH-mm-ss"), Path.GetFileName(i + ".json")));

            start_download_cookie(LoadList);

            textlog("Загрузка списка закладок завершена!");
            CategoryList();
            cancel = false;
        }

        public string replace (string text)
        {        
            return text.Replace(":", "_").Replace("?", ".").Replace("'", "&#39;");
        }

        private void button14_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ispublic = checkBox3.Checked;
            Properties.Settings.Default.web_server = checkBox2.Checked;
            int port = 0;
            if (int.TryParse(textBox4.Text, out port))
            {
                Properties.Settings.Default.port = port;
            }
            
            textlog("Настройки сохранены.");
            textlog("Перезапустите приложение для их применения.");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linkLabel1.Text);
        }
    }
}
