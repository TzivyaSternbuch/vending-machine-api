using Microsoft.Extensions.Logging;
using Requests.Application.Exceptions;
using Requests.Application.Machines;
using Requests.Application.Persistence;
using Requests.Domain.Machines;

namespace Requests.Application.Shelves;

public class ShelfService(
    IMachineRepository machines,
    IUnitOfWork unitOfWork,
    AddShelfRequestValidator validator,
    TimeProvider timeProvider,
    ILogger<ShelfService> logger)
{
    public async Task<ShelfResponse> AddAsync(
        string machineId,
        AddShelfRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            await validator.ValidateAsync(request, cancellationToken);
        }
        catch (ValidationException exception)
        {
            logger.LogDebug("Shelf for machine {MachineId} rejected: {Field} is invalid", machineId, exception.Field);
            throw;
        }

        // The count and the insert share one transaction, so concurrent adds can't pass MaxShelves.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var machine = await machines.FindForUpdateAsync(machineId, cancellationToken);

        if (machine is null)
        {
            logger.LogInformation("Machine {MachineId} not found when {UserId} added a shelf", machineId, userId);
            throw new NotFoundException($"Machine '{machineId}' was not found.");
        }

        var shelfCount = await machines.CountShelvesAsync(machineId, cancellationToken);

        if (shelfCount >= machine.MaxShelves)
        {
            logger.LogWarning(
                "Shelf limit reached on machine {MachineId}: {ShelfCount} of {MaxShelves}, by {UserId}",
                machineId,
                shelfCount,
                machine.MaxShelves,
                userId);
            throw new ConflictException($"Machine already has its maximum of {machine.MaxShelves} shelves.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var shelf = new Shelf
        {
            Id = Guid.NewGuid().ToString(),
            MachineId = machineId,
            ProductTypeCode = request.ProductTypeCode!.Value,
            Capacity = request.Capacity!.Value,
            Price = request.Price!.Value,
            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId,
        };

        machines.AddShelf(shelf);
        machine.UpdatedAt = now;
        machine.UpdatedBy = userId;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Shelf {ShelfId} added to machine {MachineId} by {UserId}: product type {ProductTypeCode}, capacity {Capacity}",
            shelf.Id,
            machineId,
            userId,
            shelf.ProductTypeCode,
            shelf.Capacity);

        return ToResponse(shelf);
    }

    public static ShelfResponse ToResponse(Shelf shelf) => new()
    {
        Id = shelf.Id,
        ProductTypeCode = shelf.ProductTypeCode,
        Capacity = shelf.Capacity,
        Price = shelf.Price,
        Quantity = shelf.Quantity,
    };
}
