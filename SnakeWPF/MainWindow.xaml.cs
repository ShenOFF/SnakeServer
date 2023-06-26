using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SnakeWPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow mainWindow;
        public ViewModelUserSettings ViewModelUserSettings = new ViewModelUserSettings();
        public List<ViewModelGames> ViewModelGames = null;
        public static IPAddress remoteIPAddress = IPAddress.Parse("127.0.0.1");
        public static int remotePort = 5001;
        public Thread tRec;
        public UdpClient receivingUdpClient;
        public Pages.Home Home = new Pages.Home();
        public Pages.Game Game = new Pages.Game();

        public MainWindow()
        {
            InitializeComponent();
            //Запоминаем MainWindows в переменную
            mainWindow = this;
            //Открываем начальную страницу
            OpenPage(Home);
        }
        public void StartReceiver()
        {
            // Мощдаем поток для прослушивания куанала
            tRec = new Thread(new ThreadStart(Receiver));
            //Запускаем поток
            tRec.Start();
        }
        public void OpenPage(Page PageOpen)
        {
            // Создаём анимацию
            DoubleAnimation startAnimation = new DoubleAnimation();
            // Задаём начальное значение анимации
            startAnimation.From = 1;
            // Задаём кренечное значение анимации
            startAnimation.To = 0;
            // Задаём время анимации
            startAnimation.Duration = TimeSpan.FromSeconds(0.6);
            // // Подписываемся на выполнение анимации
            startAnimation.Completed += delegate
            {
                frame.Navigate(PageOpen);
                DoubleAnimation endAnimation = new DoubleAnimation();
                // Задаём начальное значение анимации
                endAnimation.From = 0;
                // Задаём конечное значение анимации
                endAnimation.To = 1;
                // Задаём время анимации
                endAnimation.Duration = TimeSpan.FromSeconds(0.6);
                frame.BeginAnimation(OpacityProperty, endAnimation);
            };
            // Воспризводим анимаацию на frame, анимация прозрачности
            frame.BeginAnimation(OpacityProperty, startAnimation);
        }
        public void Receiver()
        {
            // Создаём клиент для прослушивания
            receivingUdpClient = new UdpClient(int.Parse(ViewModelUserSettings.Port));
            // Конечная сетевая точка
            IPEndPoint RemoteIpEndPoint = null;

            try
            {
                // Слушаем постоянно
                while (true)
                {
                    // Ожидание дейтаграммы
                    byte[] receiveBytes = receivingUdpClient.Receive(
                       ref RemoteIpEndPoint);

                    // Преобразуем и отображаем данные
                    string returnData = Encoding.UTF8.GetString(receiveBytes);
                    // Если у нас не существует данных от сервера (значит мы тольконачали игру и нам необходимо сменить экран
                    if (ViewModelGames == null)
                    {
                        // Говорим что выполняем вне потока
                        Dispatcher.Invoke(() =>
                        {
                            // Открываем окно с игрой
                            OpenPage(Game);
                        });
                    }

                    // Конвертируем данные в модель
                    ViewModelGames = JsonConvert.DeserializeObject<List<ViewModelGames>>(returnData.ToString());
                    // Если игрок проиграл
                    if (ViewModelGames.Any(x => x.SnakesPlayers.GameOver))
                    {
                        // Выполняем вне потока
                        Dispatcher.Invoke(() =>
                        {
                            // Открываем окно с окончанием игры
                            OpenPage(new Pages.EndGame());
                        });
                    }
                    else// Вызываем создание U
                    {
                        Game.CreateUI();
                    }
                }// если что - то пошло не по плану, выводим ошибку в кончоль проекта
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
        }
        public static void Send(string datagram)
        {
            // Создаем UdpClient
            UdpClient sender = new UdpClient();
            // Создаем endPoint по информации об удаленном хосте
            IPEndPoint endPoint = new IPEndPoint(remoteIPAddress, remotePort);
            try
            {
                // Преобразуем данные в массив байтов
                byte[] bytes = Encoding.UTF8.GetBytes(datagram);

                // Отправляем данные
                sender.Send(bytes, bytes.Length, endPoint);
                
                
            }
            catch (Exception ex)
            {//Если что то пошло не по плану, выводи ошибку в консоль прилодения
                Debug.WriteLine("Возникло исключение: " + ex.ToString() + "\n  " + ex.Message);
            }
            finally
            {
                // Закрыть соединение
                sender.Close();
            }
        }
        private void EventKeyUp(object sender, KeyEventArgs e)
        {// Проверяем что у игрока есть IP 
            if (!string.IsNullOrEmpty(ViewModelUserSettings.IPAddress) &&
                !string.IsNullOrEmpty(ViewModelUserSettings.Port) &&
                (ViewModelGames != null && !ViewModelGames.Any(x => x.SnakesPlayers.GameOver)))
            {
                // Если нажата клавиша вверх
                if (e.Key == Key.Up)// Отправляем на сервер сообщение о том что команда вверх и данные игрока
                    Send($"Up|{JsonConvert.SerializeObject(ViewModelUserSettings)}");
                // Если нажата клавиша вниз
                else if (e.Key == Key.Down)// Отправляем на сервер сообщение о том что команда вниз и данные игрока
                    Send($"Down|{JsonConvert.SerializeObject(ViewModelUserSettings)}");
                // Если нажата клавиша влево
                else if (e.Key == Key.Left)// Отправляем на сервер сообщение о том что команда влево и данные игрока
                    Send($"Left|{JsonConvert.SerializeObject(ViewModelUserSettings)}");
                // Если нажата клавиша впрваво
                else if (e.Key == Key.Right)// Отправляем на сервер сообщение о том что команда вправо и данные игрока
                    Send($"Right|{JsonConvert.SerializeObject(ViewModelUserSettings)}");
            }
        }
        private void QuitApplication(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //закрываем соединение
            receivingUdpClient.Close();
            //останавливаем поток
            tRec.Abort();
        }
    }
}
