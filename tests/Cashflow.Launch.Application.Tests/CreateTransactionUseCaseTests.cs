using Cashflow.Launch.Application.DTOs;
using Cashflow.Launch.Application.Ports;
using Cashflow.Launch.Application.UseCases;
using Cashflow.Launch.Domain;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cashflow.Launch.Application.Tests;

public class CreateTransactionUseCaseTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepository;
    private readonly Mock<IMessageBrokerPublisher> _mockMessageBrokerPublisher;
    private readonly Mock<ILogger<CreateTransactionUseCase>> _mockLogger;
    private readonly CreateTransactionUseCase _useCase;

    public CreateTransactionUseCaseTests()
    {
        _mockTransactionRepository = new Mock<ITransactionRepository>();
        _mockMessageBrokerPublisher = new Mock<IMessageBrokerPublisher>();
        _mockLogger = new Mock<ILogger<CreateTransactionUseCase>>();

        _useCase = new CreateTransactionUseCase(
            _mockTransactionRepository.Object,
            _mockMessageBrokerPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Persist_And_Publish_When_Request_Is_Valid()
    {
        var request = new CreateTransactionRequest(DateTime.Now, 150.75m, "Credit", "pix");

        await _useCase.ExecuteAsync(request);

        _mockTransactionRepository.Verify(
            repo => repo.AddAsync(It.Is<Transaction>(t => t.Amount == request.Amount)),
            Times.Once);

        _mockMessageBrokerPublisher.Verify(
            pub => pub.PublishAsync(It.Is<TransactionCreatedEvent>(e => e.Amount == request.Amount)),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ThrowArgumentException_When_Type_Is_Invalid()
    {
        var request = new CreateTransactionRequest(DateTime.Now, 100m, "InvalidType", "pix");

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request));

        _mockTransactionRepository.Verify(repo => repo.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _mockMessageBrokerPublisher.Verify(pub => pub.PublishAsync(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Should_ThrowArgumentException_When_DomainExceptionOccurs()
    {
        var request = new CreateTransactionRequest(DateTime.Now, 0m, "Debit", "pix");

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request));

        _mockTransactionRepository.Verify(repo => repo.AddAsync(It.IsAny<Transaction>()), Times.Never);
        _mockMessageBrokerPublisher.Verify(pub => pub.PublishAsync(It.IsAny<object>()), Times.Never);
    }
}