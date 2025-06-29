🚀 Features

👨‍⚕️ Doctor Module
Registration & Authentication: Secure account creation and login for doctors.

Patient Management:

View an organized patient list (starts empty on first use).

Create detailed patient records.

🧑‍⚕️ Patient Module
Appointment Scheduling: Add appointments tied to patient profiles.

Medical Image Uploads: Upload images (such as X-rays, MRIs) to associate with appointments.

📈 Analysis & Visualization
2D View: Examine uploaded medical images in a standard 2D viewer.

3D View: Render and explore images in a 3D visualization to assess complex cases.

📝 Reporting
2D Reports: Generate detailed reports based on 2D analyses.

3D Reports: Produce comprehensive 3D diagnostic reports.

🗑️ Appointment Management
Delete Appointments: Easily remove outdated or erroneous appointments, maintaining data integrity.

💻 Tech Stack
Backend: ASP.NET Core

Database: SQL Server

ORM & Data Access: Entity Framework Core

Frontend: Razor Pages (or MVC Views) (adjust this if using a different frontend stack)

3D & 2D Processing: Integrated libraries for medical image rendering

🔄 Typical Workflow
Doctor registers and logs into the system.

Creates patient profiles and schedules appointments.

Uploads medical images for each appointment.

Analyzes images using 2D and 3D viewers.

Generates and downloads 2D or 3D reports.

Deletes appointments when no longer needed.

🚀 Getting Started


Clone the repository and run the project using the .NET CLI.


git clone https://github.com/Mohamed-Dawood9/RFST
cd your-repo-name
dotnet build
dotnet ef database update
dotnet run
Then navigate to https://localhost:5001 (or your configured port) to start using the system.

Note: Ensure SQL Server is installed and your appsettings.json is configured with the correct connection string.
