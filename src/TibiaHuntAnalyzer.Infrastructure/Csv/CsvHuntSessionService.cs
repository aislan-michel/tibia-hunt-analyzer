using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using TibiaHuntAnalyzer.Application.Csv;
using TibiaHuntAnalyzer.Domain;
using TibiaHuntAnalyzer.Infrastructure.Persistence;

namespace TibiaHuntAnalyzer.Infrastructure.Csv;

public sealed class CsvHuntSessionService(
    IDbContextFactory<HuntAnalyzerDbContext> dbContextFactory) : ICsvHuntSessionService
{
    private const string SessionRow = "Session";
    private const string MonsterRow = "Monster";
    private const string ItemRow = "Item";

    private static readonly string[] Headers =
    [
        nameof(CsvSessionRow.RecordType),
        nameof(CsvSessionRow.SessionId),
        nameof(CsvSessionRow.CharacterId),
        nameof(CsvSessionRow.HuntLocationId),
        nameof(CsvSessionRow.CharacterLevel),
        nameof(CsvSessionRow.SessionStart),
        nameof(CsvSessionRow.SessionEnd),
        nameof(CsvSessionRow.DurationTicks),
        nameof(CsvSessionRow.InputFormat),
        nameof(CsvSessionRow.OriginalInput),
        nameof(CsvSessionRow.Balance),
        nameof(CsvSessionRow.Damage),
        nameof(CsvSessionRow.DamagePerHour),
        nameof(CsvSessionRow.Healing),
        nameof(CsvSessionRow.HealingPerHour),
        nameof(CsvSessionRow.Loot),
        nameof(CsvSessionRow.Supplies),
        nameof(CsvSessionRow.XpGain),
        nameof(CsvSessionRow.XpPerHour),
        nameof(CsvSessionRow.RawXpGain),
        nameof(CsvSessionRow.RawXpPerHour),
        nameof(CsvSessionRow.RecordName),
        nameof(CsvSessionRow.RecordCount)
    ];

    public async Task ExportAsync(TextWriter writer, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var sessions = await dbContext.HuntSessions
            .AsNoTracking()
            .Include(session => session.Character)
            .Include(session => session.HuntLocation)
            .Include(session => session.KilledMonsters)
            .Include(session => session.LootedItems)
            .ToListAsync(cancellationToken);
        sessions = sessions
            .OrderBy(session => session.SessionStart)
            .ToList();

        using var csv = CreateCsvWriter(writer);
        csv.WriteHeader<CsvSessionRow>();
        await csv.NextRecordAsync();

        var rows = new List<CsvSessionRow>();
        foreach (var session in sessions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(ToSessionRow(session));

            foreach (var monster in session.KilledMonsters)
            {
                rows.Add(new CsvSessionRow
                {
                    RecordType = MonsterRow,
                    SessionId = session.Id.ToString("D"),
                    RecordName = monster.Name,
                    RecordCount = Format(monster.Count)
                });
            }

            foreach (var item in session.LootedItems)
            {
                rows.Add(new CsvSessionRow
                {
                    RecordType = ItemRow,
                    SessionId = session.Id.ToString("D"),
                    RecordName = item.Name,
                    RecordCount = Format(item.Quantity)
                });
            }
        }

        await csv.WriteRecordsAsync(rows, cancellationToken);
        await csv.FlushAsync();
    }

    public async Task<CsvImportResult> ImportAsync(
        TextReader reader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var errors = new List<CsvImportError>();
        var sessions = new Dictionary<Guid, PendingSession>();
        try
        {
            using var csv = CreateCsvReader(reader);
            if (!await csv.ReadAsync())
            {
                return CsvImportResult.Failure([
                    new CsvImportError(1, "Header", "The CSV file is empty.")
                ]);
            }

            csv.ReadHeader();
            if (csv.HeaderRecord is null || !Headers.SequenceEqual(csv.HeaderRecord, StringComparer.Ordinal))
            {
                return CsvImportResult.Failure([
                    new CsvImportError(1, "Header", $"Expected the schema columns in this exact order: {string.Join(",", Headers)}.")
                ]);
            }

            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rowNumber = csv.Parser.Row;
                CsvSessionRow row;
                try
                {
                    row = csv.GetRecord<CsvSessionRow>();
                }
                catch (CsvHelperException exception)
                {
                    errors.Add(new CsvImportError(rowNumber, "Row", exception.Message));
                    continue;
                }

                if (!Guid.TryParse(row.SessionId, out var sessionId) || sessionId == Guid.Empty)
                {
                    errors.Add(new CsvImportError(rowNumber, nameof(row.SessionId), "A non-empty session grouping ID is required."));
                    continue;
                }

                switch (row.RecordType)
                {
                    case SessionRow:
                        if (sessions.ContainsKey(sessionId))
                        {
                            errors.Add(new CsvImportError(rowNumber, nameof(row.SessionId), "A session ID may have only one Session row per file."));
                            continue;
                        }

                        var pending = ParseSessionRow(row, rowNumber, errors);
                        if (pending is not null)
                        {
                            sessions.Add(sessionId, pending);
                        }

                        break;
                    case MonsterRow:
                        AddChildRow(sessions, sessionId, row, rowNumber, errors, isMonster: true);
                        break;
                    case ItemRow:
                        AddChildRow(sessions, sessionId, row, rowNumber, errors, isMonster: false);
                        break;
                    default:
                        errors.Add(new CsvImportError(rowNumber, nameof(row.RecordType), "RecordType must be Session, Monster, or Item."));
                        break;
                }
            }
        }
        catch (CsvHelperException exception)
        {
            errors.Add(new CsvImportError(1, "CSV", exception.Message));
        }

        if (errors.Count > 0)
        {
            return CsvImportResult.Failure(errors);
        }

        if (sessions.Count == 0)
        {
            return CsvImportResult.Failure([
                new CsvImportError(1, nameof(CsvSessionRow.RecordType), "At least one Session row is required.")
            ]);
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var characterIds = sessions.Values.Select(session => session.CharacterId).Distinct().ToArray();
        var locationIds = sessions.Values.Select(session => session.HuntLocationId).Distinct().ToArray();
        var characters = await dbContext.Characters
            .Where(character => characterIds.Contains(character.Id) && character.IsActive)
            .ToDictionaryAsync(character => character.Id, cancellationToken);
        var locations = await dbContext.HuntLocations
            .Where(location => locationIds.Contains(location.Id) && location.IsActive)
            .ToDictionaryAsync(location => location.Id, cancellationToken);

        foreach (var session in sessions.Values)
        {
            if (!characters.ContainsKey(session.CharacterId))
            {
                errors.Add(new CsvImportError(session.Row, nameof(CsvSessionRow.CharacterId), "The character does not exist or is inactive."));
            }

            if (!locations.ContainsKey(session.HuntLocationId))
            {
                errors.Add(new CsvImportError(session.Row, nameof(CsvSessionRow.HuntLocationId), "The hunt location does not exist or is inactive."));
            }
        }

        if (errors.Count > 0)
        {
            return CsvImportResult.Failure(errors);
        }

        var entities = sessions.Values.Select(session => new HuntSession(
            characters[session.CharacterId],
            locations[session.HuntLocationId],
            session.CharacterLevel,
            session.SessionStart,
            session.SessionEnd,
            session.Duration,
            session.Statistics,
            session.Monsters.Select(record => new KilledMonster(record.Name, record.Count)),
            session.Items.Select(record => new LootedItem(record.Name, record.Count)),
            session.InputFormat,
            session.OriginalInput)).ToArray();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.HuntSessions.AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return CsvImportResult.Success(entities.Length);
    }

    private static CsvWriter CreateCsvWriter(TextWriter writer) =>
        new(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            NewLine = "\r\n"
        });

    private static CsvReader CreateCsvReader(TextReader reader) =>
        new(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            BadDataFound = args => throw new CsvHelperException(args.Context, $"Malformed CSV data: {args.Field}")
        });

    private static CsvSessionRow ToSessionRow(HuntSession session)
    {
        var statistics = session.Statistics;
        return new CsvSessionRow
        {
            RecordType = SessionRow,
            SessionId = session.Id.ToString("D"),
            CharacterId = session.CharacterId.ToString("D"),
            HuntLocationId = session.HuntLocationId.ToString("D"),
            CharacterLevel = Format(session.CharacterLevel),
            SessionStart = session.SessionStart.ToString("O", CultureInfo.InvariantCulture),
            SessionEnd = session.SessionEnd.ToString("O", CultureInfo.InvariantCulture),
            DurationTicks = Format(session.Duration.Ticks),
            InputFormat = session.InputFormat.ToString(),
            OriginalInput = session.OriginalInput,
            Balance = Format(statistics.Balance),
            Damage = Format(statistics.Damage),
            DamagePerHour = Format(statistics.DamagePerHour),
            Healing = Format(statistics.Healing),
            HealingPerHour = Format(statistics.HealingPerHour),
            Loot = Format(statistics.Loot),
            Supplies = Format(statistics.Supplies),
            XpGain = Format(statistics.XpGain),
            XpPerHour = Format(statistics.XpPerHour),
            RawXpGain = Format(statistics.RawXpGain),
            RawXpPerHour = Format(statistics.RawXpPerHour)
        };
    }

    private static PendingSession? ParseSessionRow(
        CsvSessionRow row,
        int rowNumber,
        ICollection<CsvImportError> errors)
    {
        var valid = true;
        var characterId = ParseGuid(row.CharacterId, rowNumber, nameof(row.CharacterId), errors, ref valid);
        var locationId = ParseGuid(row.HuntLocationId, rowNumber, nameof(row.HuntLocationId), errors, ref valid);
        var level = ParseInt(row.CharacterLevel, rowNumber, nameof(row.CharacterLevel), errors, ref valid);
        var start = ParseDate(row.SessionStart, rowNumber, nameof(row.SessionStart), errors, ref valid);
        var end = ParseDate(row.SessionEnd, rowNumber, nameof(row.SessionEnd), errors, ref valid);
        var durationTicks = ParseLong(row.DurationTicks, rowNumber, nameof(row.DurationTicks), errors, ref valid);
        if (durationTicks < 0)
        {
            errors.Add(new CsvImportError(rowNumber, nameof(row.DurationTicks), "DurationTicks cannot be negative."));
            valid = false;
        }

        if (level <= 0)
        {
            errors.Add(new CsvImportError(rowNumber, nameof(row.CharacterLevel), "CharacterLevel must be positive."));
            valid = false;
        }

        if (end < start)
        {
            errors.Add(new CsvImportError(rowNumber, nameof(row.SessionEnd), "SessionEnd cannot be earlier than SessionStart."));
            valid = false;
        }

        if (!Enum.TryParse<SessionInputFormat>(row.InputFormat, ignoreCase: false, out var inputFormat) ||
            !Enum.IsDefined(inputFormat))
        {
            errors.Add(new CsvImportError(rowNumber, nameof(row.InputFormat), "InputFormat must be Json or Text."));
            valid = false;
        }

        var statistics = ParseStatistics(row, rowNumber, errors, ref valid);
        if (!valid)
        {
            return null;
        }

        return new PendingSession(
            rowNumber,
            characterId,
            locationId,
            level,
            start,
            end,
            TimeSpan.FromTicks(durationTicks),
            inputFormat,
            row.OriginalInput,
            statistics);
    }

    private static HuntStatistics ParseStatistics(
        CsvSessionRow row,
        int rowNumber,
        ICollection<CsvImportError> errors,
        ref bool valid)
    {
        var balance = ParseNullableDecimal(
            row.Balance,
            rowNumber,
            nameof(row.Balance),
            errors,
            ref valid,
            allowNegative: true);
        var damage = ParseNullableLong(row.Damage, rowNumber, nameof(row.Damage), errors, ref valid);
        var damagePerHour = ParseNullableDecimal(row.DamagePerHour, rowNumber, nameof(row.DamagePerHour), errors, ref valid);
        var healing = ParseNullableLong(row.Healing, rowNumber, nameof(row.Healing), errors, ref valid);
        var healingPerHour = ParseNullableDecimal(row.HealingPerHour, rowNumber, nameof(row.HealingPerHour), errors, ref valid);
        var loot = ParseNullableDecimal(row.Loot, rowNumber, nameof(row.Loot), errors, ref valid);
        var supplies = ParseNullableDecimal(row.Supplies, rowNumber, nameof(row.Supplies), errors, ref valid);
        var xpGain = ParseNullableLong(row.XpGain, rowNumber, nameof(row.XpGain), errors, ref valid);
        var xpPerHour = ParseNullableDecimal(row.XpPerHour, rowNumber, nameof(row.XpPerHour), errors, ref valid);
        var rawXpGain = ParseNullableLong(row.RawXpGain, rowNumber, nameof(row.RawXpGain), errors, ref valid);
        var rawXpPerHour = ParseNullableDecimal(row.RawXpPerHour, rowNumber, nameof(row.RawXpPerHour), errors, ref valid);

        try
        {
            return new HuntStatistics(
                balance,
                damage,
                damagePerHour,
                healing,
                healingPerHour,
                loot,
                supplies,
                xpGain,
                xpPerHour,
                rawXpGain,
                rawXpPerHour);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            errors.Add(new CsvImportError(rowNumber, exception.ParamName ?? "Statistics", exception.Message));
            valid = false;
            return new HuntStatistics();
        }
    }

    private static void AddChildRow(
        IReadOnlyDictionary<Guid, PendingSession> sessions,
        Guid sessionId,
        CsvSessionRow row,
        int rowNumber,
        ICollection<CsvImportError> errors,
        bool isMonster)
    {
        if (!sessions.TryGetValue(sessionId, out var session))
        {
            errors.Add(new CsvImportError(rowNumber, nameof(row.SessionId), "A child row must follow its Session row."));
            return;
        }

        if (string.IsNullOrWhiteSpace(row.RecordName))
        {
            errors.Add(new CsvImportError(rowNumber, nameof(row.RecordName), "RecordName is required for child rows."));
            return;
        }

        long? count = null;
        if (!string.IsNullOrWhiteSpace(row.RecordCount))
        {
            if (!long.TryParse(row.RecordCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCount) ||
                parsedCount <= 0)
            {
                errors.Add(new CsvImportError(rowNumber, nameof(row.RecordCount), "RecordCount must be a positive whole number when provided."));
                return;
            }

            count = parsedCount;
        }

        var record = new PendingRecord(row.RecordName.Trim(), count);
        if (isMonster)
        {
            session.Monsters.Add(record);
        }
        else
        {
            session.Items.Add(record);
        }
    }

    private static Guid ParseGuid(
        string value,
        int row,
        string field,
        ICollection<CsvImportError> errors,
        ref bool valid)
    {
        if (Guid.TryParse(value, out var parsed) && parsed != Guid.Empty)
        {
            return parsed;
        }

        errors.Add(new CsvImportError(row, field, "Expected a non-empty GUID."));
        valid = false;
        return Guid.Empty;
    }

    private static int ParseInt(
        string value,
        int row,
        string field,
        ICollection<CsvImportError> errors,
        ref bool valid)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        errors.Add(new CsvImportError(row, field, "Expected a whole number."));
        valid = false;
        return 0;
    }

    private static long ParseLong(
        string value,
        int row,
        string field,
        ICollection<CsvImportError> errors,
        ref bool valid)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        errors.Add(new CsvImportError(row, field, "Expected a whole number."));
        valid = false;
        return 0;
    }

    private static DateTimeOffset ParseDate(
        string value,
        int row,
        string field,
        ICollection<CsvImportError> errors,
        ref bool valid)
    {
        if (DateTimeOffset.TryParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed))
        {
            return parsed;
        }

        errors.Add(new CsvImportError(row, field, "Expected an ISO 8601 round-trip timestamp."));
        valid = false;
        return default;
    }

    private static long? ParseNullableLong(
        string value,
        int row,
        string field,
        ICollection<CsvImportError> errors,
        ref bool valid)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed >= 0)
        {
            return parsed;
        }

        errors.Add(new CsvImportError(row, field, "Expected a non-negative whole number."));
        valid = false;
        return null;
    }

    private static decimal? ParseNullableDecimal(
        string value,
        int row,
        string field,
        ICollection<CsvImportError> errors,
        ref bool valid,
        bool allowNegative = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) &&
            (allowNegative || parsed >= 0))
        {
            return parsed;
        }

        errors.Add(new CsvImportError(row, field, "Expected an invariant-culture decimal value."));
        valid = false;
        return null;
    }

    private static string Format(long? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private static string Format(decimal? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Format(long value) => value.ToString(CultureInfo.InvariantCulture);

    private sealed class PendingSession(
        int row,
        Guid characterId,
        Guid huntLocationId,
        int characterLevel,
        DateTimeOffset sessionStart,
        DateTimeOffset sessionEnd,
        TimeSpan duration,
        SessionInputFormat inputFormat,
        string originalInput,
        HuntStatistics statistics)
    {
        public int Row { get; } = row;
        public Guid CharacterId { get; } = characterId;
        public Guid HuntLocationId { get; } = huntLocationId;
        public int CharacterLevel { get; } = characterLevel;
        public DateTimeOffset SessionStart { get; } = sessionStart;
        public DateTimeOffset SessionEnd { get; } = sessionEnd;
        public TimeSpan Duration { get; } = duration;
        public SessionInputFormat InputFormat { get; } = inputFormat;
        public string OriginalInput { get; } = originalInput;
        public HuntStatistics Statistics { get; } = statistics;
        public List<PendingRecord> Monsters { get; } = [];
        public List<PendingRecord> Items { get; } = [];
    }

    private sealed record PendingRecord(string Name, long? Count);
}
