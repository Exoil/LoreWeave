using System;

using LoreWeave.Domain.Entities.Boards.Commands;
using LoreWeave.Domain.Exceptions;

using Shouldly;

namespace LoreWeave.Domain.Test.Entities.Boards.Commands;

[Trait(Constants.TraitName, Constants.TestTitle)]
public class CreateBoardTest
{
    [Theory]
    [InlineData("C")]
    [InlineData("Curse of Strahd")]
    public void Create_WithValidName_SetsProperties(string name)
    {
        var id = Guid.NewGuid();

        var createBoard = new CreateBoard(id, name);

        createBoard.Id.ShouldBe(id);
        createBoard.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_WithNameAtMaxLength_SetsName()
    {
        var name = new string('a', 50);

        var createBoard = new CreateBoard(Guid.NewGuid(), name);

        createBoard.Name.ShouldBe(name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Create_WithInvalidNameLength_Throws_Validation_Exception(int nameLength)
    {
        var act = () => new CreateBoard(Guid.NewGuid(), new string('a', nameLength));

        var exception = act.ShouldThrow<ValueObjectException>();
        exception.ValidationErrors.TryGetValue(nameof(CreateBoard.Name), out _).ShouldBeTrue();
    }
}
