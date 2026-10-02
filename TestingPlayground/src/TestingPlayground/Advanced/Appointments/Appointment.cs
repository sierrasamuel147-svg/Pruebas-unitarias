namespace TestingPlayground.Advanced.Appointments;

public record Appointment(
    Guid Id,
    string Customer,
    DateTime Start,
    DateTime End
);
