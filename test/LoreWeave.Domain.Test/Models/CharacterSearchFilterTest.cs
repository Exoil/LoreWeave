using LoreWeave.Domain.Exceptions;
using LoreWeave.Domain.Models;

using Shouldly;

namespace LoreWeave.Domain.Test.Models;

[Trait(Constants.TraitName, Constants.TestTitle)]
public class CharacterSearchFilterTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Gandalf")]
    public void Create_WithValidName_SetsName(string name)
    {
        var filter = new CharacterSearchFilter(name);

        filter.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_WithNameAtMaxLength_SetsName()
    {
        var name = new string('a', 100);

        var filter = new CharacterSearchFilter(name);

        filter.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_WithNameOverMaxLength_Throws_Validation_Exception()
    {
        var act = () => new CharacterSearchFilter(new string('a', 101));

        var exception = act.ShouldThrow<ValueObjectException>();
        exception.ValidationErrors.TryGetValue(nameof(CharacterSearchFilter.Name), out _).ShouldBeTrue();
    }
}
