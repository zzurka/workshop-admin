namespace WorkshopAdmin.Domain.Exceptions;

public class NotFoundException(string entityName, object id) : DomainException($"{entityName} with ID '{id}' was not found.")
{
}
