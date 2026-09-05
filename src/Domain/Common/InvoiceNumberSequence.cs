// <copyright file="InvoiceNumberSequence.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using SharedKernel;

namespace Domain.Common;

public sealed class InvoiceNumberSequence : Entity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    public InvoiceDocumentType DocumentType { get; private set; }

    public string Period { get; private set; }

    public int NextNumber { get; private set; }

    public int Version { get; private set; }

    private InvoiceNumberSequence()
    {
    }

    private InvoiceNumberSequence(
        Guid id,
        InvoiceDocumentType documentType,
        string period,
        int nextNumber)
        : base(id)
    {
        this.DocumentType = documentType;
        this.Period = period;
        this.NextNumber = nextNumber;
    }

    public static InvoiceNumberSequence Create(
        InvoiceDocumentType documentType,
        string period,
        int nextNumber) =>
        new(Guid.CreateVersion7(), documentType, period, nextNumber);

    public int AllocateNext()
    {
        int allocatedNumber = this.NextNumber;
        this.NextNumber++;
        this.Version++;
        return allocatedNumber;
    }
}

public enum InvoiceDocumentType
{
    Sales = 1,
    CustomerPurchase = 2,
}
