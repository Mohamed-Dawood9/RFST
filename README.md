# 🎓 Graduation Project: Radiation-Free Scoliosis Tracking

This project is a comprehensive software system for **analyzing human back posture**, focusing on **spinal alignment and curvature**. It integrates **2D and 3D processing techniques** to provide detailed assessments, supporting early detection and monitoring of scoliosis and other spinal issues.

---

## 🚀 Features

### 👨‍⚕️ Doctor Module
- **Registration & Authentication:** Secure account creation and login for doctors.
- **Patient Management:**
  - View an organized patient list (starts empty on first use).
  - Create detailed patient records.

### 🧑‍⚕️ Patient Module
- **Appointment Scheduling:** Add appointments tied to patient profiles.
- **Medical Image Uploads:** Upload images to associate with appointments.

### 📈 Analysis & Visualization
- **2D View:** Examine uploaded medical images in a standard 2D viewer.
- **3D View:** Render and explore images in a 3D visualization to assess complex cases.

### 📝 Reporting
- **2D Reports:** Generate detailed reports based on 2D analyses.
- **3D Reports:** Produce comprehensive 3D diagnostic reports.

### 🗑️ Appointment Management
- **Delete Appointments:** Easily remove outdated or erroneous appointments, maintaining data integrity.

---

## 💻 Tech Stack
- **Backend:** ASP.NET Core
- **Database:** SQL Server
- **ORM & Data Access:** Entity Framework Core
- **Frontend:** Razor Pages (or MVC Views)
- **2D & 3D Processing:** Integrated Python & .NET libraries for medical image rendering

---

## 🔄 Typical Workflow
1. Doctor registers and logs into the system.
2. Creates patient profiles and schedules appointments.
3. Uploads medical images for each appointment.
4. Analyzes images using **2D and 3D viewers**.
5. Generates and downloads **2D or 3D reports**.
6. Deletes appointments when no longer needed.

---

## 🚀 Getting Started

Follow these steps to set up the project and prepare the **2D & 3D processing services**.

---

### 📦 1. Download Required Files
Download necessary assets and scripts from:

```
https://drive.google.com/drive/folders/1MebPs8p9xLd7gmIA9jNZyE899L8pCe8I?usp=sharing
```

---

### 🔧 2. Configure 3D Processing
- The 3D processing script is referenced in:

  ```
  GP.BLL/Services/SpineProcessingService.cs
  ```

- Update the path inside this file to point to your actual script in:

  ```
  3d/script2
  ```

- Then open `script2` and make sure to **enter a valid Tripo 3D API key** so it can correctly execute your 3D analysis logic.

---

### 🐍 3. Configure 2D Processing
- In the `2d` directory, there’s a Python script for 2D analysis.
- Make sure this script references the uncompressed model files exactly like:

  ```
  content/runsmside/pose_6ptsmside_model/weights/best.pt
  content/runsm/pose_6ptsm_model/weights/best.pt
  ```

This ensures the detection pipeline loads the correct weights for **side and main pose estimation**.

---

### 🌐 4. Run 2D Analysis Service Locally (with ngrok)
- Start your Python 2D API locally:

  ```
  python 2D.py
  ```

- Open a new terminal and run ngrok to forward HTTP on port `5000` (or your local API port):

  ```
  ngrok http 5000
  ```

- You’ll get a forwarding URL like:

  ```
  Forwarding https://8c4f-197-55-xxx-xxx.ngrok.io -> http://localhost:5000
  ```

- Use this URL in:

  ```
  GP.BLL/Services/SpineProcessingService.cs
  ```

so the backend can reach your **2D analysis API**.

---

### ⚙️ 5. Run the .NET Project
- Clone the repository and build the project:

  ```bash
  git clone https://github.com/Mohamed-Dawood9/RFST
  cd RFST
  dotnet build
  dotnet ef database update
  dotnet run
  ```

- Then navigate to:

  ```
  https://localhost:5001
  ```

(or your configured port) to start using the system.

---

### 📝 Notes
✅ Ensure **SQL Server** is installed and your `appsettings.json` is configured with the correct connection string.

---

## 🤝 Contributing
Pull requests are welcome!  
For major changes, please open an issue first to discuss what you’d like to modify.
