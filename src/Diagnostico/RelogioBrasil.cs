namespace MiniLlmDidatico.Diagnostico;

public static class RelogioBrasil
{
    private static readonly TimeZoneInfo FusoBrasilSp = ResolverFusoBrasilSp();

    public static string NomeFuso => FusoBrasilSp.Id;

    public static DateTimeOffset AgoraBrasilSp()
    {
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, FusoBrasilSp);
    }

    public static DateTimeOffset ConverterUtcParaBrasilSp(DateTime instanteUtc)
    {
        var dateTimeUtc = instanteUtc.Kind == DateTimeKind.Utc
            ? instanteUtc
            : DateTime.SpecifyKind(instanteUtc, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTime(new DateTimeOffset(dateTimeUtc), FusoBrasilSp);
    }

    private static TimeZoneInfo ResolverFusoBrasilSp()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }
}
