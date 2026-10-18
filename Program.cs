using System;
using System.Collections.Generic;
using System.Linq;

// ===================================================================
// 1. ІНКАПСУЛЯЦІЯ ТА НАСЛІДУВАННЯ: Базовий абстрактний клас Пасажира
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
    public Ticket? CurrentTicket { get; private set; }

    protected Passenger(string name)
    {
        Name = name;
        HasPaid = false;
        CurrentTicket = null;
    }

    // ПОЛІМОРФІЗМ: Абстрактні методи
    public abstract decimal CalculateFare(decimal baseFare);
    public abstract string GetCategoryName();

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
        Console.WriteLine($"[Пасажир] {Name} | Категорія: {GetCategoryName()} | Статус: {status}");
    }
}

// ===================================================================
// 2. НАСЛІДУВАННЯ ТА ПОЛІМОРФІЗМ: Класи-нащадки
// ===================================================================

// 1 - Звичайний пасажир (100% вартості)
public class StandardPassenger : Passenger
{
    public StandardPassenger(string name) : base(name) { }

    public override decimal CalculateFare(decimal baseFare) => baseFare;
    public override string GetCategoryName() => "Звичайний";
}

// 2 - Студент (-50% вартості)
public class StudentPassenger : Passenger
{
    public StudentPassenger(string name) : base(name) { }

    public override decimal CalculateFare(decimal baseFare) => baseFare * 0.5m;
    public override string GetCategoryName() => "Студентський (-50%)";
}

// 3 - Пільговик / Пенсіонер (Безкоштовно)
public class SeniorPassenger : Passenger
{
    public SeniorPassenger(string name) : base(name) { }

    public override decimal CalculateFare(decimal baseFare) => 0m;
    public override string GetCategoryName() => "Пільговий (безкоштовно)";
}

// ===================================================================
// 3. ІНКАПСУЛЯЦІЯ: Клас квитка
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
// 4. ІНКАПСУЛЯЦІЯ: Клас зупинки
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
// 5. ІНКАПСУЛЯЦІЯ ТА ПОЛІМОРФІЗМ: Клас автобуса
// ===================================================================
public class Bus
{
    private readonly List<Passenger> _passengers = new();
    private decimal _revenue;

    public int Capacity { get; }
    public decimal BaseFare { get; }
    public decimal Revenue => _revenue;
    
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
}

// ===================================================================
// 6. ГОЛОВНИЙ КЛАС
// ===================================================================
public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=== Симулятор Автобуса (Лабораторна №2: ООП) ===");
        
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
                // Зчитування об'єкта пасажира через вибір числа (1, 2 або 3)
                Passenger passenger = ReadPassengerInput();
                currentStop.AddPassenger(passenger);
            }

            // Посадка пасажирів у автобус
            while (currentStop.HasPassengers)
            {
                Passenger p = currentStop.GetNextPassenger();
                bus.Board(p);
            }

            bus.DisplayStatus();
        }

        Console.WriteLine("\n[Маршрут завершено] Автобус прибув у депо.");
        bus.DisplayStatus();
        Console.ReadLine();
    }

    // Зчитування імені та вибір категорії за числом з валідацією
    private static Passenger ReadPassengerInput()
    {
        Console.Write("Ім'я пасажира: ");
        string name = Console.ReadLine()?.Trim() ?? "";
        if (string.IsNullOrEmpty(name)) name = "Невідомий";

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
                    return new StandardPassenger(name);
                case "2":
                    return new StudentPassenger(name);
                case "3":
                    return new SeniorPassenger(name);
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