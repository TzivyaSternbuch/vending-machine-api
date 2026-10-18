using Microsoft.Extensions.Logging;
using Requests.Application.Exceptions;
using Requests.Application.Machines;
using Requests.Application.Persistence;
using Requests.Domain.Machines;

namespace Requests.Application.Shelves;

public class InventoryService(
    IMachineRepository machines,
    IUnitOfWork unitOfWork,
    LoadInventoryRequestValidator validator,
    TimeProvider timeProvider,
    ILogger<InventoryService> logger)
{
    public async Task<ShelfResponse> LoadAsync(
        string machineId,
        LoadInventoryRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            validator.Validate(request);
        }
        catch (ValidationException exception)
        {
            logger.LogDebug("Inventory load on machine {MachineId} rejected: {Field} is invalid", machineId, exception.Field);
            throw;
        }

        var shelfId = request.ShelfId!;
        var productTypeCode = request.ProductTypeCode!.Value;
        var quantity = request.Quantity!.Value;

        // The capacity check and the update share one transaction, so concurrent loads can't overfill a shelf.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var machine = await FindMachineAsync(machineId, userId, cancellationToken);
        var shelf = await FindShelfToLoadAsync(machineId, shelfId, productTypeCode, quantity, userId, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        shelf.Quantity += quantity;
        shelf.UpdatedAt = now;
        shelf.UpdatedBy = userId;
        machine.UpdatedAt = now;
        machine.UpdatedBy = userId;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Inventory loaded on shelf {ShelfId} of machine {MachineId} by {UserId}: {Quantity} of product type {ProductTypeCode}, now {NewQuantity}",
            shelfId,
            machineId,
            userId,
            quantity,
            productTypeCode,
            shelf.Quantity);

        return ShelfService.ToResponse(shelf);
    }

    private async Task<VendingMachine> FindMachineAsync(string machineId, string userId, CancellationToken cancellationToken)
    {
        var machine = await machines.FindForUpdateAsync(machineId, cancellationToken);

        if (machine is null)
        {
            logger.LogInformation("Machine {MachineId} not found when {UserId} loaded inventory", machineId, userId);
            throw new NotFoundException($"Machine '{machineId}' was not found.");
        }

        return machine;
    }

    // Must run inside the load transaction, so the room it checks can't change before the update.
    private async Task<Shelf> FindShelfToLoadAsync(
        string machineId,
        string shelfId,
        int productTypeCode,
        int quantity,
        string userId,
        CancellationToken cancellationToken)
    {
        var shelf = await machines.FindShelfAsync(machineId, shelfId, cancellationToken);

        if (shelf is null)
        {
            logger.LogInformation(
                "Shelf {ShelfId} not found on machine {MachineId} when {UserId} loaded inventory",
                shelfId,
                machineId,
                userId);
            throw new NotFoundException($"Shelf '{shelfId}' was not found on machine '{machineId}'.");
        }

        if (productTypeCode != shelf.ProductTypeCode)
        {
            logger.LogInformation(
                "Product type {ProductTypeCode} does not match shelf {ShelfId} on machine {MachineId}, by {UserId}",
                productTypeCode,
                shelfId,
                machineId,
                userId);
            throw new ValidationException("productTypeCode", $"must be {shelf.ProductTypeCode}, the shelf's product type");
        }

        var room = shelf.Capacity - shelf.Quantity;

        if (quantity > room)
        {
            logger.LogWarning(
                "Shelf {ShelfId} on machine {MachineId} is too full: {Quantity} of {Capacity}, {Requested} requested by {UserId}",
                shelfId,
                machineId,
                shelf.Quantity,
                shelf.Capacity,
                quantity,
                userId);
            throw new ConflictException($"Shelf has room for {room} more items; {quantity} requested.");
        }

        return shelf;
    }
}
