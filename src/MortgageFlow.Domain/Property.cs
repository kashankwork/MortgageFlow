namespace MortgageFlow.Domain;

public sealed class Property
{
    private Property()
    {
        StreetAddress = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
        EstimatedValue = Money.Usd(0);
        OccupancyType = OccupancyType.PrimaryResidence;
    }

    private Property(
        string streetAddress,
        string city,
        string state,
        string postalCode,
        Money estimatedValue,
        OccupancyType occupancyType)
    {
        StreetAddress = streetAddress;
        City = city;
        State = state;
        PostalCode = postalCode;
        EstimatedValue = estimatedValue;
        OccupancyType = occupancyType;
    }

    public string StreetAddress { get; private set; }

    public string City { get; private set; }

    public string State { get; private set; }

    public string PostalCode { get; private set; }

    public Money EstimatedValue { get; private set; }

    public OccupancyType OccupancyType { get; private set; }

    public static Property Create(
        string streetAddress,
        string city,
        string state,
        string postalCode,
        Money estimatedValue,
        OccupancyType occupancyType = OccupancyType.PrimaryResidence)
    {
        if (string.IsNullOrWhiteSpace(streetAddress))
        {
            throw new DomainValidationException("Property street address is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new DomainValidationException("Property city is required.");
        }

        if (string.IsNullOrWhiteSpace(state) || state.Trim().Length != 2)
        {
            throw new DomainValidationException("Property state must be a two-letter code.");
        }

        if (string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainValidationException("Property postal code is required.");
        }

        return new Property(
            streetAddress.Trim(),
            city.Trim(),
            state.Trim().ToUpperInvariant(),
            postalCode.Trim(),
            estimatedValue,
            occupancyType);
    }
}
