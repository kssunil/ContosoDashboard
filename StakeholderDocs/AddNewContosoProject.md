# Add New Project Feature - Requirements

## Overview

Contoso Corporation needs to add new project when they are finalized organizationally.

## Business Need

Currently project are added as data fix which cause the following troubles for IT team:

- Data fix need to be created and approved by compiance
- Testing the data fix every time a project need to be addeed
- Going through cumbersom approval process across multiple organizations

The add new project feature addresses these issues by providing a centralizedeasy to enter project create screen that reduces effort from multiple teams who are required to currently add a new feature.

## Target Users

Project Manager and Admin who use the ContosoDashboard application will have access to creae project features, with permissions based on their existing roles:

- **Employees**: no access
- **Team Leads**: no access
- **Project Managers**: have full access
- **Administrators**: have full access

## Core Requirements

### 1. Project Creation

**Project Creation**

- Projects screen will have a "New Project" button to transition to the Create New Project screen. Only Program Manager and Admin should be able to see this "New Project" button.
- New project create screen will have all fields with their labels and appropriate controls to enter the data. 
- List the names of all project managers in a single-select dropdown. Project Manager can select any of the available project managers for this project. 
- Project Title should be unique across app. 
- Start Date and Target Completion can be past dates for backfilling project information for completed projects.
- It will have Save and Cancel buttons. Save button will save the project information to database. Cancel will cancel the operations and go back to the previous screen. 
- There should be table for the list of project tasks in the project create screen. Use same UI template similar to Project Details screen currently there. Table header on the top right corner will have a "New Task" button which will open a modal dialog box to enter the project information. Modal dialog box should have two buttons (Ok and Cancel). When clicked Ok, the entered data is added to the table after appropriate validation. Title is required. 
- Project create screen will also have a "Upload Documents" button to upload documents associated to the project. It should also have a table with list of selected documents. 
- Project create screen will also have multiselect dropdown to select the list of team members. Once selected all the selected ones are shown as comma separated text below the dropdown.
- Users with access to create project must be able to select create button in My Projects form to trasition to Project Create form
- Users once in the create project screen should be able to enter all the details of project and press save to save the project and cancel to cancel the project creation. If users clicks Save, system will save the project information and if successful will transition to My Projects screen and newly added project will be visible along with the existing projects. If there are any errors, system will show a user friendly message of the error and stay in the create project create screen. If Cancel button is clicked user should transition back to My Projects screen. 


**Project Metadata**

- When uploading, users must provide:
  - Description (required)
  - Status (active or inactive) (required)
  - Progress (numeric value of 0 - 100) If nothing specified, its a 0  (optional)
  - Project Manager (single selection from a list of project managers in a dropdown) (required)
  - Start Date (required)
  - End Date (required)

**Validation and Security**
  - Only Project Manager and Admin roles users be shown the Project create button in the My Projects screen.
  - Only Project Manager and Admin roles users should be able to access to this New Project screen.


### 6. Reporting and Audit

**Activity Tracking**

- System should log all project creations with created by and created time details

## User Experience Goals

- **Simplicity**: Should be easy to add a new project with mouse clicks or tabs in the keyboard
- **Speed**: Creation should feel instant
- **Clarity**: Users is always shown a message if success or failure with appropriate details

## Success Metrics


## Technical Constraints


## Implementation Approach


**Data Layer:**

**Storage Layer:**

**Business Logic Layer:**
- Save button initiates the save to database action:
  1. Title should be unique across app
  2. Start Date cannot go back more than 12 months
  3. Create database record in existing Projects table
- Authorization checks prevent unauthorized project access (IDOR protection)
- Service layer enforces all security rules before data access


This architecture ensures security, maintainability while keeping the training implementation simple and offline-capable.

### Database Setup Requirements


## Assumptions


## Out of Scope


## Next Steps

