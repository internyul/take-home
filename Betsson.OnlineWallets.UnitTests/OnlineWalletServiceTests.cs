using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Exceptions;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Services;
using Moq;

namespace Betsson.OnlineWallets.UnitTests;

public class OnlineWalletServiceTests
{
    [Fact]
    [Trait("Category", "Balance")]
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

    [Theory]
    [Trait("Category", "Balance")]
    [InlineData(100.25, 200.0, 300.25)]
    [InlineData(-50.75, 150.0, 99.25)]
    public async Task GetBalanceAsync_WithTransactions_ReturnsExpectedBalance(decimal lastAmount, decimal lastBalanceBefore, decimal expected)
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = lastAmount,
            BalanceBefore = lastBalanceBefore
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);

        var service = new OnlineWalletService(mockRepo.Object);

        // Act
        var balance = await service.GetBalanceAsync();

        // Assert
        Assert.Equal(expected, balance.Amount);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Balance")]
    public async Task GetBalanceAsync_RepositoryThrows_PropagatesException()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ThrowsAsync(new InvalidOperationException("Database error"));

        var service = new OnlineWalletService(mockRepo.Object);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetBalanceAsync());

        // Assert
        Assert.Equal("Database error", exception.Message);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
    }

    [Theory]
    [Trait("Category", "Deposit")]
    [InlineData(50.0, 150.0, 75.0, 275.0)]
    [InlineData(100.0, 200.0, 0.0, 300.0)]
    public async Task DepositFundsAsync_DepositAmount_UpdatesBalance(decimal lastAmount, decimal lastBalanceBefore, decimal depositAmount, decimal expectedNewBalance)
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = lastAmount,
            BalanceBefore = lastBalanceBefore
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

        var service = new OnlineWalletService(mockRepo.Object);
        var deposit = new Deposit { Amount = depositAmount };

        // Act
        var newBalance = await service.DepositFundsAsync(deposit);

        // Assert
        Assert.Equal(expectedNewBalance, newBalance.Amount);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(entry =>
            entry.Amount == depositAmount &&
            entry.BalanceBefore == lastAmount + lastBalanceBefore
        )), Times.Once);
    }

    [Fact]
    [Trait("Category", "Deposit")]
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

    [Fact]
    [Trait("Category", "Deposit")]
    public async Task DepositFundsAsync_GetLastOnlineWalletEntryThrows_PropagatesException()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ThrowsAsync(new InvalidOperationException("GetLast failed"));

        var service = new OnlineWalletService(mockRepo.Object);
        var deposit = new Deposit
        {
            Amount = 10.0m
        };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DepositFundsAsync(deposit));

        // Assert
        Assert.Equal("GetLast failed", exception.Message);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Never);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Deposit")]
    public async Task DepositFundsAsync_NoPreviousTransactions_StartsFromZero()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync((OnlineWalletEntry?)null);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

        var service = new OnlineWalletService(mockRepo.Object);
        var deposit = new Deposit { Amount = 50.0m };

        // Act
        var newBalance = await service.DepositFundsAsync(deposit);

        // Assert
        Assert.Equal(50.0m, newBalance.Amount);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(entry =>
            entry.Amount == 50.0m &&
            entry.BalanceBefore == 0.0m
        )), Times.Once);
    }

    [Theory]
    [Trait("Category", "Withdraw")]
    [InlineData(100.0, 50.0, 75.0, 75.0)]
    [InlineData(200.0, 100.0, 300.0, 0.0)]
    public async Task WithdrawFundsAsync_Amount_UpdatesBalance(decimal lastAmount, decimal lastBalanceBefore, decimal withdrawalAmount, decimal expectedNewBalance)
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = lastAmount,
            BalanceBefore = lastBalanceBefore
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

        var service = new OnlineWalletService(mockRepo.Object);
        var withdrawal = new Withdrawal { Amount = withdrawalAmount };

        // Act
        var newBalance = await service.WithdrawFundsAsync(withdrawal);

        // Assert
        Assert.Equal(expectedNewBalance, newBalance.Amount);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(entry =>
            entry.Amount == -withdrawalAmount &&
            entry.BalanceBefore == lastAmount + lastBalanceBefore
        )), Times.Once);
    }

    [Fact]
    [Trait("Category", "Withdraw")]
    public async Task WithdrawFundsAsync_InsufficientBalance_ThrowsInsufficientBalanceException()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = 50.0m,
            BalanceBefore = 50.0m
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

        var service = new OnlineWalletService(mockRepo.Object);
        var withdrawal = new Withdrawal
        {
            Amount = 200.0m
        };

        // Act
        var exception = await Assert.ThrowsAsync<InsufficientBalanceException>(() => service.WithdrawFundsAsync(withdrawal));

        // Assert
        Assert.IsType<InsufficientBalanceException>(exception);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Withdraw")]
    public async Task WithdrawFundsAsync_RepositoryThrows_PropagatesException()
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
                .ThrowsAsync(new InvalidOperationException("Database error during withdrawal"));

        var service = new OnlineWalletService(mockRepo.Object);
        var withdrawal = new Withdrawal
        {
            Amount = 50.0m
        };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.WithdrawFundsAsync(withdrawal));

        // Assert
        Assert.Equal("Database error during withdrawal", exception.Message);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Withdraw")]
    public async Task WithdrawFundsAsync_GetLastOnlineWalletEntryThrows_PropagatesException()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ThrowsAsync(new InvalidOperationException("GetLast failed"));

        var service = new OnlineWalletService(mockRepo.Object);
        var withdrawal = new Withdrawal
        {
            Amount = 10.0m
        };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.WithdrawFundsAsync(withdrawal));

        // Assert
        Assert.Equal("GetLast failed", exception.Message);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Withdraw")]
    public async Task WithdrawFundsAsync_NoPreviousTransactions_ThrowsInsufficientBalanceException()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync((OnlineWalletEntry?)null);

        var service = new OnlineWalletService(mockRepo.Object);
        var withdrawal = new Withdrawal
        {
            Amount = 10.0m
        };

        // Act
        var exception = await Assert.ThrowsAsync<InsufficientBalanceException>(() => service.WithdrawFundsAsync(withdrawal));

        // Assert
        Assert.IsType<InsufficientBalanceException>(exception);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Never);
    }
}
