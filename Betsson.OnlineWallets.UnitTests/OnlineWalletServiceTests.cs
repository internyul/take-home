using Moq;
using Betsson.OnlineWallets.Services;
using Betsson.OnlineWallets.Data.Repositories;
using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Models;
using Betsson.OnlineWallets.Exceptions;

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

    [Fact]
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
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Never);
    }

    [Fact]
    public async Task WithdrawFundsAsync_SufficientBalance_UpdatesBalance()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = 100.0m,
            BalanceBefore = 50.0m
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

        var service = new OnlineWalletService(mockRepo.Object);
        var withdrawal = new Withdrawal
        {
            Amount = 75.0m
        };

        // Act
        var newBalance = await service.WithdrawFundsAsync(withdrawal);

        // Assert
        Assert.Equal(75.0m, newBalance.Amount);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(entry =>
            entry.Amount == -75.0m &&
            entry.BalanceBefore == 150.0m
        )), Times.Once);
    }

    [Fact]
    public async Task WithdrawFundsAsync_ExactBalance_ResultsInZeroBalance()
    {
        // Arrange
        var mockRepo = new Mock<IOnlineWalletRepository>();
        var lastEntry = new OnlineWalletEntry
        {
            Amount = 200.0m,
            BalanceBefore = 100.0m
        };
        mockRepo.Setup(repo => repo.GetLastOnlineWalletEntryAsync())
                .ReturnsAsync(lastEntry);
        mockRepo.Setup(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()))
                .Returns(Task.CompletedTask);

        var service = new OnlineWalletService(mockRepo.Object);
        var withdrawal = new Withdrawal
        {
            Amount = 300.0m
        };

        // Act
        var newBalance = await service.WithdrawFundsAsync(withdrawal);

        // Assert
        Assert.Equal(0.0m, newBalance.Amount);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.Is<OnlineWalletEntry>(entry =>
            entry.Amount == -300.0m &&
            entry.BalanceBefore == 300.0m
        )), Times.Once);
    }

    [Fact]
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
        Assert.Equal("Invalid withdrawal amount. There are insufficient funds.", exception.Message);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Never);
    }

    [Fact]
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
        Assert.Equal("Invalid withdrawal amount. There are insufficient funds.", exception.Message);
        mockRepo.Verify(repo => repo.GetLastOnlineWalletEntryAsync(), Times.Once);
        mockRepo.Verify(repo => repo.InsertOnlineWalletEntryAsync(It.IsAny<OnlineWalletEntry>()), Times.Never);
    }
}
