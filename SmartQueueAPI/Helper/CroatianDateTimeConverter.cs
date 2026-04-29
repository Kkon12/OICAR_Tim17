using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartQueueAPI.Helpers
{
    public class CroatianDateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly TimeZoneInfo CroatianTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

        private const string DateFormat = "dd/MM/yyyy HH:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader,
            Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (DateTime.TryParseExact(str, DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
                return result;

            return DateTime.Parse(str!, CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer,
            DateTime value, JsonSerializerOptions options)
        {
            // Convert UTC → Croatian local time before displaying
            var croatianTime = TimeZoneInfo.ConvertTimeFromUtc(
                value.Kind == DateTimeKind.Utc
                    ? value
                    : DateTime.SpecifyKind(value, DateTimeKind.Utc),
                CroatianTimeZone);

            writer.WriteStringValue(
                croatianTime.ToString(DateFormat, CultureInfo.InvariantCulture));
        }
    }

    // Same for nullable DateTime?
    public class CroatianNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        private readonly CroatianDateTimeConverter _inner = new();

        public override DateTime? Read(ref Utf8JsonReader reader, 
            Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return _inner.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(Utf8JsonWriter writer,
            DateTime? value, JsonSerializerOptions options)
        {
            if (value == null) { writer.WriteNullValue(); return; }
            _inner.Write(writer, value.Value, options);
        }
    }
}


/*Why two converters: One handles DateTime, 
 * the other handles DateTime? (nullable) , both are common in  models (e.g. CalledAt?, CompletedAt?).
Why UTC internally: Croatia is UTC+1 (winter) and UTC+2 (summer/DST). 
Storing UTC means the DB is always correct regardless of daylight saving time changes.
We only convert at the display layer.*/