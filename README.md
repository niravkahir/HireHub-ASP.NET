# 💼 HireHub - Job Application & Recruitment Portal

HireHub is a comprehensive, full-stack web application designed to bridge the gap between job seekers and employers. Built with **ASP.NET Core MVC** and **Entity Framework Core**, it provides a centralized ecosystem where recruiters can post jobs and manage applicants, while job seekers can search, apply, and track their application statuses in real-time.

## 🚀 Key Features

### For Job Seekers
*   **Secure Authentication:** Register and log in securely via ASP.NET Core Identity.
*   **Profile Management:** Manage skills, education, experience, and contact details.
*   **Job Search & Filtering:** Browse jobs with advanced filters (Keyword, Location, Job Type, Experience Level) and server-side pagination.
*   **Apply with Resume:** Upload resumes (PDF/DOC) and submit cover letters. Strict file validation (size and format) is enforced.
*   **Deadline Enforcement:** The system automatically prevents applications to jobs whose deadlines have passed.
*   **Application Tracking:** Track real-time status changes (Applied ➔ Shortlisted ➔ Interview Scheduled ➔ Hired/Rejected).
*   **Withdraw Applications:** Option to withdraw an application if it is not in a final state.
*   **Save Jobs:** Bookmark jobs for later viewing.

### For Recruiters
*   **Company Profiles:** Create and manage company details, including uploading a company logo.
*   **Job Posting:** Post new job openings with detailed descriptions, requirements, salary ranges, and deadlines.
*   **Manage Jobs:** Edit postings, toggle their status (Active/Closed), or delete them.
*   **Applicant Review:** View all applicants for a specific job, read their cover letters, and download their resumes.
*   **Status Locking:** Update application statuses (Shortlist, Reject, Hire). Once hired or rejected, the status is permanently locked to maintain data integrity.
*   **Interview Scheduling:** Schedule interviews for shortlisted candidates with options for online (video link) or onsite locations.

### For Admins
*   **Dashboard:** View system statistics (users, jobs, applications).
*   **User Management:** Activate, deactivate, or delete user accounts.
*   **Content Moderation:** Remove inappropriate job postings.

## 🛠️ Technology Stack
*   **Backend Framework:** ASP.NET Core MVC (.NET 10 LTS)
*   **Language:** C#
*   **Database:** SQL Server (LocalDB for development)
*   **ORM:** Entity Framework Core (Code-First approach)
*   **Authentication:** ASP.NET Core Identity (Cookie-based)
*   **Frontend:** HTML5, CSS3, Bootstrap 5, JavaScript (jQuery)
*   **Version Control:** Git & GitHub

## ⚙️ Prerequisites
Before running this project, ensure you have the following installed:
*   [.NET 10 SDK](https://dotnet.microsoft.com/download)
*   [Visual Studio 2022/2026](https://visualstudio.microsoft.com/) (with ASP.NET and web development workload)
*   SQL Server Express / LocalDB (usually installed with Visual Studio)

## 🏃‍♂️ How to Run Locally

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/HireHub.git
    cd HireHub
