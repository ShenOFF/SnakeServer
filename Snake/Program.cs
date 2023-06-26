using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using System.Threading;
using System.IO;

namespace Snake
{
    internal class Program
    {
        /// <summary> Коллекция рекордов
        public static List<Leaders> Leaders = new List<Leaders>();
        /// </// <summary> Коллекция View Model User Setting, содержащая IP адрес игрока, порт,
        public static List<ViewModelUserSettings> remoteIPAddress = new List<ViewModelUserSettings>();
        /// <summary> Коллекция ViewModelGames, содержащая точки змеи, точку на карте
        public static List<ViewModelGames> viewModelGames = new List<ViewModelGames>();
        /// <summary> Локальный порт, который прослушивается для ответов
        private static int localPort = 5001;
        /// <summary> Максимальная скорость движения змейки
        public static int MaxSpeed = 15;
        static void Main(string[] args)
        {
            try
            {
                // Создаем поток для прослушивания
                Thread tRec = new Thread(new ThreadStart(Receiver));
                // Запускаем поток прослушивания
                tRec.Start();
                // Создаём таймер для управления игрой
                Thread tTime = new Thread(Timer);
                // Запускаем таймер для управления игрой
                tTime.Start();
            }// Если что-то пошло не так, выводим сообщение о том что
            catch (Exception ex)
            {
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }
        private static void Send()
        {
            foreach (ViewModelUserSettings User in remoteIPAddress)
            {
                // Создаем UdpClient
                UdpClient sender = new UdpClient();
                // Создаем endPoint по информации об удаленном хосте
                IPEndPoint endPoint = new IPEndPoint(
                    IPAddress.Parse(User.IPAddress),
                    int.Parse(User.Port));

                try
                {
                    // Преобразуем данные в массив байтов
                    byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelGames));

                    // Отправляем данные
                    sender.Send(bytes, bytes.Length, endPoint);
                    Console.WriteLine($"Отправил данные пользователю: {User.IPAddress}:{User.Port}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
                }
                finally
                {
                    sender.Close();
                }
            }
        }
        public static void Receiver()
        {
            // Создаем UdpClient для чтения входящих данных
            UdpClient receivingUdpClient = new UdpClient(localPort);
            // Конечная сетевая точка
            IPEndPoint RemoteIpEndPoint = null;

            try
            {
                Console.WriteLine("Команды сервера:");

                while (true)
                {
                    // Ожидание дейтаграммы
                    byte[] receiveBytes = receivingUdpClient.Receive(
                       ref RemoteIpEndPoint);

                    // Преобразуем и отображаем данные
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    Console.WriteLine("Получил команду: " + returnData.ToString());

                    // начало игры
                    if (returnData.ToString().Contains("/start"))
                    {
                        // делим данные на команду и данные Json
                        string[] dataMessage = returnData.ToString().Split('|');
                        // Конвертируем данные в модель
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        Console.WriteLine($"Подключился пользователь: {viewModelUserSettings.IPAddress}:{viewModelUserSettings.Port}");
                        // Добавляем данные в коллекцию для того, чтобы отправлять пользователю
                        remoteIPAddress.Add(viewModelUserSettings);
                        // добавляем змею
                        viewModelUserSettings.IdSnake = AddSnake();
                        // связываем змею и игрока
                        viewModelGames[viewModelUserSettings.IdSnake].IdSnake = viewModelUserSettings.IdSnake;
                    }
                    else
                    {
                        // управление змеёй
                        string[] dataMessage = returnData.ToString().Split('|');
                        // Конвертируем данные в модель
                        ViewModelUserSettings viewModelUserSettings = JsonConvert.DeserializeObject<ViewModelUserSettings>(dataMessage[1]);
                        int IdPlayer = -1;

                        IdPlayer = remoteIPAddress.FindIndex(x => x.IPAddress == viewModelUserSettings.IPAddress
                            && x.Port == viewModelUserSettings.Port);

                        if (IdPlayer != -1)
                        {
                            if (dataMessage[0] == "Up" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Down)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Up;
                            else if (dataMessage[0] == "Down" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Up)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Down;
                            else if (dataMessage[0] == "Left" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Right)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Left;
                            else if (dataMessage[0] == "Right" &&
                                viewModelGames[IdPlayer].SnakesPlayers.direction != Snakes.Direction.Left)
                                viewModelGames[IdPlayer].SnakesPlayers.direction = Snakes.Direction.Right;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }
        public static int AddSnake()
        {
            // Создаём змею пользователю
            ViewModelGames viewModelGamesPlayer = new ViewModelGames();
            //Указывает стартовые координаты змеи
            viewModelGamesPlayer.SnakesPlayers = new Snakes()
            {
                //Точка змеи
                Points = new List<Snakes.Point>() {
                        new Snakes.Point() { X = 30, Y = 10 },
                        new Snakes.Point() { X = 20, Y = 10 },
                        new Snakes.Point() { X = 10, Y = 10 },
                },
                //Направление змеи
                direction = Snakes.Direction.Start
            };
            //Создание рандомной точки на карте
            viewModelGamesPlayer.Points = new Snakes.Point(new Random().Next(10, 783), new Random().Next(10, 410));
            //Добавление змеи в общей список всех змей
            viewModelGames.Add(viewModelGamesPlayer);
            //Возвращение ID змеи чтобы связать игрока и змею
            return viewModelGames.FindIndex(x => x == viewModelGamesPlayer);
        }
        public static void Timer()
        {

            while (true)
            {
                Thread.Sleep(1000);


                // Получаем удалённых змей
                List<ViewModelGames> RemoteSnakes = viewModelGames.FindAll(x => x.SnakesPlayers.GameOver);
                if (RemoteSnakes.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    // Перебираем удалённых змей
                    foreach (ViewModelGames DeadSnake in RemoteSnakes)
                    {
                        Console.WriteLine($"Отключил пользоватлеля: {remoteIPAddress.Find(x => x.IdSnake == DeadSnake.IdSnake).IPAddress}:{remoteIPAddress.Find(x => x.IdSnake == DeadSnake.IdSnake).Port}");
                        // Удаляем пользователя
                        remoteIPAddress.RemoveAll(x => x.IdSnake == DeadSnake.IdSnake);
                    }
                    viewModelGames.RemoveAll(x => x.SnakesPlayers.GameOver);
                }

                foreach (ViewModelUserSettings User in remoteIPAddress)
                {
                    Snakes Snake = viewModelGames.Find(x => x.IdSnake == User.IdSnake).SnakesPlayers;
                    for (int i = Snake.Points.Count - 1; i >= 0; i--)
                    {
                        if (i != 0)
                        {
                            Snake.Points[i] = Snake.Points[i - 1];
                        }
                        else
                        {
                            int Speed = 10 + (int)Math.Round(Snake.Points.Count / 20f);
                            if (Speed > MaxSpeed) Speed = MaxSpeed;


                            if (Snake.direction == Snakes.Direction.Right)
                            {
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X + Speed, Y = Snake.Points[i].Y };
                            }
                            else if (Snake.direction == Snakes.Direction.Down)
                            {
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X, Y = Snake.Points[i].Y + Speed };
                            }
                            else if (Snake.direction == Snakes.Direction.Up)
                            {
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X, Y = Snake.Points[i].Y - Speed };
                            }
                            else if (Snake.direction == Snakes.Direction.Left)
                            {
                                Snake.Points[i] = new Snakes.Point() { X = Snake.Points[i].X - Speed, Y = Snake.Points[i].Y };
                            }
                        }
                    }

                    // проверяем змею на столкновение с препядствием
                    if (Snake.Points[0].X <= 0 || Snake.Points[0].X >= 793)
                    {
                        // игра окончена
                        Snake.GameOver = true;
                    }
                    else if (Snake.Points[0].Y <= 0 || Snake.Points[0].Y >= 420)
                    {
                        // игра окончена
                        Snake.GameOver = true;
                    }

                    // проверяем что мы не столкнулись сами с собой
                    if (Snake.direction != Snakes.Direction.Start)
                    {
                        for (int i = 1; i < Snake.Points.Count; i++)
                        {
                            if (Snake.Points[0].X >= Snake.Points[i].X - 1 && Snake.Points[0].X <= Snake.Points[i].X + 1)
                            {
                                if (Snake.Points[0].Y >= Snake.Points[i].Y - 1 && Snake.Points[0].Y <= Snake.Points[i].Y + 1)
                                {
                                    // игра окончена
                                    Snake.GameOver = true;
                                    break;
                                }
                            }
                        }
                    }

                    // проверяем змею на съедание точки
                    if (Snake.Points[0].X >= viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points.X - 10 && Snake.Points[0].X <= viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points.X + 10)
                    {
                        if (Snake.Points[0].Y >= viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points.Y - 10 && Snake.Points[0].Y <= viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points.Y + 10)
                        {
                            // создаём новую точку
                            viewModelGames.Find(x => x.IdSnake == User.IdSnake).Points = new Snakes.Point(new Random().Next(10, 783), new Random().Next(10, 410));
                            // увеличиваем змею
                            Snake.Points.Add(new Snakes.Point()
                            {
                                X = Snake.Points[Snake.Points.Count - 1].X,
                                Y = Snake.Points[Snake.Points.Count - 1].Y
                            });

                            // загружаем таблицу
                            LoadLeaders();
                            // добавляем нас в таблицу
                            Leaders.Add(new Leaders()
                            {
                                Name = User.Name,
                                Points = Snake.Points.Count - 3
                            });
                            // сортирируем таблицу по двум значениям
                            Leaders = Leaders.OrderByDescending(x => x.Points).ThenBy(x => x.Name).ToList();
                            // ищем себя в списке
                            viewModelGames.Find(x => x.IdSnake == User.IdSnake).Top = Leaders.FindIndex(x => x.Points == Snake.Points.Count - 3 && x.Name == User.Name) + 1;

                        }
                    }

                    if (Snake.GameOver)
                    {
                        // загружаем таблицу
                        LoadLeaders();
                        // добавляем нас в таблицу
                        Leaders.Add(new Leaders()
                        {
                            Name = User.Name,
                            Points = Snake.Points.Count - 3
                        });
                        SaveLeaders();
                    }
                }

                Send();
            }
        }
        public static void SaveLeaders()
        {
            // Преобразуем данные игроков в JSON
            string json = JsonConvert.SerializeObject(Leaders);
            // Записываем в файл
            StreamWriter SW = new StreamWriter("./leaders.txt");
            // Пишем строку
            SW.WriteLine(json);
            // Закрываем файл
            SW.Close();
        }
        public static void LoadLeaders()
        {// Проверяем что есть файл
            if (File.Exists("./leaders.txt"))
            {// // Открываем файл
                StreamReader SR = new StreamReader("./leaders.txt");
                // читаем первую строку
                string json = SR.ReadLine();
                // Закрываем файл
                SR.Close();
                // Если есто что читать
                if (!string.IsNullOrEmpty(json))
                    // Преобразуем троку в объект
                    Leaders = JsonConvert.DeserializeObject<List<Leaders>>(json);
                // Возвращаем пустой результат
                else
                    // Возвращаем пустой результат
                    Leaders = new List<Leaders>();
            }
            else
                Leaders = new List<Leaders>();
        }
    }
}
