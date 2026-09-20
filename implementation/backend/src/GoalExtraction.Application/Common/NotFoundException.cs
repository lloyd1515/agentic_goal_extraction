namespace GoalExtraction.Application.Common;

public class NotFoundException : KeyNotFoundException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' ({key}) was not found.")
    {
    }
}
