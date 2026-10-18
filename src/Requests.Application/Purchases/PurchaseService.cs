using Microsoft.Extensions.Logging;
using Requests.Application.Exceptions;
using Requests.Application.Machines;
using Requests.Application.Persistence;
using Requests.Domain.Machines;

namespace Requests.Application.Purchases;

public class PurchaseService(
    IMachineRepository machines,
    IUnitOfWork unitOfWork,
    ILogger<PurchaseService> logger)
{
    public async Task<PurchaseResponse> PurchaseAsync(
        string machineId,
        string shelfId,
        string userId,
        CancellationToken cancellationToken)
    {
        // The stock check and the decrement share one transaction, so two buyers can't both take the last item.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var shelf = await FindShelfToSellAsync(machineId, shelfId, userId, cancellationToken);

        shelf.Quantity -= 1;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Item purchased from shelf {ShelfId} of machine {MachineId} by {UserId}: product type {ProductTypeCode} at {Price}, {RemainingQuantity} left",
            shelfId,
            machineId,
            userId,
            shelf.ProductTypeCode,
            shelf.Price,
            shelf.Quantity);

        return new PurchaseResponse
        {
            MachineId = machineId,
            ShelfId = shelfId,
            ProductTypeCode = shelf.ProductTypeCode,
            Price = shelf.Price,
            RemainingQuantity = shelf.Quantity,
        };
    }

    private async Task<Shelf> FindShelfToSellAsync(
        string machineId,
        string shelfId,
        string userId,
        CancellationToken cancellationToken)
    {
        var machine = await machines.FindForUpdateAsync(machineId, cancellationToken);

        if (machine is null)
        {
            logger.LogInformation("Machine {MachineId} not found when {UserId} bought from shelf {ShelfId}", machineId, userId, shelfId);
            throw new NotFoundException($"Machine '{machineId}' was not found.");
        }

        var shelf = await machines.FindShelfAsync(machineId, shelfId, cancellationToken);

        if (shelf is null)
        {
            logger.LogInformation(
                "Shelf {ShelfId} not found on machine {MachineId} when {UserId} made a purchase",
                shelfId,
                machineId,
                userId);
            throw new NotFoundException($"Shelf '{shelfId}' was not found on machine '{machineId}'.");
        }

        if (shelf.Quantity <= 0)
        {
            logger.LogWarning(
                "Shelf {ShelfId} on machine {MachineId} is empty: product type {ProductTypeCode}, requested by {UserId}",
                shelfId,
                machineId,
                shelf.ProductTypeCode,
                userId);
            throw new ConflictException($"Shelf {shelfId} is empty.");
        }

        return shelf;
    }
}
