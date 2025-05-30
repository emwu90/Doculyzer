# 📄 Doculyzer

**Doculyzer** is an AI-powered agent that answers natural language questions about invoices stored in your cloud storage. It supports multiple formats, understands financial context, and delivers fast, accurate insights—making invoice analysis effortless.

## 🔍 Key Features

- **Natural Language Interface**: Ask questions like “What’s the total amount due in March invoices?” or “Which invoices are overdue?”
- **Storage Integration**: Connects to your Azure Blob Storage to access and analyze invoice documents.
- **Multi-format Support**: Works with PDFs.
- **Context-Aware Answers**: Understands invoice structure and terminology to provide accurate, context-rich responses.
- **Extensible Agent Framework**: Built to be modular—easily extend to other document types or storage backends.

## 🧠 Use Cases

- Finance teams automating invoice audits
- Developers building document Q&A systems
- Back-office bots for procurement or accounting
- AI-powered dashboards for financial insights

## Architecture Overview

This system uses a modern, scalable architecture with the following components:

### Azure Services Used:
1. **Azure Functions** - Serverless compute platform hosting the API
2. **Azure Blob Storage** - Stores PDF invoice files with metadata
3. **Azure AI Document Intelligence** - Extracts structured data from PDF invoices
4. **Azure OpenAI Service** - Processes natural language queries and generates responses
5. **Azure AI Search** - Provides high-performance search capabilities for invoice metadata
6. **Azure Application Insights** - Monitoring and logging

### Design Patterns:
- **Mediator Pattern** - Decouples request handling using own mediator implementation
- **Factory Pattern** - Creates Azure service clients with proper configuration
- **Repository Pattern** - Abstracts data access logic
- **Dependency Injection** - Manages service dependencies

### Performance Optimizations:
- **Azure AI Search Integration** - Instead of iterating through 100,000+ blob metadata entries, we use Azure AI Search for efficient querying
- **Metadata-Based Filtering** - Quick filtering using blob metadata before expensive operations
- **Async/Await Pattern** - Non-blocking operations throughout
- **Streaming** - Efficient handling of large PDF files

## API Endpoints

### POST /api/invoices/query
Processes natural language queries about invoices.

**Request Body:**
```json
{
  "prompt": "What's the total amount of invoices in March 2024?"
}
```

**Response:**
```json
{
  "answer": "The total amount of invoices in March 2024 is $45,230.50 across 23 invoices.",
  "relevantInvoices": [...],
  "queryInterpretation": "DateRange query for March 2024",
  "success": true
}
```

## Setup Instructions

1. **Create Azure Resources:**
   - Storage Account with 'invoices' container
   - Azure OpenAI Service with GPT-4 deployment
   - Document Intelligence Service
   - Azure AI Search Service
   - Function App

2. **Configure Azure AI Search Index:**
   Create an index with the following fields:
   - Id (string, key)
   - BlobName (string, searchable)
   - InvoiceNumber (string, searchable)
   - InvoiceDate (datetime, filterable)
   - CustomerName (string, searchable)
   - CustomerId (string, filterable)
   - TotalAmount (decimal, filterable)
   - Currency (string, filterable)
   - LineItems (complex collection)

3. **Set Environment Variables:**
   Update local.settings.json with your Azure service endpoints and keys.

4. **Deploy:**
   Use Azure Functions Core Tools or Visual Studio for deployment.

## Usage Examples

**Query Examples:**
- "What's the total amount of invoices in March 2024?"
- "Give me list of products sold to customer ABC123 in April"
- "How much did we invoice customer XYZ last quarter?"
- "What are the top 5 products by revenue this year?"

**Automatic Processing:**
- Upload PDF invoices to the 'invoices' blob container
- The system automatically extracts data and updates search index
- Invoices become immediately queryable via natural language

## Performance Considerations

- **Search-First Approach**: Uses Azure AI Search instead of blob enumeration
- **Metadata Optimization**: Key invoice data stored in blob metadata for quick filtering
- **Streaming**: Large PDF files handled via streaming to minimize memory usage
- **Async Processing**: All operations are asynchronous for better scalability

## Error Handling

- Comprehensive try-catch blocks with meaningful error messages
- Graceful degradation when services are unavailable
- Proper HTTP status codes and error responses