﻿using GalaSoft.MvvmLight.Ioc;
using PhexensWuerfelraum.Logic.ClientServer;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PhexensWuerfelraum.Logic.Ui
{
    public class Chatroom : BaseViewModel
    {
        private SimpleClient _client;
        private Task _connectionTask;
        private Task<bool> _listenTask;
        private bool _pinged = false;
        private DateTime _pingLastSent;
        private DateTime _pingSent;
        private Task _updateTask;
        private UserModel _ownUser;
        private UserModel _selectedUser;
        private readonly SettingsViewModel SettingsViewModel = SimpleIoc.Default.GetInstance<SettingsViewModel>();
        private MediaPlayer mediaPlayer1;
        private MediaPlayer mediaPlayer2;
        private MediaPlayer mediaPlayer3;
        private MediaPlayer mediaPlayer4;
        private MediaPlayer mediaPlayer5;

        public Chatroom()
        {
            Messages = new ObservableCollection<ChatPacket>();
            Users = new ObservableCollection<UserModel>();
            Status = "Verbinden";
        }

        public bool IsRunning { get; set; }
        public bool Connected { get; set; }
        public ObservableCollection<ChatPacket> Messages { get; set; }
        public string Status { get; set; }
        public ObservableCollection<UserModel> Users { get; set; }
        public Guid Recipient { get; set; } = Guid.Empty;

        public UserModel SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;

                if (_selectedUser == null)
                {
                    Recipient = Guid.Empty;
                }
                else
                {
                    Recipient = new Guid(_selectedUser.UserGuid);
                }
            }
        }

        public void Clear()
        {
            Messages.Clear();
            Users.Clear();
        }

        public async Task Connect(UserModel user, string address, int port)
        {
            Status = "Verbinde...";

            if (SetupClient(user.UserName, address, port))
            {
                try
                {
                    var packet = await GetNewConnectionPacket(user);
                    await InitializeConnection(packet);
                    _ownUser = user;
                }
                catch (Exception)
                {
                }
            }
        }

        public async Task Disconnect()
        {
            Status = "Trenne...";

            if (IsRunning)
            {
                IsRunning = false;

                await _connectionTask;
                await _updateTask;

                _client.Disconnect();
            }

            Connected = false;
            Status = "Verbinden";

            Application.Current.Dispatcher.Invoke(delegate
            {
                Messages.Add(new ChatPacket
                {
                    FromUsername = string.Empty,
                    Message = "Die Verbindung zum Server wurde getrennt oder konnte nicht aufgebaut werden.",
                    UserColor = "black",
                    DateTime = DateTime.Now
                });
                Users.Clear();
            });
        }

        public async Task Send(string username, string message, string colorCode, Guid recipient, string recipientUsername, MessageType messageType = MessageType.Text)
        {
            if (recipient != Guid.Empty)
            {
                if (messageType == MessageType.Roll)
                {
                    messageType = MessageType.RollWhisper;
                }
                else
                {
                    messageType = MessageType.Whisper;
                }
            }

            ChatPacket cap = new ChatPacket
            {
                MessageType = messageType,
                UserColor = colorCode,
                SenderGuid = _client.ClientGuid,
                FromUsername = username,
                RecipientGuid = recipient,
                ToUsername = recipientUsername,
                Message = message,
            };

            await _client.SendObject(cap);
        }

        private async Task<PersonalPacket> GetNewConnectionPacket(UserModel user)
        {
            _listenTask = Task.Run(() => _client.Connect());

            IsRunning = await _listenTask;
            Connected = IsRunning;

            var notifyServer = new UserConnectionPacket
            {
                Username = user.UserName,
                UserType = user.UserType,
                IsJoining = true,
                UserGuid = _client.ClientGuid.ToString()
            };

            var personalPacket = new PersonalPacket
            {
                GuidId = _client.ClientGuid.ToString(),
                Package = notifyServer
            };

            return personalPacket;
        }

        private async Task InitializeConnection(PersonalPacket connectionPacket)
        {
            _pinged = false;

            if (IsRunning)
            {
                _updateTask = Task.Run(() => Update());
                await _client.SendObject(connectionPacket);
                _connectionTask = Task.Run(() => MonitorConnection());
                Status = "Verbunden";
                Connected = true;
            }
            else
            {
                Status = "Fehler";
                await Disconnect();
                Connected = false;
            }
        }

        private bool ManagePacket(object packet)
        {
            if (packet != null)
            {
                if (packet is ChatPacket chatP)
                {
                    Messages.Add(chatP);

                    if (SettingsViewModel.Setting.SoundEffectsEnabled)
                    {
                        if (chatP.FromUsername != _ownUser.UserName)
                        {
                            mediaPlayer1 = new MediaPlayer();
                            mediaPlayer1.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/Notification.wav"));
                            mediaPlayer1.Play();
                        }

                        if (chatP.MessageType == MessageType.Roll || chatP.MessageType == MessageType.RollWhisper)
                        {
                            if (chatP.Message.Contains("Doppel 1!"))
                            {
                                mediaPlayer2 = new MediaPlayer();
                                mediaPlayer2.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/roll-1-1.wav"));
                                mediaPlayer2.Play();
                            }
                            else if (chatP.Message.Contains("dreifach 1!"))
                            {
                                mediaPlayer3 = new MediaPlayer();
                                mediaPlayer3.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/roll-1-1-1.wav"));
                                mediaPlayer3.Play();
                            }
                            else if (chatP.Message.Contains("doppel 20;"))
                            {
                                mediaPlayer4 = new MediaPlayer();
                                mediaPlayer4.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/roll-20-20.wav"));
                                mediaPlayer4.Play();
                            }
                            else if (chatP.Message.Contains("dreifach 20;"))
                            {
                                mediaPlayer5 = new MediaPlayer();
                                mediaPlayer5.Open(new Uri(Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "/Resources/Sounds/roll-20-20-20.wav"));
                                mediaPlayer5.Play();
                            }
                        }
                    }
                }

                if (packet is UserConnectionPacket connectionP)
                {
                    Users.Clear();
                    foreach (var user in connectionP.Users)
                    {
                        Users.Add(user);
                    }
                }

                if (packet is PingPacket pingP)
                {
                    _pingLastSent = DateTime.Now;
                    _pingSent = _pingLastSent;
                    _pinged = false;
                }

                return true;
            }

            return false;
        }

        private async Task MonitorConnection()
        {
            _pingSent = DateTime.Now;
            _pingLastSent = DateTime.Now;

            while (IsRunning)
            {
                Thread.Sleep(1);
                var timePassed = (_pingSent.TimeOfDay - _pingLastSent.TimeOfDay);
                if (timePassed > TimeSpan.FromSeconds(5))
                {
                    if (!_pinged)
                    {
                        var result = await _client.PingConnection();
                        _pinged = true;

                        Thread.Sleep(5000);

                        if (_pinged)
                        {
#pragma warning disable 4014 // Because this call is not awaited, execution of the current method continues before the call is completed.
                            Task.Run(() => Disconnect()); // doesn't work if called async
#pragma warning disable 4014
                        }
                    }
                }
                else
                {
                    _pingSent = DateTime.Now;
                }
            }
        }

        private async Task<bool> MonitorData()
        {
            var newObject = await _client.RecieveObject();

            Application.Current.Dispatcher.Invoke(delegate
            {
                return ManagePacket(newObject);
            });

            return false;
        }

        private bool SetupClient(string username, string address, int port)
        {
            _client = new SimpleClient(address, port)
            {
                ClientUserName = username,
                IsGameMaster = SettingsViewModel.Setting.GameMasterMode,
            };

            return true;
        }

        private async Task Update()
        {
            while (IsRunning)
            {
                Thread.Sleep(1);
                var recieved = await MonitorData();

                if (recieved)
                {
                    Trace.WriteLine(recieved);
                }
            }
        }
    }
}