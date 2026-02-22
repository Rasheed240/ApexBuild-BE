# ApexBuild Platform Guide
> Complete reference for all roles — from onboarding to advanced workflows.

---

## 1. Introduction to ApexBuild
ApexBuild is a comprehensive construction and project management platform designed for organisations that manage multiple projects, departments, contractors, and field teams. It brings together task assignment, progress tracking, multi-stage review workflows, and subscription billing in one place.

This guide covers every feature of the platform and is organised by topic. Use the sidebar to jump to any section, or read straight through for a complete understanding.

> [!TIP]
> **New user?** Start with [Getting Started](#3-getting-started), then read [Roles & Permissions](#2-roles-permissions) to understand what you can see and do.

### Core Concepts
- **Organisation** — the top-level entity, owned by a company or individual.
- **Project** — a work scope within an organisation (e.g. a building site).
- **Department** — a sub-division of a project (e.g. Electrical, Plumbing).
- **Contractor** — an external company hired to work on one or more departments.
- **Task / Subtask** — units of work assigned to users within a department.
- **Task Update** — a progress submission (with media proof) that travels through a review chain before being fully approved.

---

## 2. Roles & Permissions
Every user in ApexBuild is assigned a role. Roles are project-scoped — you can hold different roles in different projects. The hierarchy from lowest to highest is:

### Roles Table
| Role | Scope | Key Permissions |
| :--- | :--- | :--- |
| **FieldWorker** | Project | View assigned tasks, submit progress updates, view own update history |
| **ContractorAdmin** | Project | FieldWorker + review & approve/reject updates for their contractor |
| **DepartmentSupervisor** | Project | ContractorAdmin + manage departments, review supervisor-stage updates |
| **ProjectAdministrator** | Project | Full project control — create tasks, manage members, final update approval |
| **ProjectOwner** | Project | ProjectAdministrator + billing and project deletion |
| **PlatformAdmin** | Organisation | Manage all projects & users within an organisation |
| **SuperAdmin** | Platform | Full platform access — all organisations, subscriptions, user manuals |

### Detailed Role Descriptions

#### 👷 FieldWorker
The most common role. Receives task assignments and submits photo/video proof of work. Does not review other people's updates.

#### 💼 ContractorAdmin
Manages a contracted team. Reviews updates from their field workers before passing them up to the Supervisor.

#### 👁️ DepartmentSupervisor
Oversees one or more departments. Reviews updates after the ContractorAdmin (for contracted tasks) or directly from FieldWorkers (non-contracted tasks).

#### 👑 ProjectAdministrator / Owner
Manages the entire project. Gives final sign-off on all task updates, creates milestones, and manages members.

#### ⭐ PlatformAdmin / SuperAdmin
Manages the platform or a whole organisation. Has access to subscriptions, billing, all projects, and system-level settings.

> [!NOTE]
> A user can belong to multiple projects simultaneously, each with a different role. For example, someone may be a **ProjectAdministrator** on Project A and a **FieldWorker** on Project B.

---

## 3. Getting Started

### New Users — Registration
1. **Create an account**: Visit the ApexBuild landing page and click **Get Started**. Fill in your name, email, and a secure password (min 8 chars, one number, one symbol).
2. **Verify your email**: Check your inbox for a confirmation email. Click the link to activate your account.
3. **Create or join an organisation**: After login, you'll be prompted to either create a new organisation or accept an invitation from an existing one. If you received an email invitation, check the *Invitations* section of your profile.
4. **Set up 2FA (recommended)**: Go to **Settings → Security** and enable Two-Factor Authentication via an authenticator app (Google Authenticator, Authy, etc.).

### Existing Users — Switching between Sessions
When you log in, the app remembers your session via a secure refresh token (7-day validity). If you are inactive, you will be automatically logged out.

> [!WARNING]
> Never share your login credentials. Each user must have a unique account — accounts are tracked per seat for billing purposes.

### First Steps by Role
| Role | First things to do |
| :--- | :--- |
| **FieldWorker** | Check My Tasks → start work on assigned tasks → submit your first progress update with photo proof |
| **ContractorAdmin** | Check Reviews → review pending updates from your team → explore the project you are assigned to |
| **DepartmentSupervisor** | Check Reviews → view your departments → review supervisor-stage updates |
| **ProjectAdministrator** | Create departments → create milestones → invite team members → assign tasks |
| **SuperAdmin** | Set up your first organisation → invite admins → configure subscription |

---

## 4. Organisations
An **organisation** is the root entity. Everything — projects, members, billing — belongs to an organisation. A user can be a member of *multiple* organisations simultaneously.

### Creating an Organisation
1. Go to **Organisations** in the sidebar.
2. Click **'New Organisation'**: Enter a name, description, and optionally upload a logo.
3. **You become the owner**: The creating user is automatically assigned the **PlatformAdmin** role for that organisation.

### Switching between Organisations
Use the **organisation switcher** in the top-left corner of the sidebar. Click the currently selected organisation name, then choose another from the dropdown. All dashboard data — projects, tasks, stats — will reload for the selected organisation.

> [!NOTE]
> The organisation switcher filters data *globally* across the app. Reviews, tasks, and project progress all respect the selected organisation.

### Managing Organisation Members
Go to **Members** in the sidebar. Here you can:
- View all members and their roles.
- Invite new members via email.
- Remove members.
Members invited at the organisation level still need to be assigned to specific projects to gain project-level roles.

---

## 5. Project Management
Projects are the main work containers. Each project has departments, contractors, milestones, and tasks.

### Creating a Project
1. Go to **Projects → New Project**.
2. **Fill in details**: Name, description, start and end dates, status (Planning / Active / On Hold / Completed), priority, and budget.
3. **Add departments**: From the project detail page, go to the **Departments** tab. Departments organise work by trade or discipline (e.g. Electrical, Civil Works).
4. **Add contractors (optional)**: If a department is contracted out, go to **Contractors** tab. Link the contractor to a department and assign a ContractorAdmin user.
5. **Create milestones**: Use the **Milestones** tab to define project phases with due dates. Milestones track overall project progress.
6. **Assign project members**: Go to the **Members** tab on the project page and assign users with their project roles.

### Project Tabs Overview
| Tab | What it shows |
| :--- | :--- |
| **Overview** | Summary cards, milestone progress, task pipeline stats |
| **Tasks** | All tasks in this project with filters by status, priority, assignee |
| **Milestones** | Ordered project phases with progress tracking |
| **Contractors** | External companies assigned to departments in this project |
| **Departments** | Organisational divisions of the project with member counts |

### Departments vs Contractors
A **Department** is an internal division (e.g. "Plumbing Team"). A **Contractor** is an external company that manages workers within a department.
- A department can exist without a contractor (work done in-house).
- When a contractor is assigned, their ContractorAdmin user is responsible for reviewing field worker updates first.

### Milestones
Milestones are ordered phases of a project. Each milestone has:
- **Order** — determines the sequence.
- **Due date** — deadline for the milestone.
- **Progress** (0–100%) — manually or automatically tracked.
- **Status** — NotStarted, InProgress, Completed, OnHold.

> [!TIP]
> Link tasks to milestones when creating them so that milestone progress auto-reflects task completion.

---

## 6. Tasks & Subtasks
Tasks are the smallest unit of work. They are created by supervisors or admins and assigned to field workers.

### Creating a Task
1. Go to **Projects → [Project] → Tasks** tab, or **My Tasks**.
2. Click **'New Task'**: Fill in: title, description, department, assignee(s), priority (Low/Medium/High/Critical), due date, milestone (optional), and contractor (optional).
3. **Add media (optional)**: Attach reference images, blueprints, or documents to the task for workers to reference.
4. **Create subtasks (optional)**: Inside a task, use the **Subtasks** section to break large tasks into smaller check-off items.

### Task Statuses
| Status | Meaning |
| :--- | :--- |
| **Not Started** | Task created but work has not begun |
| **In Progress** | Work is underway |
| **Under Review** | A progress update has been submitted and is awaiting approval |
| **Completed** | All required updates have been approved through the full review chain |
| **Cancelled** | Task has been cancelled and will not be completed |
| **On Hold** | Work temporarily paused |

### Submitting a Progress Update (FieldWorker)
1. Open the task from **My Tasks** or the project tasks list.
2. Go to the **Updates** tab: The submit form is automatically visible for field workers.
3. **Fill in the update**: Describe what was done, set a progress percentage (0–100%), and upload proof media (photos, video, audio notes).
4. **Submit**: The update enters the review chain. You'll see its status change under the Updates tab.

> [!NOTE]
> You can track the status of your submitted update in real time — from "Submitted" through each review stage to "Fully Approved" or a rejected state with feedback.

### Subtasks
Subtasks are simple checkbox items within a task. They help track granular completion. They are not subject to the update/review flow — they are toggled directly by the assigned user or the task owner.

---

## 7. Update Review Flows
When a FieldWorker submits a task update, it must pass through a structured review chain before being counted as approved. The chain depends on whether the task is *contracted* or *non-contracted*.

### Review Chains
| Task Type | Review Flow |
| :--- | :--- |
| **Non-contracted** | FieldWorker → DepartmentSupervisor → ProjectAdministrator |
| **Contracted** | FieldWorker → ContractorAdmin → DepartmentSupervisor → ProjectAdministrator |

### Update Status Lifecycle
| Status | Meaning | Who acts next |
| :--- | :--- | :--- |
| **Submitted** | Update just submitted by worker | ContractorAdmin (if contracted) OR Supervisor |
| **Under Contractor Admin Review** | ContractorAdmin is reviewing | ContractorAdmin (approve or reject) |
| **Contractor Admin Approved** | Passed contractor review | DepartmentSupervisor |
| **Contractor Admin Rejected** | Rejected with feedback, resubmission needed | FieldWorker (re-submit) |
| **Under Supervisor Review** | Supervisor is reviewing | DepartmentSupervisor (approve or reject) |
| **Supervisor Approved** | Passed supervisor review | ProjectAdministrator |
| **Supervisor Rejected** | Rejected with feedback | FieldWorker (re-submit) |
| **Under Admin Review** | Admin is reviewing for final sign-off | ProjectAdministrator (approve or reject) |
| **Admin Approved (Fully)** | Fully approved — task progress updated | — |
| **Admin Rejected** | Final rejection with feedback | FieldWorker (re-submit) |

### How to Review an Update
1. Go to **Reviews** in the sidebar: The page shows only updates that require YOUR action at this stage (based on your role).
2. **Click an update to view details**: See the submitted description, progress percentage, and all attached media (photos, video).
3. **Approve or reject**:
   - **Approve** — passes the update to the next reviewer.
   - **Reject** — requires a feedback note. The worker will see this and must resubmit.

> [!WARNING]
> Reviews page only shows updates at YOUR review stage. A FieldWorker sees only their own submitted updates (not other people's). A ContractorAdmin sees only updates awaiting their review. And so on up the chain.

### Resubmitting after Rejection
If your update was rejected, return to the task's Updates tab. You'll see the rejection reason in the feedback trail. Submit a new update addressing the feedback — this begins the review cycle again from the start.

---

## 8. Team & Members
Members are users assigned to an organisation or project. Access is controlled by role.

### Inviting Users
1. Go to **Members → Invite**: Enter the user's email address.
2. **Invitation sent**: The user receives an email with a secure invite link (valid for 7 days).
3. **User accepts**: They create an account (or log in) and are added to the organisation.
4. **Assign to a project**: From the project's Members tab, assign the user a project role.

### One Role per Project per User
Each user can only hold *one* role within a given project. If you need to change their role, remove them and re-add with the new role.

> [!NOTE]
> Users can hold different roles in different projects. For example, **Alice** might be a **ProjectAdministrator** on the Lagos Housing project and a **FieldWorker** on the Abuja Office project.

### Removing a Member
Go to the project Members tab or the organisation Members page, find the user, and click Remove. This does not delete their account — it only removes them from that project or organisation.

---

## 9. Subscriptions & Billing
ApexBuild uses a per-active-user, per-month billing model.

### Pricing Model
| Plan | Rate | Notes |
| :--- | :--- | :--- |
| **Standard** | $20 / active user / month | All features included. Active = logged in within the billing period. |
| **Free (SuperAdmin org)** | $0 | The SuperAdmin's own organisation is permanently free. |

> [!NOTE]
> **Active users** are counted per organisation per billing period. A user who has not logged in during the cycle is not charged.

### Managing Subscriptions (SuperAdmin / PlatformAdmin)
Go to **Subscriptions** in the sidebar. Here you can:
- View current active user count and monthly estimate.
- See billing history and payment transactions.
- Update payment method (via Stripe).
- View seat usage.

### Payment Flow
Payments are processed via Stripe. ApexBuild does not store card details directly — all card information is managed securely by Stripe.

> [!WARNING]
> Subscription payments are automatically charged at the start of each billing cycle. If a payment fails, you will receive a notification and have a 3-day grace period before access is restricted.

---

## 10. Profile & Settings

### Profile Page
Access your profile via the **Profile** link in the sidebar. Here you can:
- Update your name, phone number, bio, and profile picture.
- View your organisation memberships and project roles.

### Settings Page
The **Settings** page (sidebar → Settings) contains:
| Section | Purpose |
| :--- | :--- |
| **Change Password** | Update your login password (requires current password) |
| **Security / 2FA** | Enable or disable TOTP two-factor authentication |

### Two-Factor Authentication (2FA)
1. Go to **Settings → Security**.
2. Click **Enable 2FA**: A QR code is displayed.
3. **Scan with your authenticator app** (Google Authenticator, Authy, or similar).
4. **Enter the 6-digit code to confirm setup**.

> [!NOTE]
> Once enabled, every login will require your password AND the 6-digit time-based code from your app. This significantly improves account security.

### Notifications
Go to **Notifications** in the sidebar to see all platform notifications — task assignments, review decisions, and system alerts. Unread notifications appear as a badge on the bell icon in the top bar. Click *Mark all read* to clear them.

---

## 11. FAQ

**Q: I submitted an update but the task is still "In Progress". Why?**
A: Task status changes to "Completed" only after the final admin approval (last stage in the review chain). Until then it remains "Under Review" or the previous status.

**Q: I can't see the Approve/Reject buttons. Why?**
A: Buttons are only shown for updates that are at YOUR review stage. FieldWorkers never see review buttons. If you are a ContractorAdmin, you only see updates awaiting Contractor Admin review, not updates at the Supervisor or Admin stage.

**Q: How do I switch to a different organisation?**
A: Click the organisation name in the top-left sidebar dropdown. All page data (tasks, reviews, stats) will reload for the selected org.

**Q: Why does my review list show updates from another organisation?**
A: Make sure you have the correct organisation selected in the switcher. All review queries are scoped to the selected organisation.

**Q: Can I belong to multiple organisations?**
A: Yes. You can be a member of any number of organisations and projects, each with different roles. Use the org switcher to move between them.

**Q: What happens when an update is rejected?**
A: The worker is notified. The update status shows the rejection reason (visible in the update detail under the Updates tab of the task). The worker must submit a new update to restart the review flow.

**Q: How is the subscription calculated?**
A: You are billed $20 per active user per month, per organisation. An active user is one who has logged in or performed actions within the billing cycle. The SuperAdmin's own organisation is always free.

**Q: Who can upload the PDF user manual?**
A: Only users with the SuperAdmin or PlatformAdmin role can upload a new PDF manual via the Guide page. All other users can read it online and download the uploaded PDF.

**Q: Can a task have multiple updates?**
A: Yes. A task can have multiple submitted updates over its lifetime. Each update goes through the full review chain independently. The task's progress percentage reflects the most recently approved update.

**Q: What media types can be attached to an update?**
A: Field workers can attach images (JPG, PNG), videos (MP4), audio files (MP3 — verbal notes), and documents (PDF) when submitting a progress update.
