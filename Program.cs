using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ===================================================================
// 1. ІНТЕРФЕЙСИ ТА ЛОГУВАННЯ
// ===================================================================
public interface ILogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
}

public class FileLogger : ILogger
{
    private readonly string _logFilePath;

    public FileLogger(string logFilePath)
    {
        _logFilePath = logFilePath;
        File.AppendAllText(_logFilePath, $"\n[{DateTime.Now}] --- ЗАПУСК СИСТЕМИ ---\n");
    }

    public void LogInfo(string message) => WriteLog("INFO", message);
    public void LogWarning(string message) => WriteLog("WARN", message);
    public void LogError(string message) => WriteLog("ERROR", message);

    private void WriteLog(string level, string message)
    {
        string logEntry = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
        Console.WriteLine(logEntry);
        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
    }
}

public interface IPassenger
{
    string Name { get; }
    bool HasPaid { get; }
    bool HasBaggage { get; }
    Ticket? CurrentTicket { get; }
    
    decimal CalculateFare(decimal baseFare);
    string GetCategoryName();
    decimal GetBaggageFee();
    void Pay(Ticket ticket);
    void DisplayInfo(ILogger logger);
}

// ===================================================================
// 2. ВЛАСНИЙ КЛАС ПОМИЛКИ (EXCEPTION)
// ===================================================================
public class BusOvercrowdedException : Exception
{
    public BusOvercrowdedException(string message) : base(message) { }
}

// ===================================================================
// 3. БАЗОВИЙ АБСТРАКТНИЙ КЛАС ПАСАЖИРА
// ===================================================================
public abstract class Passenger : IPassenger
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        protected set { _name = string.IsNullOrWhiteSpace(value) ? "Невідомий пасажир" : value.Trim(); }
    }

    public bool HasPaid { get; private set; }
    public bool HasBaggage { get; }
    public Ticket? CurrentTicket { get; private set; }

    protected Passenger(string name, bool hasBaggage = false)
    {
        Name = name;
        HasBaggage = hasBaggage;
        HasPaid = false;
        CurrentTicket = null;
    }

    public abstract decimal CalculateFare(decimal baseFare);
    public abstract string GetCategoryName();

    public decimal GetBaggageFee() => HasBaggage ? 20m : 0m;

    public void Pay(Ticket ticket)
    {
        CurrentTicket = ticket;
        HasPaid = true;
    }

    public virtual void DisplayInfo(ILogger logger)
    {
        string status = (HasPaid && CurrentTicket != null) 
            ? $"Оплачено ({CurrentTicket.Price:F2} грн)" 
            : "Не оплачено";
        string baggageInfo = HasBaggage ? " | 🧳 З багажем" : "";
        
        logger.LogInfo($"[Пасажир] {Name} | {GetCategoryName()}{baggageInfo} | {status}");
    }
}

// ===================================================================
// 4. КЛАСИ-НАЩАДКИ ПАСАЖИРІВ
// ===================================================================
public class StandardPassenger : Passenger
{
    public StandardPassenger(string name, bool hasBaggage) : base(name, hasBaggage) { }
    public override decimal CalculateFare(decimal baseFare) => baseFare + GetBaggageFee();
    public override string GetCategoryName() => "Звичайний";
}

public class StudentPassenger : Passenger
{
    public StudentPassenger(string name, bool hasBaggage) : base(name, hasBaggage) { }
    public override decimal CalculateFare(decimal baseFare) => (baseFare * 0.5m) + GetBaggageFee();
    public override string GetCategoryName() => "Студентський";
}

public class SeniorPassenger : Passenger
{
    public SeniorPassenger(string name, bool hasBaggage) : base(name, hasBaggage) { }
    public override decimal CalculateFare(decimal baseFare) => 0m + GetBaggageFee();
    public override string GetCategoryName() => "Пільговий";
}

// ===================================================================
// 5. КЛАСИ КВИТКА ТА ЗУПИНКИ
// ===================================================================
public class Ticket
{
    public string PassengerName { get; }
    public decimal Price { get; }
    public string TariffType { get; }
    public DateTime IssuedAt { get; }

    public Ticket(string passengerName, decimal price, string tariffType)
    {
        PassengerName = passengerName;
        Price = price < 0 ? 0 : price;
        TariffType = tariffType;
        IssuedAt = DateTime.Now;
    }
}

public class BusStop
{
    public string Name { get; }
    private readonly Queue<IPassenger> _waitingQueue = new();

    public BusStop(string name) { Name = string.IsNullOrWhiteSpace(name) ? "Невідома зупинка" : name.Trim(); }
    public void AddPassenger(IPassenger passenger) => _waitingQueue.Enqueue(passenger);
    public bool HasPassengers => _waitingQueue.Count > 0;
    public IPassenger GetNextPassenger() => _waitingQueue.Dequeue();
}

// ===================================================================
// 6. КЛАС АВТОБУСА 
// ===================================================================
public class Bus
{
    private readonly List<IPassenger> _passengers = new();
    private readonly List<Ticket> _ticketHistory = new();
    private decimal _revenue;
    private readonly ILogger _logger;

    public int Capacity { get; }
    public decimal BaseFare { get; }
    public decimal Revenue => _revenue;
    public int TotalPassengersServed => _ticketHistory.Count;
    public IReadOnlyList<IPassenger> Passengers => _passengers.AsReadOnly();
    public BusStop? CurrentStop { get; private set; } 

    public Bus(int capacity, decimal baseFare, ILogger logger)
    {
        Capacity = capacity > 0 ? capacity : 10;
        BaseFare = baseFare >= 0 ? baseFare : 0m;
        _logger = logger;
    }

    private Ticket IssueTicket(IPassenger passenger)
    {
        return new Ticket(passenger.Name, passenger.CalculateFare(BaseFare), passenger.GetCategoryName());
    }

    public void ArriveAtStop(BusStop stop)
    {
        CurrentStop = stop;
        _logger.LogInfo($"\n🚌 Автобус прибув на зупинку: \"{CurrentStop.Name}\"");
    }

    public void Board(IPassenger passenger)
    {
        if (_passengers.Count >= Capacity)
        {
            throw new BusOvercrowdedException($"Автобус ПЕРЕПОВНЕНИЙ! Місць: {_passengers.Count}/{Capacity}.");
        }

        Ticket ticket = IssueTicket(passenger);
        passenger.Pay(ticket);
        _passengers.Add(passenger);
        _ticketHistory.Add(ticket);
        _revenue += ticket.Price;

        _logger.LogInfo($"✅ {passenger.Name} сів у автобус. Видано квиток: {ticket.Price:F2} грн.");
    }

    public void AutoExit(int count)
    {
        if (count <= 0 || _passengers.Count == 0) return;

        _logger.LogInfo($"\n[АЛГОРИТМ ВИСАДКИ] Розпочато автоматичну висадку {count} пасажирів (Пільговики -> Студенти -> Звичайні).");

        var exitingPassengers = _passengers
            .OrderBy(p => p is SeniorPassenger ? 1 : p is StudentPassenger ? 2 : 3)
            .Take(count)
            .ToList();

        foreach (var p in exitingPassengers)
        {
            _passengers.Remove(p);
            _logger.LogInfo($"🚪 {p.Name} ({p.GetCategoryName()}) вийшов(-ла) з автобуса.");
        }
    }

    public void InspectPassengers()
    {
        _logger.LogInfo("\n👮 [КОНТРОЛЬ] Контролер зайшов у салон...");
        if (_passengers.Count == 0)
        {
            _logger.LogInfo("Салон порожній.");
            return;
        }

        int validCount = 0;
        foreach (var passenger in _passengers)
        {
            if (passenger.HasPaid) { _logger.LogInfo($"🟢 {passenger.Name} — квиток дійсний."); validCount++; }
            else { _logger.LogError($"🔴 {passenger.Name} — КВИТОК ВІДСУТНІЙ! Штраф."); }
        }
        _logger.LogInfo($"Перевірку завершено: {validCount}/{_passengers.Count} з квитками.");
    }

    public void DisplayStatus()
    {
        _logger.LogInfo($"\n--- Стан автобуса --- Пасажирів: {_passengers.Count}/{Capacity} | Виручка: {_revenue:F2} грн");
        foreach (var p in _passengers) p.DisplayInfo(_logger);
    }

    // НОВЕ: ВИВЕДЕННЯ (Запис звіту у текстовий файл)
    public void ExportReportToFile(string filePath)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                writer.WriteLine("==========================================");
                writer.WriteLine("📊 ПІДСУМКОВИЙ ЗВІТ МАРШРУТУ");
                writer.WriteLine("==========================================");
                writer.WriteLine($"Дата генерації: {DateTime.Now}");
                writer.WriteLine($"Всього перевезено пасажирів: {TotalPassengersServed}");
                writer.WriteLine($"Загальний виторг: {_revenue:F2} грн\n");
                
                writer.WriteLine("Історія виданих квитків:");
                foreach (var ticket in _ticketHistory)
                {
                    writer.WriteLine($"- {ticket.PassengerName} | {ticket.TariffType} | {ticket.Price:F2} грн | Час: {ticket.IssuedAt:HH:mm:ss}");
                }
            }
            _logger.LogInfo($"💾 Фінальний звіт успішно збережено у файл: {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ Помилка при записі у файл: {ex.Message}");
        }
    }
}

// ===================================================================
// 7. ГОЛОВНИЙ КЛАС
// ===================================================================
public class Program
{
    private static readonly Random _rand = new Random();

    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        ILogger logger = new FileLogger("bus_log.txt");
        logger.LogInfo("=== Симулятор Автобуса (Лабораторна №3) ===");
        
        int capacity = 0;
        decimal baseFare = 0;

        // НОВЕ: ВВЕДЕННЯ (Читання налаштувань з текстового файлу)
        Console.Write("Завантажити налаштування з файлу 'config.txt'? (1 - так, 0 - ні): ");
        if (Console.ReadLine()?.Trim() == "1")
        {
            if (!LoadSettingsFromFile("config.txt", out capacity, out baseFare, logger))
            {
                logger.LogWarning("⚠️ Не вдалося завантажити файл. Перехід до ручного введення.");
                capacity = ReadInt("Введіть місткість автобуса: ");
                baseFare = ReadDecimal("Введіть базову вартість квитка (грн): ");
            }
        }
        else
        {
            capacity = ReadInt("Введіть місткість автобуса: ");
            baseFare = ReadDecimal("Введіть базову вартість квитка (грн): ");
            SaveSettingsToFile("config.txt", capacity, baseFare); // Зберігаємо для наступного разу
        }

        Bus bus = new Bus(capacity, baseFare, logger);

        while (true)
        {
            Console.WriteLine("\nВведіть назву наступної зупинки (або 'кінець' для завершення): ");
            string stopName = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(stopName)) continue;
            if (stopName.ToLower() == "кінець") break;

            BusStop currentStop = new BusStop(stopName);
            bus.ArriveAtStop(currentStop);

            if (bus.Passengers.Count > 0)
            {
                int maxExit = bus.Passengers.Count;
                int exitCount = ReadInt($"Скільки людей виходить на цій зупинці? (від 0 до {maxExit}): ");
                if (exitCount > maxExit) exitCount = maxExit;
                bus.AutoExit(exitCount);
            }

            // РЕАЛІСТИЧНИЙ НАТОВП
            int totalWaiting = ReadInt("\nСкільки загалом людей стоїть на зупинці?: ");
            int waitingForBus = 0;

            if (totalWaiting > 0)
            {
                double percentage = _rand.Next(10, 81) / 100.0;
                waitingForBus = (int)(totalWaiting * percentage);
                if (totalWaiting <= 3) waitingForBus = _rand.Next(0, totalWaiting + 1);

                int waitingForOthers = totalWaiting - waitingForBus;
                logger.LogWarning($"👀 З {totalWaiting} людей на зупинці, {waitingForOthers} чекають на інший транспорт.");
                logger.LogInfo($"🚌 На ваш автобус хочуть сісти: {waitingForBus} осіб.");
            }

            if (waitingForBus > 0)
            {
                // ВИБІР: РУЧНЕ АБО АВТОМАТИЧНЕ
                Console.Write("\nЯк оформити пасажирів? (1 - Вручну кожного, 2 - Згенерувати автоматично): ");
                bool autoGenerate = Console.ReadLine()?.Trim() == "2";

                for (int i = 0; i < waitingForBus; i++)
                {
                    IPassenger passenger;
                    if (autoGenerate)
                    {
                        passenger = GenerateRandomPassenger(i + 1);
                    }
                    else
                    {
                        Console.WriteLine($"\n--- Дані пасажира {i + 1} з {waitingForBus} ---");
                        passenger = ReadPassengerInput();
                    }
                    currentStop.AddPassenger(passenger);
                }

                while (currentStop.HasPassengers)
                {
                    var p = currentStop.GetNextPassenger();
                    try
                    {
                        bus.Board(p);
                    }
                    catch (BusOvercrowdedException ex)
                    {
                        logger.LogError($"🚨 {ex.Message} Пасажир {p.Name} не вліз і залишився чекати.");
                        break; 
                    }
                }
            }

            Console.Write("\nПеревірка контролером? (1 - так, 0 - ні): ");
            if (Console.ReadLine()?.Trim() == "1") bus.InspectPassengers();

            bus.DisplayStatus();
        }

        logger.LogInfo($"\n[Маршрут завершено] Виторг: {bus.Revenue:F2} грн. Перевезено: {bus.TotalPassengersServed} осіб.");
        
        // ВИКЛИК ВИВЕДЕННЯ В ТЕКСТОВИЙ ФАЙЛ
        bus.ExportReportToFile("report.txt"); 
    }

    // НОВЕ: Читання конфігурації (Введення з файлу)
    private static bool LoadSettingsFromFile(string filePath, out int capacity, out decimal baseFare, ILogger logger)
    {
        capacity = 0;
        baseFare = 0;
        try
        {
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    capacity = int.Parse(reader.ReadLine() ?? "10");
                    baseFare = decimal.Parse(reader.ReadLine() ?? "20");
                }
                logger.LogInfo($"✅ Налаштування завантажено з '{filePath}' (Місткість: {capacity}, Вартість: {baseFare})");
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Помилка читання файлу '{filePath}': {ex.Message}");
        }
        return false;
    }

    // НОВЕ: Збереження конфігурації
    private static void SaveSettingsToFile(string filePath, int capacity, decimal baseFare)
    {
        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine(capacity);
            writer.WriteLine(baseFare);
        }
    }

    private static IPassenger GenerateRandomPassenger(int number)
    {
        string name = $"Пасажир #{number}";
        bool hasBaggage = _rand.Next(0, 100) < 25; 
        int categoryChance = _rand.Next(1, 101);

        if (categoryChance <= 60) return new StandardPassenger(name, hasBaggage);
        else if (categoryChance <= 85) return new StudentPassenger(name, hasBaggage);
        else return new SeniorPassenger(name, hasBaggage);
    }

    private static IPassenger ReadPassengerInput()
    {
        Console.Write("Ім'я: ");
        string name = Console.ReadLine()?.Trim() ?? "Невідомий";
        Console.Write("Багаж? (1 - так, 0 - ні): ");
        bool hasBaggage = Console.ReadLine()?.Trim() == "1";

        while (true)
        {
            Console.Write("Категорія (1-Звичайний, 2-Студент, 3-Пільговик): ");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": return new StandardPassenger(name, hasBaggage);
                case "2": return new StudentPassenger(name, hasBaggage);
                case "3": return new SeniorPassenger(name, hasBaggage);
                default: Console.WriteLine("❌ Некоректний вибір!\n"); break;
            }
        }
    }

    private static int ReadInt(string message)
    {
        int result;
        while (true)
        {
            Console.Write(message);
            if (int.TryParse(Console.ReadLine(), out result) && result >= 0) return result;
        }
    }

    private static decimal ReadDecimal(string message)
    {
        decimal result;
        while (true)
        {
            Console.Write(message);
            if (decimal.TryParse(Console.ReadLine()?.Replace('.', ','), out result) && result >= 0) return result;
        }
    }
}