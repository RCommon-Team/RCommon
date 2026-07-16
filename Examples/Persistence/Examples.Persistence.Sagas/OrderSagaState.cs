using RCommon.Persistence.Sagas;

namespace Examples.Persistence.Sagas;

public class OrderSagaState : SagaState<Guid>
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentTransactionId { get; set; }
    public bool InventoryReserved { get; set; }
}
