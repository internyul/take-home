using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
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

    private void AddWalletEntry(decimal resultingBalance, decimal balanceBefore = 0m) =>
        _factory.SetupWalletData(db =>
        {
            db.Transactions.Add(new OnlineWalletEntry
            {
                Amount = resultingBalance,
                BalanceBefore = balanceBefore,
                EventTime = DateTimeOffset.UtcNow
            });
        });

    [Fact]
    public async Task GetBalance_EmptyWallet_ReturnsZero()
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
        AddWalletEntry(100.0m);

        // Act
        var response = await _httpClient.GetAsync("/OnlineWallet/Balance");
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
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
        Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(100.0m, balance?.Amount);
    }

    [Fact]
    public async Task Deposit_NegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var deposit = new DepositRequest { Amount = -200m };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit);
        var errorContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("'Amount' must be greater than or equal to '0'.", errorContent);
    }

    [Fact]
    public async Task Deposit_ZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        var deposit = new DepositRequest { Amount = 0m };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0.0m, balance?.Amount);
    }

    [Fact]
    public async Task Deposit_LargeAmount_ReturnsOk()
    {
        // Arrange
        var deposit = new DepositRequest { Amount = 1_000_000_000m };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1_000_000_000m, balance?.Amount);
    }

    [Fact]
    public async Task Deposit_FractionalAmounts_AreRoundedCorrectly()
    {
        // Arrange
        var deposit = new DepositRequest { Amount = 0.015m };
        var deposit2 = new DepositRequest { Amount = 0.005m };

        // Act
        await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit);
        var response2 = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", deposit2);
        var balance = await response2.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(0.02m, balance?.Amount);
    }

    [Fact]
    public async Task Deposit_EmptyRequest_ReturnsBadRequest()
    {
        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Deposit", null as DepositRequest);
        var errorContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("A non-empty request body is required.", errorContent);
    }

    [Fact]
    public async Task Withdraw_ValidRequest_ReturnsOk()
    {
        // Arrange
        var withdraw = new WithdrawalRequest { Amount = 100.0m };

        AddWalletEntry(200.0m);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Withdraw", withdraw);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(100.0m, balance?.Amount);
    }


    [Fact]
    public async Task Withdraw_ZeroAmount_ReturnsOk()
    {
        // Arrange
        var withdraw = new WithdrawalRequest { Amount = 0m };

        AddWalletEntry(200.0m);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Withdraw", withdraw);
        var balance = await response.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(200.0m, balance?.Amount);
    }

    [Fact]
    public async Task Withdraw_InsufficientFunds_ReturnsBadRequest()
    {
        // Arrange
        var withdraw = new WithdrawalRequest { Amount = 100.01m };

        AddWalletEntry(100.0m);

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
    public async Task Withdraw_NegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var withdraw = new WithdrawalRequest { Amount = -100m };
        AddWalletEntry(100.0m);

        // Act
        var response = await _httpClient.PostAsJsonAsync("/OnlineWallet/Withdraw", withdraw);
        var errorContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("'Amount' must be greater than or equal to '0'.", errorContent);
    }

    [Fact]
    public async Task Withdraw_SequentialWithdrawals_ReturnsZero()
    {
        // Arrange
        var withdraw1 = new WithdrawalRequest { Amount = 100m };
        var withdraw2 = new WithdrawalRequest { Amount = 100m };
        AddWalletEntry(200.0m);

        // Act
        await _httpClient.PostAsJsonAsync("/OnlineWallet/Withdraw", withdraw1);
        var response2 = await _httpClient.PostAsJsonAsync("/OnlineWallet/Withdraw", withdraw2);
        var balance = await response2.Content.ReadFromJsonAsync<BalanceResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(0.0m, balance?.Amount);
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