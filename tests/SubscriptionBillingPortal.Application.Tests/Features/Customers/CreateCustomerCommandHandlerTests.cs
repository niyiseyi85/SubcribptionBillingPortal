using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SubscriptionBillingPortal.Application.Contracts.Persistence;
using SubscriptionBillingPortal.Application.Contracts.Services;
using SubscriptionBillingPortal.Application.Features.Customers.Commands.CreateCustomer;
using SubscriptionBillingPortal.Application.Mappings;
using SubscriptionBillingPortal.Domain.Aggregates;

namespace SubscriptionBillingPortal.Application.Tests.Features.Customers;

/// <summary>
/// Application-layer tests for CreateCustomerCommandHandler.
/// Uses Moq to isolate the handler from infrastructure dependencies.
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
        // Arrange
        var command = new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
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
        // Arrange
        var command = new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _customerRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdempotencyKeyAlreadyProcessed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _idempotencyServiceMock
            .Setup(s => s.HasBeenProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new CreateCustomerCommand("Jane", "Doe", "jane@example.com", Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been processed*");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldMarkIdempotencyKeyAsProcessed()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid();
        var command = new CreateCustomerCommand("Jane", "Doe", "jane@example.com", idempotencyKey);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _idempotencyServiceMock.Verify(s =>
            s.MarkAsProcessedAsync(idempotencyKey, nameof(CreateCustomerCommand), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
