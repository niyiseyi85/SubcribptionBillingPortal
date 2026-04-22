using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text.Json;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.DTOs;
using SubscriptionBillingPortal.Application.Features.Customers.Commands.CreateCustomer;
using SubscriptionBillingPortal.Application.Mappings;
using SubscriptionBillingPortal.Domain.Aggregates;

namespace SubscriptionBillingPortal.UnitTests.Application.Features.Customers;

/// <summary>
/// Unit tests for CreateCustomerCommandHandler.
/// All dependencies are mocked — no infrastructure involved.
/// </summary>
public sealed class CreateCustomerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock;
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        MappingConfiguration.Configure();

        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Customers).Returns(_customerRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _idempotencyServiceMock = new Mock<IIdempotencyService>();
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _handler = new CreateCustomerCommandHandler(
            _unitOfWorkMock.Object,
            _idempotencyServiceMock.Object,
            NullLogger<CreateCustomerCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnCustomerDto()
    {
        var command = new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Doe");
        result.FullName.Should().Be("Jane Doe");
        result.Email.Should().Be("jane@example.com");
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCallAddAndSave()
    {
        var command = new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.NewGuid());

        await _handler.Handle(command, CancellationToken.None);

        _customerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldReturnCachedResponse()
    {
        var cachedDto = new CustomerDto(
            Guid.NewGuid(), "Jane", "Doe", "Jane Doe", "jane@example.com", DateTimeOffset.UtcNow);

        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _idempotencyServiceMock
            .Setup(s => s.GetResponseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(JsonSerializer.Serialize(cachedDto));

        var command = new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.NewGuid());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(cachedDto.Id);
        result.Email.Should().Be(cachedDto.Email);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldMarkIdempotencyKeyAsProcessed()
    {
        var idempotencyKey = Guid.NewGuid();
        var command = new CreateCustomerCommand("Jane", "Doe", "jane@example.com", idempotencyKey);

        await _handler.Handle(command, CancellationToken.None);

        _idempotencyServiceMock.Verify(s =>
            s.MarkAsProcessedAsync(idempotencyKey, nameof(CreateCustomerCommand), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
