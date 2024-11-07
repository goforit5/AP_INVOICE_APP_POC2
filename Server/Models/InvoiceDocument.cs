// InvoiceDocument.cs
using System;
using System.Collections.Generic;

public class InvoiceDocument
{
    public required string id { get; set; }  // Cosmos DB requires 'id' as a string

    // Existing property
    public required Metadata Metadata { get; set; }

    // New properties
    public required ProcessingStatus ProcessingStatus { get; set; }
    public List<ProcessingStep> ProcessingSteps { get; set; } = new List<ProcessingStep>();

    // Future properties can be added here
    // public DocumentAnalysis DocumentAnalysis { get; set; }
    // public ValidatedData ValidatedData { get; set; }
}
