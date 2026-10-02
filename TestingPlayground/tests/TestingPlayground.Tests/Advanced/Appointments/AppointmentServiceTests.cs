using FluentAssertions;
using Moq;
using TestingPlayground.Advanced.Appointments;
using TestingPlayground.Advanced.Common;
using Xunit;

namespace TestingPlayground.Tests.Advanced.Appointments;

public class AppointmentServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 2, 15, 30, 0, DateTimeKind.Utc);

    private readonly Mock<IAppointmentRepository> _repository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        _clock.Setup(clock => clock.UtcNow).Returns(Now);

        _service = new AppointmentService(_repository.Object, _clock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ScheduleAsync_CustomerIsEmpty_ThrowsArgumentException(string? customer)
    {
        Func<Task> act = () => _service.ScheduleAsync(customer!, Now.AddHours(1), Now.AddHours(2));

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("customer");
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-24 * 60)]
    public async Task ScheduleAsync_StartIsInPast_ThrowsArgumentException(int minutesFromNow)
    {
        // minutesFromNow = 0 covers the boundary start == UtcNow, which is also rejected.
        var start = Now.AddMinutes(minutesFromNow);

        Func<Task> act = () => _service.ScheduleAsync("Ana", start, start.AddHours(1));

        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("start");
        _repository.Verify(
            repository => repository.HasOverlapAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            repository => repository.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public async Task ScheduleAsync_EndIsBeforeOrEqualToStart_ThrowsArgumentException(int endMinutesFromStart)
    {
        var start = Now.AddDays(1);
        var end = start.AddMinutes(endMinutesFromStart);

        // Plain xUnit equivalent of act.Should().ThrowAsync<ArgumentException>().WithParameterName("end").
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ScheduleAsync("Ana", start, end));

        Assert.Equal("end", exception.ParamName);
        _repository.Verify(
            repository => repository.HasOverlapAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            repository => repository.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ScheduleAsync_AppointmentOverlaps_ThrowsInvalidOperationException()
    {
        var start = Now.AddDays(1);
        var end = start.AddHours(1);
        _repository
            .Setup(repository => repository.HasOverlapAsync(start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Func<Task> act = () => _service.ScheduleAsync("Ana", start, end);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*overlaps*");
        _repository.Verify(
            repository => repository.HasOverlapAsync(start, end, It.IsAny<CancellationToken>()),
            Times.Once);
        _repository.Verify(
            repository => repository.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ScheduleAsync_ValidAppointment_CreatesAndSavesAppointment()
    {
        var start = Now.AddDays(1);
        var end = start.AddHours(1);
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;

        Appointment? savedAppointment = null;
        _repository
            .Setup(repository => repository.HasOverlapAsync(start, end, token))
            .ReturnsAsync(false);
        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()))
            .Callback<Appointment, CancellationToken>((appointment, _) => savedAppointment = appointment)
            .Returns(Task.CompletedTask);

        var result = await _service.ScheduleAsync("Ana", start, end, token);

        result.Should().BeEquivalentTo(new { Customer = "Ana", Start = start, End = end });
        result.Id.Should().NotBeEmpty();

        Assert.NotNull(savedAppointment);
        savedAppointment.Should().BeSameAs(result);

        // Passing the concrete token (not It.IsAny) proves the caller's token reaches the repository.
        _repository.Verify(repository => repository.HasOverlapAsync(start, end, token), Times.Once);
        _repository.Verify(repository => repository.AddAsync(result, token), Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_StartOneTickAfterNow_IsAccepted()
    {
        var start = Now.AddTicks(1);

        var result = await _service.ScheduleAsync("Ana", start, start.AddHours(1));

        result.Start.Should().Be(start);
        _repository.Verify(
            repository => repository.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_CustomerWithSurroundingSpaces_StoresTrimmedName()
    {
        var result = await _service.ScheduleAsync("  Ana  ", Now.AddDays(1), Now.AddDays(1).AddHours(1));

        result.Customer.Should().Be("Ana");
    }

    [Fact]
    public async Task ScheduleAsync_TwoValidAppointments_GenerateDifferentIds()
    {
        var first = await _service.ScheduleAsync("Ana", Now.AddDays(1), Now.AddDays(1).AddHours(1));
        var second = await _service.ScheduleAsync("Luis", Now.AddDays(2), Now.AddDays(2).AddHours(1));

        second.Id.Should().NotBe(first.Id);
        _repository.Verify(
            repository => repository.AddAsync(It.IsAny<Appointment>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
