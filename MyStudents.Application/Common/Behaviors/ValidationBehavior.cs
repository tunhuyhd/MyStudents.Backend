using FluentValidation;
using MediatR;

namespace MyStudents.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
        {
            throw new Exceptions.ValidationException(
                failures.GroupBy(f => f.PropertyName.StartsWith("Request.") ? f.PropertyName[8..] : f.PropertyName)
                    .ToDictionary(g => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(g.Key),
                        g => g.Select(f => f.ErrorMessage).Distinct().ToArray()));
        }

        return await next();
    }
}
