using Moq;
using Betsson.OnlineWallets.Services;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Models;

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
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
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
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
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
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
    }

    [Fact]
    public async Task DepositFundsAsync_ValidDeposit_UpdatesBalance()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = 50.0m,
            BalanceBefore = 150.0m
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

        var service = new OnlineWalletService(mockRepo.Object);
        var deposit = new Deposit
        {
            Amount = 75.0m
        };

        // Act
        var newBalance = await service.DepositFundsAsync(deposit);

        // Assert
        Assert.Equal(275.0m, newBalance.Amount);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(entry =>
            entry.Amount == 75.0m &&
            entry.BalanceBefore == 200.0m
        )), Times.Once);
    }

    [Fact]
    public async Task DepositFundsAsync_ZeroDeposit_NoBalanceChange()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = 100.0m,
            BalanceBefore = 200.0m
        };

        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

        var service = new OnlineWalletService(mockRepo.Object);
        var deposit = new Deposit
        {
            Amount = 0.0m
        };

        // Act
        var newBalance = await service.DepositFundsAsync(deposit);

        // Assert
        Assert.Equal(300.0m, newBalance.Amount);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(entry =>
            entry.Amount == 0.0m &&
            entry.BalanceBefore == 300.0m
        )), Times.Once);
    }

    [Fact]
    public async Task DepositFundsAsync_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = 100.0m,
            BalanceBefore = 200.0m
        };

        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

        var service = new OnlineWalletService(mockRepo.Object);
        var deposit = new Deposit
        {
            Amount = 50.0m
        };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DepositFundsAsync(deposit));

        // Assert
        Assert.Equal("Database error", exception.Message);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
    }
}