

Gold Store ERP – Professional ERD
## Identity Module
## Users
Id (PK)
DisplayName
## Email
PhoneNumber
PasswordHash
IsActive
CreatedAt
## Roles
Id (PK)
## Name
## Permissions
Id (PK)
## Name
UserRoles
UserId (FK → Users)
RoleId (FK → Roles)
Composite PK:
(UserId, RoleId)
RolePermissions
RoleId (FK → Roles)
PermissionId (FK → Permissions)
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 1

Composite PK:
(RoleId, PermissionId)
## Configuration Module
## Categories
(Self-reference hierarchy)
Id (PK)
ParentCategoryId (FK → Categories)
## Name
SortOrder
## Karats
Id (PK)
## Value
## Examples:
## 18
## 21
## 24
GoldPrices
Id (PK)
PriceDate
## Price21
## Currency
CreatedByUserId (FK → Users)
## Unique:
(PriceDate, Currency)
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 2

## Finance Module
## Accounts
## Represents:
## Cash Accounts
## Bank Accounts
Id (PK)
## Name
AccountType
## Currency
BankName
AccountNumber
IsActive
FinancialLedgerEntries
## Unified Money Ledger
Id (PK)
AccountId (FK → Accounts)
## Currency
## Amount
TransactionDirection
## (IN / OUT)
TransactionType
ReferenceType
ReferenceId
## Notes
CreatedByUserId
CreatedAt
## Indexes:
(AccountId)
(TransactionType)
(ReferenceType, ReferenceId)
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 3

(CreatedAt)
## Supplier Module
## Suppliers
Id (PK)
## Name
PrimaryPhone
SecondaryPhone
BankAccountNumber
## Notes
SupplierDeliveries
(Header)
Id (PK)
SupplierId (FK)
DeliveryDate
FeePerGram
TotalManufacturingFee
ReceivedByUserId
## Notes
SupplierDeliveryItems
Id (PK)
SupplierDeliveryId (FK)
## Weight
KaratId
Equivalent21Weight
SupplierPayments
(Header)
Id (PK)
SupplierId (FK)
PaymentDate
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 4

## Notes
ReceivedByUserId
SupplierScrapPaymentItems
Id (PK)
SupplierPaymentId (FK)
## Weight
KaratId
Equivalent21Weight
SupplierManufacturingPayments
Id (PK)
SupplierPaymentId (FK)
AccountId (FK)
## Currency
## Amount
## Sales Module
SalesInvoices
(Header)
Id (PK)
InvoiceNumber
InvoiceDate
CustomerName
CustomerBankAccount
SellerUserId
## Notes
## Unique:
InvoiceNumber
SalesInvoiceItems
Id (PK)
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 5

SalesInvoiceId (FK)
CategoryId (FK)
## Weight
KaratId
GramPrice
## Currency
TotalAmount
SalesInvoicePayments
Id (PK)
SalesInvoiceId (FK)
AccountId (FK)
PaymentMethod
## Currency
## Amount
## Customer Gold Purchase Module
CustomerPurchaseInvoices
(Header)
Id (PK)
InvoiceNumber
PurchaseDate
CustomerName
NationalId
## Phone
## Address
BirthYear
BankAccount
BuyerUserId
## Notes
CustomerPurchaseItems
Id (PK)
CustomerPurchaseInvoiceId (FK)
CategoryId
## Weight
KaratId
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 6

GramPrice
## Currency
TotalPrice
CustomerPurchasePayments
Id (PK)
CustomerPurchaseInvoiceId (FK)
AccountId
## Currency
## Amount
## Inventory Module
GoldLedgerEntries
## Unified Gold Ledger
Id (PK)
KaratId (FK)
## Weight
Equivalent21Weight
MovementDirection
## (IN / OUT)
MovementType
## Examples:
SupplierDelivery
CustomerPurchase
## Sale
SupplierScrapPayment
InventoryAdjustment
ReferenceType
ReferenceId
CreatedByUserId
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 7

CreatedAt
## Indexes:
(KaratId)
(MovementType)
(ReferenceType, ReferenceId)
(CreatedAt)
InventoryAdjustments
(Header)
Id (PK)
AdjustmentDate
AdjustmentReason
## Notes
CreatedByUserId
InventoryAdjustmentItems
Id (PK)
InventoryAdjustmentId
KaratId
## Weight
Equivalent21Weight
AdjustmentType
## Increase
## Decrease
## Damage
## Loss
## Correction
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 8

## Expense Module
ExpenseCategories
Id (PK)
## Name
## Expenses
Id (PK)
ExpenseCategoryId
AccountId
## Currency
## Amount
## Description
ExpenseDate
CreatedByUserId
HR Module
## Employees
Id (PK)
## Name
## Phone
## Position
## Salary
SalaryPayments
Id (PK)
EmployeeId
AccountId
## Currency
## Amount
SalaryMonth
PaymentDate
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## •
## 9

## Suggested Views
CurrentGoldStockView
## Grouped By:
## Karat
## Returns:
CurrentWeight
Equivalent21Weight
AccountBalanceView
## Grouped By:
## Account
## Returns:
CurrentBalance
SupplierGoldBalanceView
## Returns:
## Supplier Delivered Gold
minus
## Supplier Scrap Payments
SupplierManufacturingBalanceView
## Returns:
## Manufacturing Fees
minus
## •
## •
## •
## •
## •
## 10

## Manufacturing Payments
## Main Relationships
Supplier 1 → Many SupplierDeliveries
SupplierDelivery 1 → Many SupplierDeliveryItems
Supplier 1 → Many SupplierPayments
SalesInvoice 1 → Many SalesInvoiceItems
SalesInvoice 1 → Many SalesInvoicePayments
CustomerPurchaseInvoice 1 → Many CustomerPurchaseItems
CustomerPurchaseInvoice 1 → Many CustomerPurchasePayments
Account 1 → Many FinancialLedgerEntries
Karat 1 → Many GoldLedgerEntries
Employee 1 → Many SalaryPayments
Category (Self-reference)
## User 1 → Many Transactions
## 11