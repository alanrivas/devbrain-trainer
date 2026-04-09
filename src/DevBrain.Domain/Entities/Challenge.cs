using DevBrain.Domain.Enums;
using DevBrain.Domain.Exceptions;

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

    private Challenge(
        Guid id,
        string title,
        string description,
        ChallengeCategory category,
        Difficulty difficulty,
        string correctAnswer,
        int timeLimitSecs,
        DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Category = category;
        Difficulty = difficulty;
        CorrectAnswer = correctAnswer;
        TimeLimitSecs = timeLimitSecs;
        CreatedAt = createdAt;
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
            createdAt: DateTimeOffset.UtcNow
        );
    }

    public bool IsCorrectAnswer(string attempt) =>
        string.Equals(attempt.Trim(), CorrectAnswer, StringComparison.OrdinalIgnoreCase);
}
