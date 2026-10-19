using System;
using System.Collections.Generic;
using System.Linq;

// ===================================================================
// 1. БАЗОВИЙ АБСТРАКТНИЙ КЛАС ПАСАЖИРА (з підтримкою багажу)
// ===================================================================
public abstract class Passenger
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        protected set
        {
            if (string.IsNullOrWhiteSpace(value))
                _name = "Невідомий пасажир";
            else
                _name = value.Trim();
        }
    }

    public bool HasPaid { get; private set; }
    public bool HasBaggage { get; } // Нова функція: наявність багажу
    public Ticket? CurrentTicket { get; private set; }

    protected Passenger(string name, bool hasBaggage = false)
    {
        Name = name;
        HasBaggage = hasBaggage;
        HasPaid = false;
        CurrentTicket = null;
    }

    // ПОЛІМОРФІЗМ: Абстрактні методи
    public abstract decimal CalculateFare(decimal baseFare);
    public abstract string GetCategoryName();

    // Розрахунок вартості багажу (за наявності +20 грн)
    public decimal GetBaggageFee() => HasBaggage ? 20m : 0m;

    public void Pay(Ticket ticket)
    {
        CurrentTicket = ticket;
        HasPaid = true;
    }

    public virtual void DisplayInfo()
    {
        string status = (HasPaid && CurrentTicket != null) 
            ? $"Оплачено ({CurrentTicket.Price:F2} грн)" 
            : "Не оплачено";
        string baggageInfo = HasBaggage ? " | 🧳 З багажем (+20 грн)" : "";
        
        Console.WriteLine($"[Пасажир] {Name} | Категорія: {GetCategoryName()}{baggageInfo} | Статус: {status}");
    }
}

// ===================================================================
// 2. КЛАСИ-НАЩАДКИ ПАСАЖИРІВ
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
    public override string GetCategoryName() => "Студентський (-50%)";
}

public class SeniorPassenger : Passenger
{
    public SeniorPassenger(string name, bool hasBaggage) : base(name, hasBaggage) { }

    // Для пільговика сам проїзд 0 грн, але якщо є багаж — багаж оплачується
    public override decimal CalculateFare(decimal baseFare) => 0m + GetBaggageFee();
    public override string GetCategoryName() => "Пільговий (безкоштовно)";
}

// ===================================================================
// 3. КЛАС КВИТКА
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

    public void DisplayInfo()
    {
        Console.WriteLine($"  -> Квиток для {PassengerName}: {Price:F2} грн (Тариф: {TariffType})");
    }
}

// ===================================================================
// 4. КЛАС ЗУПИНКИ
// ===================================================================
public class BusStop
{
    public string Name { get; }
    private readonly Queue<Passenger> _waitingQueue = new();

    public BusStop(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Невідома зупинка" : name.Trim();
    }

    public void AddPassenger(Passenger passenger)
    {
        _waitingQueue.Enqueue(passenger);
    }

    public bool HasPassengers => _waitingQueue.Count > 0;

    public Passenger GetNextPassenger()
    {
        return _waitingQueue.Dequeue();
    }
}

// ===================================================================
// 5. КЛАС АВТОБУСА (з перевіркою контролера та статистикою)
// ===================================================================
public class Bus
{
    private readonly List<Passenger> _passengers = new();
    private readonly List<Ticket> _ticketHistory = new(); // Для підсумкової статистики
    private decimal _revenue;

    public int Capacity { get; }
    public decimal BaseFare { get; }
    public decimal Revenue => _revenue;
    public int TotalPassengersServed => _ticketHistory.Count;
    
    public IReadOnlyList<Passenger> Passengers => _passengers.AsReadOnly();
    public BusStop? CurrentStop { get; private set; } 

    public Bus(int capacity, decimal baseFare)
    {
        Capacity = capacity > 0 ? capacity : 10;
        BaseFare = baseFare >= 0 ? baseFare : 0m;
        _revenue = 0m;
    }

    private Ticket IssueTicket(Passenger passenger)
    {
        decimal price = passenger.CalculateFare(BaseFare);
        return new Ticket(passenger.Name, price, passenger.GetCategoryName());
    }

    public void ArriveAtStop(BusStop stop)
    {
        CurrentStop = stop;
        Console.WriteLine("\n==========================================");
        Console.WriteLine($"🚌 Автобус прибув на зупинку: \"{CurrentStop.Name}\"");
        Console.WriteLine("==========================================");
    }

    public bool Board(Passenger passenger)
    {
        if (_passengers.Count >= Capacity)
        {
            Console.WriteLine($"❌ {passenger.Name}: автобус переповнений! (Місць: {_passengers.Count}/{Capacity})");
            return false;
        }

        Ticket ticket = IssueTicket(passenger);
        passenger.Pay(ticket);
        _passengers.Add(passenger);
        _ticketHistory.Add(ticket); // Зберігаємо квиток для підсумкового звіту
        _revenue += ticket.Price;

        Console.WriteLine($"✅ {passenger.Name}: посадка успішна.");
        ticket.DisplayInfo();
        return true;
    }

    public void Exit(string passengerName)
    {
        var passenger = _passengers.FirstOrDefault(p => p.Name.Equals(passengerName, StringComparison.OrdinalIgnoreCase));
        if (passenger == null)
        {
            Console.WriteLine($"⚠️ Пасажир '{passengerName}' не знайдений у салоні.");
            return;
        }

        _passengers.Remove(passenger);
        Console.WriteLine($"🚪 {passenger.Name} вийшов(-ла) з автобуса.");
    }

    // Нова функція: Контролер перевіряє квитки у салоні
    public void InspectPassengers()
    {
        Console.WriteLine("\n👮 [КОНТРОЛЬ] Контролер зайшов у салон для перевірки квитків...");
        if (_passengers.Count == 0)
        {
            Console.WriteLine("   Салон порожній, перевіряти нікого.");
            return;
        }

        int validCount = 0;
        foreach (var passenger in _passengers)
        {
            if (passenger.HasPaid && passenger.CurrentTicket != null)
            {
                Console.WriteLine($"  🟢 {passenger.Name} — квиток дійсний ({passenger.CurrentTicket.TariffType}).");
                validCount++;
            }
            else
            {
                Console.WriteLine($"  🔴 {passenger.Name} — КВИТОК ВІДСУТНІЙ! Виписано штраф.");
            }
        }
        Console.WriteLine($"   Перевірку завершено: {validCount}/{_passengers.Count} пасажирів з квитками.");
    }

    public void DisplayStatus()
    {
        Console.WriteLine("\n--- Стан автобуса ---");
        Console.WriteLine($"Пасажирів у салоні: {_passengers.Count}/{Capacity}");
        Console.WriteLine($"Загальна виручка: {_revenue:F2} грн");
        if (_passengers.Count > 0)
        {
            Console.WriteLine("Пасажири на борту:");
            foreach (var p in _passengers)
            {
                p.DisplayInfo();
            }
        }
        Console.WriteLine("---------------------");
    }

    // Нова функція: Детальний підсумковий звіт
    public void DisplayFinalReport()
    {
        Console.WriteLine("\n📊 ==========================================");
        Console.WriteLine("📋 ПІДСУМКОВИЙ ЗВІТ МАРШРУТУ");
        Console.WriteLine("==========================================");
        Console.WriteLine($"Всього перевезено пасажирів: {TotalPassengersServed}");
        Console.WriteLine($"Загальний виторг: {_revenue:F2} грн");

        if (_ticketHistory.Count > 0)
        {
            Console.WriteLine("\nРозподіл за категоріями:");
            var groups = _ticketHistory.GroupBy(t => t.TariffType);
            foreach (var group in groups)
            {
                Console.WriteLine($"  • {group.Key}: {group.Count()} осіб | Сума: {group.Sum(t => t.Price):F2} грн");
            }
        }
        Console.WriteLine("==========================================\n");
    }
}

// ===================================================================
// 6. ГОЛОВНИЙ КЛАС ЗІ РОЗШИРЕНИМ МЕНЮ
// ===================================================================
public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=== Симулятор Автобуса (Лабораторна №2: Розширені функції) ===");
        
        int capacity = ReadInt("Введіть місткість автобуса: ");
        decimal baseFare = ReadDecimal("Введіть базову вартість квитка (грн): ");
        
        Bus bus = new Bus(capacity, baseFare);

        while (true)
        {
            Console.WriteLine("\nВведіть назву наступної зупинки (або 'кінець' для завершення маршруту): ");
            string stopName = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(stopName)) continue;
            if (stopName.ToLower() == "кінець") break;

            BusStop currentStop = new BusStop(stopName);
            bus.ArriveAtStop(currentStop);

            // 1. Висадка пасажирів
            if (bus.Passengers.Count > 0)
            {
                Console.WriteLine("\nХто хоче вийти на цій зупинці? (Введіть імена через кому, або натисніть Enter):");
                string exitNames = Console.ReadLine()?.Trim() ?? "";
                
                if (!string.IsNullOrEmpty(exitNames))
                {
                    string[] namesToExit = exitNames.Split(',');
                    foreach (var name in namesToExit)
                    {
                        bus.Exit(name.Trim());
                    }
                }
            }

            // 2. Посадка пасажирів
            int peopleWaiting = ReadInt("\nСкільки людей чекає на зупинці? (введіть число): ");
            
            for (int i = 0; i < peopleWaiting; i++)
            {
                Console.WriteLine($"\n--- Дані пасажира {i + 1} ---");
                Passenger passenger = ReadPassengerInput();
                currentStop.AddPassenger(passenger);
            }

            // Посадка пасажирів у автобус
            while (currentStop.HasPassengers)
            {
                Passenger p = currentStop.GetNextPassenger();
                bus.Board(p);
            }

            // Перевірка контролером (нова можливість)
            Console.Write("\nПровести перевірку квитків контролером? (1 - так, 0 - ні): ");
            if (Console.ReadLine()?.Trim() == "1")
            {
                bus.InspectPassengers();
            }

            bus.DisplayStatus();
        }

        Console.WriteLine("\n[Маршрут завершено] Автобус прибув у депо.");
        bus.DisplayFinalReport(); // Вивід підсумкової статистики
        Console.ReadLine();
    }

    private static Passenger ReadPassengerInput()
    {
        Console.Write("Ім'я пасажира: ");
        string name = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrEmpty(name)) name = "Невідомий";

        Console.Write("Чи є у пасажира багаж? (1 - так, 0 - ні): ");
        bool hasBaggage = Console.ReadLine()?.Trim() == "1";

        while (true)
        {
            Console.WriteLine("Оберіть категорію:");
            Console.WriteLine(" 1 - Звичайний");
            Console.WriteLine(" 2 - Студент");
            Console.WriteLine(" 3 - Пільговик / Пенсіонер");
            Console.Write("Ваш вибір (1-3): ");

            string input = Console.ReadLine()?.Trim() ?? "";

            switch (input)
            {
                case "1":
                    return new StandardPassenger(name, hasBaggage);
                case "2":
                    return new StudentPassenger(name, hasBaggage);
                case "3":
                    return new SeniorPassenger(name, hasBaggage);
                default:
                    Console.WriteLine("❌ Некоректний вибір! Введіть число 1, 2 або 3.\n");
                    break;
            }
        }
    }

    private static int ReadInt(string message)
    {
        int result;
        while (true)
        {
            Console.Write(message);
            if (int.TryParse(Console.ReadLine(), out result) && result >= 0)
                return result;
            Console.WriteLine("Будь ласка, введіть коректне додатне число.");
        }
    }

    private static decimal ReadDecimal(string message)
    {
        decimal result;
        while (true)
        {
            Console.Write(message);
            string input = Console.ReadLine()?.Replace('.', ',') ?? "0";
            if (decimal.TryParse(input, out result) && result >= 0)
                return result;
            Console.WriteLine("Будь ласка, введіть коректну суму.");
        }
    }
}