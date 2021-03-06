﻿using GalaSoft.MvvmLight.Ioc;
using PhexensWuerfelraum.Logic.ClientServer;
using SimpleSockets;
using SimpleSockets.Client;
using SimpleSockets.Messaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PhexensWuerfelraum.Logic.Ui
{
    public class Chatroom : BaseViewModel
    {
        #region properties

        private static Chatroom ChatRoom = SimpleIoc.Default.GetInstance<Chatroom>();

        private static bool UseSSL;
        private static readonly string Password = "Password";
        private static SimpleSocketClient _client;
        private static AuthPacket ClientAuthInfo;
        private static int OwnId { get => ClientAuthInfo.UserModel.Id; }

        private UserModel _selectedUser;
        private readonly SettingsViewModel SettingsViewModel = SimpleIoc.Default.GetInstance<SettingsViewModel>();

        private MediaPlayer mediaPlayer1;
        private MediaPlayer mediaPlayer2;
        private MediaPlayer mediaPlayer3;
        private MediaPlayer mediaPlayer4;
        private MediaPlayer mediaPlayer5;

        public bool IsRunning { get; set; }
        public bool Connected { get; set; }
        public ObservableCollection<ChatPacket> Messages { get; set; }
        public string Status { get; set; }
        public ObservableCollection<UserModel> Users { get; set; }
        public int Recipient { get; set; } = 0;

        public UserModel SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;

                if (_selectedUser == null)
                {
                    Recipient = 0;
                }
                else
                {
                    Recipient = _selectedUser.Id;
                }
            }
        }

        #endregion properties

        public Chatroom()
        {
            Messages = new ObservableCollection<ChatPacket>();
            Users = new ObservableCollection<UserModel>();
            Status = "Verbinden";
        }

        public void Clear()
        {
            Messages.Clear();
            Users.Clear();
        }

        public void Connect(string address, int port)
        {
            Status = "Verbinde...";
            UseSSL = SettingsViewModel.Setting.EnableSSL;

            if (UseSSL)
            {
                var cert = new X509Certificate2(new System.Text.ASCIIEncoding().GetBytes(SettingsViewModel.Setting.PublicKey));
                _client = new SimpleSocketTcpSslClient(cert);
            }
            else
            {
                _client = new SimpleSocketTcpClient();
            }

            _client.EnableExtendedAuth = false; // When the client sets 'EnableExtendedAuth' to true it will send a GUID, OSVersion, PC Name and DomainName to the server
            _client.ObjectSerializer = new BinarySerializer();
            _client.AllowReceivingFiles = true;

            BindEvents();

            _client.StartClient(address, port);
        }

        public void Disconnect()
        {
            Status = "Trenne...";
            _client.Close();
        }

        public async Task Send(string username, string message, string colorCode, int toId, string toName, ChatMessageType messageType = ChatMessageType.Text)
        {
            if (!string.IsNullOrEmpty(message) && !string.IsNullOrWhiteSpace(message))
            {
                ChatPacket chatPacket = new ChatPacket(messageType, message, 0, username, toId, toName, colorCode);
                await _client.SendObjectAsync(chatPacket);
            }
        }

        #region Events

        private static void BindEvents()
        {
            //_client.ProgressFileReceived += Progress;
            _client.SslAuthStatus += ClientOnSslAuthStatus;
            //_client.FileReceiver += ClientOnFileReceiver;
            //_client.FolderReceiver += ClientOnFolderReceiver;
            _client.DisconnectedFromServer += Disconnected;
            _client.MessageUpdateFileTransfer += ClientOnMessageUpdateFileTransfer;
            _client.MessageUpdate += ClientOnMessageUpdate;
            _client.ConnectedToServer += ConnectedToServer;
            _client.ClientErrorThrown += ErrorThrown;
            _client.MessageReceived += ServerMessageReceived;
            _client.MessageSubmitted += ClientMessageSubmitted;
            _client.MessageFailed += MessageFailed;
            //_client.CustomHeaderReceived += CustomHeader;
            _client.ObjectReceived += ClientOnObjectReceived;
        }

        private static void ClientOnSslAuthStatus(AuthStatus status)
        {
            //if (status == AuthStatus.Failed)
            //    WriteLine("Failed to authenticate.");
            //if (status == AuthStatus.Success)
            //    WriteLine("Authenticated with success.");
        }

        private static void ClientOnObjectReceived(SimpleSocketClient simpleSocketClient, object obj, Type objType)
        {
            //WriteLine("Received an object of type = " + objType.FullName);
            if (obj.GetType() == typeof(ChatPacket))
            {
                ChatRoom.ManageChatPacket((ChatPacket)obj);
            }
            else if (obj.GetType() == typeof(ChatroomPacket))
            {
                var c = (ChatroomPacket)obj;

                Application.Current.Dispatcher.Invoke(delegate
                {
                    ChatRoom.Users.Clear();

                    foreach (var user in c.Users)
                    {
                        ChatRoom.Users.Add(user);
                    }
                });
            }
            else if (obj.GetType() == typeof(AuthPacket))
            {
                var a = (AuthPacket)obj;
                ClientAuthInfo = a;
            }
        }

        private static void ClientOnMessageUpdate(SimpleSocketClient a, string msg, string header, MessageType msgType, MessageState state)
        {
            // WriteLine("Sending message to client: msg = " + msg + ", header = " + header);
        }

        private static void ClientOnMessageUpdateFileTransfer(SimpleSocketClient a, string origin, string loc, double percentageDone, MessageState state)
        {
            //WriteLine("Sending message to server: " + percentageDone + "%");
        }

        //private static void ClientOnFileReceiver(SimpleSocketClient a, int currentPart, int totalParts, string loc, MessageState state)
        //{
        //    if (state == MessageState.ReceivingData)
        //    {
        //        //if (progress == null)
        //        //{
        //        //    progress = new ProgressBar();
        //        Console.Write("Receiving a File... ");
        //        //}

        //        var progressDouble = ((double)currentPart / totalParts);

        //        //progress.Report(progressDouble);

        //        if (progressDouble >= 1.00)
        //        {
        //            //progress.Dispose();
        //            //progress = null;
        //            Thread.Sleep(200);
        //            WriteLine("File Transfer Complete");
        //        }
        //    }

        //    if (state == MessageState.Decrypting)
        //        WriteLine("Decrypting File this might take a while.");
        //    if (state == MessageState.Decompressing)
        //        WriteLine("Decompressing the File this might take a while.");
        //    if (state == MessageState.DecompressingDone)
        //        WriteLine("Decompressing has finished.");
        //    if (state == MessageState.DecryptingDone)
        //        WriteLine("Decrypting has finished.");
        //    if (state == MessageState.Completed)
        //        WriteLine("File received and stored at location: " + loc);
        //}

        //private static void CustomHeader(SimpleSocket a, string msg, string header)
        //{
        //    //WriteLine("Bytes received from server with header = " + header + " and message = " + msg);
        //}

        private static void ErrorThrown(SimpleSocket socketClient, Exception error)
        {
            //if (error.GetType() == typeof(Exception) ||
            //    error.GetType() == typeof(IOException) ||
            //    error.GetType() == typeof(ObjectDisposedException))
            //{
            //    Application.Current.Dispatcher.Invoke(delegate
            //    {
            //        ChatRoom.Messages.Add(new ChatPacket(ChatMessageType.Text, "Ein Fehler ist aufgetreten. Die Verbindung zum Server wurde unerwartet geschlossen.", 0, "", 0, ""));
            //    });
            //}
            //else
            //{
            throw error;
            //}
            //WriteLine("The client has thrown an error: " + error.Message);
            //WriteLine("Stacktrace: " + error.StackTrace);
        }

        private static void ConnectedToServer(SimpleSocket a)
        {
            var settings = SimpleIoc.Default.GetInstance<SettingsViewModel>().Setting;
            var character = SimpleIoc.Default.GetInstance<CharacterViewModel>().Character;
            string username = SimpleIoc.Default.GetInstance<CharacterViewModel>().SelectedCharacter.Name;

            if (string.IsNullOrEmpty(username))
            {
                username = string.IsNullOrEmpty(character.Name) ? "User" + new Random().Next(1000, 9999) : character.Name;
            }

            if (string.IsNullOrEmpty(username))
            {
                username = settings.StaticUserName;
            }

            UserType userType = (settings.GameMasterMode) ? UserType.GameMaster : UserType.Player;

            var userModel = new UserModel(username, userType);
            var authPacket = new AuthPacket(userModel, Password, "test"); // ToDo: channel
            _client.SendObject(authPacket);

            ChatRoom.Status = "Verbunden";
            ChatRoom.Connected = true;
        }

        private static void ServerMessageReceived(SimpleSocket a, string msg)
        {
            //WriteLine("Message received from the server: " + msg);
        }

        private static void Disconnected(SimpleSocket a)
        {
            //WriteLine("The client has disconnected from the server with ip " + a.Ip + "on port " + a.Port);
            ChatRoom.Status = "Verbinden";
            ChatRoom.Connected = false;

            ChatRoom.Users.Clear();

            Application.Current.Dispatcher.Invoke(delegate
            {
                ChatRoom.Messages.Add(new ChatPacket(ChatMessageType.Text, "Die Verbindung zum Server wurde getrennt oder konnte nicht aufgebaut werden.", 0, "", 0, ""));
            });
        }

        private static void ClientMessageSubmitted(SimpleSocket a, bool close)
        {
            //WriteLine("The client has submitted a message to the server.");
        }

        private static void MessageFailed(SimpleSocket tcpClient, byte[] messageData, Exception exception)
        {
            //WriteLine("The client has failed to send a message.");
            //WriteLine("Error: " + exception);
        }

        #endregion Events

        private void ManageChatPacket(ChatPacket chatPacket)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                ChatRoom.Messages.Add(chatPacket);

                if (SettingsViewModel.Setting.SoundEffectsEnabled)
                {
                    if (chatPacket.FromId != OwnId) // only play notification sound for messages from other users
                    {
                        ChatRoom.mediaPlayer1 = new MediaPlayer();
                        ChatRoom.mediaPlayer1.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/Notification.wav"));
                        ChatRoom.mediaPlayer1.Play();
                    }

                    if (chatPacket.MessageType == ChatMessageType.Roll || chatPacket.MessageType == ChatMessageType.RollWhisper)
                    {
                        if (chatPacket.Message.Contains("Doppel 1!"))
                        {
                            ChatRoom.mediaPlayer2 = new MediaPlayer();
                            ChatRoom.mediaPlayer2.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/roll-1-1.wav"));
                            ChatRoom.mediaPlayer2.Play();
                        }
                        else if (chatPacket.Message.Contains("dreifach 1!"))
                        {
                            ChatRoom.mediaPlayer3 = new MediaPlayer();
                            ChatRoom.mediaPlayer3.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/roll-1-1-1.wav"));
                            ChatRoom.mediaPlayer3.Play();
                        }
                        else if (chatPacket.Message.Contains("doppel 20;"))
                        {
                            ChatRoom.mediaPlayer4 = new MediaPlayer();
                            ChatRoom.mediaPlayer4.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/roll-20-20.wav"));
                            ChatRoom.mediaPlayer4.Play();
                        }
                        else if (chatPacket.Message.Contains("dreifach 20;"))
                        {
                            ChatRoom.mediaPlayer5 = new MediaPlayer();
                            ChatRoom.mediaPlayer5.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/roll-20-20-20.wav"));
                            ChatRoom.mediaPlayer5.Play();
                        }
                    }
                }
            });
        }
    }
}