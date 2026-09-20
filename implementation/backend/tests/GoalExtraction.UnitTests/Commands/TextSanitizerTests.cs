namespace GoalExtraction.UnitTests.Commands;

using FluentAssertions;
using GoalExtraction.Application.Common;
using Xunit;

public class TextSanitizerTests
{
    [Fact]
    public void SanitizeTranscript_RemovesControlCharacters_PreservesNewlinesAndTabs()
    {
        // Arrange
        var raw = "Hello\u0000\u0007 World!\r\nLine 2\tTabbed\u001F.";

        // Act
        var result = TextSanitizer.SanitizeTranscript(raw);

        // Assert
        result.Should().Be("Hello World!\nLine 2\tTabbed.");
        result.Should().NotContain("\u0000");
        result.Should().NotContain("\u0007");
        result.Should().NotContain("\u001F");
        result.Should().Contain("\n");
        result.Should().Contain("\t");
    }

    [Fact]
    public void SanitizeTranscript_NullOrEmpty_ReturnsEmptyString()
    {
        TextSanitizer.SanitizeTranscript(null).Should().Be(string.Empty);
        TextSanitizer.SanitizeTranscript(string.Empty).Should().Be(string.Empty);
        TextSanitizer.SanitizeTranscript("   ").Should().Be(string.Empty);
    }

    [Fact]
    public void ComputeSha256Hash_ProducesConsistentLowercaseHexString()
    {
        // Arrange
        var text1 = "Consistent Transcript Content";
        var text2 = "Consistent Transcript Content";

        // Act
        var hash1 = TextSanitizer.ComputeSha256Hash(text1);
        var hash2 = TextSanitizer.ComputeSha256Hash(text2);

        // Assert
        hash1.Should().NotBeNullOrWhiteSpace();
        hash1.Length.Should().Be(64);
        hash1.Should().Be(hash2);
        hash1.Should().Be(hash1.ToLowerInvariant());
    }

    [Fact]
    public void ComputeSha256Hash_DifferentContent_ProducesDifferentHashes()
    {
        var hash1 = TextSanitizer.ComputeSha256Hash("Content A");
        var hash2 = TextSanitizer.ComputeSha256Hash("Content B");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void SanitizeTranscript_StripsAllNonPrintableAsciiControlCharactersAndDel()
    {
        // Construct string with all control characters 0-31 and 127
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 32; i++)
        {
            sb.Append((char)i);
        }
        sb.Append((char)127); // DEL
        sb.Append("Valid Content");

        var sanitized = TextSanitizer.SanitizeTranscript(sb.ToString());

        // Should strip all control characters except \n (10), \r (13 -> normalized to \n), and \t (9)
        for (int i = 0; i < 32; i++)
        {
            if (i == 9 || i == 10 || i == 13) continue;
            sanitized.Should().NotContain(((char)i).ToString(), $"Control character {i} must be stripped");
        }
        sanitized.Should().NotContain(((char)127).ToString(), "DEL character (127) must be stripped");
        sanitized.Should().Contain("Valid Content");
    }

    [Fact]
    public void ComputeSha256Hash_MatchesStandardSha256Vector_Deterministically()
    {
        // Standard NIST test vector: SHA-256("abc") = ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad
        const string expectedHash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        
        for (int i = 0; i < 50; i++)
        {
            var hash = TextSanitizer.ComputeSha256Hash("abc");
            hash.Should().Be(expectedHash, "SHA-256 must be deterministic across multiple calls");
        }
    }

    [Fact]
    public void ComputeSha256Hash_NullInput_ComputesEmptyStringHash()
    {
        // Arrange & Act
        var hash = TextSanitizer.ComputeSha256Hash(null);

        // Assert - SHA-256 of empty string is e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
        hash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Theory]
    [InlineData("Quick test string")]
    [InlineData(null)]
    public void Sanitize_Alias_MatchesSanitizeTranscript(string? input)
    {
        TextSanitizer.Sanitize(input).Should().Be(TextSanitizer.SanitizeTranscript(input));
    }

    [Theory]
    [InlineData("SHA alias test")]
    [InlineData(null)]
    public void ComputeSha256_Alias_MatchesComputeSha256Hash(string? input)
    {
        TextSanitizer.ComputeSha256(input).Should().Be(TextSanitizer.ComputeSha256Hash(input));
    }

    [Fact]
    public void SanitizeTranscript_PreservesMultiByteUtf8Characters()
    {
        // Arrange
        var multiByteText = "Cyrillic: План развития сотрудников 🚀 | Japanese: テストカバレッジ向上 | Accents: Résumé à Zürich";

        // Act
        var sanitized = TextSanitizer.SanitizeTranscript(multiByteText);

        // Assert
        sanitized.Should().Be(multiByteText);
    }

    [Fact]
    public void SanitizeTranscript_EdgeControlCharacters_NormalizesLoneCarriageReturns()
    {
        // Arrange - Lone \r (classic Mac CR) without \n
        var textWithLoneCr = "Line One\rLine Two\rLine Three";

        // Act
        var result = TextSanitizer.SanitizeTranscript(textWithLoneCr);

        // Assert
        result.Should().Be("Line One\nLine Two\nLine Three");
    }

    [Fact]
    public void SanitizeTranscript_EdgeControlCharacters_PreservesPrintableBoundaries()
    {
        // Char 31 is control, Char 32 is space (printable), Char 126 is '~' (printable), Char 127 is DEL, Char 128 is extended ASCII/UTF-8
        var mixed = "Before\u001F Space~\u007FAfter\u0080";

        // Act
        var result = TextSanitizer.SanitizeTranscript(mixed);

        // Assert
        result.Should().Be("Before Space~After\u0080");
        result.Should().NotContain("\u001F");
        result.Should().NotContain("\u007F");
    }

    [Fact]
    public void SanitizeTranscript_OnlyControlCharactersAndWhitespace_ReturnsEmptyString()
    {
        // Arrange
        var text = "\u0000\u0001\u0002\u001E\u001F\t\r\n  \r\n\t";

        // Act
        var result = TextSanitizer.SanitizeTranscript(text);

        // Assert
        result.Should().Be(string.Empty);
    }
}

