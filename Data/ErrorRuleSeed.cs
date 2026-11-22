using JavaMaster.Api.Entities;

namespace JavaMaster.Api.Data;

/// <summary>The 15 starting rows. First matching rule (lowest SortOrder) wins.</summary>
public static class ErrorRuleSeed
{
    private static ErrorRule R(int n, string pattern, string message, string help) =>
        new() { Id = n, SortOrder = n, Pattern = pattern, Message = message, Help = help, IsActive = true };

    public static ErrorRule[] All => new[]
    {
        R(1,  @"^';' expected", "Java needs a ; at the end of this line.", "Add the missing ; and run it again."),
        R(2,  @"^unclosed string literal", "This text is missing its closing quotation mark.", "Add a \" at the end of the text and run it again."),
        R(3,  @"^cannot find symbol \[variable (\w+)\] \[missing-type\]", "“{1}” needs a type before it.", "Write the type first, like int, double or String."),
        R(4,  @"\[(?:class|variable) (\w+)\] \[did-you-mean (\w+)\]", "Java doesn't know “{1}”. Did you mean “{2}”?", "{2} starts with a capital letter. Fix it and run it again."),
        R(5,  @"^cannot find symbol \[method (\w+)\]", "Java doesn't know the command “{1}”.", "Check the spelling and the capital letters."),
        R(6,  @"^cannot find symbol \[class (\w+)\]", "Java doesn't know the type “{1}”.", "Check the spelling and the capital letters."),
        R(7,  @"^cannot find symbol \[variable (\w+)\]", "Java doesn't know the name “{1}”.", "If it's text, put it inside quotation marks. If it's a variable, create it first."),
        R(8,  @"possible lossy conversion from double to int", "An int can't hold decimals. Use double for numbers like 3.14.", "Change int to double and run it again."),
        R(9,  @"^incompatible types: (\w+) cannot be converted to (\w+)", "A {1} value can't be stored in a {2} variable.", "Check that the value matches the variable's type."),
        R(10, @"^variable (\w+) is already defined", "You already created a variable named “{1}”.", "Use a different name, or remove the second one."),
        R(11, @"^not a statement", "This line isn't a complete instruction.", "Did you forget a command such as System.out.println?"),
        R(12, @"^class, interface, enum, or record expected", "Some code is outside the class.", "Check your curly braces { }."),
        R(13, @"^reached end of file while parsing", "A closing curly brace } is missing.", "Every { needs a } to close it."),
        R(14, @"^'(.)' expected", "Java expected a {1} here.", "Check this line for a missing symbol."),
        R(15, @"^(?:illegal start of (?:expression|type)|<identifier> expected)", "Something on this line is out of place.", "Look for an extra or missing character, or check the spelling near this spot."),
    };
}