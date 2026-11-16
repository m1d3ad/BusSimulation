using Xunit;
using System;

public class BusTests
{
    private class DummyLogger : ILogger
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message) { }
    }

    [Fact]
    public void StandardPassenger_CalculateFare_WithoutBaggage_ReturnsBaseFare()
    {
        var passenger = new StandardPassenger("Іван", hasBaggage: false);
        decimal fare = passenger.CalculateFare(50m);
        Assert.Equal(50m, fare);
    }

    [Fact]
    public void Bus_Boarding_ThrowsException_WhenOvercrowded()
    {
        // Arrange
        var logger = new DummyLogger();
        var bus = new Bus(capacity: 2, baseFare: 10m, logger);
        
        // Act
        bus.Board(new StandardPassenger("P1", false));
        bus.Board(new StandardPassenger("P2", false));

        // Assert - Перевіряємо, чи генерується помилка при спробі посадити 3-го пасажира
        Assert.Throws<BusOvercrowdedException>(() => 
        {
            bus.Board(new StandardPassenger("P3", false));
        });
    }

    [Fact]
    public void Bus_AutoExit_UsesPriorityAlgorithm()
    {
        // Arrange
        var logger = new DummyLogger();
        var bus = new Bus(capacity: 10, baseFare: 10m, logger);
        
        bus.Board(new StandardPassenger("Звичайний", false));
        bus.Board(new SeniorPassenger("Пільговик", false));
        bus.Board(new StudentPassenger("Студент", false));

        // Act - висаджуємо 1 людину
        bus.AutoExit(1);

        // Assert - перевіряємо алгоритм: пільговик мав вийти першим, тому його не має бути в салоні
        Assert.Equal(2, bus.Passengers.Count);
        Assert.DoesNotContain(bus.Passengers, p => p.Name == "Пільговик");
    }
}