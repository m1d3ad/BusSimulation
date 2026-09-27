using System;
using System.Collections.Generic;
using System.Linq;

// 1. Клас пасажира
public class Passenger
{
    public string Name { get; }
    public string Category { get; } // "Звичайний", "Студент", "Пільговик"
    public bool HasPaid { get; private set; }
    public Ticket? CurrentTicket { get; private set; } // Додано ? (може бути null до оплати)

    public Passenger(string name, string category = "Звичайний")
    {
        Name = name;
        Category = category;
        HasPaid = false;
        CurrentTicket = null;
    }

    public void Pay(Ticket ticket)
    {
        CurrentTicket = ticket;
        HasPaid = true;
    }

    public void DisplayInfo()
    {
        string status = (HasPaid && CurrentTicket != null) 
            ? $"Оплачено ({CurrentTicket.Price:F2} грн)" 
            : "Не оплачено";
        Console.WriteLine($"[Пасажир] {Name} | Категорія: {Category} | Статус: {status}");
    }
}

// 2. Клас квитка
public class Ticket
{
    public string PassengerName { get; }
    public decimal Price { get; }
    public string TariffType { get; }
    public DateTime IssuedAt { get; }

    public Ticket(string passengerName, decimal price, string tariffType)
    {
        PassengerName = passengerName;
        Price = price;
        TariffType = tariffType;
        IssuedAt = DateTime.Now;
    }

    public void DisplayInfo()
    {
        Console.WriteLine($"  -> Квиток для {PassengerName}: {Price:F2} грн (Тариф: {TariffType})");
    }
}

// 3. Клас розрахунку тарифів (пільги)
public class FareCalculator
{
    public decimal StandardFare { get; }

    public FareCalculator(decimal standardFare)
    {
        StandardFare = standardFare;
    }

    public Ticket IssueTicket(Passenger passenger)
    {
        decimal finalPrice;
        string tariffType;

        switch (passenger.Category.Trim().ToLower())
        {
            case "студент":
                finalPrice = StandardFare * 0.5m; // 50% знижка
                tariffType = "Студентський (-50%)";
                break;
            case "пільговик":
            case "пенсіонер":
                finalPrice = 0m; // 100% знижка
                tariffType = "Пільговий (безкоштовно)";
                break;
            default:
                finalPrice = StandardFare;
                tariffType = "Стандартний";
                break;
        }

        return new Ticket(passenger.Name, finalPrice, tariffType);
    }
}

// 4. Клас зупинки
public class BusStop
{
    public string Name { get; }
    public Queue<Passenger> WaitingQueue { get; } = new();

    public BusStop(string name)
    {
        Name = name;
    }

    public void AddPassenger(Passenger passenger)
    {
        WaitingQueue.Enqueue(passenger);
    }

    public bool HasPassengers => WaitingQueue.Count > 0;

    public Passenger GetNextPassenger()
    {
        return WaitingQueue.Dequeue();
    }
}

// 5. Клас автобуса
public class Bus
{
    public int Capacity { get; }
    public List<Passenger> Passengers { get; } = new();
    public Queue<BusStop> Route { get; } = new();
    public FareCalculator FareSystem { get; }
    public decimal Revenue { get; private set; }
    public BusStop? CurrentStop { get; private set; } // Додано ? (може бути null на старті/фініші)

    public Bus(int capacity, decimal baseFare)
    {
        Capacity = capacity;
        FareSystem = new FareCalculator(baseFare);
        Revenue = 0m;
        CurrentStop = null;
    }

    public void AddStopToRoute(BusStop stop)
    {
        Route.Enqueue(stop);
    }

    // Переміщення до наступної зупинки через Dequeue
    public bool MoveToNextStop()
    {
        if (Route.Count == 0)
        {
            Console.WriteLine("\n[Кінець маршруту] Більше зупинок немає.");
            CurrentStop = null;
            return false;
        }

        CurrentStop = Route.Dequeue();
        Console.WriteLine("\n==========================================");
        Console.WriteLine($"🚌 Автобус прибув на зупинку: \"{CurrentStop.Name}\"");
        Console.WriteLine("==========================================");
        return true;
    }

    // Посадка пасажира з перевіркою місткості та оплатою
    public bool Board(Passenger passenger)
    {
        if (Passengers.Count >= Capacity)
        {
            Console.WriteLine($"❌ {passenger.Name}: автобус переповнений! (Місць: {Passengers.Count}/{Capacity})");
            return false;
        }

        Ticket ticket = FareSystem.IssueTicket(passenger);
        passenger.Pay(ticket);
        Passengers.Add(passenger);
        Revenue += ticket.Price;

        Console.WriteLine($"✅ {passenger.Name}: посадка успішна.");
        ticket.DisplayInfo();
        return true;
    }

    // Висадка пасажира з перевіркою наявності в салоні
    public void Exit(string passengerName)
    {
        var passenger = Passengers.FirstOrDefault(p => p.Name.Equals(passengerName, StringComparison.OrdinalIgnoreCase));
        if (passenger == null)
        {
            Console.WriteLine($"⚠️ Помилка: Пасажир '{passengerName}' не знайдений у салоні автобуса.");
            return;
        }

        Passengers.Remove(passenger);
        Console.WriteLine($"🚪 {passenger.Name} вийшов(-ла) з автобуса.");
    }

    public void DisplayStatus()
    {
        Console.WriteLine("--- Стан автобуса ---");
        Console.WriteLine($"Пасажирів у салоні: {Passengers.Count}/{Capacity}");
        Console.WriteLine($"Загальна виручка: {Revenue:F2} грн");
        if (Passengers.Count > 0)
        {
            Console.WriteLine("Пасажири на борту:");
            foreach (var p in Passengers)
            {
                p.DisplayInfo();
            }
        }
        Console.WriteLine("---------------------");
    }
}

// 6. Головний клас запуску симуляції
public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Автобус місткістю 3 місця, базовий квиток — 16.00 грн
        Bus bus = new Bus(capacity: 3, baseFare: 16.00m);

        // Створення зупинок і додавання пасажирів у чергу очікування
        BusStop stop1 = new BusStop("Залізничний вокзал");
        stop1.AddPassenger(new Passenger("Олександр", "Звичайний"));
        stop1.AddPassenger(new Passenger("Марія", "Студент"));
        stop1.AddPassenger(new Passenger("Іван Петрович", "Пільговик"));
        stop1.AddPassenger(new Passenger("Дмитро", "Звичайний"));

        BusStop stop2 = new BusStop("Театральна площа");
        stop2.AddPassenger(new Passenger("Олена", "Студент"));

        BusStop stop3 = new BusStop("Університет");

        // Формування маршруту (FIFO Queue)
        bus.AddStopToRoute(stop1);
        bus.AddStopToRoute(stop2);
        bus.AddStopToRoute(stop3);

        // --- Зупинка 1: Залізничний вокзал ---
        bus.MoveToNextStop();
        while (bus.CurrentStop != null && bus.CurrentStop.HasPassengers)
        {
            Passenger p = bus.CurrentStop.GetNextPassenger();
            bus.Board(p);
        }
        bus.DisplayStatus();

        // --- Зупинка 2: Театральна площа ---
        bus.MoveToNextStop();
        bus.Exit("Марія"); // успішний вихід
        bus.Exit("Тарас"); // спроба виходу пасажира, якого немає

        while (bus.CurrentStop != null && bus.CurrentStop.HasPassengers)
        {
            Passenger p = bus.CurrentStop.GetNextPassenger();
            bus.Board(p);
        }
        bus.DisplayStatus();

        // --- Зупинка 3: Університет ---
        bus.MoveToNextStop();
        bus.Exit("Олександр");
        bus.Exit("Іван Петрович");
        bus.Exit("Олена");

        bus.DisplayStatus();

        // Спроба поїхати далі, коли маршрут вичерпано
        bus.MoveToNextStop();
    }
}