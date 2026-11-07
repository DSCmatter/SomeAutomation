using Xunit;

namespace ResilientForm.Tests;

public class TextMatchTests
{
    [Fact]
    public void Exact_label_scores_100_after_spacing_and_case()
    {
        Assert.Equal(100, TextMatch.Score("First Name", "  first   name "));
    }

    [Fact]
    public void Similar_labels_stay_below_the_accept_line()
    {
        Assert.True(TextMatch.Score("First Name", "Last Name") < FieldFinder.AcceptScore);
        Assert.True(TextMatch.Score("State", "State of mind") < FieldFinder.AcceptScore);
        Assert.True(TextMatch.Score("India", "Indonesia") < FieldFinder.AcceptScore);
    }

    [Fact]
    public void One_extra_token_still_matches()
    {
        var score = TextMatch.Score("First Name", "First Name required");
        Assert.True(score >= FieldFinder.AcceptScore);
    }

    [Fact]
    public void Placeholder_fragment_does_not_outrank_a_real_label()
    {
        Assert.True(TextMatch.Score("First Name", "Name") < 40);
    }
}
