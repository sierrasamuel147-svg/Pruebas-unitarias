using TestingPlayground.Advanced.Common;

namespace TestingPlayground.Advanced.Appointments;

public class AppointmentService
{
    private readonly IAppointmentRepository _repository;
    private readonly IClock _clock;

    public AppointmentService(IAppointmentRepository repository, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(clock);

        _repository = repository;
        _clock = clock;
    }

    public async Task<Appointment> ScheduleAsync(
        string customer,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customer))
        {
            throw new ArgumentException("Customer is required.", nameof(customer));
        }

        if (start <= _clock.UtcNow)
        {
            throw new ArgumentException("Appointment start must be in the future.", nameof(start));
        }

        if (end <= start)
        {
            throw new ArgumentException("Appointment end must be after its start.", nameof(end));
        }

        if (await _repository.HasOverlapAsync(start, end, cancellationToken))
        {
            throw new InvalidOperationException("The appointment overlaps an existing appointment.");
        }

        var appointment = new Appointment(Guid.NewGuid(), customer.Trim(), start, end);

        await _repository.AddAsync(appointment, cancellationToken);

        return appointment;
    }
}
