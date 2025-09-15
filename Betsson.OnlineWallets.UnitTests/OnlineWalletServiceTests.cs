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

        // Act
        var service = new OnlineWalletService(mockRepo.Object);
        var balance = await service.GetBalanceAsync();


        // Assert
        Assert.Equal(0, balance.Amount);
    }
}