using DevBrain.Domain.Exceptions;

namespace DevBrain.Domain.ValueObjects;

public sealed class CodeTestCase
{
    public string Input { get; }
    public string ExpectedOutput { get; }
    public string Description { get; }

    private CodeTestCase(string input, string expectedOutput, string description)
    {
        Input = input;
        ExpectedOutput = expectedOutput;
        Description = description;
    }

    public static CodeTestCase Create(string input, string expectedOutput, string description)
    {
        if (string.IsNullOrWhiteSpace(expectedOutput))
            throw new DomainException("ExpectedOutput is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        return new CodeTestCase(input ?? string.Empty, expectedOutput.Trim(), description.Trim());
    }
}
