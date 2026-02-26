using Generators.NamingPolicyConverter;
using System.Text;
using Xunit;

namespace UnitTests;

public class NamingPolicyConverterTests
{
    [Theory]
    [InlineData("hello world", "helloWorld", "HelloWorld", "hello_world", "HELLO_WORLD", "hello-world", "HELLO-WORLD", TestDisplayName = "hello world")]
    [InlineData("abcDef", "abcDef", "AbcDef", "abc_def", "ABC_DEF", "abc-def", "ABC-DEF", TestDisplayName = "camelCase")]
    [InlineData("AbcDef", "abcDef", "AbcDef", "abc_def", "ABC_DEF", "abc-def", "ABC-DEF", TestDisplayName = "PascalCase")]
    [InlineData("abc_def", "abcDef", "AbcDef", "abc_def", "ABC_DEF", "abc-def", "ABC-DEF", TestDisplayName = "snake_case")]
    [InlineData("ABC_DEF", "abcDef", "AbcDef", "abc_def", "ABC_DEF", "abc-def", "ABC-DEF", TestDisplayName = "SCREAMING_CASE")]
    [InlineData("abc-def", "abcDef", "AbcDef", "abc_def", "ABC_DEF", "abc-def", "ABC-DEF", TestDisplayName = "kebab-case")]
    [InlineData("ABC-DEF", "abcDef", "AbcDef", "abc_def", "ABC_DEF", "abc-def", "ABC-DEF", TestDisplayName = "UPPER-KEBAB-CASE")]
    [InlineData("abcあ亜กdef", "abcあ亜กDef", "Abcあ亜กDef", "abc_あ亜ก_def", "ABC_あ亜ก_DEF", "abc-あ亜ก-def", "ABC-あ亜ก-DEF", TestDisplayName = "other letters")]
    [InlineData("abc-あ亜ก-def", "abcあ亜กDef", "Abcあ亜กDef", "abc_あ亜ก_def", "ABC_あ亜ก_DEF", "abc-あ亜ก-def", "ABC-あ亜ก-DEF", TestDisplayName = "other-letters")]
    [InlineData("__abcDef", "abcDef", "AbcDef", "__abc_def", "__ABC_DEF", "--abc-def", "--ABC-DEF", TestDisplayName = "starts with __")]
    [InlineData("BβбДdδΛlл", "bβбДdδΛlл", "BβбДdδΛlл", "bβб_дdδ_λlл", "BΒБ_ДDΔ_ΛLЛ", "bβб-дdδ-λlл", "BΒБ-ДDΔ-ΛLЛ", TestDisplayName = "non-ASCII case letters")]
    [InlineData("abc1ab12a123", "abc1Ab12A123", "Abc1Ab12A123", "abc1_ab12_a123", "ABC1_AB12_A123", "abc1-ab12-a123", "ABC1-AB12-A123", TestDisplayName = "split after number")]
    [InlineData("aáâÀàaÆÿがa", "aáâÀàaÆÿがA", "AáâÀàaÆÿがA", "aáâ_ààa_æÿ_が_a", "AÁÂ_ÀÀA_ÆŸ_が_A", "aáâ-ààa-æÿ-が-a", "AÁÂ-ÀÀA-ÆŸ-が-A", TestDisplayName = "combining marks")]
    public void Convert(string source, string camel, string pascal, string snake, string snakeUpper, string kebab, string kebabUpper)
    {
        Assert.Equal(camel, source.Convert(NamingPolicy.CamelCase));
        Assert.Equal(pascal, source.Convert(NamingPolicy.PascalCase));
        Assert.Equal(snake, source.Convert(NamingPolicy.SnakeCaseLower));
        Assert.Equal(snakeUpper, source.Convert(NamingPolicy.SnakeCaseUpper));
        Assert.Equal(kebab, source.Convert(NamingPolicy.KebabCaseLower));
        Assert.Equal(kebabUpper, source.Convert(NamingPolicy.KebabCaseUpper));
    }

    [Theory]
    [InlineData("hello world", "hello world", TestDisplayName = "hello world")]
    [InlineData("abcDef", "abc Def", TestDisplayName = "camelCase")]
    [InlineData("AbcDef", "Abc Def", TestDisplayName = "PascalCase")]
    [InlineData("abc_def", "abc def", TestDisplayName = "snake_case")]
    [InlineData("ABC_DEF", "ABC DEF", TestDisplayName = "SCREAMING_CASE")]
    [InlineData("abc-def", "abc def", TestDisplayName = "kebab-case")]
    [InlineData("ABC-DEF", "ABC DEF", TestDisplayName = "UPPER-KEBAB-CASE")]
    [InlineData("abcあ亜กdef", "abc あ亜ก def", TestDisplayName = "other letters")]
    [InlineData("abc-あ亜ก-def", "abc あ亜ก def", TestDisplayName = "other-letters")]
    [InlineData("BβбДdδΛlл", "Bβб Дdδ Λlл", TestDisplayName = "non-ASCII case letters")]
    [InlineData("abc1ab12a123", "abc1 ab12 a123", TestDisplayName = "split after number")]
    [InlineData("aáâÀàaÆÿがa", "aáâ Ààa Æÿ が a", TestDisplayName = "combining marks")]
    public void SplitWord(string source, string spaceSplit)
    {
        var expected = spaceSplit.Split(' ');
        var actual = new List<string>();
        foreach (var w in source.SplitWord()) actual.Add(w.ToString());
        Assert.Equal(expected, actual);
    }
}
