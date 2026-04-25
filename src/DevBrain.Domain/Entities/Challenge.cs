using DevBrain.Domain.Enums;
using DevBrain.Domain.Exceptions;
using DevBrain.Domain.ValueObjects;

namespace DevBrain.Domain.Entities;

public sealed class Challenge
{
    public Guid Id { get; }
    public string Title { get; }
    public string Description { get; }
    public ChallengeCategory Category { get; }
    public Difficulty Difficulty { get; }
    public string CorrectAnswer { get; }
    public int TimeLimitSecs { get; }
    public DateTimeOffset CreatedAt { get; }
    public ChallengeType Type { get; }
    public IReadOnlyList<string> Options { get; }
    public string StarterCode { get; }
    public IReadOnlyList<CodeTestCase> TestCases { get; }
    public IReadOnlyList<string> Items { get; }

    private Challenge(
        Guid id,
        string title,
        string description,
        ChallengeCategory category,
        Difficulty difficulty,
        string correctAnswer,
        int timeLimitSecs,
        DateTimeOffset createdAt,
        ChallengeType type,
        IReadOnlyList<string> options,
        string starterCode,
        IReadOnlyList<CodeTestCase> testCases,
        IReadOnlyList<string> items)
    {
        Id = id;
        Title = title;
        Description = description;
        Category = category;
        Difficulty = difficulty;
        CorrectAnswer = correctAnswer;
        TimeLimitSecs = timeLimitSecs;
        CreatedAt = createdAt;
        Type = type;
        Options = options;
        StarterCode = starterCode;
        TestCases = testCases;
        Items = items;
    }

    public static Challenge Create(
        string title,
        string description,
        ChallengeCategory category,
        Difficulty difficulty,
        string correctAnswer,
        int timeLimitSecs)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            throw new DomainException("Title must be between 5 and 100 characters.");

        if (title.Trim().Length > 100)
            throw new DomainException("Title must be between 5 and 100 characters.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        if (string.IsNullOrWhiteSpace(correctAnswer))
            throw new DomainException("Correct answer is required.");

        if (timeLimitSecs < 30 || timeLimitSecs > 300)
            throw new DomainException("Time limit must be between 30 and 300 seconds.");

        return new Challenge(
            id: Guid.NewGuid(),
            title: title.Trim(),
            description: description.Trim(),
            category: category,
            difficulty: difficulty,
            correctAnswer: correctAnswer.Trim(),
            timeLimitSecs: timeLimitSecs,
            createdAt: DateTimeOffset.UtcNow,
            type: ChallengeType.OpenText,
            options: Array.Empty<string>(),
            starterCode: string.Empty,
            testCases: Array.Empty<CodeTestCase>(),
            items: Array.Empty<string>()
        );
    }

    public static Challenge CreateMultipleChoice(
        string title,
        string description,
        ChallengeCategory category,
        Difficulty difficulty,
        string correctAnswer,
        int timeLimitSecs,
        IEnumerable<string> options)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            throw new DomainException("Title must be between 5 and 100 characters.");

        if (title.Trim().Length > 100)
            throw new DomainException("Title must be between 5 and 100 characters.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        if (timeLimitSecs < 30 || timeLimitSecs > 300)
            throw new DomainException("Time limit must be between 30 and 300 seconds.");

        var trimmedOptions = options
            .Select(o => o?.Trim() ?? string.Empty)
            .ToList();

        if (trimmedOptions.Count < 2 || trimmedOptions.Count > 4)
            throw new DomainException("Multiple choice challenges must have between 2 and 4 options.");

        if (trimmedOptions.Any(string.IsNullOrEmpty))
            throw new DomainException("Options cannot be empty or whitespace.");

        var distinct = trimmedOptions
            .Select(o => o.ToLowerInvariant())
            .Distinct()
            .Count();

        if (distinct != trimmedOptions.Count)
            throw new DomainException("Options must be unique (case-insensitive).");

        var trimmedAnswer = correctAnswer?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmedAnswer))
            throw new DomainException("Correct answer is required.");

        var answerInOptions = trimmedOptions
            .Any(o => string.Equals(o, trimmedAnswer, StringComparison.OrdinalIgnoreCase));

        if (!answerInOptions)
            throw new DomainException("Correct answer must match one of the provided options.");

        return new Challenge(
            id: Guid.NewGuid(),
            title: title.Trim(),
            description: description.Trim(),
            category: category,
            difficulty: difficulty,
            correctAnswer: trimmedAnswer,
            timeLimitSecs: timeLimitSecs,
            createdAt: DateTimeOffset.UtcNow,
            type: ChallengeType.MultipleChoice,
            options: trimmedOptions.AsReadOnly(),
            starterCode: string.Empty,
            testCases: Array.Empty<CodeTestCase>(),
            items: Array.Empty<string>()
        );
    }

    public static Challenge CreateCodeRunner(
        string title,
        string description,
        ChallengeCategory category,
        Difficulty difficulty,
        int timeLimitSecs,
        string starterCode,
        IEnumerable<CodeTestCase> testCases)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            throw new DomainException("Title must be between 5 and 100 characters.");

        if (title.Trim().Length > 100)
            throw new DomainException("Title must be between 5 and 100 characters.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        if (timeLimitSecs < 30 || timeLimitSecs > 300)
            throw new DomainException("Time limit must be between 30 and 300 seconds.");

        var caseList = testCases?.ToList() ?? [];

        if (caseList.Count < 1 || caseList.Count > 5)
            throw new DomainException("CodeRunner challenges must have between 1 and 5 test cases.");

        return new Challenge(
            id: Guid.NewGuid(),
            title: title.Trim(),
            description: description.Trim(),
            category: category,
            difficulty: difficulty,
            correctAnswer: "PASS",
            timeLimitSecs: timeLimitSecs,
            createdAt: DateTimeOffset.UtcNow,
            type: ChallengeType.CodeRunner,
            options: Array.Empty<string>(),
            starterCode: starterCode ?? string.Empty,
            testCases: caseList.AsReadOnly(),
            items: Array.Empty<string>()
        );
    }

    public static Challenge CreateOrdering(
        string title,
        string description,
        ChallengeCategory category,
        Difficulty difficulty,
        int timeLimitSecs,
        IEnumerable<string> items,
        IEnumerable<string> correctOrder)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length < 5)
            throw new DomainException("Title must be between 5 and 100 characters.");

        if (title.Trim().Length > 100)
            throw new DomainException("Title must be between 5 and 100 characters.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        if (timeLimitSecs < 30 || timeLimitSecs > 300)
            throw new DomainException("Time limit must be between 30 and 300 seconds.");

        var trimmedItems = items?.Select(i => i?.Trim() ?? string.Empty).ToList()
            ?? throw new DomainException("Items are required.");

        if (trimmedItems.Count < 2 || trimmedItems.Count > 6)
            throw new DomainException("Ordering challenges must have between 2 and 6 items.");

        if (trimmedItems.Any(string.IsNullOrEmpty))
            throw new DomainException("Items cannot be empty or whitespace.");

        var distinctCount = trimmedItems.Select(i => i.ToLowerInvariant()).Distinct().Count();
        if (distinctCount != trimmedItems.Count)
            throw new DomainException("Items must be unique (case-insensitive).");

        var trimmedOrder = correctOrder?.Select(i => i?.Trim() ?? string.Empty).ToList()
            ?? throw new DomainException("Correct order is required.");

        if (trimmedOrder.Count != trimmedItems.Count)
            throw new DomainException("Correct order must contain exactly the same items.");

        var itemsSet = trimmedItems.Select(i => i.ToLowerInvariant()).ToHashSet();
        foreach (var item in trimmedOrder)
        {
            if (!itemsSet.Contains(item.ToLowerInvariant()))
                throw new DomainException($"Correct order contains item not present in items: '{item}'.");
        }

        var correctAnswer = string.Join("|", trimmedOrder);

        return new Challenge(
            id: Guid.NewGuid(),
            title: title.Trim(),
            description: description.Trim(),
            category: category,
            difficulty: difficulty,
            correctAnswer: correctAnswer,
            timeLimitSecs: timeLimitSecs,
            createdAt: DateTimeOffset.UtcNow,
            type: ChallengeType.Ordering,
            options: Array.Empty<string>(),
            starterCode: string.Empty,
            testCases: Array.Empty<CodeTestCase>(),
            items: trimmedItems.AsReadOnly()
        );
    }

    // For EF Core seeding only - uses fixed values to ensure deterministic migrations
    public static Challenge CreateForSeeding(
        Guid id,
        string title,
        string description,
        ChallengeCategory category,
        Difficulty difficulty,
        string correctAnswer,
        int timeLimitSecs,
        DateTimeOffset createdAt,
        ChallengeType type = ChallengeType.OpenText,
        IEnumerable<string>? options = null,
        string starterCode = "",
        IEnumerable<CodeTestCase>? testCases = null,
        IEnumerable<string>? items = null)
    {
        return new Challenge(
            id: id,
            title: title.Trim(),
            description: description.Trim(),
            category: category,
            difficulty: difficulty,
            correctAnswer: correctAnswer.Trim(),
            timeLimitSecs: timeLimitSecs,
            createdAt: createdAt,
            type: type,
            options: options?.Select(o => o.Trim()).ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>(),
            starterCode: starterCode ?? string.Empty,
            testCases: testCases?.ToList().AsReadOnly() ?? (IReadOnlyList<CodeTestCase>)Array.Empty<CodeTestCase>(),
            items: items?.Select(i => i.Trim()).ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>()
        );
    }

    public bool IsCorrectAnswer(string attempt) =>
        string.Equals(attempt.Trim(), CorrectAnswer, StringComparison.OrdinalIgnoreCase);
}
