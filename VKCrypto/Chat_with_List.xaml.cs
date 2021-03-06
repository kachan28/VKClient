﻿using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO.Compression;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using Path = System.IO.Path;
using Image = System.Windows.Controls.Image;
using LTH = System.Windows.LogicalTreeHelper;
using VTH = System.Windows.Media.VisualTreeHelper;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace VKCrypto
{
    /// <summary>
    /// Логика взаимодействия для Chat_with_List.xaml
    /// </summary>
    public partial class Chat_with_List : System.Windows.Controls.Page
    {

        long ActiveUserId; string ActivePubKey; string ActiveSimKey;
        public Chat_with_List()
        {
            InitializeComponent();
            Task GetMesTask = new Task(new Action(GetMessages));
            GetMesTask.Start();
            MainWindow.chatloaded = true;
        }

        public void GetMessages()
            {
                var longPoll = MainWindow.api.Messages.GetLongPollServer(needPts: true);
                try
                {
                    LongPollHistoryResponse serv = MainWindow.api.Messages.GetLongPollHistory(@params: new MessagesGetLongPollHistoryParams() { Pts = longPoll.Pts });
                    int mescount = 0;
                    while (true)
                    {
                        try
                        {
                            serv = MainWindow.api.Messages.GetLongPollHistory(@params: new MessagesGetLongPollHistoryParams() { Pts = longPoll.Pts });
                            //MessageBox.Show(serv.Messages[serv.Messages.Count-1].Text);
                            if (serv.Messages.Count != mescount)
                            {
                                Message LastMessage = serv.Messages[serv.Messages.Count - 1];
                                if (LastMessage.Type == VkNet.Enums.MessageType.Received && ActiveUserId == LastMessage.FromId)
                                {
                                    
                                    Dispatcher.BeginInvoke(new ThreadStart(delegate { Chat.Document.Blocks.Add(new Paragraph(new Run(LastMessage.FromId.ToString()+LastMessage.Text))); }));
                                    /*long? message_id = serv.Messages[serv.Messages.Count - 1].Id;
                                    var idlist = new List<ulong>();
                                    idlist.Add((ulong)message_id);
                                    var Message = MainWindow.api.Messages.GetById(messageIds: idlist, fields: Enumerable.Empty<string>());
                                    Attachment documentAttachment = Message[0].Attachments.First(x => x.Type == typeof(Photo));
                                    string uri = ((Photo)documentAttachment.Instance).Sizes[0].Url.ToString();
                                    MessageBox.Show(uri);*/
                                    mescount = serv.Messages.Count;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.ToString());
                            longPoll = MainWindow.api.Messages.GetLongPollServer(needPts: true);
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }

        public void OnLoad(object sender, RoutedEventArgs e)
            {
                var CreateFriendListThread = new Thread(() => CreateFriendsList());
                CreateFriendListThread.SetApartmentState(ApartmentState.STA);
                CreateFriendListThread.IsBackground = true;
                CreateFriendListThread.Start();
            }

        public void CreateFriendsList()
        {
            var friend_list = MainWindow.api.Friends.Get(new FriendsGetParams
            {
                Order = FriendsOrder.Hints,
                Fields = ProfileFields.FirstName | ProfileFields.Photo50,
                Count = 6000,
                NameCase = NameCase.Nom,
            });
            /*_ = Dispatcher.BeginInvoke(DispatcherPriority.Render, new ThreadStart(delegate
                {
                    FullFriendList.Items.Add(MainWindow.api.Account.GetProfileInfo().FirstName + ' ' + MainWindow.api.Account.GetProfileInfo().LastName + " ID:" + MainWindow.api.UserId);
                }));*/
            int friendnum = 0;
            foreach (var friend in friend_list)
            {
                using (WebClient webClient = new WebClient())
                {
                    try
                    {
                        webClient.DownloadFile(friend.Photo50.AbsoluteUri, (Path.Combine(Environment.CurrentDirectory, "Friendava" + friendnum.ToString() + ".jpg")));
                    }
                    catch { }
                }
                Dispatcher.BeginInvoke(new ThreadStart(delegate {
                    ListBoxItem itemfriend = new ListBoxItem(); itemfriend.Name = "frienditem" + friendnum.ToString();
                    StackPanel itemstack = new StackPanel(); itemstack.Name = "friendstack" + friendnum.ToString();
                    Grid itemgrid = new Grid(); for (int i = 0; i < 8; i++) { itemgrid.ColumnDefinitions.Add(new ColumnDefinition()); }
                    for (int i = 0; i < 3; i++) { itemgrid.RowDefinitions.Add(new RowDefinition()); }
                    itemgrid.Name = "friendgrid"+friendnum.ToString();
                    itemstack.Children.Add(itemgrid);
                    TextBlock friendinfo = new TextBlock(); friendinfo.Text = "  " + friend.FirstName + " " + friend.LastName;
                    friendinfo.FontSize = 15;
                    Grid.SetColumn(friendinfo, 5); Grid.SetColumnSpan(friendinfo, 4);
                    Grid.SetRow(friendinfo, 1); Grid.SetRowSpan(friendinfo, 2); 
                    itemgrid.Children.Add(friendinfo);
                    itemfriend.Content = itemstack;
                    itemfriend.Tag = friend.Id;
                    FullFriendList.Items.Add(itemfriend); FullFriendList.UpdateLayout();
                }));
                friendnum++;
            }

            _ = Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                int num = 0;
                System.Collections.IEnumerable  nodes = LTH.GetChildren(FullFriendList);
                foreach (var node in nodes)
                {
                    //MessageBox.Show(node.GetType().ToString());
                    if (node is string)
                    {
                        continue;
                    }
                    Visual listitem = (Visual)node;
                    System.Collections.IEnumerable items = LTH.GetChildren(listitem);
                    foreach (var item in items)
                    {
                        if (item is StackPanel)
                        {
                            Grid frgr = VTH.GetChild(item as DependencyObject, 0) as Grid;
                            ImageBrush friendava = new ImageBrush();
                            BitmapImage friendbit = new BitmapImage(new Uri(Path.Combine(Environment.CurrentDirectory, "Friendava" + num.ToString() + ".jpg")));
                            friendava.ImageSource = friendbit;
                            Ellipse ava = new Ellipse();
                            ava.Height = 50;
                            ava.Width = 50;
                            ava.Fill = friendava;
                            Grid.SetColumn(ava, 0); Grid.SetColumnSpan(ava, 2);
                            Grid.SetRow(ava, 0); Grid.SetRowSpan(ava, 3);
                            frgr.Children.Add(ava);
                        }
                    }
                    num++;
                }
            }));
        }
        /*private void IDSearch_Is_Focused(object sender, System.Windows.RoutedEventArgs e)
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(224, 255, 255));
            IDSearch.Text = "";
            IDSearch.Foreground = brush;
        }

        private void IDSearch_Lost_Focus(object sender, System.Windows.RoutedEventArgs e)
        {
            IDSearch.Text = "Find person with ID";
            IDSearch.Foreground = Brushes.Silver;
        }

        private void ID_Entered(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                idsob = Convert.ToInt32(IDSearch.Text);
                var StartDialogThread = new Thread(() => StartDialog(idsob, MainWindow.api, Utils.AsimDecryptor.GetPrivKey(), Utils.SimCrypto.GetSimKeyforMes()));
                StartDialogThread.Start();
            }
        }*/

        private void ElementSelected(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem listitem = (ListBoxItem)FullFriendList.SelectedItem;
            object item = listitem.Content;
            Grid frgr = VTH.GetChild(item as DependencyObject, 0) as Grid;
            TextBlock text = VTH.GetChild(frgr as DependencyObject, 0) as TextBlock;
            object id = listitem.Tag;
            ActiveUserId = Convert.ToInt64(id.ToString());
            using (SQLiteConnection CrFile_Connection = new SQLiteConnection("Data Source=Current_Dialogs.sqlite;"))
            {
                CrFile_Connection.Open();
                string sql = string.Format("select * from active_users where id=={0}", Convert.ToInt32(id.ToString()));
                SQLiteCommand command = new SQLiteCommand(sql, CrFile_Connection);
                SQLiteDataReader res = command.ExecuteReader();
                if (res.HasRows == true)
                {
                    if (res.Read())
                    {
                        int subid = (int)res["id"]; string pubkey = (string)res["pubkey"]; string simkey = (string)res["simkey"];
                        ActivePubKey = pubkey; ActiveSimKey = simkey;
                    }
                }
                //string result = command.ExecuteScalar().ToString();
                CrFile_Connection.Close();
            }
            //var StartDialogThread = new Thread(() => StartDialog(idsob, MainWindow.api, Utils.AsimDecryptor.GetPrivKey(), Utils.SimCrypto.GetSimKeyforMes()));
            //StartDialogThread.Start();
        }

        private void StartDialog(int idsob, VkApi api, string privkey, string SimKey)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Clear();
            }));

            var GetMesThread = new Thread(Get_Mes);
            bool me_or_him = true;

            Random random = new Random();

            if (!Check_Key_Without_GUI(MainWindow.api, idsob))
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = ActiveUserId,
                        RandomId = random.Next(99999),
                        Message = "Using VKMessenger by MK"
                    });
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Chat.Document.Blocks.Add(new Paragraph(new Run("Using VKMessenger by MK")));
                    }));
                }
                catch
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams
                        {
                            UserId = idsob,
                            RandomId = random.Next(99999),
                            Message = "Using VKMessenger by MK"
                        });
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            Chat.Document.Blocks.Add(new Paragraph(new Run("Using VKMessenger by MK")));
                        }));
                    }
                    catch (Exception e)
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            Chat.Document.Blocks.Add(new Paragraph(new Run(e.ToString())));
                        }));
                    }
                }
            }

            var newpubkey = ChangeKeys(api, idsob, ref me_or_him);
            Utils.AsimEncryptor.SetPubKey(newpubkey);

            if (me_or_him)
            {
                Send_Sim_Key(api, idsob, SimKey);
            }
            else
            {
                SimKey = Get_Sim_Key(api, idsob);
                Utils.SimCrypto.SetSimKeyforMes(SimKey);
            }

            string npub = Utils.AsimEncryptor.GetPubKey();

            object mesargums = new object[] { api, idsob };

            //GetMesThread.SetApartmentState(ApartmentState.STA);
            GetMesThread.IsBackground = true;
            GetMesThread.Start(mesargums);
        }

        private void Send_Sim_Key(VkApi api, int idsob, string SimKey)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Идет отправка симметричного ключа...")));
            }));
            var random = new Random();
            var randid = random.Next(99999);
            var CryptedSimKey = Utils.AsimEncryptor.RSAEncryption(SimKey);
            api.Messages.Send(new MessagesSendParams
            {
                UserId = idsob,
                RandomId = randid,
                Message = CryptedSimKey
            });
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ успешно отправлен!!!")));
            }));
        }
        private string Get_Sim_Key(VkApi api, int idsob)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Получаем ключ симметричного шифрования...")));
            }));
            string newkey;
            while (true)
            {
#pragma warning disable CS0618 // Тип или член устарел
                MessagesGetObject getDialogs = api.Messages.GetDialogs(new MessagesDialogsGetParams
                {
                    Count = 200
                });
#pragma warning restore CS0618 // Тип или член устарел
                Thread.Sleep(500);
                var curmessage = "";
                var state = false;
                int i;
                for (i = 0; i < 200; i++)
                {
                    if (getDialogs.Messages[i].UserId == idsob)
                    {
                        state = (bool)getDialogs.Messages[i].Out;
                        curmessage = getDialogs.Messages[i].Body;
                        break;
                    }
                }

                if (state == false && curmessage.Substring(0, 13) != "<RSAKeyValue>")
                {
                    newkey = Utils.AsimDecryptor.RSADecryption(curmessage);
                    break;
                }
            }

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ получен!!!")));
            }));
            return newkey;
        }

        private void Send_Message(VkApi api, long idsob, string fullmessage)
        {
            Random random = new Random();
            int randid = random.Next(999999);
            string pattern = "<Image>";
            string message = "";
            string date = DateTime.Now.ToString();

            Regex regex = new Regex(pattern);

            Match match = Regex.Match(fullmessage, pattern);
            if (match.Length > 0)
            {
                message = fullmessage.Substring(0, match.Index);
            }
            else
            {
                message = fullmessage;
            }
            message += "<VkMKDateMes>" + date;
            string crmessage = Utils.SimCrypto.Encryption(message);
            string attachmentstr = "", crattachments = "";
            string ImageFilePath = Environment.CurrentDirectory;

            if (match.Length > 0)
            {
                attachmentstr = fullmessage.Substring(match.Index);
                crattachments = Utils.SimCrypto.Encryption(attachmentstr);
                File.WriteAllText(Path.Combine(ImageFilePath, "Images.txt"), string.Empty);
                File.WriteAllText(Path.Combine(ImageFilePath, "Images.txt"), CompressString(crattachments));

                UploadServerInfo uploadServer = MainWindow.api.Docs.GetUploadServer();
                // Загрузить файл.
                WebClient wc = new WebClient();
                string responseFile = Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, Path.Combine(ImageFilePath, "Images.txt")));
                // Сохранить загруженный файл
                var attachments = MainWindow.api.Docs.Save(responseFile, "doc").Select(x => x.Instance);


                try
                {
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = idsob,
                        RandomId = randid,
                        Message = crmessage,
                        Attachments = attachments
                    });
                }
                catch
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams
                        {
                            UserId = idsob,
                            RandomId = randid,
                            Message = crmessage,
                            Attachments = attachments
                        });
                    }
                    catch
                    {
                        try
                        {
                            api.Messages.Send(new MessagesSendParams
                            {
                                UserId = idsob,
                                RandomId = randid,
                                Message = crmessage,
                                Attachments = attachments
                            });
                        }
                        catch
                        {
                            try
                            {
                                api.Messages.Send(new MessagesSendParams
                                {
                                    UserId = idsob,
                                    RandomId = randid,
                                    Message = crmessage,
                                    Attachments = attachments
                                });
                            }
                            catch (Exception e) { MessageBox.Show(e.ToString()); }
                        }
                    }
                }
            }
            else
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = idsob,
                        RandomId = randid,
                        Message = crmessage
                    });
                }
                catch
                {
                    try
                    {
                        api.Messages.Send(new MessagesSendParams
                        {
                            UserId = idsob,
                            RandomId = randid,
                            Message = crmessage
                        });
                    }
                    catch
                    {
                        try
                        {
                            api.Messages.Send(new MessagesSendParams
                            {
                                UserId = idsob,
                                RandomId = randid,
                                Message = crmessage
                            });
                        }
                        catch
                        {
                            try
                            {
                                api.Messages.Send(new MessagesSendParams
                                {
                                    UserId = idsob,
                                    RandomId = randid,
                                    Message = crmessage
                                });
                            }
                            catch (Exception e) {MessageBox.Show(e.ToString()); }
                        }
                    }
                }
            }
        }

        private void SendMes(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string message = new TextRange(MyMes.Document.ContentStart, MyMes.Document.ContentEnd).Text;
                Paragraph fullmessage = new Paragraph();
                Bold name = new Bold(new Run(MainWindow.myname + '\n'))
                {
                    Foreground = Brushes.Blue
                };
                fullmessage.Inlines.Add(name);
                fullmessage.Inlines.Add(message);

                List<Image> images = GetImages(Images);
                foreach (Image image in images)
                {
                    try
                    {
                        message += "<Image>" + Convert.ToBase64String(ImageToByte(image.Source as BitmapImage)) + "</Image>";
                    }
                    catch
                    {
                        message += "<Image>" + Convert.ToBase64String(ImageToByte(image.Source as TransformedBitmap)) + "</Image>";
                    }

                }
                message = Regex.Replace(message, @"\t|\n|\r", "");
                string messtr = message;
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    MyMes.Document.Blocks.Clear();
                    MyMes.Margin = new Thickness(0, 71, 10, 10);
                    Images.Document.Blocks.Clear();
                    Images.Margin = new Thickness(160, -8, 10, 10);
                }));
                foreach (Image image in images)
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        BitmapImage bitmap = image.Source as BitmapImage;
                        TransformedBitmap transformed = new TransformedBitmap();
                        if (bitmap == null)
                        {
                            transformed = new TransformedBitmap(image.Source as TransformedBitmap, new ScaleTransform(1, 1));
                            if (transformed.Height <= 1000 && transformed.Height >= 250 && transformed.Width <= 1000 && transformed.Width >= 250)
                            {
                                transformed = new TransformedBitmap(image.Source as TransformedBitmap, new ScaleTransform(0.6, 0.6));
                            }
                            else
                            {
                                if (transformed.Height <= 2000 && transformed.Width <= 2000)
                                {
                                    transformed = new TransformedBitmap(image.Source as TransformedBitmap, new ScaleTransform(0.3, 0.3));
                                }
                            }
                            image.Source = transformed;
                            image.Height = transformed.Height;
                            image.Width = transformed.Width;
                            fullmessage.Inlines.Add(image);
                            fullmessage.Inlines.Add("\n");
                            fullmessage.Inlines.Add("\n");
                            Chat.CaretPosition = Chat.Document.ContentEnd;
                            Chat.ScrollToEnd();
                        }
                        else
                        {
                            image.Height = 360.0 / 1.5;
                            image.Width = 720.0 / 1.5;
                            fullmessage.Inlines.Add(image);
                            fullmessage.Inlines.Add("\n");
                            fullmessage.Inlines.Add("\n");
                            Chat.CaretPosition = Chat.Document.ContentEnd;
                            Chat.ScrollToEnd();
                        }

                    }));
                }
                Send_Message(MainWindow.api, ActiveUserId, message);
                Chat.Document.Blocks.Add(fullmessage);
                Chat.Focus();
                Chat.CaretPosition = Chat.Document.ContentEnd;
                Chat.ScrollToEnd();
                MyMes.Focus();
                MyMes.Document.Blocks.Clear();
                MyMes.ScrollToHome();
            }
        }

        //[STAThread]
        private void Get_Mes(object mesargums)
        {
            string sobname = MainWindow.api.Users.Get(new long[] { ActiveUserId }).FirstOrDefault().FirstName;
            string predmessage = "zhзущшепгтзкищшекгвьезипщывьгпизшщкеигекзипщцнзуищкшецкещицшугеихцущзpweouetvpowiertupmesotrmuser[topetr[vpeto,ivwe[opybiemr[po";
            Array mesargar = new object[3];
            mesargar = (Array)mesargums;
            VkApi get = (VkApi)mesargar.GetValue(0);
            int userid = (int)mesargar.GetValue(1);

            bool messtate = false;
            while (true)
            {
                string curmessage = "";
                MessagesGetObject getDialogs;
                try
                {
#pragma warning disable CS0618 // Тип или член устарел
                    getDialogs = get.Messages.GetDialogs(new MessagesDialogsGetParams
                    {
                        Count = 200
                    });
#pragma warning restore CS0618
                }
                catch
                {
#pragma warning disable CS0618
                    getDialogs = get.Messages.GetDialogs(new MessagesDialogsGetParams
                    {
                        Count = 200
                    });
#pragma warning restore CS0618 
                }

                int pos;
                for (pos = 0; pos < 200; pos++)
                {
                    if (getDialogs.Messages[pos].UserId == userid)
                    {
                        curmessage = getDialogs.Messages[pos].Body;
                        messtate = (bool)getDialogs.Messages[pos].Out;
                        break;
                    }
                }

                string decmessage = curmessage;
                try
                {
                    decmessage = Utils.SimCrypto.Decryption(curmessage);
                }
                catch
                {
                    decmessage = curmessage;
                }

                if (predmessage != decmessage && !messtate)
                {
                    _ = Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        string dectext = "";
                        Regex regnt = new Regex("<Image>");
                        Regex regkt = new Regex("</Image>");
                        try
                        {
                            Attachment documentAttachment = getDialogs.Messages[pos].Attachments.First(x => x.Type == typeof(Document));
                            string uri = ((Document)documentAttachment.Instance).Uri;
                            using (WebClient webClient = new WebClient())
                            {
                                webClient.DownloadFile(uri, (Path.Combine(Environment.CurrentDirectory, "Dec_Images.txt")));
                            }

                            string enctext = DecompressString(File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Dec_Images.txt")));
                            dectext = Utils.SimCrypto.Decryption(enctext);
                            //File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "dectestImages.txt"), dectext);
                        }
                        catch (Exception e)
                        {
                            //MessageBox.Show(e.ToString());
                        }

                        Paragraph fullmessage = new Paragraph();
                        Bold name = new Bold(new Run(MainWindow.myname + '\n'))
                        {
                            Foreground = Brushes.Red
                        };
                        fullmessage.Inlines.Add(name);
                        fullmessage.Inlines.Add(Regex.Split(decmessage, "<VkMKDateMes>")[0]);
                        Chat.Document.Blocks.Add(fullmessage);
                        Chat.Focus();
                        Chat.CaretPosition = Chat.Document.ContentEnd;
                        Chat.ScrollToEnd();
                        MyMes.Focus();

                        try
                        {
                            while (dectext.Length > 0)
                            {
                                string imagetext = dectext.Substring(dectext.IndexOf("<Image>") + 7, dectext.IndexOf("</Image>") - dectext.IndexOf("<Image>") - 7);
                                try
                                {
                                    dectext = dectext.Substring(dectext.IndexOf("</Image>") + 8);
                                }
                                catch
                                {
                                    dectext = "";
                                }
                                File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "dectestImages.txt"), dectext);
                                BitmapImage bitmap = ToImage(StringToByte(imagetext));
                                Image img = new Image();
                                img.Source = bitmap;
                                TransformedBitmap transformed = new TransformedBitmap();
                                if (bitmap.Height <= 1000 && bitmap.Height >= 250 && bitmap.Width <= 1000 && bitmap.Width >= 250)
                                {
                                    transformed = new TransformedBitmap(bitmap, new ScaleTransform(0.6, 0.6));
                                }
                                else
                                {
                                    if (bitmap.Height <= 2000 && bitmap.Width <= 2000)
                                    {
                                        transformed = new TransformedBitmap(bitmap, new ScaleTransform(0.3, 0.3));
                                    }
                                }
                                img.Source = transformed;
                                img.Stretch = Stretch.None;
                                img.Height = transformed.Height;
                                img.Width = transformed.Width;
                                if (dectext != "")
                                {
                                    fullmessage.Inlines.Add(Environment.NewLine);
                                    fullmessage.Inlines.Add(Environment.NewLine);
                                    fullmessage.Inlines.Add(img);
                                    fullmessage.Inlines.Add(Environment.NewLine);
                                }
                                Chat.Focus();
                                Chat.CaretPosition = Chat.Document.ContentEnd;
                                Chat.ScrollToEnd();
                                MyMes.Focus();
                            }
                        }
                        catch (Exception e)
                        {
                            //MessageBox.Show(e.ToString());
                        }

                    }));
                }
                predmessage = decmessage;
                Thread.Sleep(50);
            }
        }

        private string ChangeKeys(VkApi api, int idsob, ref bool me_or_him)
        {
            string newpubkey;
            if (Check_Key(api, idsob) == false)
            {
                Send_Key(api, Utils.AsimEncryptor.GetPubKey(), idsob, true);
                newpubkey = Get_Key(api, idsob);
                me_or_him = true;
            }
            else
            {
                me_or_him = false;
                newpubkey = Get_Key(api, idsob);
                Send_Key(api, Utils.AsimEncryptor.GetPubKey(), idsob, false);
            }

            return newpubkey;
        }

        private bool Check_Key(VkApi api, int idsob)
        {
            bool pr = true;

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Идет проверка на присутствие ключа в чате...")));
            }));

#pragma warning disable CS0618 // Тип или член устарел
            var getDialogs = api.Messages.GetDialogs(new MessagesDialogsGetParams
            {
                Count = 200
            });
#pragma warning restore CS0618 // Тип или член устарел
            var curmessage = "";
            var state = false;
            for (var i = 0; i < 200; i++)
            {
                if (getDialogs.Messages[i].UserId == idsob)
                {
                    state = (bool)getDialogs.Messages[i].Out;
                    curmessage = getDialogs.Messages[i].Body;
                    break;
                }
            }

            if (curmessage.Length < 13)
            {
                pr = false;
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Собеседник не отправил Вам свой ключ((")));
                }));

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Опять вся надежда на Вас!!!")));
                }));

                return false;
            }

            if (curmessage.Substring(0, 13) == "<RSAKeyValue>" && state == false)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ есть!" + Environment.NewLine)));
                }));

                return true;
            }

            if (pr)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Собеседник не отправил Вам свой ключ((")));
                }));

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Опять вся надежда на Вас!!!")));
                }));

                return false;
            }

            return false;
        }

        private bool Check_Key_Without_GUI(VkApi api, int idsob)
        {
            bool pr = true;

#pragma warning disable CS0618 // Тип или член устарел
            MessagesGetObject getDialogs = api.Messages.GetDialogs(new MessagesDialogsGetParams
            {
                Count = 200
            });
#pragma warning restore CS0618 // Тип или член устарел
            var curmessage = "";
            var state = false;
            for (var i = 0; i < 200; i++)
            {
                if (getDialogs.Messages[i].UserId == idsob)
                {
                    state = (bool)getDialogs.Messages[i].Out;
                    curmessage = getDialogs.Messages[i].Body;
                    break;
                }
            }

            if (curmessage.Length < 13)
            {
                return false;
            }

            if (curmessage.Substring(0, 13) == "<RSAKeyValue>" && state == false)
            {
                return true;
            }

            if (pr)
            {
                return false;
            }

            return false;
        }

        private void Send_Key(VkApi api, string pubkey, int idsob, bool pr)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Идет отправка публичного ключа...")));
            }));
            var random = new Random();
            var randid = random.Next(99999);
            try
            {
                api.Messages.Send(new MessagesSendParams
                {
                    UserId = idsob,
                    RandomId = randid,
                    Message = pubkey
                });
            }
            catch
            {
                try
                {
                    api.Messages.Send(new MessagesSendParams
                    {
                        UserId = idsob,
                        RandomId = randid,
                        Message = pubkey
                    });
                }
                catch (Exception e)
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Chat.Document.Blocks.Add(new Paragraph(new Run(e.ToString())));
                    }));
                }
            }
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ успешно отправлен!!!")));
            }));
            if (pr)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Дожидаемся получения ключа...")));
                    Chat.Document.Blocks.Add(new Paragraph(new Run("Можете сходить за кофе")));
                }));
            }
        }

        private string Get_Key(VkApi api, int idsob)
        {
            string newkey;
            while (true)
            {
#pragma warning disable CS0618 // Тип или член устарел
                MessagesGetObject getDialogs = api.Messages.GetDialogs(new MessagesDialogsGetParams
                {
                    Count = 200
                });
#pragma warning restore CS0618 // Тип или член устарел
                Thread.Sleep(500);
                var curmessage = "";
                var state = false;
                int i;
                for (i = 0; i < 200; i++)
                {
                    if (getDialogs.Messages[i].UserId == idsob)
                    {
                        state = (bool)getDialogs.Messages[i].Out;
                        curmessage = getDialogs.Messages[i].Body;
                        break;
                    }
                }

                if (curmessage.Length < 13)
                {
                    continue;
                }

                if (curmessage.Substring(0, 13) == "<RSAKeyValue>" && state == false)
                {
                    newkey = curmessage;
                    break;
                }
            }

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Chat.Document.Blocks.Add(new Paragraph(new Run("Ключ получен")));
            }));
            return newkey;
        }

        public void Content_Inputed(object sender, TextChangedEventArgs e)
        {
            ResizeRtbImages(sender as RichTextBox);
        }

        private void ResizeRtbImages(RichTextBox rtb)
        {
            foreach (Block block in rtb.Document.Blocks)
            {
                if (block is Paragraph)
                {
                    Paragraph paragraph = (Paragraph)block;
                    foreach (Inline inline in paragraph.Inlines)
                    {
                        if (inline is InlineUIContainer)
                        {
                            InlineUIContainer uiContainer = (InlineUIContainer)inline;
                            if (uiContainer.Child is Image)
                            {
                                Image img = (Image)uiContainer.Child;
                                img.Height = 75;
                                img.Width = 150;
                                if (Images.Margin == new Thickness(160, -8, 10, 10))
                                {
                                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        MyMes.Margin = new Thickness(0, 71, 150, 10);
                                        Images.Margin = new Thickness(5, -5, 10, 10);
                                    }));
                                }
                                Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    uiContainer.Child = null;
                                    Images.Document.Blocks.Add(new BlockUIContainer(img));
                                    Images.Focus();
                                    Images.CaretPosition = Images.Document.ContentEnd;
                                    Images.ScrollToEnd();
                                    MyMes.Focus();
                                }));
                            }
                        }
                    }
                }
                if (block is BlockUIContainer)
                {
                    BlockUIContainer blockui = (BlockUIContainer)block;
                    if (blockui.Child is Image)
                    {
                        Image img = (Image)blockui.Child;
                        img.Height = 75;
                        img.Width = 150;
                        if (Images.Margin == new Thickness(160, -8, 10, 10))
                        {
                            Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                MyMes.Margin = new Thickness(0, 71, 150, 10);
                                Images.Margin = new Thickness(5, -5, 10, 10);
                            }));
                        }
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            blockui.Child = null;
                            Images.Document.Blocks.Add(new BlockUIContainer(img));
                            Images.Focus();
                            Images.CaretPosition = Images.Document.ContentEnd;
                            Images.ScrollToEnd();
                            MyMes.Focus();
                        }));
                    }
                }
            }
        }

        private List<Image> GetImages(RichTextBox rtb)
        {
            List<Image> images = new List<Image>();
            foreach (Block block in rtb.Document.Blocks)
            {
                if (block is BlockUIContainer)
                {
                    BlockUIContainer blockui = (BlockUIContainer)block;
                    if (blockui.Child is Image)
                    {
                        Image img = (Image)blockui.Child;
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            blockui.Child = null;
                        }));
                        images.Add(img);
                    }
                }
            }
            return images;
        }

        public static byte[] ImageToByte(BitmapImage image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        public static byte[] ImageToByte(TransformedBitmap image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(ms);
                return ms.ToArray();
            }
        }

        private void ImageEntered(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && files.Where(IsImageFile).Any())
            {
                e.Handled = true;
            }
        }

        private void ImageDropped(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            try
            {
                if (Path.GetExtension(files[0]) == ".jpg" || Path.GetExtension(files[0]) == ".jpeg" || Path.GetExtension(files[0]) == ".png")
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(files[0]));
                    TransformedBitmap targetBitmap = new TransformedBitmap(bitmap, new ScaleTransform(1, 1));
                    Image img = new Image();
                    img.Source = targetBitmap;
                    img.Stretch = Stretch.None;
                    img.Height = 75;
                    img.Width = 150;
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        MyMes.Margin = new Thickness(0, 71, 150, 10);
                        Images.Margin = new Thickness(5, -5, 10, 10);
                    }));
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        Images.Document.Blocks.Add(new BlockUIContainer(img));
                        Images.Focus();
                        Images.CaretPosition = Images.Document.ContentEnd;
                        Images.ScrollToEnd();
                        MyMes.Focus();
                    }));
                }
            }
            catch { }
        }

        private static bool IsImageFile(string fileName)
        {
            return true;
        }

        public BitmapImage ToImage(byte[] array)
        {
            using (var ms = new MemoryStream(array))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; // here
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        public static byte[] StringToByte(string s)
        {
            byte[] encbytes = new byte[0];
            while (s.Length > 100)
            {
                encbytes = Combine(encbytes, Convert.FromBase64String(s.Substring(0, 100)));
                s = s.Substring(100);
            }
            encbytes = Combine(encbytes, Convert.FromBase64String(s.Substring(0)));
            return encbytes;
        }

        public static byte[] Combine(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public static string CompressString(string value)
        {
            byte[] byteArray = new byte[0];
            if (!string.IsNullOrEmpty(value))
            {
                byteArray = Encoding.UTF8.GetBytes(value);
                using (MemoryStream stream = new MemoryStream())
                {
                    using (GZipStream zip = new GZipStream(stream, CompressionMode.Compress))
                    {
                        zip.Write(byteArray, 0, byteArray.Length);
                    }
                    byteArray = stream.ToArray();
                }
            }
            return Convert.ToBase64String(byteArray);
        }

        public static string DecompressString(string decstring)
        {
            byte[] value = StringToByte(decstring);
            string resultString = string.Empty;
            if (value != null && value.Length > 0)
            {
                using (MemoryStream stream = new MemoryStream(value))
                using (GZipStream zip = new GZipStream(stream, CompressionMode.Decompress))
                using (StreamReader reader = new StreamReader(zip))
                {
                    resultString = reader.ReadToEnd();
                }
            }
            return resultString;
        }
    }
}
