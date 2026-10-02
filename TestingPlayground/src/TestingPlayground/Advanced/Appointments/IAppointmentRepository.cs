namespace TestingPlayground.Advanced.Appointments;

public interface IAppointmentRepository
{
    Task<bool> HasOverlapAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        Appointment appointment,
        CancellationToken cancellationToken = default
    );
}
