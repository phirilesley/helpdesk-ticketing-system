namespace HelpDeskSystem.Domain.ValueObjects;

public class TicketNumber
{
    public string Value { get; private set; }

    private TicketNumber(string value)
    {
        Value = value;
    }

    public static TicketNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Ticket number cannot be empty");

        return new TicketNumber(value);
    }

    public override string ToString() => Value;
}