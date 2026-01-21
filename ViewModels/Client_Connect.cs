using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfApp1_client;
using WpfApp1_client.Models;
using WpfApp1_client.ViewModels;
using WpfApp1_client.xaml;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.IO; // Добавляем это пространство имен для File

using Newtonsoft.Json;

namespace WpfApp1_client.ViewModels
{
    public class Client_Connect : Window
    {
        public List<MembersList> Client = new List<MembersList>();
        public Dictionary<string, ServersList> Servers = new Dictionary<string, ServersList>();
        // Сетевые соединения
        private TcpClient textClient;
        private TcpClient audioClient;
        private TcpClient videoClient;
        private NetworkStream textStream;
        private NetworkStream audioStream;
        private NetworkStream videoStream;
        private string username;
        private bool isRunning = false;

        // Аудио компоненты
        private WaveInEvent waveIn;
        private WaveOutEvent waveOut;
        private BufferedWaveProvider waveProvider;
        private const int SAMPLE_RATE = 16000;
        private const int CHANNELS = 1;
        private const int BUFFER_MS = 50;
        private const int AUDIO_BUFFER_SIZE = 4096;

        // Видео компоненты
        private Thread screenCaptureThread;
        private Thread videoReceiveThread;
        private bool isStreamingScreen = false;
        private bool isWatchingScreen = false;
        private int screenCaptureInterval = 100; // мс между кадрами
        private int screenQuality = 50; // качество JPEG (1-100)
        private int screenWidth = 1024; // ширина скриншота
        private int screenHeight = 768; // высота скриншота

        // Состояние
        private bool isAudioEnabled = false;
        private bool isInCall = false;
        public string currentRoom = null;
        public string currentServerID = null;


        private List<string> availableRooms = new List<string>();


        private Dictionary<string, string> roomIdToName = new Dictionary<string, string>(); // ID -> Name

        // Потоки
        private Thread textReceiveThread;
        private Thread audioReceiveThread;

        // События для уведомления о пользователях
        public event EventHandler<MembersList> UserAdded;
        public event EventHandler<ServersList> ServerAdded;

        public event EventHandler<string> UpdateCurrentServerID;

        public event EventHandler<Dictionary<string, ServersList>> UpdateServerList;

        public event EventHandler<UserStatusEventArgs> UserStatusChanged;
        public event EventHandler<string> RemoveChannel;

        // Метод для вызова события
        private void OnUserAdded(MembersList username)
        {
            UserAdded?.Invoke(this, username);
        }
        private void OnServerAdded(ServersList server)
        {
            ServerAdded?.Invoke(this, server);
        }
        private void OnUpdateServerList(Dictionary<string, ServersList> servers)
        {
            UpdateServerList?.Invoke(this, servers);
        }
        private void OnUpdateCurrentServerID(string serverID)
        {
            UpdateCurrentServerID?.Invoke(this, serverID);
        }

        private void OnUserStatusChanged(string username, bool isOnline, string statusText = "")
        {
            UserStatusChanged?.Invoke(this, new UserStatusEventArgs(username, isOnline, statusText));
        }
        private void OnRemoveChannel(string channelName)
        {
            RemoveChannel?.Invoke(this, channelName);
        }

        public void Connect(string serverIP, string username)
        {
            try
            {
                this.username = username;

                //    Console.Clear();
                //     Console.WriteLine($"=== Голосовой чат с демонстрацией экрана ===");
                //     Console.WriteLine($"Подключение к {serverIP} как {username}...");

                // 1. Текстовое соединение
                textClient = new TcpClient();
                textClient.Connect(serverIP, 6001);
                textStream = textClient.GetStream();
                textStream.Write(Encoding.UTF8.GetBytes(username), 0, username.Length);

                // 2. Аудио соединение
                audioClient = new TcpClient();
                audioClient.Connect(serverIP, 6000);
                audioStream = audioClient.GetStream();
                audioStream.Write(Encoding.UTF8.GetBytes(username), 0, username.Length);

                // 3. Видео соединение
                videoClient = new TcpClient();
                videoClient.Connect(serverIP, 6002);
                videoStream = videoClient.GetStream();
                videoStream.Write(Encoding.UTF8.GetBytes(username), 0, username.Length);

                isRunning = true;

                //     Console.WriteLine($"=== Подключено успешно! ===");
                //     Console.WriteLine("Используйте /help для списка команд");
                //     Console.WriteLine(new string('=', 40));

                // Инициализируем аудио
                InitializeAudio();

                // Запускаем потоки приема
                textReceiveThread = new Thread(ReceiveTextMessages);
                textReceiveThread.Start();

                audioReceiveThread = new Thread(ReceiveAudio);
                audioReceiveThread.Start();

                videoReceiveThread = new Thread(ReceiveVideo);
                videoReceiveThread.Start();

                // Основной цикл ввода
                HandleUserInput();
            }
            catch (Exception ex)
            {
                //     Console.WriteLine($"Ошибка подключения: {ex.Message}");
                Console.ReadKey();
            }
        }
        public void Disconnect()
        {
            isRunning = false;
            isInCall = false;
            currentRoom = null;

            // Останавливаем стриминг и просмотр
            StopScreenStreaming();
            StopWatchingScreen();

            // Отправляем команды отключения
            if (textClient?.Connected == true)
            {
                SendMessage("/leave");
                SendMessage("/endcall");
                Thread.Sleep(100);
            }

            // Останавливаем аудио
            DisableMicrophone();
            waveOut?.Stop();
            waveOut?.Dispose();
            waveIn?.Dispose();

            // Закрываем соединения
            textStream?.Close();
            textClient?.Close();
            audioStream?.Close();
            audioClient?.Close();
            videoStream?.Close();
            videoClient?.Close();

            // Ждем завершения потоков
            textReceiveThread?.Join(1000);
            audioReceiveThread?.Join(1000);
            videoReceiveThread?.Join(1000);
            screenCaptureThread?.Join(1000);

            //   Console.WriteLine("\n[СИСТЕМА] Отключено от сервера");
        }
        private void HandleUserInput()
        {
            //   while (isRunning)
            {
                //   ShowPrompt();
                //     string input = Console.ReadLine();

                //    if (string.IsNullOrEmpty(null)) continue;

                ProcessCommand("");
            }
        }
        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (!isAudioEnabled || audioStream == null || !audioClient.Connected)
                return;

            bool canSendAudio = (!string.IsNullOrEmpty(currentRoom) || isInCall) && isAudioEnabled;

            if (canSendAudio && e.BytesRecorded > 0)
            {
                try
                {
                    byte[] sizeBytes = BitConverter.GetBytes(e.BytesRecorded);
                    audioStream.Write(sizeBytes, 0, 4);
                    audioStream.Write(e.Buffer, 0, e.BytesRecorded);
                    audioStream.Flush();
                }
                catch (Exception ex)
                {
                    //  Console.WriteLine($"[СИСТЕМА] Ошибка отправки аудио: {ex.Message}");
                }
            }
        }
        private bool isConnected = false;
        public static T LoadFromFile<T>(string filePath) where T : new()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Файл не найден: {filePath}");
                    return new T();
                }

                // Читаем JSON из файла
                string json = File.ReadAllText(filePath);

                // Десериализуем JSON в объект
                T data = JsonConvert.DeserializeObject<T>(json);

                Console.WriteLine($"Данные успешно загружены из файла: {filePath}");
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
                return new T();
            }
        }
        public static string SaveToFile<T>(T data, string filePath)
        {

            try
            {
                // Сериализуем объект в JSON с форматированием для читаемости
                string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

                // Записываем в файл
                File.WriteAllText(filePath, json);

                Console.WriteLine($"Данные успешно сохранены в файл: {filePath}");
                return json;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении файла: {ex.Message}");
            }
            return null;
        }
        private void ProcessServerMessage(string message)
        {
            if (message.StartsWith("Connected"))
            {
                isConnected = true; //флаг подключения к серверу
                SendMessage("/infoforme");
            }
            if (message.StartsWith("Client\n{\r\n  \"Name\""))//получаем информацию о себе
            {
                message = message.Replace("Client\n", "");
                var user = JsonConvert.DeserializeObject<MembersList>(message);
                OnUserAdded(user);
                SendMessage("/server_list");
            }
            if (message.StartsWith("ServerList\n{\r\n"))
            {
                message = message.Replace("ServerList\n", "");
                var Servers= JsonConvert.DeserializeObject<Dictionary<string, ServersList>>(message);
                OnUpdateServerList(Servers);
            }
            if (message.StartsWith("СurrentServerID:"))//получение ИД нового сервера если зашли на сервер
            {
                //  UpdateRoomList(message.Substring(13));
                message = message.Replace("СurrentServerID: ", "");
                currentServerID = message;
                OnUpdateCurrentServerID(currentServerID);
                SendMessage("/server_list");
                //  SendMessage($"/my_server_info {currentServerID}"); //запрашиваем информацию о новом сервере
            }
            if (message.StartsWith("SERVER\n{\r\n  \"ServerID\""))
            {
                message = message.Replace("SERVER\n", "");
                var Server = JsonConvert.DeserializeObject<ServersList>(message);
                OnServerAdded(Server);
                //   OnUserAdded(user);
            }

            // Обновление списка пользователей на сервере
            if (message.StartsWith("UPDATE_USERS_IN_SERVER:"))
            {
                //  UpdateRoomList(message.Substring(13));
                //message = message.Replace("UPDATE_USERS_IN_SERVER:\n", "");
                //var users = JsonConvert.DeserializeObject<MembersList[]>(message);
                //OnUpdateCurrentServerID(users);
            }



            // Обработка создания комнаты - сервер отправляет ID комнаты
            if (message.Contains("Комната создана! ID:"))
            {
                // Извлекаем ID комнаты из сообщения
                // Формат: "Комната создана! ID: ABC12345"
                int idIndex = message.IndexOf("ID: ");
                if (idIndex > 0)
                {
                    string roomId = message.Substring(idIndex + 4).Trim();
                    currentRoom = roomId;
                    //     Console.WriteLine($"\n[СИСТЕМА] Комната создана! ID: {roomId}");
                    //     Console.WriteLine($"[СИСТЕМА] Вы вошли в созданную комнату");

                    // Включаем микрофон при создании комнаты
                    if (!isAudioEnabled)
                    {
                        EnableMicrophone();
                    }
                }
            }
            // Обработка входа в существующую комнату
            else if (message.Contains("Вы присоединились к комнате"))
            {
                // Формат: "Вы присоединились к комнате 'НазваниеКомнаты'"
                // Нужно найти ID комнаты по названию
                int startIndex = message.IndexOf("'");
                int endIndex = message.LastIndexOf("'");
                if (startIndex > 0 && endIndex > startIndex)
                {
                    string roomName = message.Substring(startIndex + 1, endIndex - startIndex - 1);

                    // Ищем комнату по имени в словаре
                    foreach (var kvp in roomIdToName)
                    {
                        if (kvp.Value == roomName)
                        {
                            currentRoom = kvp.Key;
                            //       Console.WriteLine($"\n[СИСТЕМА] Вы вошли в комнату: {roomName} (ID: {currentRoom})");

                            // Включаем микрофон при входе в комнату
                            if (!isAudioEnabled)
                            {
                                EnableMicrophone();
                            }
                            break;
                        }
                    }
                }
            }
            // Активация микрофона при звонке
            else if (message.Contains("звонит вам") ||
                     message.Contains("Звонок установлен") ||
                     message.Contains("Входящий звонок"))
            {
                isInCall = true;
                if (!isAudioEnabled)
                {
                    EnableMicrophone();
                }
            }
            // Обработка выхода из комнаты
            else if ((message.Contains("покинул комнату") && message.Contains(username)) ||
                     (message.Contains("исключен из комнаты") && message.Contains(username)) ||
                     message.Contains("Вы покинули комнату") ||
                     message.Contains("Вы были исключены из комнаты"))
            {
                //   Console.WriteLine($"\n[СИСТЕМА] Вы вышли из комнаты {currentRoom}");
                currentRoom = null;

                if (isStreamingScreen)
                {
                    StopScreenStreaming();
                }
                if (isWatchingScreen)
                {
                    StopWatchingScreen();
                }
                if (isAudioEnabled && !isInCall)
                {
                    DisableMicrophone();
                }
            }
            // Обработка завершения звонка
            else if (message.Contains("Звонок завершен") ||
                     message.Contains("отключился") && isInCall)
            {
                isInCall = false;
                if (isAudioEnabled && string.IsNullOrEmpty(currentRoom))
                {
                    DisableMicrophone();
                }
            }

            // Информация о демонстрации экрана
            if (message.Contains("начал демонстрацию экрана") && !message.Contains(username))
            {
                //   Console.WriteLine("\n[СИСТЕМА] Кто-то начал демонстрацию экрана.");
                //   Console.WriteLine("[СИСТЕМА] Используйте /watch чтобы начать просмотр");
            }

            if (message.Contains("остановил демонстрацию экрана") && isWatchingScreen)
            {
                //    Console.WriteLine("\n[СИСТЕМА] Демонстрация экрана остановлена");
                StopWatchingScreen();
            }

            if (message.Contains("Онлайн пользователи:"))
            {
                int startIndex = message.IndexOf(":");
                string usersText = message.Substring(startIndex + 1).Trim();
                var users = usersText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Where(line => line.Contains("[")).Select(line => new
                {
                    Username = line.Split(' ')[0],
                    Status = "В сети"
                }).ToList();

                foreach (var user in users)
                {

                    if (user.Username != UserContext.UserName)
                    {
                        ///  OnUserAdded(user.Username);
                    }
                }

            }
            if (message.Contains("отключился"))
            {
                //  string username = ExtractUsernameFromMessage(message);
                if (!string.IsNullOrEmpty(username))
                {
                    OnUserStatusChanged(username, false, "Не в сети");
                }
            }
            if (message.Contains("присоединился к чату"))
            {
                //  string username = ExtractUsernameFromMessage(message);
                if (!string.IsNullOrEmpty(username))
                {
                    OnUserStatusChanged(username, true, "В сети");
                }
            }
            if (message.Contains("Удалена пустая комната"))
            {
                OnRemoveChannel("Голосовой_1");
            }

            DisplayMessage(message);
        }

        private void UpdateRoomList(string roomData)
        {
            availableRooms.Clear();
            roomIdToName.Clear();

            var rooms = roomData.Split(';');

            Console.WriteLine($"\n[СИСТЕМА] Обновлен список комнат: {rooms.Length} комнат");
            foreach (var room in rooms)
            {
                // Формат: "ID:Название:Участники:Макс"
                var parts = room.Split(':');
                if (parts.Length >= 2)
                {
                    string roomId = parts[0];
                    string roomName = parts[1];

                    availableRooms.Add(room);
                    roomIdToName[roomId] = roomName;

                    Console.WriteLine($"  - ID: {roomId}, Название: {roomName}");
                }
            }

            // Отладочная информация о текущей комнате
            if (!string.IsNullOrEmpty(currentRoom))
            {
                if (roomIdToName.ContainsKey(currentRoom))
                {
                    //   Console.WriteLine($"[СИСТЕМА] Вы находитесь в комнате: {roomIdToName[currentRoom]} (ID: {currentRoom})");
                }
                else
                {
                    //   Console.WriteLine($"[СИСТЕМА] Вы находитесь в комнате ID: {currentRoom} (не в списке)");
                }
            }
        }
        public void ProcessCommand(string input)
        {
            string[] parts = input.Split(' ');
            string command = parts[0].ToLower();
            string parameter = parts.Length > 1 ? parts[1] : "";

            switch (command)
            {
                case "/exit":
                    Disconnect();
                    Environment.Exit(0);
                    break;

                case "/help":
                    ShowHelp();
                    break;

                case "/status":
                    ShowStatus();
                    break;

                case "/mic":
                    ToggleMicrophone();
                    break;

                case "/stream":
                    StartScreenStreaming();
                    break;

                case "/stopstream":
                    StopScreenStreaming();
                    break;

                case "/watch":
                    StartWatchingScreen();
                    break;

                case "/stopwatch":
                    StopWatchingScreen();
                    break;

                case "/streamquality":
                    if (int.TryParse(parameter, out int quality) && quality >= 1 && quality <= 100)
                    {
                        screenQuality = quality;
                        //    Console.WriteLine($"[ВИДЕО] Качество установлено: {quality}%");
                    }
                    else
                    {
                        //      Console.WriteLine("[ВИДЕО] Использование: /streamquality 1-100");
                    }
                    break;

                case "/streamfps":
                    if (int.TryParse(parameter, out int fps) && fps >= 1 && fps <= 30)
                    {
                        screenCaptureInterval = 1000 / fps;
                        //     Console.WriteLine($"[ВИДЕО] Частота кадров установлена: {fps} FPS");
                    }
                    else
                    {
                        //    Console.WriteLine("[ВИДЕО] Использование: /streamfps 1-30");
                    }
                    break;

                case "/streamsize":
                    var sizeParts = parameter.Split('x');
                    if (sizeParts.Length == 2 &&
                        int.TryParse(sizeParts[0], out int width) &&
                        int.TryParse(sizeParts[1], out int height) &&
                        width > 0 && height > 0)
                    {
                        screenWidth = width;
                        screenHeight = height;
                        //    Console.WriteLine($"[ВИДЕО] Размер установлен: {width}x{height}");
                    }
                    else
                    {
                        //    Console.WriteLine("[ВИДЕО] Использование: /streamsize ШИРИНАxВЫСОТА (например: 800x600)");
                    }
                    break;

                case "/join":
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        // Если уже в комнате, сначала выходим
                        if (!string.IsNullOrEmpty(currentRoom) && parameter != currentRoom)
                        {
                            //       Console.WriteLine($"[СИСТЕМА] Вы уже в комнате {currentRoom}. Сначала используйте /leave");
                        }
                        else
                        {
                            SendMessage($"/join {parameter}");
                        }
                    }
                    else
                    {
                        //   Console.WriteLine("[СИСТЕМА] Использование: /join ID_комнаты [пароль]");
                    }
                    break;

                case "/create":
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        SendMessage($"/create {parameter}");
                    }
                    else
                    {
                        //  Console.WriteLine("[СИСТЕМА] Использование: /create название_комнаты [пароль]");
                    }
                    break;

                case "/private":
                    if (!string.IsNullOrEmpty(parameter))
                    {
                        //   SendMessage($"/private {parameter}");
                    }
                    else
                    {
                        //  Console.WriteLine("[СИСТЕМА] Использование: /private имя_пользователя");
                    }
                    break;

                case "/leave":
                    SendMessage("/leave");
                    break;

                case "/endcall":
                    SendMessage("/endcall");
                    isInCall = false;
                    Thread.Sleep(300);
                    if (isAudioEnabled && string.IsNullOrEmpty(currentRoom))
                    {
                        DisableMicrophone();
                    }
                    break;

                default:
                    SendMessage(input);
                    break;
            }
        }
        private void ShowStatus()
        {
            //Console.WriteLine("\n=== ТЕКУЩИЙ СТАТУС ===");
            //Console.WriteLine($"Пользователь: {username}");

            string roomInfo = "Нет";
            if (!string.IsNullOrEmpty(currentRoom))
            {
                roomInfo = roomIdToName.ContainsKey(currentRoom)
                    ? $"{roomIdToName[currentRoom]} (ID: {currentRoom})"
                    : $"ID: {currentRoom}";
            }
            //Console.WriteLine($"Комната: {roomInfo}");

            //Console.WriteLine($"Микрофон: {(isAudioEnabled ? "ВКЛ" : "ВЫКЛ")}");
            //Console.WriteLine($"Звонок: {(isInCall ? "Активен" : "Нет")}");
            //Console.WriteLine($"Демонстрация экрана: {(isStreamingScreen ? "Активна" : "Неактивна")}");
            //Console.WriteLine($"Просмотр демонстрации: {(isWatchingScreen ? "Активен" : "Неактивен")}");
            //Console.WriteLine($"Доступных комнат: {availableRooms.Count}");

            if (!string.IsNullOrEmpty(currentRoom))
            {
                //     Console.WriteLine($"\nКоманды для комнаты {currentRoom}:");
                //     Console.WriteLine("  /stream - начать демонстрацию экрана");
                //     Console.WriteLine("  /watch - смотреть демонстрацию других");
                //     Console.WriteLine("  /leave - покинуть комнату");
            }
            //  Console.WriteLine("====================\n");
        }
        private void ShowPrompt()
        {
            string status;
            ConsoleColor color;

            if (isInCall)
            {
                status = "ЗВОНОК";
                color = ConsoleColor.Red;
            }
            else if (!string.IsNullOrEmpty(currentRoom))
            {
                status = "КОМНАТА";
                color = ConsoleColor.Yellow;
            }
            else
            {
                status = "ОБЩИЙ";
                color = ConsoleColor.Green;
            }

            //    Console.ForegroundColor = color;
            //   Console.Write($"[{status}] {username}");

            // Статус микрофона
            //   Console.ForegroundColor = isAudioEnabled ? ConsoleColor.Green : ConsoleColor.Red;
            //   Console.Write($" [MIC: {(isAudioEnabled ? "ON" : "OFF")}]");

            // Статус стрима
            //  Console.ForegroundColor = isStreamingScreen ? ConsoleColor.Cyan : ConsoleColor.Gray;
            //  Console.Write($" [СТРИМ: {(isStreamingScreen ? "ON" : "OFF")}]");

            // Статус просмотра
            //   Console.ForegroundColor = isWatchingScreen ? ConsoleColor.Magenta : ConsoleColor.Gray;
            //   Console.Write($" [ПРОСМОТР: {(isWatchingScreen ? "ON" : "OFF")}]");

            //   Console.ForegroundColor = color;
            //   Console.Write(" > ");
            //  Console.ResetColor();
        }
        private void ShowHelp()
        {
            string help = @"
            === КОМАНДЫ ===
Общие:
/status - текущий статус (рекомендуется!)
/users - список пользователей
/rooms - список комнат
/msg имя сообщение - личное сообщение
/mic - вкл/выкл микрофон
/exit - выход

Комнаты:
/create название [пароль] - создать комнату (автоматический вход)
/join ID [пароль] - войти в комнату по ID
/leave - покинуть комнату

Демонстрация экрана:
/stream - начать демонстрацию своего экрана (только в комнате)
/stopstream - остановить демонстрацию
/watch - начать просмотр демонстрации в комнате
/stopwatch - прекратить просмотр
/streamquality 1-100 - качество изображения (по умолчанию 50)
/streamfps 1-30 - частота кадров (по умолчанию 10)
/streamsize ШИРИНАxВЫСОТА - размер изображения (по умолчанию 1024x768)

Звонки:
/private имя - приватный звонок
/endcall - завершить звонок

ВАЖНО:
- При создании комнаты вы автоматически в нее входите
- Используйте /status чтобы проверить текущую комнату
- Для демонстрации экрана нужно быть в комнате
- Полученные кадры демонстрации сохраняются в файлы screen_frame_*.jpg
";
            Console.WriteLine(help);
        }





        #region Управление текстом
        private void ReceiveTextMessages()
        {
            byte[] buffer = new byte[4096];

            while (isRunning && textClient.Connected)
            {
                try
                {
                    int bytesRead = textStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessServerMessage(message);
                    }
                }
                catch { break; }
            }
        }
        public void SendCommand(string command)
        {
            if (textStream != null && textClient.Connected)
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(command);
                    textStream.Write(buffer, 0, buffer.Length);
                    textStream.Flush();
                }
                catch (Exception ex)
                {
                    // Обработка ошибок
                    MessageBox.Show($"Ошибка отправки команды: {ex.Message}");
                }
            }
        }
        public void SendMessage(string message)
        {
            if (textStream != null && textClient.Connected)
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    textStream.Write(buffer, 0, buffer.Length);
                    textStream.Flush();
                }
                catch (Exception ex)
                {
                    //    Console.WriteLine($"[СИСТЕМА] Ошибка отправки: {ex.Message}");
                }
            }
        }
        private void DisplayMessage(string message)
        {
            Console.WriteLine($"\n{message}");
        }
        #endregion


        #region Управление аудио
        private void InitializeAudio()
        {
            try
            {
                waveProvider = new BufferedWaveProvider(new WaveFormat(SAMPLE_RATE, CHANNELS));
                waveOut = new WaveOutEvent();
                waveOut.Init(waveProvider);
                waveOut.Play();

                waveIn = new WaveInEvent
                {
                    DeviceNumber = 0,
                    WaveFormat = new WaveFormat(SAMPLE_RATE, CHANNELS),
                    BufferMilliseconds = BUFFER_MS
                };

                waveIn.DataAvailable += WaveIn_DataAvailable;

                //     Console.WriteLine("[СИСТЕМА] Аудиоустройства инициализированы");
            }
            catch (Exception ex)
            {
                //        Console.WriteLine($"[СИСТЕМА] Ошибка инициализации аудио: {ex.Message}");
            }
        }
        private void ReceiveAudio()
        {
            byte[] buffer = new byte[AUDIO_BUFFER_SIZE + 4];

            while (isRunning && audioClient.Connected)
            {
                try
                {
                    // Читаем размер пакета
                    int bytesRead = 0;
                    while (bytesRead < 4 && audioClient.Connected)
                    {
                        bytesRead += audioStream.Read(buffer, bytesRead, 4 - bytesRead);
                    }

                    if (bytesRead < 4) continue;

                    int packetSize = BitConverter.ToInt32(buffer, 0);

                    // Читаем аудиоданные
                    bytesRead = 0;
                    while (bytesRead < packetSize && audioClient.Connected)
                    {
                        bytesRead += audioStream.Read(buffer, 4 + bytesRead, packetSize - bytesRead);
                    }

                    if (bytesRead == packetSize)
                    {
                        byte[] audioData = new byte[packetSize];
                        Buffer.BlockCopy(buffer, 4, audioData, 0, packetSize);
                        PlayAudio(audioData);
                    }
                }
                catch { Thread.Sleep(10); }
            }
        }
        private void PlayAudio(byte[] audioData)
        {
            if (waveProvider == null || audioData.Length == 0) return;

            try
            {
                waveProvider.AddSamples(audioData, 0, audioData.Length);
            }
            catch { }
        }
        private void ToggleMicrophone()
        {
            if (isAudioEnabled)
            {
                DisableMicrophone();
            }
            else if (isInCall || !string.IsNullOrEmpty(currentRoom))
            {
                EnableMicrophone();
            }
            else
            {
                //      Console.WriteLine("[СИСТЕМА] Микрофон можно включить только во время звонка или в комнате");
            }
        }
        private void EnableMicrophone()
        {
            if (isAudioEnabled || waveIn == null) return;

            try
            {
                waveIn.StartRecording();
                isAudioEnabled = true;
                //   Console.WriteLine("[СИСТЕМА] Микрофон ВКЛЮЧЕН");
            }
            catch (Exception ex)
            {
                //  Console.WriteLine($"[СИСТЕМА] Ошибка включения микрофона: {ex.Message}");
            }
        }
        private void DisableMicrophone()
        {
            if (!isAudioEnabled || waveIn == null) return;

            try
            {
                waveIn.StopRecording();
                isAudioEnabled = false;
                //   Console.WriteLine("[СИСТЕМА] Микрофон ВЫКЛЮЧЕН");
            }
            catch (Exception ex)
            {
                //   Console.WriteLine($"[СИСТЕМА] Ошибка выключения микрофона: {ex.Message}");
            }
        }

        #endregion



        #region Демонстрация экрана
        private void ReceiveVideo()
        {
            byte[] buffer = new byte[1024];

            while (isRunning && videoClient.Connected)
            {
                try
                {
                    int bytesRead = videoStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string command = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        ProcessVideoCommand(command);
                    }
                }
                catch { Thread.Sleep(10); }
            }
        }
        private void ReceiveVideoFrame(int frameSize)
        {
            try
            {
                byte[] frameData = new byte[frameSize];
                int totalRead = 0;

                while (totalRead < frameSize && videoClient.Connected)
                {
                    int bytesRead = videoStream.Read(frameData, totalRead, frameSize - totalRead);
                    if (bytesRead == 0) break;
                    totalRead += bytesRead;
                }

                if (totalRead == frameSize && isWatchingScreen)
                {
                    // Сохраняем кадр в файл для тестирования
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff");
                    string filename = $"screen_frame_{timestamp}.jpg";
                    File.WriteAllBytes(filename, frameData);

                    //     Console.WriteLine($"[ВИДЕО] Получен кадр: {frameSize} байт, сохранен в {filename}");

                    // Можно автоматически открыть файл для просмотра
                    try
                    {
                        Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                //     Console.WriteLine($"[ВИДЕО] Ошибка получения кадра: {ex.Message}");
            }
        }
        public void StartScreenStreaming()
        {
            if (string.IsNullOrEmpty(currentRoom))
            {
                //    Console.WriteLine("[СИСТЕМА] Вы должны быть в комнате для демонстрации экрана");
                return;
            }

            if (isStreamingScreen)
            {
                //      Console.WriteLine("[СИСТЕМА] Демонстрация уже запущена");
                return;
            }

            // Отправляем команду серверу
            SendVideoCommand($"START_STREAM:{currentRoom}");

            isStreamingScreen = true;

            // Запускаем захват экрана
            screenCaptureThread = new Thread(CaptureScreen);
            screenCaptureThread.Start();

            //     Console.WriteLine($"[СИСТЕМА] Демонстрация экрана запущена в комнате {currentRoom}");
        }
        public void StopScreenStreaming()
        {
            if (!isStreamingScreen) return;

            isStreamingScreen = false;

            // Останавливаем поток захвата
            if (screenCaptureThread != null && screenCaptureThread.IsAlive)
            {
                screenCaptureThread.Join(1000);
            }

            // Отправляем команду серверу
            SendVideoCommand("STOP_STREAM");

            //      Console.WriteLine("[СИСТЕМА] Демонстрация экрана остановлена");
        }
        private void CaptureScreen()
        {
            try
            {
                // Получаем кодировщик JPEG
                ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                if (jpegCodec == null)
                {
                    //   Console.WriteLine("[ВИДЕО] Не удалось найти кодировщик JPEG");
                    return;
                }
                System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(myEncoder, screenQuality);

                while (isStreamingScreen && isRunning && videoClient.Connected)
                {
                    try
                    {
                        DateTime captureStart = DateTime.Now;

                        // Создаем тестовое изображение
                        byte[] imageData = CaptureRealScreen();

                        // Отправляем на сервер
                        SendVideoFrame(imageData);

                        DateTime captureEnd = DateTime.Now;
                        TimeSpan captureTime = captureEnd - captureStart;

                        // Регулируем задержку для поддержания FPS
                        int delay = Math.Max(0, screenCaptureInterval - (int)captureTime.TotalMilliseconds);
                        if (delay > 0)
                        {
                            Thread.Sleep(delay);
                        }
                    }
                    catch (Exception ex)
                    {
                        //     Console.WriteLine($"[ВИДЕО] Ошибка захвата экрана: {ex.Message}");
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                //    Console.WriteLine($"[ВИДЕО] Ошибка в потоке захвата: {ex.Message}");
            }
        }
        private byte[] CaptureRealScreen()
        {
            using (Bitmap bitmap = new Bitmap(screenWidth, screenHeight))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    // Заливаем фон
                    g.Clear(System.Drawing.Color.DarkBlue);

                    // Рисуем текст
                    Font font = new Font("Arial", 24, System.Drawing.FontStyle.Bold);
                    string text = $"Демонстрация экрана: {username}";
                    SizeF textSize = g.MeasureString(text, font);

                    // Центрируем текст
                    float x = (screenWidth - textSize.Width) / 2;
                    float y = (screenHeight - textSize.Height) / 2;

                    // Тень
                    g.DrawString(text, font, System.Drawing.Brushes.Black, x + 2, y + 2);
                    // Основной текст
                    g.DrawString(text, font, System.Drawing.Brushes.White, x, y);

                    // Время
                    string timeText = DateTime.Now.ToString("HH:mm:ss");
                    Font timeFont = new Font("Arial", 18);
                    SizeF timeSize = g.MeasureString(timeText, timeFont);
                    g.DrawString(timeText, timeFont, System.Drawing.Brushes.Yellow,
                        screenWidth - timeSize.Width - 10, 10);

                    // Статистика
                    string stats = $"Качество: {screenQuality}% | FPS: {1000 / screenCaptureInterval}";
                    Font statsFont = new Font("Arial", 14);
                    g.DrawString(stats, statsFont, System.Drawing.Brushes.LightGreen, 10, 10);
                }

                // Конвертируем в JPEG
                using (MemoryStream ms = new MemoryStream())
                {
                    ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                    if (jpegCodec != null)
                    {
                        System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                        EncoderParameters encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(myEncoder, screenQuality);
                        bitmap.Save(ms, jpegCodec, encoderParams);
                    }
                    else
                    {
                        bitmap.Save(ms, ImageFormat.Jpeg);
                    }
                    return ms.ToArray();
                }
            }
            /* try
             {
                 // Для Windows Forms
                 using (Bitmap screenshot = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height))
                 {
                     using (Graphics g = Graphics.FromImage(screenshot))
                     {
                         g.CopyFromScreen(0, 0, 0, 0, screenshot.Size);
                     }

                     // Изменяем размер
                     using (Bitmap resized = new Bitmap(screenshot, screenWidth, screenHeight))
                     {
                         using (MemoryStream ms = new MemoryStream())
                         {
                             ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                             if (jpegCodec != null)
                             {

                                 System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                                 EncoderParameters encoderParams = new EncoderParameters(1);
                                 encoderParams.Param[0] = new EncoderParameter(myEncoder, screenQuality);

                                 resized.Save(ms, jpegCodec, encoderParams);
                             }
                             else
                             {
                                 resized.Save(ms, ImageFormat.Jpeg);
                             }
                             return ms.ToArray();
                         }
                     }
                 }
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[ВИДЕО] Ошибка захвата экрана: {ex.Message}");
                 return CaptureRealScreen(); // Возвращаем тестовое изображение при ошибке
             }*/
        }
        private void SendVideoFrame(byte[] imageData)
        {
            if (videoStream != null && videoClient.Connected && !string.IsNullOrEmpty(currentRoom))
            {
                try
                {
                    // Отправляем команду с размером кадра
                    string header = $"FRAME:{currentRoom}:{imageData.Length}:";
                    byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                    videoStream.Write(headerBytes, 0, headerBytes.Length);

                    // Отправляем данные кадра
                    videoStream.Write(imageData, 0, imageData.Length);
                    videoStream.Flush();

                    // Периодически выводим информацию (каждые 10 кадров)
                    if (DateTime.Now.Second % 2 == 0) // Простая проверка для редкого вывода
                    {
                        //      Console.WriteLine($"[ВИДЕО] Отправлен кадр в комнату {currentRoom}: {imageData.Length} байт");
                    }
                }
                catch (Exception ex)
                {
                    //     Console.WriteLine($"[ВИДЕО] Ошибка отправки кадра: {ex.Message}");
                }
            }
        }
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            try
            {
                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                foreach (ImageCodecInfo codec in codecs)
                {
                    if (codec.FormatID == format.Guid)
                    {
                        return codec;
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        public void StartWatchingScreen()
        {
            if (string.IsNullOrEmpty(currentRoom))
            {
                //    Console.WriteLine("[СИСТЕМА] Вы должны быть в комнате для просмотра демонстрации");
                return;
            }

            if (isWatchingScreen)
            {
                //     Console.WriteLine("[СИСТЕМА] Вы уже смотрите демонстрацию");
                return;
            }

            // Отправляем команду серверу
            SendVideoCommand($"START_WATCH:{currentRoom}");

            isWatchingScreen = true;
            //   Console.WriteLine($"[СИСТЕМА] Начали просмотр демонстрации в комнате {currentRoom}");
        }
        public void StopWatchingScreen()
        {
            if (!isWatchingScreen) return;

            SendVideoCommand("STOP_WATCH");
            isWatchingScreen = false;
            //   Console.WriteLine("[СИСТЕМА] Прекратили просмотр демонстрации");
        }
        private void SendVideoCommand(string command)
        {
            if (videoStream != null && videoClient.Connected)
            {
                try
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(command);
                    videoStream.Write(buffer, 0, buffer.Length);
                    videoStream.Flush();
                    //    Console.WriteLine($"[ВИДЕО] Отправлена команда: {command}");
                }
                catch (Exception ex)
                {
                    //     Console.WriteLine($"[ВИДЕО] Ошибка отправки команды: {ex.Message}");
                }
            }
        }
        private void ProcessVideoCommand(string command)
        {
            string[] parts = command.Split(':');
            string cmd = parts[0];

            switch (cmd)
            {
                case "STREAM_STARTED":
                    //     Console.WriteLine("\n[ВИДЕО] Подключение к трансляции...");
                    break;

                case "STREAM_STOPPED":
                    //     Console.WriteLine("\n[ВИДЕО] Трансляция завершена");
                    isWatchingScreen = false;
                    break;

                case "FRAME":
                    if (parts.Length > 1)
                    {
                        int frameSize = int.Parse(parts[1]);
                        ReceiveVideoFrame(frameSize);
                    }
                    break;

                case "NEW_VIEWER":
                    if (parts.Length > 1)
                    {
                        //        Console.WriteLine($"[ВИДЕО] {parts[1]} начал смотреть ваш экран");
                    }
                    break;

                case "VIEWER_LEFT":
                    if (parts.Length > 1)
                    {
                        //     Console.WriteLine($"[ВИДЕО] {parts[1]} перестал смотреть ваш экран");
                    }
                    break;
            }
        }

        #endregion

        private string GetInitialsFromUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
                return "??";

            var parts = username.Split(' ');
            if (parts.Length >= 2)
            {
                // Если есть имя и фамилия
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            }
            else if (parts.Length == 1 && parts[0].Length >= 2)
            {
                // Если только одно слово
                return parts[0].Substring(0, 2).ToUpper();
            }

            return "??";
        }


    }
    public class UserStatusEventArgs : EventArgs
    {
        public string Username { get; }
        public bool IsOnline { get; }
        public string StatusText { get; }

        public UserStatusEventArgs(string username, bool isOnline, string statusText = "")
        {
            Username = username;
            IsOnline = isOnline;
            StatusText = statusText;
        }
    }
}
