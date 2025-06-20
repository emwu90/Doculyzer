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

### POST /api/agent
Processes natural language queries about invoices.

**Request Body:**
```json
{
  "Prompt": "What's the total amount of invoices in March 2024?"
}
```

**Response:**
```json
{
  "Answer": "The total amount of invoices in March 2024 is $45,230.50 across 23 invoices.",
  "RelevantInvoices": [...],
  "IsSuccessful": true,
  "ErrorMessage": null
}
```

## Setup Instructions

1. **Create Azure Resources:**
   - Storage Account with 'invoices' container
   - Azure OpenAI Service with GPT-4 deployment model
   - Document Intelligence Service
   - Azure AI Search Service
   - Content Safety Service
   - Function App
   - Azure Cosmos DB
   - Key vault
   - Application Insights

2. **Configure Azure AI Search Index:**
   Create an index with the following fields:
   - Id (string, key)
   - BlobName (string, filterable, searchable)
   - InvoiceNumber (string, filterable, searchable)
   - InvoiceDate (DateTimeOffset, filterable)
   - CustomerName (string, filterable, searchable)
   - CustomerId (string, filterable, searchable)
   - TotalAmount (double, filterable)
   - Currency (string, filterable, searchable)
   - LineItems (complex collection)
		- ProductName (string, filterable, searchable)
		- ProductCode (string, filterable, searchable)
		- Quantity (int32, filterable)
		- UnitPrice (int32, filterable)
		- TotalPrice (int32, filterable)
		- Description (string, filterable, searchable)

3. **Configure Cosmos DB:**
   Create database with container for metrics. Use '/id' as partition key.

4. **Set Environment Variables for Function App:**
   - ServicesConfig:ContentSafetyApiKey (Key vault reference)
   - ServicesConfig:ContentSafetyEndpoint
   - ServicesConfig:CosmosDBContainerName
   - ServicesConfig:CosmosDBDatabaseName
   - ServicesConfig:CosmosDBEndpoint
   - ServicesConfig:CosmosDBPrimaryKey (Key vault reference)
   - ServicesConfig:DocumentIntelligenceApiKey (Key vault reference)
   - ServicesConfig:DocumentIntelligenceEndpoint
   - ServicesConfig:InvoiceContainerName
   - ServicesConfig:OpenAIApiKey (Key vault reference)
   - ServicesConfig:OpenAIDeploymentName
   - ServicesConfig:OpenAIEndpoint
   - ServicesConfig:SearchIndexName
   - ServicesConfig:SearchServiceApiKey (Key vault reference)
   - ServicesConfig:SearchServiceEndpoint
   - ServicesConfig:StorageConnectionString (Key vault reference)
   Update local.settings.json with your Azure service endpoints and keys.

5. **Deploy:**
   Use Azure Functions Core Tools or Visual Studio for deployment.

## Usage Examples

**Query Examples:**
- "What's the total amount of invoices in March 2024?"
- "Give me list of products sold to customer ABC123 in April"
- "How much did we invoice customer XYZ last quarter?"
- "Can you give me total amout for invoices between January 2024 and March 2024?"

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
- Proper HTTP status codes and error responses