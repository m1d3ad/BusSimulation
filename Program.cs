using System;
using System.Collections.Generic;
using System.Linq;

// 1. Клас пасажира
public class Passenger
{
    public string Name { get; }
    public string Category { get; } 
    public bool HasPaid { get; private set; }
    public Ticket? CurrentTicket { get; private set; } 

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

// 3. Клас зупинки
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

// 4. Клас автобуса (Тепер логіка калькулятора знаходиться тут)
public class Bus
{
    public int Capacity { get; }
    public decimal BaseFare { get; } // Замість класу калькулятора зберігаємо базовий тариф
    public List<Passenger> Passengers { get; } = new();
    public decimal Revenue { get; private set; }
    public BusStop? CurrentStop { get; private set; } 

    public Bus(int capacity, decimal baseFare)
    {
        Capacity = capacity;
        BaseFare = baseFare;
        Revenue = 0m;
        CurrentStop = null;
    }

    // Внутрішній метод розрахунку тарифу (замінив клас FareCalculator)
    private Ticket IssueTicket(Passenger passenger)
    {
        decimal finalPrice;
        string tariffType;

        switch (passenger.Category.Trim().ToLower())
        {
            case "студент":
                finalPrice = BaseFare * 0.5m; 
                tariffType = "Студентський (-50%)";
                break;
            case "пільговик":
            case "пенсіонер":
                finalPrice = 0m; 
                tariffType = "Пільговий (безкоштовно)";
                break;
            default:
                finalPrice = BaseFare;
                tariffType = "Стандартний";
                break;
        }

        return new Ticket(passenger.Name, finalPrice, tariffType);
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
        if (Passengers.Count >= Capacity)
        {
            Console.WriteLine($"❌ {passenger.Name}: автобус переповнений! (Місць: {Passengers.Count}/{Capacity})");
            return false;
        }

        Ticket ticket = IssueTicket(passenger); // Використовуємо внутрішній метод
        passenger.Pay(ticket);
        Passengers.Add(passenger);
        Revenue += ticket.Price;

        Console.WriteLine($"✅ {passenger.Name}: посадка успішна.");
        ticket.DisplayInfo();
        return true;
    }

    public void Exit(string passengerName)
    {
        var passenger = Passengers.FirstOrDefault(p => p.Name.Equals(passengerName, StringComparison.OrdinalIgnoreCase));
        if (passenger == null)
        {
            Console.WriteLine($"⚠️ Пасажир '{passengerName}' не знайдений у салоні.");
            return;
        }

        Passengers.Remove(passenger);
        Console.WriteLine($"🚪 {passenger.Name} вийшов(-ла) з автобуса.");
    }

    public void DisplayStatus()
    {
        Console.WriteLine("\n--- Стан автобуса ---");
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

// 5. Головний клас з інтерактивним консольним меню
public class Program
{
    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("=== Налаштування автобуса ===");
        
        int capacity = ReadInt("Введіть місткість автобуса: ");
        decimal baseFare = ReadDecimal("Введіть базову вартість квитка (грн): ");
        
        Bus bus = new Bus(capacity, baseFare);

        while (true)
        {
            Console.WriteLine("\nВведіть назву наступної зупинки (або 'кінець' для завершення маршруту): ");
            string stopName = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(stopName)) continue;
            if (stopName.ToLower() == "кінець") break;

            BusStop currentStop = new BusStop(stopName);
            bus.ArriveAtStop(currentStop);

            // 1. Висадка пасажирів
            if (bus.Passengers.Count > 0)
            {
                Console.WriteLine("\nХто хоче вийти на цій зупинці? (Введіть імена через кому, або просто натисніть Enter, якщо ніхто):");
                string exitNames = Console.ReadLine()?.Trim();
                
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
                Console.Write("Ім'я: ");
                string pName = Console.ReadLine()?.Trim() ?? "Невідомий";
                
                Console.Write("Категорія (Звичайний / Студент / Пільговик): ");
                string pCategory = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(pCategory)) pCategory = "Звичайний";

                currentStop.AddPassenger(new Passenger(pName, pCategory));
            }

            // Запускаємо їх в автобус
            while (currentStop.HasPassengers)
            {
                Passenger p = currentStop.GetNextPassenger();
                bus.Board(p);
            }

            // Показуємо статус після кожної зупинки
            bus.DisplayStatus();
        }

        Console.WriteLine("\n[Маршрут завершено] Автобус прибув у депо.");
        bus.DisplayStatus();
        Console.ReadLine(); // Щоб консоль не закривалась відразу
    }

    // Допоміжний метод для безпечного зчитування цілих чисел
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

    // Допоміжний метод для безпечного зчитування дробових чисел (ціни)
    private static decimal ReadDecimal(string message)
    {
        decimal result;
        while (true)
        {
            Console.Write(message);
            string input = Console.ReadLine()?.Replace('.', ','); // Підтримка і крапки, і коми
            if (decimal.TryParse(input, out result) && result >= 0)
                return result;
            Console.WriteLine("Будь ласка, введіть коректну суму.");
        }
    }
}