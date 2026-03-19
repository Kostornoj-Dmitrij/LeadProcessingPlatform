using MediatR;
using Microsoft.Extensions.Logging;
using SharedKernel.Base;

namespace LeadService.Application.Common.Behaviors;

/// <summary>
/// Поведение для автоматического управления транзакциями
/// </summary>
public class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is ICommand)
        {
            logger.LogDebug("Beginning transaction for {RequestType}", typeof(TRequest).Name);
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next(cancellationToken);

                await unitOfWork.CommitTransactionAsync(cancellationToken);

                logger.LogDebug("Transaction committed for {RequestType}", typeof(TRequest).Name);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction rolled back for {RequestType}", typeof(TRequest).Name);
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        return await next(cancellationToken);
    }
}