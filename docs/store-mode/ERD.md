# Mağaza rejimi — ERD

Bu fayl EF modelindən avtomatik yaradılır (`src/Tests/ErdGenerator.cs`); əl ilə dəyişdirməyin. Testləri işlədəndə yenilənir.

Stok və maliyyə balansları hərəkət/ledger cədvəllərindən (`StoreStockMovements`, `StoreLedgerEntries`, `StoreBonusEntries`) hesablanır; `StoreStockBalances` performans üçün snapshot-dır (RowVersion ilə). Satış sətrində qiymət, endirim, ƏDV və maya snapshot kimi saxlanır. Unikal indekslər: `(CompanyId, OperationId)` təkrar əməliyyatın qarşısını alır.

```mermaid
erDiagram
  StoreApprovalLog {
    int32 Id PK
    string Action
    int32 ApprovedByUserId
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    int32 DocumentId
    string DocumentNo
    string DocumentType
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Reason
  }
  StoreBonusEntry {
    int32 Id PK
    decimal Amount
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    int32 DocumentId
    string DocumentNo
    string DocumentType
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Note
    int32 StoreCustomerId FK
    int Type
  }
  StoreBranchPrice {
    int32 Id PK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int32 RestaurantId
    decimal SalePrice
    int32 StoreProductId FK
  }
  StoreCashMovement {
    int32 Id PK
    decimal Amount
    int32 ApprovedByUserId
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Reason
    int32 StoreCashShiftId FK
    int Type
  }
  StoreCashRegister {
    int32 Id PK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    bool IsActive
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Name
    int32 RestaurantId FK
    int32 WarehouseId FK
  }
  StoreCashShift {
    int32 Id PK
    decimal ActualCash
    decimal CashDifference
    int32 CashierUserId
    string CloseNote
    datetime ClosedAtUtc
    int32 ClosedByUserId
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    decimal ExpectedCash
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    datetime OpenedAtUtc
    decimal OpeningCash
    int32 RestaurantId
    int Status
    int32 StoreCashRegisterId FK
  }
  StoreCategory {
    int32 Id PK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    bool EarnsBonus
    bool IsActive
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Name
    int32 ParentCategoryId FK
  }
  StoreCustomer {
    int32 Id PK
    decimal BonusPercentOverride
    string Code
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    decimal CreditLimit
    string Email
    bool IsActive
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Name
    string Note
    string Phone
  }
  StoreCustomerPayment {
    int32 Id PK
    decimal Amount
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string DocumentNo
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int Method
    string Note
    string Reference
    string ReverseReason
    datetime ReversedAtUtc
    int32 ReversedByUserId
    int32 StoreCashShiftId FK
    int32 StoreCustomerId FK
  }
  StoreHeldSale {
    int32 Id PK
    string CartJson
    int32 CashierUserId
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    int32 ItemCount
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int32 StoreCashRegisterId FK
    string Title
    decimal Total
  }
  StoreInventory {
    int32 Id PK
    datetime CancelledAtUtc
    int32 CategoryId FK
    int32 CompanyId FK
    datetime ConfirmedAtUtc
    int32 ConfirmedByUserId
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string DocumentNo
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Note
    int Status
    int32 WarehouseId FK
  }
  StoreInventoryLine {
    int32 Id PK
    decimal CountedQuantity
    datetime CreatedAtUtc
    int32 CreatedByUserId
    decimal Difference
    decimal ExpectedQuantity
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Reason
    int32 StoreInventoryId FK
    int32 StoreProductId FK
    decimal UnitCost
  }
  StoreLedgerEntry {
    int32 Id PK
    decimal Amount
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    int32 DocumentId
    string DocumentNo
    string DocumentType
    datetime EntryDate
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Note
    int32 PartyId
    int PartyType
    int Type
  }
  StorePaymentAllocation {
    int32 Id PK
    decimal Amount
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int32 StorePurchaseId FK
    int32 StoreSupplierPaymentId FK
  }
  StorePriceHistory {
    int32 Id PK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime EffectiveFromUtc
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    decimal NewPrice
    decimal OldPrice
    int PriceType
    string Reason
    int32 StoreProductId FK
  }
  StoreProduct {
    int32 Id PK
    decimal AverageCost
    string Brand
    datetime CampaignFromUtc
    decimal CampaignPrice
    datetime CampaignToUtc
    int32 CategoryId FK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    bool EarnsBonus
    string ImageUrl
    bool IsActive
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    decimal MinimumStock
    string Name
    decimal PurchasePrice
    decimal SalePrice
    string Sku
    bool TracksSerial
    int Unit
    decimal VatPercent
    decimal WholesalePrice
  }
  StoreProductBarcode {
    int32 Id PK
    string Barcode
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    bool IsPrimary
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int32 StoreProductId FK
  }
  StorePurchase {
    int32 Id PK
    string CancelReason
    datetime CancelledAtUtc
    int32 CancelledByUserId
    int32 CompanyId FK
    datetime ConfirmedAtUtc
    int32 ConfirmedByUserId
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string DocumentNo
    datetime DueDate
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Note
    datetime PurchaseDate
    int Status
    int32 StoreSupplierId FK
    string SupplierInvoiceNo
    decimal Total
    decimal VatTotal
    int32 WarehouseId FK
  }
  StorePurchaseLine {
    int32 Id PK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    decimal DiscountPercent
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    decimal LineTotal
    decimal NetUnitCost
    decimal Quantity
    string SerialNumbers
    int32 StoreProductId FK
    int32 StorePurchaseId FK
    decimal UnitPrice
    decimal VatPercent
  }
  StorePurchaseReturn {
    int32 Id PK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string DocumentNo
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Reason
    int32 StorePurchaseId FK
    int32 StoreSupplierId
    decimal Total
    int32 WarehouseId
  }
  StorePurchaseReturnLine {
    int32 Id PK
    decimal Amount
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string ProductName
    decimal Quantity
    int32 StoreProductId
    int32 StorePurchaseLineId FK
    int32 StorePurchaseReturnId FK
    decimal UnitAmount
  }
  StoreQuickSlot {
    int32 Id PK
    string Color
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string Label
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int32 SlotNumber
    int32 StoreCashRegisterId FK
    int32 StoreProductId FK
  }
  StoreSale {
    int32 Id PK
    decimal BonusEarned
    decimal BonusSpent
    string CancelReason
    datetime CancelledAtUtc
    int32 CancelledByUserId
    decimal ChangeAmount
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    decimal DebtAmount
    decimal DiscountTotal
    string DocumentNo
    string FiscalError
    string FiscalReceiptNo
    int FiscalStatus
    bool IsReceiptless
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Note
    guid OperationId
    decimal PaidTotal
    int32 RestaurantId
    int Status
    int32 StoreCashRegisterId FK
    int32 StoreCashShiftId FK
    int32 StoreCustomerId FK
    decimal SubTotal
    decimal Total
    decimal VatTotal
    int32 WarehouseId
  }
  StoreSaleLine {
    int32 Id PK
    string Barcode
    datetime CreatedAtUtc
    int32 CreatedByUserId
    decimal DiscountAmount
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    decimal LineTotal
    decimal ListPrice
    string PriceChangeReason
    string ProductName
    decimal Quantity
    string Sku
    int32 StoreProductId FK
    int32 StoreSaleId FK
    decimal UnitCost
    decimal UnitPrice
    decimal VatAmount
    decimal VatPercent
  }
  StoreSalePayment {
    int32 Id PK
    decimal Amount
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int Method
    string Reference
    int32 StoreSaleId FK
  }
  StoreSaleReturn {
    int32 Id PK
    decimal BonusClawbackAmount
    decimal BonusRefundAmount
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    decimal DebtCreditAmount
    string DocumentNo
    int32 ExchangeSaleId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    guid OperationId
    string Reason
    decimal RefundAmount
    int RefundMethod
    int32 RestaurantId
    int32 StoreCashRegisterId FK
    int32 StoreCashShiftId FK
    int32 StoreSaleId FK
    decimal TotalAmount
    int32 WarehouseId
  }
  StoreSaleReturnLine {
    int32 Id PK
    decimal Amount
    int Condition
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string ProductName
    decimal Quantity
    int32 StoreProductId
    int32 StoreSaleLineId FK
    int32 StoreSaleReturnId FK
    decimal UnitAmount
    decimal UnitCost
  }
  StoreSerialEvent {
    int32 Id PK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    int32 DocumentId
    string DocumentNo
    string DocumentType
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Note
    int32 StoreSerialUnitId FK
    int Type
    int32 WarehouseId
  }
  StoreSerialUnit {
    int32 Id PK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string SerialNumber
    datetime SoldAtUtc
    int Status
    int32 StoreProductId FK
    int32 StoreSaleLineId FK
    int32 WarehouseId FK
  }
  StoreSettings {
    int32 Id PK
    bool AllowNegativeStock
    bool AllowReceiptlessSale
    decimal BonusEarnPercent
    int32 BonusExpiryDays
    decimal BonusMaxPaymentPercent
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    bool FiscalEnabled
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    decimal MaxDiscountPercent
    int32 QuickSlotCount
    string ReceiptFooter
    string ReceiptHeader
    int32 ReceiptWidthMm
  }
  StoreStockBalance {
    int32 Id PK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    decimal Quantity
    bytes RowVersion
    int32 StoreProductId FK
    int32 WarehouseId FK
  }
  StoreStockMovement {
    int32 Id PK
    decimal BalanceAfter
    decimal BalanceBefore
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    int32 DocumentId
    string DocumentNo
    string DocumentType
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    decimal Quantity
    string Reason
    int32 StoreProductId FK
    int Type
    decimal UnitCost
    int32 WarehouseId FK
  }
  StoreSupplier {
    int32 Id PK
    string Address
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string Email
    bool IsActive
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string LegalName
    string Name
    string Note
    int32 PaymentTermDays
    string Phone
    string TaxNumber
  }
  StoreSupplierPayment {
    int32 Id PK
    decimal Amount
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string DocumentNo
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int Method
    string Note
    datetime PaymentDate
    string Reference
    string ReverseReason
    datetime ReversedAtUtc
    int32 ReversedByUserId
    int32 StoreSupplierId FK
  }
  StoreSyncOperation {
    int32 Id PK
    int32 AttemptCount
    datetime ClientCreatedAtUtc
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string DeviceId
    string Error
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    guid OperationId
    datetime ProcessedAtUtc
    string ResultDocumentNo
    int32 ResultEntityId
    int Status
    string Type
  }
  StoreTransfer {
    int32 Id PK
    datetime CancelledAtUtc
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    string DocumentNo
    int32 FromWarehouseId FK
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    string Note
    datetime ReceivedAtUtc
    int32 ReceivedByUserId
    datetime ShippedAtUtc
    int32 ShippedByUserId
    int Status
    int32 ToWarehouseId FK
  }
  StoreTransferLine {
    int32 Id PK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    decimal Quantity
    decimal ReceivedQuantity
    string ReceivedSerialNumbers
    string SerialNumbers
    decimal ShippedQuantity
    decimal ShortageQuantity
    int32 StoreProductId FK
    int32 StoreTransferId FK
    decimal UnitCost
  }
  StoreUserBranch {
    int32 Id PK
    int32 CompanyId FK
    datetime CreatedAtUtc
    int32 CreatedByUserId
    datetime LastModifiedAtUtc
    int32 LastModifiedByUserId
    int32 RestaurantId
    int32 UserId
  }
  Company ||--o{ StoreApprovalLog : "CompanyId"
  Company ||--o{ StoreBonusEntry : "CompanyId"
  Company ||--o{ StoreBranchPrice : "CompanyId"
  Company ||--o{ StoreCashMovement : "CompanyId"
  Company ||--o{ StoreCashRegister : "CompanyId"
  Company ||--o{ StoreCashShift : "CompanyId"
  Company ||--o{ StoreCategory : "CompanyId"
  Company ||--o{ StoreCustomer : "CompanyId"
  Company ||--o{ StoreCustomerPayment : "CompanyId"
  Company ||--o{ StoreHeldSale : "CompanyId"
  Company ||--o{ StoreInventory : "CompanyId"
  Company ||--o{ StoreLedgerEntry : "CompanyId"
  Company ||--o{ StorePriceHistory : "CompanyId"
  Company ||--o{ StoreProduct : "CompanyId"
  Company ||--o{ StoreProductBarcode : "CompanyId"
  Company ||--o{ StorePurchase : "CompanyId"
  Company ||--o{ StorePurchaseReturn : "CompanyId"
  Company ||--o{ StoreQuickSlot : "CompanyId"
  Company ||--o{ StoreSale : "CompanyId"
  Company ||--o{ StoreSaleReturn : "CompanyId"
  Company ||--o{ StoreSerialUnit : "CompanyId"
  Company ||--o{ StoreSettings : "CompanyId"
  Company ||--o{ StoreStockBalance : "CompanyId"
  Company ||--o{ StoreStockMovement : "CompanyId"
  Company ||--o{ StoreSupplier : "CompanyId"
  Company ||--o{ StoreSupplierPayment : "CompanyId"
  Company ||--o{ StoreSyncOperation : "CompanyId"
  Company ||--o{ StoreTransfer : "CompanyId"
  Company ||--o{ StoreUserBranch : "CompanyId"
  Restaurant ||--o{ StoreCashRegister : "RestaurantId"
  StoreCashRegister ||--o{ StoreCashShift : "StoreCashRegisterId"
  StoreCashRegister ||--o{ StoreHeldSale : "StoreCashRegisterId"
  StoreCashRegister ||--o{ StoreQuickSlot : "StoreCashRegisterId"
  StoreCashRegister ||--o{ StoreSale : "StoreCashRegisterId"
  StoreCashRegister ||--o{ StoreSaleReturn : "StoreCashRegisterId"
  StoreCashShift ||--o{ StoreCashMovement : "StoreCashShiftId"
  StoreCashShift ||--o{ StoreCustomerPayment : "StoreCashShiftId"
  StoreCashShift ||--o{ StoreSale : "StoreCashShiftId"
  StoreCashShift ||--o{ StoreSaleReturn : "StoreCashShiftId"
  StoreCategory ||--o{ StoreCategory : "ParentCategoryId"
  StoreCategory ||--o{ StoreInventory : "CategoryId"
  StoreCategory ||--o{ StoreProduct : "CategoryId"
  StoreCustomer ||--o{ StoreBonusEntry : "StoreCustomerId"
  StoreCustomer ||--o{ StoreCustomerPayment : "StoreCustomerId"
  StoreCustomer ||--o{ StoreSale : "StoreCustomerId"
  StoreInventory ||--o{ StoreInventoryLine : "StoreInventoryId"
  StoreProduct ||--o{ StoreBranchPrice : "StoreProductId"
  StoreProduct ||--o{ StoreInventoryLine : "StoreProductId"
  StoreProduct ||--o{ StorePriceHistory : "StoreProductId"
  StoreProduct ||--o{ StoreProductBarcode : "StoreProductId"
  StoreProduct ||--o{ StorePurchaseLine : "StoreProductId"
  StoreProduct ||--o{ StoreQuickSlot : "StoreProductId"
  StoreProduct ||--o{ StoreSaleLine : "StoreProductId"
  StoreProduct ||--o{ StoreSerialUnit : "StoreProductId"
  StoreProduct ||--o{ StoreStockBalance : "StoreProductId"
  StoreProduct ||--o{ StoreStockMovement : "StoreProductId"
  StoreProduct ||--o{ StoreTransferLine : "StoreProductId"
  StorePurchase ||--o{ StorePaymentAllocation : "StorePurchaseId"
  StorePurchase ||--o{ StorePurchaseLine : "StorePurchaseId"
  StorePurchase ||--o{ StorePurchaseReturn : "StorePurchaseId"
  StorePurchaseLine ||--o{ StorePurchaseReturnLine : "StorePurchaseLineId"
  StorePurchaseReturn ||--o{ StorePurchaseReturnLine : "StorePurchaseReturnId"
  StoreSale ||--o{ StoreSaleLine : "StoreSaleId"
  StoreSale ||--o{ StoreSalePayment : "StoreSaleId"
  StoreSale ||--o{ StoreSaleReturn : "StoreSaleId"
  StoreSaleLine ||--o{ StoreSaleReturnLine : "StoreSaleLineId"
  StoreSaleLine ||--o{ StoreSerialUnit : "StoreSaleLineId"
  StoreSaleReturn ||--o{ StoreSaleReturnLine : "StoreSaleReturnId"
  StoreSerialUnit ||--o{ StoreSerialEvent : "StoreSerialUnitId"
  StoreSupplier ||--o{ StorePurchase : "StoreSupplierId"
  StoreSupplier ||--o{ StoreSupplierPayment : "StoreSupplierId"
  StoreSupplierPayment ||--o{ StorePaymentAllocation : "StoreSupplierPaymentId"
  StoreTransfer ||--o{ StoreTransferLine : "StoreTransferId"
  Warehouse ||--o{ StoreCashRegister : "WarehouseId"
  Warehouse ||--o{ StoreInventory : "WarehouseId"
  Warehouse ||--o{ StorePurchase : "WarehouseId"
  Warehouse ||--o{ StoreSerialUnit : "WarehouseId"
  Warehouse ||--o{ StoreStockBalance : "WarehouseId"
  Warehouse ||--o{ StoreStockMovement : "WarehouseId"
  Warehouse ||--o{ StoreTransfer : "FromWarehouseId"
  Warehouse ||--o{ StoreTransfer : "ToWarehouseId"
```

## İndekslər

| Cədvəl | İndeks | Unikal |
|---|---|---|
| StoreApprovalLogs | CompanyId |  |
| StoreBonusEntries | CompanyId |  |
| StoreBonusEntries | StoreCustomerId, CreatedAtUtc |  |
| StoreBranchPrices | StoreProductId |  |
| StoreBranchPrices | CompanyId, RestaurantId, StoreProductId | bəli |
| StoreCashMovements | CompanyId |  |
| StoreCashMovements | StoreCashShiftId |  |
| StoreCashRegisters | CompanyId |  |
| StoreCashRegisters | RestaurantId |  |
| StoreCashRegisters | WarehouseId |  |
| StoreCashShifts | CompanyId |  |
| StoreCashShifts | StoreCashRegisterId | bəli |
| StoreCategories | CompanyId |  |
| StoreCategories | ParentCategoryId |  |
| StoreCustomers | CompanyId, Code | bəli |
| StoreCustomers | CompanyId, Phone |  |
| StoreCustomerPayments | StoreCashShiftId |  |
| StoreCustomerPayments | StoreCustomerId |  |
| StoreCustomerPayments | CompanyId, DocumentNo | bəli |
| StoreHeldSales | CompanyId |  |
| StoreHeldSales | StoreCashRegisterId |  |
| StoreInventories | CategoryId |  |
| StoreInventories | WarehouseId |  |
| StoreInventories | CompanyId, DocumentNo | bəli |
| StoreInventoryLines | StoreInventoryId |  |
| StoreInventoryLines | StoreProductId |  |
| StoreLedgerEntries | CompanyId, PartyType, PartyId, EntryDate |  |
| StorePaymentAllocations | StorePurchaseId |  |
| StorePaymentAllocations | StoreSupplierPaymentId |  |
| StorePriceHistories | CompanyId |  |
| StorePriceHistories | StoreProductId, EffectiveFromUtc |  |
| StoreProducts | CategoryId |  |
| StoreProducts | CompanyId, Name |  |
| StoreProducts | CompanyId, Sku | bəli |
| StoreProductBarcodes | StoreProductId |  |
| StoreProductBarcodes | CompanyId, Barcode | bəli |
| StorePurchases | WarehouseId |  |
| StorePurchases | CompanyId, DocumentNo | bəli |
| StorePurchases | StoreSupplierId, Status |  |
| StorePurchaseLines | StoreProductId |  |
| StorePurchaseLines | StorePurchaseId |  |
| StorePurchaseReturns | StorePurchaseId |  |
| StorePurchaseReturns | CompanyId, DocumentNo | bəli |
| StorePurchaseReturnLines | StorePurchaseLineId |  |
| StorePurchaseReturnLines | StorePurchaseReturnId |  |
| StoreQuickSlots | CompanyId |  |
| StoreQuickSlots | StoreProductId |  |
| StoreQuickSlots | StoreCashRegisterId, SlotNumber | bəli |
| StoreSales | StoreCashRegisterId |  |
| StoreSales | StoreCashShiftId |  |
| StoreSales | StoreCustomerId |  |
| StoreSales | CompanyId, CreatedAtUtc |  |
| StoreSales | CompanyId, DocumentNo | bəli |
| StoreSales | CompanyId, OperationId | bəli |
| StoreSaleLines | StoreProductId |  |
| StoreSaleLines | StoreSaleId |  |
| StoreSalePayments | StoreSaleId |  |
| StoreSaleReturns | StoreCashRegisterId |  |
| StoreSaleReturns | StoreCashShiftId |  |
| StoreSaleReturns | StoreSaleId |  |
| StoreSaleReturns | CompanyId, DocumentNo | bəli |
| StoreSaleReturns | CompanyId, OperationId | bəli |
| StoreSaleReturnLines | StoreSaleLineId |  |
| StoreSaleReturnLines | StoreSaleReturnId |  |
| StoreSerialEvents | StoreSerialUnitId |  |
| StoreSerialUnits | StoreSaleLineId |  |
| StoreSerialUnits | WarehouseId |  |
| StoreSerialUnits | CompanyId, SerialNumber | bəli |
| StoreSerialUnits | StoreProductId, WarehouseId, Status |  |
| StoreSettings | CompanyId | bəli |
| StoreStockBalances | CompanyId |  |
| StoreStockBalances | StoreProductId |  |
| StoreStockBalances | WarehouseId, StoreProductId | bəli |
| StoreStockMovements | CompanyId |  |
| StoreStockMovements | WarehouseId |  |
| StoreStockMovements | StoreProductId, WarehouseId, CreatedAtUtc |  |
| StoreSuppliers | CompanyId, Name |  |
| StoreSupplierPayments | StoreSupplierId |  |
| StoreSupplierPayments | CompanyId, DocumentNo | bəli |
| StoreSyncOperations | CompanyId, OperationId | bəli |
| StoreSyncOperations | CompanyId, Status |  |
| StoreTransfers | FromWarehouseId |  |
| StoreTransfers | ToWarehouseId |  |
| StoreTransfers | CompanyId, DocumentNo | bəli |
| StoreTransfers | CompanyId, Status |  |
| StoreTransferLines | StoreProductId |  |
| StoreTransferLines | StoreTransferId |  |
| StoreUserBranches | CompanyId, UserId, RestaurantId | bəli |
