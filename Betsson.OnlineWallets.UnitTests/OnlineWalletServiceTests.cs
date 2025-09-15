using Moq;
using Betsson.OnlineWallets.Services;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Data.Models;


namespace Betsson.OnlineWallets.UnitTests;

public class OnlineWalletServiceTests
{
    [Fact]
    public async Task GetBalanceAsync_NoTransactions_ReturnsZero()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync((OnlineWalletEntry?)null);

        var service = new OnlineWalletService(mockRepo.Object);

        // Act
        var balance = await service.GetBalanceAsync();

        // Assert
        Assert.Equal(0, balance.Amount);
    }

    [Fact]
    public async Task GetBalanceAsync_WithTransactions_ReturnsCorrectBalance()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = 100.25m,
            BalanceBefore = 200.0m
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);

        var service = new OnlineWalletService(mockRepo.Object);

        // Act
        var balance = await service.GetBalanceAsync();

        // Assert
        Assert.Equal(300.25m, balance.Amount);
    }

    [Fact]
    public async Task GetBalanceAsync_LastTransactionNegative_ReturnsCorrectBalance()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = -50.75m,
            BalanceBefore = 150.0m
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);

        var service = new OnlineWalletService(mockRepo.Object);

        // Act
        var balance = await service.GetBalanceAsync();

        // Assert
        Assert.Equal(99.25m, balance.Amount);
    }
}