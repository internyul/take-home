using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Betsson.OnlineWallets.Data.Models;
using Betsson.OnlineWallets.Web.Models;

namespace Betsson.OnlineWallets.ApiTests;

public class OnlineWalletApiTests
{
    private readonly HttpClient _httpClient;

    private readonly CustomWebApplicationFactory<Web.Startup> _factory;

    public OnlineWalletApiTests()
    {
        var dbName = Guid.NewGuid().ToString();
        _factory = new CustomWebApplicationFactory<Web.Startup>(dbName);
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public async Task GetBalance_EmptyWallet_ReturnsOk()
    {
        // Act
        var response = await _httpClient.GetAsync("/OnlineWallet/Balance");
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0.0m, balance?.Amount);
    }

    [Fact]
    public async Task GetBalance_ValidWallet_ReturnsOk()
    {
        // Arrange
        _factory.SetupWalletData(db =>
        {
            db.Transactions.Add(new OnlineWalletEntry
            {
                Amount = 100.0m,
                BalanceBefore = 0.0m,
                EventTime = DateTimeOffset.UtcNow
            });
        });

        // Act
        var response = await _httpClient.GetAsync("/OnlineWallet/Balance");
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(100.0m, balance?.Amount);
    }

    [Fact]
    public async Task Deposit_ValidRequest_ReturnsOk()
    {
        // Arrange
        var deposit = new DepositRequest { Amount = 100.0m };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(100.0m, balance?.Amount);
    }

    [Fact]
    public async Task Deposit_Negative_Amount_Returns_Bad_Request()
    {
        // Arrange
        var deposit = new DepositRequest { Amount = -200m };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_ValidRequest_ReturnsOk()
    {
        // Arrange
        var withdraw = new WithdrawalRequest { Amount = 200.0m };

        _factory.SetupWalletData(db =>
        {
            db.Transactions.Add(new OnlineWalletEntry
            {
                Amount = 200.0m,
                BalanceBefore = 0.0m,
                EventTime = DateTimeOffset.UtcNow
            });
        });

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Withdraw", withdraw);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0m, balance?.Amount);
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_ReturnsBadRequest()
    {
        // Arrange
        var withdraw = new WithdrawalRequest { Amount = 100.01m };

        _factory.SetupWalletData(db =>
        {
            db.Transactions.Add(new OnlineWalletEntry
            {
                Amount = 100.0m,
                BalanceBefore = 0.0m,
                EventTime = DateTimeOffset.UtcNow
            });
        });

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Withdraw", withdraw);
        var errorContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(errorContent);
        var typeValue = jsonDoc.RootElement.GetProperty("type").GetString();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("InsufficientBalanceException", typeValue);
    }

    [Fact]
    public async Task Deposit_Then_Withdraw_ReturnsCorrectBalance()
    {
        // Arrange
        var deposit = new DepositRequest { Amount = 200.0m };
        var withdraw = new WithdrawalRequest { Amount = 50.0m };

        // Act
        var depositResponse = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit);
        var balanceAfterDeposit = await depositResponse.Content.ReadFromJsonAsync<BalanceResponse>();

        var withdrawResponse = await _httpClient.PostAsJsonAsync("/OnlineWallet/Withdraw", withdraw);
        var balanceAfterWithdraw = await withdrawResponse.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, depositResponse.StatusCode);
        Assert.Equal(200.0m, balanceAfterDeposit?.Amount);
        Assert.Equal(HttpStatusCode.OK, withdrawResponse.StatusCode);
        Assert.Equal(150.0m, balanceAfterWithdraw?.Amount);
    }

    [Fact]
    public async Task Deposit_Then_GetBalance_ReturnsCorrectBalance()
    {
        // Arrange
        var deposit = new DepositRequest { Amount = 200.0m };

        // Act
        var depositResponse = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit);
        var balanceAfterDeposit = await depositResponse.Content.ReadFromJsonAsync<BalanceResponse>();

        var getBalanceResponse = await _httpClient.GetAsync("/OnlineWallet/Balance");
        var balanceAfterGet = await getBalanceResponse.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, depositResponse.StatusCode);
        Assert.Equal(200.0m, balanceAfterDeposit?.Amount);
        Assert.Equal(HttpStatusCode.OK, getBalanceResponse.StatusCode);
        Assert.Equal(200.0m, balanceAfterGet?.Amount);
    }
}