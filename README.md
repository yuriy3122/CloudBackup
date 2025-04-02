<h1 align="center">CLOUD BACKUP</h1>
<h4 align="center">CLOUD BACKUP is a backup and recovery software running in a cloud.</h4>

<img src="https://github.com/yuriy3122/CloudBackup/blob/main/ui.jpg" alt="Screenshot">

## Key Features

* Web UI wizard for creating scheduled VM backups.
* Sending notifications on backup statuses to email via custom SMTP services.
* Restore both individual disks and complete configurations of virtual machines in the cloud.

## Modules
* Common
  - Implementation of basic backup functionality that is used in other modules.
* Model
  - Declaration of basic entities based on the DDD concept.
* Repository
  - Implementation of the "Repository" pattern concept - simplification of the data quering using EF Core framework.
* Services
  - Implementing common business tasks.
* JobProcessorApp
  - Implementing a fault-tolerant, background process to perform scheduled backup tasks.
* Management
  - Implementation of WebAPI controllers using ASP.NET Core and modern web interface within DevExreme Angular libs.
* UnitTests
   - Implementation of Unit tests using Moq library.

## How To Use

To clone and run this application, you'll need Microsoft Visual Studio 2022 and .NET 8.
 
