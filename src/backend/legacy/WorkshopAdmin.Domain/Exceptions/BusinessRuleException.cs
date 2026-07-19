namespace WorkshopAdmin.Domain.Exceptions;

public class BusinessRuleException(string message) : DomainException(message)
{
}
